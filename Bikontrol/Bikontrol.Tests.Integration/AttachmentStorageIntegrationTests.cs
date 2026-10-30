using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Services;
using Bikontrol.Domain.Entities;
using Bikontrol.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// Attachments against real PostgreSQL and a real S3-compatible service (MinIO):
/// the bytes land in the bucket, are served through signed same-origin URLs, are
/// removed with the attachment or the account, and legacy rows can be migrated.
/// </summary>
[Collection("api")]
public sealed class AttachmentStorageIntegrationTests
{
    // 1x1 transparent PNG.
    private const string PngDataUrl =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private readonly PostgresApiFactory _factory;

    public AttachmentStorageIntegrationTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task Attachment_IsStoredInTheBucketAndServedThroughASignedUrl()
    {
        var (client, recordId) = await RegisterWithRecordAsync();

        var created = await AddAttachmentAsync(client, recordId);
        var storageKey = await StorageKeyOfAsync(created.Id);

        Assert.NotNull(storageKey);
        Assert.StartsWith("/api/media/attachments/", created.DataUrl);
        Assert.DoesNotContain("base64", created.DataUrl);

        // The image URL works without any Authorization header (it is an <img src>).
        var anonymous = NewAnonymousClient();
        var image = await anonymous.GetAsync(created.DataUrl);

        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ImageDataUrlValidator.Decode(PngDataUrl), await image.Content.ReadAsByteArrayAsync());
        Assert.Contains("max-age=21600", image.Headers.CacheControl?.ToString());
        Assert.NotNull(image.Headers.ETag);

        // The same bytes are really in the bucket.
        using var stored = await Storage.GetAsync(storageKey!);
        Assert.NotNull(stored);
        Assert.Equal("image/png", stored!.ContentType);

        // A conditional request revalidates with 304.
        var request = new HttpRequestMessage(HttpMethod.Get, created.DataUrl);
        request.Headers.IfNoneMatch.Add(image.Headers.ETag!);
        Assert.Equal(HttpStatusCode.NotModified, (await anonymous.SendAsync(request)).StatusCode);
    }

    [RequiresDockerFact]
    public async Task MediaUrl_WithATamperedSignatureOrAnotherId_ReturnsNotFound()
    {
        var (client, recordId) = await RegisterWithRecordAsync();
        var created = await AddAttachmentAsync(client, recordId);
        var anonymous = NewAnonymousClient();

        var tampered = await anonymous.GetAsync(created.DataUrl + "x");
        var otherId = await anonymous.GetAsync(
            created.DataUrl.Replace(created.Id.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal));
        var unsigned = await anonymous.GetAsync($"/api/media/attachments/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, tampered.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherId.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unsigned.StatusCode);
    }

    [RequiresDockerFact]
    public async Task DeletingAnAttachment_RemovesItsObjectFromTheBucket()
    {
        var (client, recordId) = await RegisterWithRecordAsync();
        var created = await AddAttachmentAsync(client, recordId);
        var storageKey = (await StorageKeyOfAsync(created.Id))!;

        var delete = await client.DeleteAsync($"/api/maintenances/records/{recordId}/attachments/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Null(await Storage.GetAsync(storageKey));
        Assert.Equal(HttpStatusCode.NotFound, (await NewAnonymousClient().GetAsync(created.DataUrl)).StatusCode);
    }

    [RequiresDockerFact]
    public async Task DeletingTheAccount_RemovesItsObjectsFromTheBucket()
    {
        var (client, recordId) = await RegisterWithRecordAsync();
        var created = await AddAttachmentAsync(client, recordId);
        var storageKey = (await StorageKeyOfAsync(created.Id))!;
        Assert.NotNull(await Storage.GetAsync(storageKey));

        var delete = await client.PostAsJsonAsync("/api/users/me/delete", new { confirmation = "ELIMINAR", password = Password });

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Null(await Storage.GetAsync(storageKey));
    }

    [RequiresDockerFact]
    public async Task AccountExport_InlinesTheStoredImage()
    {
        var (client, recordId) = await RegisterWithRecordAsync();
        await AddAttachmentAsync(client, recordId);

        var export = await client.GetFromJsonAsync<JsonElement>("/api/users/me/export");

        var attachment = Assert.Single(export.GetProperty("attachments").EnumerateArray());
        Assert.Equal(PngDataUrl, attachment.GetProperty("dataUrl").GetString());
    }

    [RequiresDockerFact]
    public async Task MigrationCommand_MovesLegacyDatabaseImagesIntoTheBucket()
    {
        var (client, recordId) = await RegisterWithRecordAsync();
        var legacyId = await InsertLegacyAttachmentAsync(recordId);

        using (var scope = _factory.Services.CreateScope())
        {
            var batch = await scope.ServiceProvider.GetRequiredService<ImageStorageMigrator>().MigrateBatchAsync(50);
            Assert.True(batch.Migrated >= 1);
            Assert.Equal(0, batch.Failed);
        }

        var key = await StorageKeyOfAsync(legacyId);
        Assert.NotNull(key);
        using var stored = await Storage.GetAsync(key);
        Assert.NotNull(stored);

        // And it is still listed and served for its owner.
        var list = await client.GetFromJsonAsync<List<AttachmentResponse>>($"/api/maintenances/records/{recordId}/attachments");
        var served = await NewAnonymousClient().GetAsync(Assert.Single(list!, a => a.Id == legacyId).DataUrl);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
    }

    private const string Password = "Secret123!";

    private IObjectStorage Storage => _factory.Services.GetRequiredService<IObjectStorage>();

    private async Task<string?> StorageKeyOfAsync(Guid attachmentId)
    {
        using var scope = _factory.Services.CreateScope();
        var attachment = await scope.ServiceProvider
            .GetRequiredService<IMaintenanceRecordAttachmentRepository>().GetByIdAsync(attachmentId);
        return attachment?.StorageKey;
    }

    private async Task<Guid> InsertLegacyAttachmentAsync(Guid recordId)
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMaintenanceRecordAttachmentRepository>();
        var legacy = new MaintenanceRecordAttachment
        {
            MotorcycleMaintenanceRecordId = recordId,
            DataUrl = PngDataUrl,
            ContentType = "image/png",
            SizeBytes = ImageDataUrlValidator.Decode(PngDataUrl).Length
        };
        await repo.AddAsync(legacy);
        await repo.SaveChangesAsync();
        return legacy.Id;
    }

    private HttpClient NewAnonymousClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ClientIpStartupFilter.HeaderName, TestClientIps.Next());
        return client;
    }

    private async Task<AttachmentResponse> AddAttachmentAsync(HttpClient client, Guid recordId)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/maintenances/records/{recordId}/attachments",
            new { dataUrl = PngDataUrl, fileName = "factura.png" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AttachmentResponse>())!;
    }

    private async Task<(HttpClient Client, Guid RecordId)> RegisterWithRecordAsync()
    {
        var client = NewAnonymousClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"storage-{Guid.NewGuid():N}@bikontrol.test",
            password = Password,
            fullName = "Storage User"
        });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var moto = await client.PostAsJsonAsync("/api/motorcycles", new
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
        moto.EnsureSuccessStatusCode();
        var motoId = (await moto.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var maintenance = await client.PostAsJsonAsync("/api/maintenances/mine", new
        {
            motorcycleId = motoId,
            name = "Cambio de aceite",
            description = "Aceite",
            trackingType = "Km",
            kmInterval = 1500,
            timeIntervalWeeks = 0
        });
        maintenance.EnsureSuccessStatusCode();
        var maintenanceId = (await maintenance.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var record = await client.PostAsJsonAsync("/api/maintenances/records", new
        {
            motorcycleId = motoId,
            userMaintenanceId = maintenanceId,
            performedAt = DateTime.UtcNow,
            performedKm = 1050
        });
        record.EnsureSuccessStatusCode();
        return (client, (await record.Content.ReadFromJsonAsync<IdResponse>())!.Id);
    }

    private sealed record IdResponse(Guid Id);

    private sealed record AuthResponse(string Token, string RefreshToken);

    private sealed record AttachmentResponse(Guid Id, string DataUrl);
}
