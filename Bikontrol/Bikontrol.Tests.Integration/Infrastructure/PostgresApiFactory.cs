using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
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

    private const string Bucket = "bikontrol-test";
    private const int StoragePort = 9090;

    // A real S3-compatible HTTP server (Adobe S3Mock), so the storage adapter and
    // the signed media endpoint are exercised end to end, not against a fake.
    private readonly IContainer _storage = new ContainerBuilder()
        .WithImage("adobe/s3mock:5.2.3")
        .WithPortBinding(StoragePort, assignRandomHostPort: true)
        .WithEnvironment("COM_ADOBE_TESTING_S3MOCK_STORE_INITIAL_BUCKETS", Bucket)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPath("/").ForPort(StoragePort)))
        .Build();

    public async Task InitializeAsync()
    {
        if (!DockerProbe.IsAvailable)
        {
            return;
        }

        await Task.WhenAll(_database.StartAsync(), _storage.StartAsync());

        var storageUrl = $"http://{_storage.Hostname}:{_storage.GetMappedPublicPort(StoragePort)}";

        Environment.SetEnvironmentVariable("Storage__Enabled", "true");
        Environment.SetEnvironmentVariable("Storage__ServiceUrl", storageUrl);
        Environment.SetEnvironmentVariable("Storage__Bucket", Bucket);
        Environment.SetEnvironmentVariable("Storage__AccessKey", "test-access-key");
        Environment.SetEnvironmentVariable("Storage__SecretKey", "test-secret-key");

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
        // No stamp caching, so a password change revokes access tokens at once.
        Environment.SetEnvironmentVariable("Jwt__SecurityStampCacheSeconds", "0");
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
            await _storage.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}
