using System.Net;
using Bikontrol.Tests.Integration.Infrastructure;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// /health is liveness (process only) and /ready is readiness (PostgreSQL).
/// </summary>
[Collection("api")]
public sealed class HealthEndpointTests
{
    private readonly PostgresApiFactory _factory;

    public HealthEndpointTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task Health_ShouldBeHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [RequiresDockerFact]
    public async Task Ready_WhenDatabaseIsReachable_ShouldBeHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
