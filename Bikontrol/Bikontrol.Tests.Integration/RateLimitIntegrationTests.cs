using System.Net;
using System.Net.Http.Json;
using Bikontrol.Tests.Integration.Infrastructure;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// Pins the auth endpoint rate limiting (policy "auth": 10 requests per minute
/// per client IP). Each test uses its own synthetic client IP so it does not
/// share the budget with the rest of the suite.
/// </summary>
[Collection("api")]
public sealed class RateLimitIntegrationTests
{
    private const int PermitLimit = 10;

    private readonly PostgresApiFactory _factory;

    public RateLimitIntegrationTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task AuthLogin_WhenExceedingPerIpLimit_ShouldReturn429()
    {
        var client = _factory.CreateClient();
        // TEST-NET-3 address unique to this test run -> its own limiter partition.
        var clientIp = $"203.0.113.{Random.Shared.Next(1, 254)}";

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < PermitLimit + 1; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new { email = "nobody@bikontrol.test", password = "Wrong123!" })
            };
            request.Headers.Add(ClientIpStartupFilter.HeaderName, clientIp);

            using var response = await client.SendAsync(request);
            statuses.Add(response.StatusCode);
        }

        // The first 10 attempts are processed (bad credentials -> 401); the 11th
        // is rejected by the limiter before reaching the endpoint.
        Assert.All(
            statuses.Take(PermitLimit),
            status => Assert.NotEqual(HttpStatusCode.TooManyRequests, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[PermitLimit]);
    }
}
