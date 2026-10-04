using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Bikontrol.Tests.Integration.Infrastructure;

/// <summary>
/// Boots the real API against a disposable PostgreSQL container, applying the
/// production migrations. Configuration is injected through environment
/// variables because <c>Program.cs</c> reads it before the host is built.
/// Docker is required; tests using it are skipped when Docker is unavailable.
/// </summary>
public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Fixed dummy key for the disposable Testcontainers host; never used outside tests.
    private const string JwtKey = "integration-test-key-0123456789abcdef0123456789"; // gitleaks:allow

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        if (!DockerProbe.IsAvailable)
        {
            return;
        }

        await _database.StartAsync();

        // Program.cs reads configuration (and fails fast on Jwt:Key) before the
        // factory's ConfigureAppConfiguration runs, so use env vars. "Testing" is
        // not Development, which makes the API apply migrations on startup.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _database.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "Bikontrol.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "Bikontrol.Tests");
        Environment.SetEnvironmentVariable("Jwt__ExpireMinutes", "15");
        Environment.SetEnvironmentVariable("Jwt__RefreshExpireDays", "30");
        Environment.SetEnvironmentVariable("Google__ClientId", "test.apps.googleusercontent.com");
        // The demo tenant is opt-in; the integration tests exercise it explicitly.
        Environment.SetEnvironmentVariable("Demo__Enabled", "true");
        // Integration tests register users directly and use their tokens, so they
        // opt out of email confirmation (which is covered by unit tests).
        Environment.SetEnvironmentVariable("EmailConfirmation__Required", "false");
        // Account lockout is exercised explicitly by Login_AfterMaxFailedAttempts;
        // keep the default (5 attempts) so the test can assert the lock.
        // Metrics are opt-in; enable them so the scrape endpoint can be asserted.
        Environment.SetEnvironmentVariable("Metrics__Enabled", "true");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Let tests pick their own client IP so the per-IP rate limiter can be
        // exercised without tests sharing one budget (TestServer has no IP).
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, ClientIpStartupFilter>());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (DockerProbe.IsAvailable)
        {
            await _database.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}
