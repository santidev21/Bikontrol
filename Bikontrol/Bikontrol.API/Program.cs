using Bikontrol.Application;
using Bikontrol.API.Extensions;
using Bikontrol.API.Middleware;
using Bikontrol.API.RateLimiting;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Options;
using Bikontrol.Infrastructure;
using Bikontrol.Infrastructure.Authentication;
using Bikontrol.Application.Mapping;
using Bikontrol.Persistence.Seed;
using Bikontrol.Application.Services;
using Bikontrol.Persistence;
using Bikontrol.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Threading.RateLimiting;
using IPNetwork = System.Net.IPNetwork;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables();

// Structured logging: JSON in production (parseable by log collectors), readable
// text in Development. Enriched with the trace id so a request's lines can be
// correlated. Output goes to stdout, which Docker captures.
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    // One template for every sink. In dev it is short; in prod it is ISO-8601 so
    // the file lines sort lexicographically and are easy to grep.
    var outputTemplate = context.HostingEnvironment.IsDevelopment()
        ? "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}"
        : "{Timestamp:o} [{Level:u3}] {TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}";

    // Daily rolling log file so it can be read/opened directly (a plain .txt/.log)
    // instead of only `docker logs`. In containers it lands on a mounted volume
    // (/app/logs); override the location with Log__Path. 14 files, 50 MB each.
    var logPath = context.Configuration["Log__Path"];
    if (string.IsNullOrWhiteSpace(logPath))
    {
        logPath = context.HostingEnvironment.IsDevelopment()
            ? Path.Combine("logs", "bikontrol-api-.log")
            : "/app/logs/bikontrol-api-.log";
    }

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Bikontrol.API")
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate: outputTemplate, formatProvider: System.Globalization.CultureInfo.InvariantCulture)
        .WriteTo.File(
            path: logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            fileSizeLimitBytes: 50_000_000,
            rollOnFileSizeLimit: true,
            shared: true,
            outputTemplate: outputTemplate,
            formatProvider: System.Globalization.CultureInfo.InvariantCulture);
});

// Error tracking (Sentry): only active when a DSN is configured, so local dev
// and CI stay no-op. User PII is not sent.
var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName;
        options.SendDefaultPii = false;
        options.TracesSampleRate = double.TryParse(builder.Configuration["Sentry:TracesSampleRate"], out var rate)
            ? rate
            : 0.1;
    });
}

// Typed, validated options. ValidateOnStart makes a bad value (missing JWT key,
// out-of-range hour, half-configured VAPID pair) fail the boot with a clear
// message instead of silently falling back to a default at runtime.
builder.Services.AddValidatedOptions<JwtOptions>(JwtOptions.Section);
builder.Services.AddValidatedOptions<SmtpOptions>(SmtpOptions.Section);
builder.Services.AddValidatedOptions<WebPushOptions>(WebPushOptions.Section);
builder.Services.AddValidatedOptions<ReminderOptions>(ReminderOptions.Section);
builder.Services.AddValidatedOptions<RetentionOptions>(RetentionOptions.Section);
builder.Services.AddValidatedOptions<DatabaseOptions>(DatabaseOptions.Section);

// Bound eagerly too: the bearer handler is configured before the container is built.
var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
var jwtErrors = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
        jwt, new System.ComponentModel.DataAnnotations.ValidationContext(jwt), jwtErrors, validateAllProperties: true))
{
    throw new InvalidOperationException(
        "Invalid Jwt configuration: " + string.Join("; ", jwtErrors.Select(e => e.ErrorMessage))
        + ". Set Jwt__Key/Jwt__Issuer/Jwt__Audience via environment variables or appsettings.Development.json.");
}

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var signingKeys = BuildSigningKeys(jwt);
        // Keep the "role" claim name as issued by JwtTokenGenerator.
        // With the default (MapInboundClaims = true) "role" is remapped to
        // ClaimTypes.Role and CurrentUserService would miss it, silently
        // treating Demo tokens as regular users.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            // Current key plus any previous ones, matched by kid, so a key can
            // be rotated without logging everybody out (see docs/specs/security).
            IssuerSigningKeyResolver = (_, _, kid, _) => signingKeys.Where(k => kid is null || k.KeyId == kid).ToList(),
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            // Reject tokens minted before a password change/reset or account
            // deletion: the stamp claim must match the user's current one.
            OnTokenValidated = async context =>
            {
                var sub = context.Principal?.FindFirst("sub")?.Value;
                var claim = context.Principal?.FindFirst(JwtTokenGenerator.SecurityStampClaim)?.Value;
                if (!Guid.TryParse(sub, out var userId) || !Guid.TryParseExact(claim, "N", out var tokenStamp))
                {
                    context.Fail("Missing identity claims.");
                    return;
                }

                // Own scope on purpose: resolving the request-scoped DbContext here
                // would build ICurrentUserService before the principal is set and
                // freeze an empty user id for the rest of the request.
                using var scope = context.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<ISecurityStampStore>();
                var current = await store.GetAsync(userId, context.HttpContext.RequestAborted);
                if (current != tokenStamp)
                    context.Fail("Token revoked.");
            }
        };
    });

// Add CORS
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins);
            }
            policy.AllowAnyHeader();
            policy.AllowAnyMethod();
            // The refresh-token cookie needs the CORS handshake in development
            // (frontend :4201 -> API :7179 are different origins). Production is
            // same-origin and needs none of this, but AllowCredentials is inert there.
            policy.AllowCredentials();
        });
});

// Trust forwarded headers only from the reverse-proxy network (nginx gateway).
// Set ReverseProxy:KnownNetworks (comma-separated CIDRs) to the gateway network;
// defaults to the private ranges Docker/loopback use.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var configuredNetworks = builder.Configuration["ReverseProxy:KnownNetworks"]?
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    var networks = configuredNetworks is { Length: > 0 }
        ? configuredNetworks
        : new[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16", "127.0.0.1/32", "::1/128", "fc00::/7" };

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    foreach (var cidr in networks)
    {
        if (IPNetwork.TryParse(cidr, out var network))
            options.KnownIPNetworks.Add(network);
    }
});

// Rate limiting (auth endpoints). Two independent fixed windows, partitioned
// per client IP: a burst window (10/min) and a sustained ceiling (per hour).
// A burst window alone still accepts a steady 10/min trickle that survives
// across rolling windows; the hourly ceiling closes that. The previous single
// fixed window throttled the whole application to 10 auth requests/min.
builder.Services.AddRateLimiter(options =>
{
    var authPerHour = int.TryParse(builder.Configuration["RateLimiting:AuthPerHour"], out var perHour) && perHour > 0
        ? perHour
        : 60;

    options.AddPolicy("auth", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        // One composite limiter per IP: burst (10/min) + sustained ceiling.
        // A single policy, because an action-level EnableRateLimiting
        // attribute would replace the class-level policy instead of adding.
        return RateLimitPartition.Get(clientIp, _ => new BurstAndSustainedLimiter(
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            },
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPerHour,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));
    });
    // Expensive endpoints (exports, PDF/CSV, uploads): per signed-in user, not
    // per IP, so one account behind a shared NAT cannot starve the others.
    AddUserPolicy(options, "heavy", builder.Configuration, "RateLimiting:HeavyPerMinute", 6);
    AddUserPolicy(options, "upload", builder.Configuration, "RateLimiting:UploadPerMinute", 20);

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);

        var (code, message) = ErrorResponse.ForStatus(StatusCodes.Status429TooManyRequests)!.Value;
        await ErrorResponse.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests, code, message);
    };
});

// Bodies are small JSON (images travel as <= 1 MB data URLs), so anything
// bigger is abuse; Kestrel answers 413 before the payload is buffered.
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Limits.MaxRequestBodySize =
        long.TryParse(builder.Configuration["Limits:MaxRequestBodyBytes"], out var maxBody) && maxBody > 0
            ? maxBody
            : 3 * 1024 * 1024;
});

// Add services to the container.

builder.Services.AddControllers();
// OpenAPI lives in one place so the API contract test generates the exact same
// document that the /openapi/v1.json endpoint serves (see docs/api/openapi.json).
builder.Services.AddBikontrolOpenApi();

// PostgreSQL connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Health: /health is a liveness probe (no dependencies, the process is up);
// /ready is a readiness probe that fails when PostgreSQL is unreachable.
// The DB check uses its own short-timeout connection so a hung database makes
// /ready fail fast instead of hanging the probe.
builder.Services.AddHealthChecks()
    .AddNpgSql(
        BuildHealthCheckConnectionString(builder.Configuration.GetConnectionString("DefaultConnection"))
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing. Set it via environment variables or appsettings."),
        name: "postgresql",
        tags: new[] { "ready" });

// Metrics: OpenTelemetry exported in Prometheus format at /metrics. Off by
// default so the endpoint is only exposed deliberately (Prometheus scrapes the
// API over the internal Docker network; the public gateway never proxies it).
var metricsEnabled = builder.Configuration.GetValue<bool>("Metrics:Enabled");
if (metricsEnabled)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("bikontrol-api"))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter());
}

// Services & Identity tools
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddPersistence();

// Configure InvalidModelStateResponseFactory
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(x => x.ErrorMessage))
            .ToList();

        var message = string.Join(" | ", errors);
        return new ObjectResult(new Dictionary<string, object?>
        {
            ["type"] = "urn:bikontrol:error:validation",
            ["title"] = message,
            ["status"] = StatusCodes.Status400BadRequest,
            ["detail"] = message,
            ["code"] = "validation",
            ["error"] = message
        })
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { ErrorResponse.ContentType }
        };
    };
});
var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Structured request logging: one line per request with method, path, status and
// duration, correlated by the trace id also emitted on error logs.
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

// In Development the frontend uses plain HTTP (:5202) to avoid the
// self-signed dev-cert trust issue on Linux (ERR_CERT_AUTHORITY_INVALID).
// HTTPS redirection stays enforced in every other environment.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
// After authentication so the per-user policies can read the principal.
app.UseRateLimiter();
app.UseAuthorization();

// Bare 401/403/404 (no controller body) get the same coded envelope.
app.UseStatusCodePages(async context =>
{
    if (ErrorResponse.ForStatus(context.HttpContext.Response.StatusCode) is { } error)
        await ErrorResponse.WriteAsync(context.HttpContext, context.HttpContext.Response.StatusCode, error.Code, error.Message);
});

app.MapControllers();
// Liveness: cheap, no dependency checks (used by the web UI/container probes).
app.MapHealthChecks("/health");
// Readiness: fails when PostgreSQL is unreachable, so orchestrators can hold
// traffic back until the API can actually serve it.
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Prometheus scrape endpoint (opt-in). Kept on the internal network only.
if (metricsEnabled)
{
    app.MapPrometheusScrapingEndpoint();
}

// Migrations can run as a dedicated deploy step (`--migrate`, idempotent,
// logged, with a pre-deploy backup) instead of racing on startup — the safer
// option once there is more than one instance. Startup migration stays the
// default for local/dev and any setup that has not moved to the CLI step.
var migrateOnly = args.Contains("--migrate");
var runMigrationsOnStartup =
    app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value.RunMigrationsOnStartup
    && !app.Environment.IsDevelopment();
if (migrateOnly || runMigrationsOnStartup)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        if (migrateOnly)
        {
            Log.Information("Migrations applied (--migrate); exiting without starting the server.");
            return;
        }
    }
}

// Demo mode is opt-in: never seed the demo tenant unless explicitly enabled.
// Production defaults to off (Demo:Enabled=false in appsettings.json).
var demoEnabled = bool.TryParse(builder.Configuration["Demo:Enabled"], out var demoSetting) && demoSetting;
if (demoEnabled)
{
    try
    {
        await DemoUserSeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        LogDemoSeedingFailed(logger, ex, ex.Message);
    }
}

await app.RunAsync();

// Exposed so the integration test project can boot the app with WebApplicationFactory<Program>.
public partial class Program
{
    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Demo seeding failed: {Message}")]
    private static partial void LogDemoSeedingFailed(
        Microsoft.Extensions.Logging.ILogger logger,
        Exception exception,
        string message);

    internal static void AddUserPolicy(
        Microsoft.AspNetCore.RateLimiting.RateLimiterOptions options,
        string name,
        IConfiguration configuration,
        string configKey,
        int defaultPerMinute)
    {
        var permits = int.TryParse(configuration[configKey], out var configured) && configured > 0
            ? configured
            : defaultPerMinute;

        options.AddPolicy(name, httpContext =>
        {
            var partition = httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter($"{name}:{partition}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        });
    }

    /// <summary>
    /// The signing key plus <c>Jwt:PreviousKeys</c> (entries of <c>Id</c> and
    /// <c>Key</c>) still accepted for validation while a rotation drains.
    /// </summary>
    internal static List<SymmetricSecurityKey> BuildSigningKeys(JwtOptions jwt)
    {
        var keys = new List<SymmetricSecurityKey>
        {
            new(Encoding.UTF8.GetBytes(jwt.Key)) { KeyId = jwt.KeyId }
        };

        foreach (var previous in jwt.PreviousKeys)
        {
            if (string.IsNullOrWhiteSpace(previous.Key) || previous.Key.Length < 32)
                continue;
            keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(previous.Key)) { KeyId = previous.Id });
        }

        return keys;
    }

    /// <summary>
    /// Bounds the readiness probe: without a short timeout a hung database would
    /// make <c>/ready</c> hang until the probe's own deadline. Only used by the
    /// health check; the app's connection string is left untouched.
    /// </summary>
    internal static string? BuildHealthCheckConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        return new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = 3,
            CommandTimeout = 3
        }.ConnectionString;
    }
}
