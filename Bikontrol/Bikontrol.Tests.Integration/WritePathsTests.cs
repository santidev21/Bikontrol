using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bikontrol.Tests.Integration.Infrastructure;

namespace Bikontrol.Tests.Integration;

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<PostgresApiFactory>
{
}

/// <summary>
/// End-to-end tests over the real API + PostgreSQL. They pin the write paths so
/// a repository/service that forgets to persist is caught (the unit tests use
/// non-persisting fakes and cannot detect that).
/// </summary>
[Collection("api")]
public sealed class WritePathsTests
{
    private static readonly SemaphoreSlim AuthLock = new(1, 1);
    private static string? _cachedToken;

    private readonly PostgresApiFactory _factory;

    public WritePathsTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task CreateMotorcycle_ShouldPersistAndExposeKm()
    {
        var client = await AuthenticatedClientAsync();

        var created = await CreateMotorcycleAsync(client, km: 1234);

        Assert.Equal(1234, created.Km);

        var mine = await client.GetFromJsonAsync<List<MotorcycleResponse>>("/api/motorcycles/mine");
        Assert.Contains(mine!, m => m.Id == created.Id && m.Km == 1234);
    }

    [RequiresDockerFact]
    public async Task UpdateMotorcycle_ShouldPersist()
    {
        var client = await AuthenticatedClientAsync();
        var created = await CreateMotorcycleAsync(client, km: 1000);

        var update = new
        {
            name = "Renombrada",
            brand = "Yamaha",
            year = DateTime.UtcNow.Year,
            nickname = "Nueva",
            km = 1000,
            displacement = 150,
            plate = "ZZZ999",
            image = "default.png"
        };
        var response = await client.PutAsJsonAsync($"/api/motorcycles/{created.Id}", update);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetched = await client.GetFromJsonAsync<MotorcycleResponse>($"/api/motorcycles/{created.Id}");
        Assert.Equal("Renombrada", fetched!.Name);
    }

    [RequiresDockerFact]
    public async Task SoftDeleteMotorcycle_ShouldPersist()
    {
        var client = await AuthenticatedClientAsync();
        var created = await CreateMotorcycleAsync(client, km: 1000);

        var response = await client.DeleteAsync($"/api/motorcycles/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var mine = await client.GetFromJsonAsync<List<MotorcycleResponse>>("/api/motorcycles/mine");
        Assert.DoesNotContain(mine!, m => m.Id == created.Id);
    }

    [RequiresDockerFact]
    public async Task AddKmHistory_ShouldPersist()
    {
        var client = await AuthenticatedClientAsync();
        var created = await CreateMotorcycleAsync(client, km: 1000);

        var response = await client.PostAsJsonAsync($"/api/motorcycles/{created.Id}/km-history", new { km = 2500 });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var current = await client.GetFromJsonAsync<CurrentKmResponse>($"/api/motorcycles/{created.Id}/km/current");
        Assert.Equal(2500, current!.Km);
    }

    [RequiresDockerFact]
    public async Task CreateAndDeleteUserMaintenance_ShouldPersist()
    {
        var client = await AuthenticatedClientAsync();
        var created = await CreateMotorcycleAsync(client, km: 1000);

        var payload = new
        {
            motorcycleId = created.Id,
            name = "Cambio de aceite",
            description = "Aceite",
            trackingType = "Km",
            kmInterval = 1500,
            timeIntervalWeeks = 0
        };
        var createResponse = await client.PostAsJsonAsync("/api/maintenances/mine", payload);
        createResponse.EnsureSuccessStatusCode();
        var maintenance = await createResponse.Content.ReadFromJsonAsync<MaintenanceResponse>();

        var afterCreate = await client.GetFromJsonAsync<List<MaintenanceResponse>>("/api/maintenances/mine");
        Assert.Contains(afterCreate!, m => m.Id == maintenance!.Id);

        var deleteResponse = await client.DeleteAsync($"/api/maintenances/mine/{maintenance!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<List<MaintenanceResponse>>("/api/maintenances/mine");
        Assert.DoesNotContain(afterDelete!, m => m.Id == maintenance.Id);
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync());
        return client;
    }

    private async Task<string> GetTokenAsync()
    {
        if (_cachedToken is not null)
        {
            return _cachedToken;
        }

        await AuthLock.WaitAsync();
        try
        {
            if (_cachedToken is null)
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
                _cachedToken = auth!.Token;
            }
        }
        finally
        {
            AuthLock.Release();
        }

        return _cachedToken!;
    }

    private static async Task<MotorcycleResponse> CreateMotorcycleAsync(HttpClient client, int km)
    {
        var payload = new
        {
            name = "XTZ 150",
            brand = "Yamaha",
            year = DateTime.UtcNow.Year,
            nickname = "La azul",
            km,
            displacement = 150,
            plate = $"ABC{km % 1000:D3}",
            image = "default.png"
        };

        var response = await client.PostAsJsonAsync("/api/motorcycles", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MotorcycleResponse>())!;
    }

    private sealed record AuthResponse(string Token, string RefreshToken);

    private sealed record MotorcycleResponse(Guid Id, string Name, int Km);

    private sealed record CurrentKmResponse(int Km);

    private sealed record MaintenanceResponse(Guid Id, string Name, bool IsEnabled);
}
