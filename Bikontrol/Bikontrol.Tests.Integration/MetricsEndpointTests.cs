using System.Net;
using Bikontrol.Tests.Integration.Infrastructure;

namespace Bikontrol.Tests.Integration;

[Collection("api")]
public sealed class MetricsEndpointTests
{
    private readonly PostgresApiFactory _factory;

    public MetricsEndpointTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task Metrics_ShouldExposePrometheusText()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.10");

        var response = await client.GetAsync("/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        // Prometheus exposition format starts with # HELP / # TYPE comment lines.
        Assert.Contains("# HELP", body);
        // The runtime instrumentation always emits the .NET meter namespace.
        Assert.Contains("dotnet_", body);
    }
}
