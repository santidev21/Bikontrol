using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Services;
using Bikontrol.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Bikontrol.Tests.Integration;

/// <summary>
/// Motorcycle photos against real PostgreSQL and a real S3-compatible service:
/// stored in the bucket, served behind signed URLs, kept when the edit form echoes
/// the URL back, replaced and removed cleanly, and migrated from the database.
/// </summary>
[Collection("api")]
public sealed class MotorcycleImageIntegrationTests
{
    // 1x1 transparent PNG.
    private const string PngDataUrl =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private const string Password = "Secret123!";

    private readonly PostgresApiFactory _factory;

    public MotorcycleImageIntegrationTests(PostgresApiFactory factory) => _factory = factory;

    [RequiresDockerFact]
    public async Task Photo_IsStoredInTheBucketAndServedThroughASignedUrl()
    {
        var client = await RegisterAsync();

        var created = await CreateAsync(client, PngDataUrl);
        var key = await ImageKeyOfAsync(created.Id);

        Assert.NotNull(key);
        Assert.StartsWith($"/api/media/motorcycles/{created.Id}?v=", created.Image);
        Assert.DoesNotContain("base64", created.Image);

        // The list carries the URL, never base64.
        var list = await client.GetFromJsonAsync<List<MotorcycleResponse>>("/api/motorcycles/mine");
        Assert.StartsWith("/api/media/motorcycles/", Assert.Single(list!, m => m.Id == created.Id).Image);

        // An <img src> has no Authorization header.
        var image = await NewAnonymousClient().GetAsync(created.Image);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ImageDataUrlValidator.Decode(PngDataUrl), await image.Content.ReadAsByteArrayAsync());
        Assert.Contains("max-age=21600", image.Headers.CacheControl?.ToString());

        using var stored = await Storage.GetAsync(key);
        Assert.NotNull(stored);
    }

    [RequiresDockerFact]
    public async Task EditingWithoutChangingThePhoto_KeepsIt_EvenWhenTheFormEchoesTheUrl()
    {
        var client = await RegisterAsync();
        var created = await CreateAsync(client, PngDataUrl);
        var keyBefore = await ImageKeyOfAsync(created.Id);

        // Exactly what the edit form does: GET, change the plate, PUT the same image field back.
        var current = await client.GetFromJsonAsync<MotorcycleResponse>($"/api/motorcycles/{created.Id}");
        var put = await client.PutAsJsonAsync($"/api/motorcycles/{created.Id}", Body(current!.Image, plate: "NEW123"));

        Assert.True(put.IsSuccessStatusCode, await put.Content.ReadAsStringAsync());
        Assert.Equal(keyBefore, await ImageKeyOfAsync(created.Id));
        Assert.NotNull(await Storage.GetAsync(keyBefore!));
        var after = await client.GetFromJsonAsync<MotorcycleResponse>($"/api/motorcycles/{created.Id}");
        Assert.Equal("NEW123", after!.Plate);
        Assert.StartsWith("/api/media/motorcycles/", after.Image);
    }

    [RequiresDockerFact]
    public async Task ReplacingThePhoto_ChangesTheUrlAndRemovesTheOldObject()
    {
        var client = await RegisterAsync();
        var created = await CreateAsync(client, PngDataUrl);
        var oldKey = (await ImageKeyOfAsync(created.Id))!;

        var put = await client.PutAsJsonAsync($"/api/motorcycles/{created.Id}", Body("data:image/webp;base64," + Convert.ToBase64String(new byte[] { 1, 2, 3 })));
        put.EnsureSuccessStatusCode();
        var after = await client.GetFromJsonAsync<MotorcycleResponse>($"/api/motorcycles/{created.Id}");
        var newKey = (await ImageKeyOfAsync(created.Id))!;

        Assert.NotEqual(oldKey, newKey);
        Assert.NotEqual(created.Image, after!.Image); // new ?v= so the browser re-fetches
        Assert.Null(await Storage.GetAsync(oldKey));
        Assert.NotNull(await Storage.GetAsync(newKey));
        var served = await NewAnonymousClient().GetAsync(after.Image);
        Assert.Equal("image/webp", served.Content.Headers.ContentType?.MediaType);
    }

    [RequiresDockerFact]
    public async Task RemovingThePhoto_FallsBackToTheDefaultAndDeletesTheObject()
    {
        var client = await RegisterAsync();
        var created = await CreateAsync(client, PngDataUrl);
        var key = (await ImageKeyOfAsync(created.Id))!;

        (await client.PutAsJsonAsync($"/api/motorcycles/{created.Id}", Body("default.png"))).EnsureSuccessStatusCode();

        var after = await client.GetFromJsonAsync<MotorcycleResponse>($"/api/motorcycles/{created.Id}");
        Assert.Equal("default.png", after!.Image);
        Assert.Null(await Storage.GetAsync(key));
        Assert.Equal(HttpStatusCode.NotFound, (await NewAnonymousClient().GetAsync(created.Image)).StatusCode);
    }

    [RequiresDockerFact]
    public async Task MediaUrl_WithATamperedSignature_ReturnsNotFound()
    {
        var client = await RegisterAsync();
        var created = await CreateAsync(client, PngDataUrl);
        var anonymous = NewAnonymousClient();

        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(created.Image + "x")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/media/motorcycles/{created.Id}")).StatusCode);
    }

    [RequiresDockerFact]
    public async Task DeletingAMotorcycle_DropsItsPhoto()
    {
        var client = await RegisterAsync();
        var created = await CreateAsync(client, PngDataUrl);
        var key = (await ImageKeyOfAsync(created.Id))!;

        var delete = await client.DeleteAsync($"/api/motorcycles/{created.Id}");

        Assert.True(delete.IsSuccessStatusCode);
        Assert.Null(await Storage.GetAsync(key));
    }

    [RequiresDockerFact]
    public async Task DeletingTheAccount_RemovesThePhotosFromTheBucket()
    {
        var client = await RegisterAsync();
        var created = await CreateAsync(client, PngDataUrl);
        var key = (await ImageKeyOfAsync(created.Id))!;

        var delete = await client.PostAsJsonAsync("/api/users/me/delete", new { confirmation = "ELIMINAR", password = Password });

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Null(await Storage.GetAsync(key));
    }

    [RequiresDockerFact]
    public async Task AccountExport_InlinesTheMotorcyclePhoto()
    {
        var client = await RegisterAsync();
        await CreateAsync(client, PngDataUrl);

        var export = await client.GetFromJsonAsync<JsonElement>("/api/users/me/export");

        var motorcycle = Assert.Single(export.GetProperty("motorcycles").EnumerateArray());
        Assert.Equal(PngDataUrl, motorcycle.GetProperty("image").GetString());
    }

    [RequiresDockerFact]
    public async Task MigrationCommand_MovesALegacyDatabasePhotoIntoTheBucket()
    {
        var client = await RegisterAsync();
        var created = await CreateAsync(client, "default.png");
        await SetLegacyPhotoAsync(created.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var batch = await scope.ServiceProvider.GetRequiredService<ImageStorageMigrator>().MigrateBatchAsync(50);
            Assert.True(batch.Migrated >= 1);
            Assert.Equal(0, batch.Failed);
        }

        var key = await ImageKeyOfAsync(created.Id);
        Assert.NotNull(key);
        var after = await client.GetFromJsonAsync<MotorcycleResponse>($"/api/motorcycles/{created.Id}");
        var served = await NewAnonymousClient().GetAsync(after!.Image);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal(ImageDataUrlValidator.Decode(PngDataUrl), await served.Content.ReadAsByteArrayAsync());
    }

    private IObjectStorage Storage => _factory.Services.GetRequiredService<IObjectStorage>();

    private async Task<string?> ImageKeyOfAsync(Guid motorcycleId)
    {
        using var scope = _factory.Services.CreateScope();
        var motorcycle = await scope.ServiceProvider.GetRequiredService<IMotorcycleRepository>().GetByIdAsync(motorcycleId);
        return motorcycle?.ImageKey;
    }

    private async Task SetLegacyPhotoAsync(Guid motorcycleId)
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMotorcycleRepository>();
        var motorcycle = (await repo.GetByIdAsync(motorcycleId))!;
        motorcycle.Image = PngDataUrl;
        motorcycle.ImageSizeBytes = ImageDataUrlValidator.Decode(PngDataUrl).Length;
        await repo.UpdateAsync(motorcycle);
        await repo.SaveChangesAsync();
    }

    private HttpClient NewAnonymousClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ClientIpStartupFilter.HeaderName, TestClientIps.Next());
        return client;
    }

    private async Task<HttpClient> RegisterAsync()
    {
        var client = NewAnonymousClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"photo-{Guid.NewGuid():N}@bikontrol.test",
            password = Password,
            fullName = "Photo User"
        });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private static object Body(string? image, string plate = "ABC123") => new
    {
        name = "XTZ 150",
        brand = "Yamaha",
        year = DateTime.UtcNow.Year,
        nickname = "La azul",
        km = 1000,
        displacement = 150,
        plate,
        image
    };

    private static async Task<MotorcycleResponse> CreateAsync(HttpClient client, string image)
    {
        var response = await client.PostAsJsonAsync("/api/motorcycles", Body(image, plate: $"ABC{Random.Shared.Next(100, 999)}"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MotorcycleResponse>())!;
    }

    private sealed record AuthResponse(string Token, string RefreshToken);

    private sealed record MotorcycleResponse(Guid Id, string Image, string Plate);
}
