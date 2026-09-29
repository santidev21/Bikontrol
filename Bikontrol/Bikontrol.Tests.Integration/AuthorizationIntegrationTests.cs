using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bikontrol.Tests.Integration.Infrastructure;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// Authorization boundary over the real API + PostgreSQL: demo accounts are
/// read-only and users cannot touch each other's motorcycles.
/// </summary>
[Collection("api")]
public sealed class AuthorizationIntegrationTests
{
    private readonly PostgresApiFactory _factory;

    public AuthorizationIntegrationTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task DemoUser_WriteMotorcycle_ShouldReturn403AndPersistNothing()
    {
        var client = _factory.CreateClient();
        var demo = await client.PostAsJsonAsync("/api/auth/demo", new { });
        demo.EnsureSuccessStatusCode();
        var auth = await demo.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var before = await client.GetFromJsonAsync<List<MotorcycleResponse>>("/api/motorcycles/mine");

        var create = await client.PostAsJsonAsync("/api/motorcycles", new
        {
            name = "XTZ 150",
            brand = "Yamaha",
            year = DateTime.UtcNow.Year,
            nickname = "Demo write",
            km = 100,
            displacement = 150,
            plate = "DEM999",
            image = "default.png"
        });

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var after = await client.GetFromJsonAsync<List<MotorcycleResponse>>("/api/motorcycles/mine");
        Assert.Equal(before!.Count, after!.Count);
    }

    [RequiresDockerFact]
    public async Task User_CannotReadOrDeleteOtherUsersMotorcycle()
    {
        var ownerClient = await RegisterClientAsync();
        var created = await CreateMotorcycleAsync(ownerClient);

        var otherClient = await RegisterClientAsync();

        var get = await otherClient.GetAsync($"/api/motorcycles/{created.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);

        var delete = await otherClient.DeleteAsync($"/api/motorcycles/{created.Id}");
        Assert.True(delete.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound, $"Unexpected {delete.StatusCode}");

        // The owner's motorcycle is untouched.
        var mine = await ownerClient.GetFromJsonAsync<List<MotorcycleResponse>>("/api/motorcycles/mine");
        Assert.Contains(mine!, m => m.Id == created.Id);
    }

    private async Task<HttpClient> RegisterClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@bikontrol.test";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123!",
            fullName = "Integration User"
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private static async Task<MotorcycleResponse> CreateMotorcycleAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/motorcycles", new
        {
            name = "XTZ 150",
            brand = "Yamaha",
            year = DateTime.UtcNow.Year,
            nickname = "La azul",
            km = 1000,
            displacement = 150,
            plate = $"ABC{Random.Shared.Next(100, 999)}",
            image = "default.png"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MotorcycleResponse>())!;
    }

    private sealed record AuthResponse(string Token, string RefreshToken);
    private sealed record MotorcycleResponse(Guid Id, string Name, int Km);
}
