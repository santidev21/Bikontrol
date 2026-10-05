using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bikontrol.Persistence;
using Bikontrol.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// Pins the destructive account-deletion path against real PostgreSQL: the owned
/// data must be gone and the account anonymized, while the audit trail keeps a
/// stable owner. Runs with its own user so it never disturbs the shared fixture
/// used by the write-path tests.
/// </summary>
[Collection("api")]
public sealed class AccountDeletionIntegrationTests
{
    private const string PngDataUrl =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private readonly PostgresApiFactory _factory;

    public AccountDeletionIntegrationTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task DeleteMyAccount_ShouldPurgeOwnedDataAndAnonymize()
    {
        const string password = "Secret123!";
        var email = $"delete-{Guid.NewGuid():N}@bikontrol.test";

        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            fullName = "Delete Me"
        });
        register.EnsureSuccessStatusCode();
        var auth = (await register.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var me = await client.GetFromJsonAsync<ProfileResponse>("/api/users/me");
        var userId = me!.Id;

        // Owned data: motorcycle → maintenance → record → attachment.
        var motoResponse = await client.PostAsJsonAsync("/api/motorcycles", new
        {
            name = "Temp",
            brand = "Yamaha",
            year = DateTime.UtcNow.Year,
            nickname = "Temp",
            km = 1000,
            displacement = 150,
            plate = "DEL123",
            image = "default.png"
        });
        motoResponse.EnsureSuccessStatusCode();
        var moto = (await motoResponse.Content.ReadFromJsonAsync<MotorcycleResponse>())!;

        var maintenanceResponse = await client.PostAsJsonAsync("/api/maintenances/mine", new
        {
            motorcycleId = moto.Id,
            name = "Aceite",
            description = "Aceite",
            trackingType = "Km",
            kmInterval = 1500,
            timeIntervalWeeks = 0
        });
        maintenanceResponse.EnsureSuccessStatusCode();
        var maintenance = (await maintenanceResponse.Content.ReadFromJsonAsync<MaintenanceResponse>())!;

        var recordResponse = await client.PostAsJsonAsync("/api/maintenances/records", new
        {
            motorcycleId = moto.Id,
            userMaintenanceId = maintenance.Id,
            performedAt = DateTime.UtcNow,
            performedKm = 1000
        });
        recordResponse.EnsureSuccessStatusCode();
        var record = (await recordResponse.Content.ReadFromJsonAsync<RecordResponse>())!;

        var attachResponse = await client.PostAsJsonAsync($"/api/maintenances/records/{record.Id}/attachments", new
        {
            dataUrl = PngDataUrl,
            fileName = "factura.png"
        });
        attachResponse.EnsureSuccessStatusCode();

        // The export must have data before deletion, so the post-delete emptiness is meaningful.
        var export = await client.GetFromJsonAsync<ExportResponse>("/api/users/me/export");
        Assert.Single(export!.Motorcycles);

        // --- Delete the account --------------------------------------------------
        var delete = await client.PostAsJsonAsync("/api/users/me/delete", new { confirmation = "ELIMINAR", password });
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

        // The old credentials no longer log in.
        var freshClient = _factory.CreateClient();
        var login = await freshClient.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.True(
            login.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"expected the deleted account to be unable to log in, got {(int)login.StatusCode}");

        // --- Everything owned is gone, the row is anonymized ---------------------
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Motorcycles.AnyAsync(m => m.UserId == userId));
        Assert.False(await db.UserMaintenances.AnyAsync(m => m.UserId == userId));
        Assert.False(await db.MotorcycleMaintenanceRecords.AnyAsync(r => r.MotorcycleId == moto.Id));
        Assert.False(await db.MaintenanceRecordAttachments.AnyAsync(a => a.MotorcycleMaintenanceRecordId == record.Id));
        Assert.False(await db.RefreshTokens.AnyAsync(t => t.UserId == userId));

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        Assert.NotEqual(email, user.Email);
        Assert.StartsWith("deleted+", user.Email);
        Assert.Equal("Cuenta eliminada", user.FullName);
        Assert.False(user.RemindersEnabled);
    }

    private sealed record AuthResponse(string Token, string RefreshToken);

    private sealed record ProfileResponse(Guid Id, string Email);

    private sealed record MotorcycleResponse(Guid Id);

    private sealed record MaintenanceResponse(Guid Id);

    private sealed record RecordResponse(Guid Id);

    private sealed record ExportResponse(List<MotorcycleResponse> Motorcycles);
}
