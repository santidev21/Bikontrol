using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bikontrol.Persistence;
using Bikontrol.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task SoftDeleteMotorcycle_ShouldPersistAndPurgeDependentData()
    {
        var client = await AuthenticatedClientAsync();
        var created = await CreateMotorcycleAsync(client, km: 1000);
        var maintenance = await CreateMaintenanceAsync(client, created.Id);
        await RegisterRecordAsync(client, created.Id, maintenance.Id, performedKm: 1000);

        var response = await client.DeleteAsync($"/api/motorcycles/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var mine = await client.GetFromJsonAsync<List<MotorcycleResponse>>("/api/motorcycles/mine");
        Assert.DoesNotContain(mine!, m => m.Id == created.Id);

        // The soft-delete must also purge the motorcycle's records and disable
        // its maintenances, so nothing is left pointing at a disabled parent.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.MotorcycleMaintenanceRecords.AnyAsync(r => r.MotorcycleId == created.Id));
        Assert.False(await db.UserMaintenances.AnyAsync(m => m.MotorcycleId == created.Id && m.IsEnabled));
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
    public async Task CreateAndDeleteUserMaintenance_ShouldPersistAndPurgeRecords()
    {
        var client = await AuthenticatedClientAsync();
        var created = await CreateMotorcycleAsync(client, km: 1000);
        var maintenance = await CreateMaintenanceAsync(client, created.Id);

        var afterCreate = await client.GetFromJsonAsync<List<MaintenanceResponse>>("/api/maintenances/mine");
        Assert.Contains(afterCreate!, m => m.Id == maintenance.Id);

        var record = await RegisterRecordAsync(client, created.Id, maintenance.Id, performedKm: 1000);

        var deleteResponse = await client.DeleteAsync($"/api/maintenances/mine/{maintenance.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<List<MaintenanceResponse>>("/api/maintenances/mine");
        Assert.DoesNotContain(afterDelete!, m => m.Id == maintenance.Id);

        // Deleting a maintenance must also purge its records (audit check 8).
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.MotorcycleMaintenanceRecords.AnyAsync(r => r.Id == record.Id));
    }

    [RequiresDockerFact]
    public async Task AddAndDeleteMaintenanceAttachment_ShouldPersist()
    {
        var client = await AuthenticatedClientAsync();
        var moto = await CreateMotorcycleAsync(client, km: 1000);

        // Create a user maintenance, then register a record for it.
        var maintenanceResponse = await client.PostAsJsonAsync("/api/maintenances/mine", new
        {
            motorcycleId = moto.Id,
            name = "Cambio de aceite",
            description = "Aceite",
            trackingType = "Km",
            kmInterval = 1500,
            timeIntervalWeeks = 0
        });
        maintenanceResponse.EnsureSuccessStatusCode();
        var maintenance = await maintenanceResponse.Content.ReadFromJsonAsync<MaintenanceResponse>();

        var recordResponse = await client.PostAsJsonAsync("/api/maintenances/records", new
        {
            motorcycleId = moto.Id,
            userMaintenanceId = maintenance!.Id,
            performedAt = DateTime.UtcNow,
            performedKm = 1000
        });
        recordResponse.EnsureSuccessStatusCode();
        var record = await recordResponse.Content.ReadFromJsonAsync<RecordResponse>();

        // Attach a 1x1 PNG and confirm it round-trips.
        var attachResponse = await client.PostAsJsonAsync($"/api/maintenances/records/{record!.Id}/attachments", new
        {
            dataUrl = PngDataUrl,
            fileName = "factura.png"
        });
        attachResponse.EnsureSuccessStatusCode();
        var attachment = await attachResponse.Content.ReadFromJsonAsync<AttachmentResponse>();
        Assert.Equal("image/png", attachment!.ContentType);

        var list = await client.GetFromJsonAsync<List<AttachmentResponse>>($"/api/maintenances/records/{record.Id}/attachments");
        Assert.Contains(list!, a => a.Id == attachment.Id);

        var deleteResponse = await client.DeleteAsync($"/api/maintenances/records/{record.Id}/attachments/{attachment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<List<AttachmentResponse>>($"/api/maintenances/records/{record.Id}/attachments");
        Assert.DoesNotContain(afterDelete!, a => a.Id == attachment.Id);
    }

    // 1x1 transparent PNG.
    private const string PngDataUrl =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    [RequiresDockerFact]
    public async Task WriteOperation_ShouldRecordAuditLog()
    {
        var client = await AuthenticatedClientAsync();
        var moto = await CreateMotorcycleAsync(client, km: 1010);

        // The audit row is written in the same SaveChanges as the motorcycle, so
        // it must be visible once the request returns.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await db.AuditLogs
            .Where(a => a.EntityName == "Motorcycle" && a.EntityId == moto.Id.ToString())
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(audit);
        Assert.Equal("Created", audit!.Action);
        Assert.NotNull(audit.UserId);
        Assert.Contains("Name", audit.Changes);

        // The same trail is exposed to the user through the API.
        var activity = await client.GetFromJsonAsync<List<ActivityResponse>>("/api/users/me/activity");
        Assert.Contains(activity!, a => a.EntityName == "Motorcycle" && a.Action == "Created");
    }

    [RequiresDockerFact]
    public async Task RegisterRecordWithCost_ShouldPersistAndAppearInStatistics()
    {
        var client = await AuthenticatedClientAsync();
        var moto = await CreateMotorcycleAsync(client, km: 2000);

        var maintenanceResponse = await client.PostAsJsonAsync("/api/maintenances/mine", new
        {
            motorcycleId = moto.Id,
            name = "Cambio de aceite",
            description = "Aceite",
            trackingType = "Km",
            kmInterval = 1500,
            timeIntervalWeeks = 0
        });
        maintenanceResponse.EnsureSuccessStatusCode();
        var maintenance = await maintenanceResponse.Content.ReadFromJsonAsync<MaintenanceResponse>();

        var recordResponse = await client.PostAsJsonAsync("/api/maintenances/records", new
        {
            motorcycleId = moto.Id,
            userMaintenanceId = maintenance!.Id,
            performedAt = DateTime.UtcNow,
            performedKm = 2000,
            cost = 55.50m
        });
        recordResponse.EnsureSuccessStatusCode();
        var record = await recordResponse.Content.ReadFromJsonAsync<RecordCostResponse>();
        Assert.Equal(55.50m, record!.Cost);

        // The cost feeds the read-only statistics summary.
        var summary = await client.GetFromJsonAsync<StatisticsResponse>("/api/statistics/summary");
        Assert.Contains(summary!.CostByMotorcycle, c => c.MotorcycleId == moto.Id && c.Cost >= 55.50m);
    }

    private static async Task<MaintenanceResponse> CreateMaintenanceAsync(HttpClient client, Guid motorcycleId)
    {
        var response = await client.PostAsJsonAsync("/api/maintenances/mine", new
        {
            motorcycleId,
            name = "Cambio de aceite",
            description = "Aceite",
            trackingType = "Km",
            kmInterval = 1500,
            timeIntervalWeeks = 0
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MaintenanceResponse>())!;
    }

    private static async Task<RecordResponse> RegisterRecordAsync(
        HttpClient client, Guid motorcycleId, Guid userMaintenanceId, int performedKm)
    {
        var response = await client.PostAsJsonAsync("/api/maintenances/records", new
        {
            motorcycleId,
            userMaintenanceId,
            performedAt = DateTime.UtcNow,
            performedKm
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RecordResponse>())!;
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

    private sealed record RecordResponse(Guid Id);

    private sealed record AttachmentResponse(Guid Id, Guid RecordId, string DataUrl, string ContentType, string? FileName);

    private sealed record ActivityResponse(long Id, string EntityName, string Action, DateTime CreatedAt);

    private sealed record RecordCostResponse(Guid Id, decimal? Cost);

    private sealed record MotorcycleCost(Guid MotorcycleId, string Name, decimal Cost, decimal? CostPerKm);

    private sealed record StatisticsResponse(decimal TotalCost, decimal? CostPerKm, List<MotorcycleCost> CostByMotorcycle);
}
