using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
    private const string JwtKey = "integration-test-key-0123456789abcdef0123456789";

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
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
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
