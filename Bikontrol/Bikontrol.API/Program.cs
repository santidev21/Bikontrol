using AutoMapper;
using Bikontrol.API.Middleware;
using Bikontrol.Application.Interfaces;
using Bikontrol.Infrastructure;
using Bikontrol.Infrastructure.Authentication;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Seed;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Persistence;
using Bikontrol.Persistence.Entities;
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
using Serilog;
using Serilog.Events;
using System.Text;
using System.Threading.RateLimiting;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

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
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Bikontrol.API")
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .WriteTo.Console(
            outputTemplate: context.HostingEnvironment.IsDevelopment()
                ? "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}"
                : "{Timestamp:o} [{Level:u3}] {TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}");
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

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or too short (must be at least 32 characters). Set Jwt__Key via environment variables or appsettings.Development.json.");
}

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(jwtKey);
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
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

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    foreach (var cidr in networks)
    {
        if (IPNetwork.TryParse(cidr, out var network))
            options.KnownNetworks.Add(network);
    }
});

// Rate limiting (auth endpoints). Partitioned per client IP: the previous
// single fixed window throttled the whole application to 10 auth requests/min.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Health: /health is a liveness probe (no dependencies, the process is up);
// /ready is a readiness probe that fails when PostgreSQL is unreachable.
// The DB check uses its own short-timeout connection so a hung database makes
// /ready fail fast instead of hanging the probe.
builder.Services.AddHealthChecks()
    .AddNpgSql(
        BuildHealthCheckConnectionString(builder.Configuration.GetConnectionString("DefaultConnection")),
        name: "postgresql",
        tags: new[] { "ready" });

// Services & Identity tools
builder.Services.AddInfrastructure();
builder.Services.AddPersistence();

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Bikontrol API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIsInR5..."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure InvalidModelStateResponseFactory
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(x => x.ErrorMessage))
            .ToList();

        var response = new { error = string.Join(" | ", errors) };
        return new BadRequestObjectResult(response);
    };
});
var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// Liveness: cheap, no dependency checks (used by the web UI/container probes).
app.MapHealthChecks("/health");
// Readiness: fails when PostgreSQL is unreachable, so orchestrators can hold
// traffic back until the API can actually serve it.
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

if (!app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
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
        logger.LogWarning(ex, "Demo seeding failed: {Message}", ex.Message);
    }
}

app.Run();

// Exposed so the integration test project can boot the app with WebApplicationFactory<Program>.
public partial class Program
{
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
