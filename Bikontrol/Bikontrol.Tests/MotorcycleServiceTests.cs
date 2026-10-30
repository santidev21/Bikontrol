using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Application.Mapping;
using Bikontrol.Application.Options;
using Bikontrol.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Bikontrol.Application.Services;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Tests;

public class MotorcycleServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistKmHistoryAndReturnInputKm()
    {
        var userId = Guid.NewGuid();
        var motos = new FakeMotorcycleRepository();
        var history = new FakeKmHistoryRepository();
        var service = CreateService(userId, motos, history);

        var result = await service.CreateAsync(new SaveMotorcycleDTO
        {
            Name = "YBR 125",
            Brand = "Yamaha",
            Year = 2024,
            Nickname = "Negra",
            Km = 1234,
            Displacement = 125,
            Plate = "ABC12D"
        });

        Assert.Equal(1234, result.Km);
        var stored = await history.GetByMotorcycleIdAsync(result.Id);
        Assert.Single(stored);
        Assert.Equal(1234, stored[0].Km);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldExposeLatestHistoryKm()
    {
        var userId = Guid.NewGuid();
        var moto = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", userId) { Id = Guid.NewGuid() };
        var history = new FakeKmHistoryRepository();
        history.Seed(moto.Id, 1000, DateTime.UtcNow.AddDays(-5));
        history.Seed(moto.Id, 4567, DateTime.UtcNow.AddDays(-1));
        var service = CreateService(userId, new FakeMotorcycleRepository(moto), history);

        var result = await service.GetByIdAsync(moto.Id);

        Assert.NotNull(result);
        Assert.Equal(4567, result!.Km);
    }

    [Fact]
    public async Task GetByIdAsync_OtherUser_ShouldThrowForbidden()
    {
        var owner = Guid.NewGuid();
        var moto = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", owner) { Id = Guid.NewGuid() };
        var service = CreateService(Guid.NewGuid(), new FakeMotorcycleRepository(moto), new FakeKmHistoryRepository());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetByIdAsync(moto.Id));
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ShouldThrowNotFound()
    {
        var service = CreateService(Guid.NewGuid(), new FakeMotorcycleRepository(), new FakeKmHistoryRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByCurrentUserAsync_ShouldExposeKmPerMotorcycleFromBatch()
    {
        var userId = Guid.NewGuid();
        var withHistory = new Motorcycle("A", "Yamaha", 2024, "A", 125, "AAA111", userId) { Id = Guid.NewGuid() };
        var withoutHistory = new Motorcycle("B", "Honda", 2023, "B", 150, "BBB222", userId) { Id = Guid.NewGuid() };
        var history = new FakeKmHistoryRepository();
        history.Seed(withHistory.Id, 8500, DateTime.UtcNow);
        var motos = new FakeMotorcycleRepository(withHistory, withoutHistory);
        var service = CreateService(userId, motos, history);

        var result = await service.GetByCurrentUserAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(8500, result.Single(m => m.Id == withHistory.Id).Km);
        Assert.Equal(0, result.Single(m => m.Id == withoutHistory.Id).Km);
    }

    [Fact]
    public async Task UpdateAsync_InvalidYear_ShouldThrowValidation()
    {
        var userId = Guid.NewGuid();
        var moto = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", userId) { Id = Guid.NewGuid() };
        var service = CreateService(userId, new FakeMotorcycleRepository(moto), new FakeKmHistoryRepository());

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(moto.Id, new SaveMotorcycleDTO
        {
            Name = "YBR",
            Brand = "Yamaha",
            Year = 1900,
            Nickname = "N",
            Km = 100,
            Displacement = 125,
            Plate = "ABC12D"
        }));
    }

    [Fact]
    public async Task UpdateAsync_OtherUser_ShouldThrowForbidden()
    {
        var owner = Guid.NewGuid();
        var moto = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", owner) { Id = Guid.NewGuid() };
        var service = CreateService(Guid.NewGuid(), new FakeMotorcycleRepository(moto), new FakeKmHistoryRepository());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.UpdateAsync(moto.Id, new SaveMotorcycleDTO
        {
            Name = "YBR",
            Brand = "Yamaha",
            Year = 2024,
            Nickname = "N",
            Km = 100,
            Displacement = 125,
            Plate = "ABC12D"
        }));
    }

    [Fact]
    public async Task SoftDeleteAsync_Owned_ShouldDisableMotorcycle()
    {
        var userId = Guid.NewGuid();
        var moto = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", userId) { Id = Guid.NewGuid() };
        var repo = new FakeMotorcycleRepository(moto);
        var service = CreateService(userId, repo, new FakeKmHistoryRepository());

        await service.SoftDeleteAsync(moto.Id);

        Assert.False(moto.IsEnabled);
        Assert.Null(await repo.GetByIdAsync(moto.Id));
    }

    [Fact]
    public async Task CreateAsync_WithNonImageDataUrl_ShouldThrowValidation()
    {
        var service = CreateService(Guid.NewGuid(), new FakeMotorcycleRepository(), new FakeKmHistoryRepository());

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new SaveMotorcycleDTO
        {
            Name = "YBR 125",
            Brand = "Yamaha",
            Year = 2024,
            Nickname = "Negra",
            Km = 100,
            Displacement = 125,
            Plate = "ABC12D",
            Image = "data:text/html;base64,PGI+"
        }));
    }

    [Fact]
    public async Task CreateAsync_WithValidJpegDataUrl_ShouldSucceed()
    {
        var service = CreateService(Guid.NewGuid(), new FakeMotorcycleRepository(), new FakeKmHistoryRepository());
        var payload = Convert.ToBase64String(new byte[16]);

        var result = await service.CreateAsync(new SaveMotorcycleDTO
        {
            Name = "YBR 125",
            Brand = "Yamaha",
            Year = 2024,
            Nickname = "Negra",
            Km = 100,
            Displacement = 125,
            Plate = "ABC12D",
            Image = "data:image/jpeg;base64," + payload
        });

        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task GetCurrentKmAsync_Owned_ShouldReturnKm()
    {
        var userId = Guid.NewGuid();
        var moto = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", userId) { Id = Guid.NewGuid() };
        var service = CreateService(userId, new FakeMotorcycleRepository(moto), new FakeKmHistoryRepository());

        Assert.Equal(0, await service.GetCurrentKmAsync(moto.Id));
    }

    [Fact]
    public async Task GetCurrentKmAsync_UnknownOrForeign_ShouldThrow()
    {
        var moto = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", Guid.NewGuid()) { Id = Guid.NewGuid() };
        var service = CreateService(Guid.NewGuid(), new FakeMotorcycleRepository(moto), new FakeKmHistoryRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetCurrentKmAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetCurrentKmAsync(moto.Id));
    }

    [Fact]
    public async Task AddKmHistoryAsync_Owned_ShouldSucceed_AndGuardsOwnership()
    {
        var userId = Guid.NewGuid();
        var mine = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", userId) { Id = Guid.NewGuid() };
        var foreign = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "XYZ12D", Guid.NewGuid()) { Id = Guid.NewGuid() };
        var service = CreateService(userId, new FakeMotorcycleRepository(mine, foreign), new FakeKmHistoryRepository());

        await service.AddKmHistoryAsync(mine.Id, 500);
        await Assert.ThrowsAsync<NotFoundException>(() => service.AddKmHistoryAsync(Guid.NewGuid(), 500));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.AddKmHistoryAsync(foreign.Id, 500));
    }

    [Fact]
    public async Task RollbackLastKmAsync_Owned_ShouldSucceed_AndGuardsOwnership()
    {
        var userId = Guid.NewGuid();
        var mine = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", userId) { Id = Guid.NewGuid() };
        var foreign = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "XYZ12D", Guid.NewGuid()) { Id = Guid.NewGuid() };
        var service = CreateService(userId, new FakeMotorcycleRepository(mine, foreign), new FakeKmHistoryRepository());

        await service.RollbackLastKmAsync(mine.Id, 400);
        await Assert.ThrowsAsync<NotFoundException>(() => service.RollbackLastKmAsync(Guid.NewGuid(), 400));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.RollbackLastKmAsync(foreign.Id, 400));
    }

    [Theory]
    [InlineData("data:image/png;base64,!!!not-base64!!!")]
    [InlineData("data:image/webp;base64,")]
    public async Task CreateAsync_WithInvalidImagePayload_ShouldThrowValidation(string image)
    {
        var service = CreateService(Guid.NewGuid(), new FakeMotorcycleRepository(), new FakeKmHistoryRepository());

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new SaveMotorcycleDTO
        {
            Name = "YBR 125",
            Brand = "Yamaha",
            Year = 2024,
            Nickname = "Negra",
            Km = 100,
            Displacement = 125,
            Plate = "ABC12D",
            Image = image
        }));
    }

    [Fact]
    public async Task CreateAsync_WithOversizedImage_ShouldThrowValidation()
    {
        var service = CreateService(Guid.NewGuid(), new FakeMotorcycleRepository(), new FakeKmHistoryRepository());
        var payload = Convert.ToBase64String(new byte[1_048_577]);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new SaveMotorcycleDTO
        {
            Name = "YBR 125",
            Brand = "Yamaha",
            Year = 2024,
            Nickname = "Negra",
            Km = 100,
            Displacement = 125,
            Plate = "ABC12D",
            Image = "data:image/png;base64," + payload
        }));
    }

    private static MotorcycleService CreateService(
        Guid userId,
        FakeMotorcycleRepository motos,
        FakeKmHistoryRepository history,
        IObjectStorage? storage = null,
        long maxBytesPerUser = 0,
        long attachmentBytes = 0)
        => new(
            motos,
            history,
            new FakeKmHistoryService(),
            new FakeCurrentUserService(userId),
            new FakeTransactionManager(),
            storage ?? new NoopObjectStorage(),
            Signer(),
            new StorageQuota(new FakeAttachmentBytes(attachmentBytes), motos, Options.Create(new StorageOptions { MaxBytesPerUser = maxBytesPerUser })),
            NullLogger<MotorcycleService>.Instance);

    private static MediaUrlSigner Signer() => new(Options.Create(new JwtOptions { Key = new string('k', 40) }));

    private static readonly byte[] PhotoBytes = { 1, 2, 3, 4, 5, 6, 7, 8 };
    private static readonly string PhotoDataUrl = "data:image/png;base64," + Convert.ToBase64String(PhotoBytes);

    private static SaveMotorcycleDTO Dto(string? image) => new()
    {
        Name = "YBR 125",
        Brand = "Yamaha",
        Year = 2024,
        Nickname = "Negra",
        Km = 100,
        Displacement = 125,
        Plate = "ABC12D",
        Image = image
    };

    private static (Guid Id, long Exp, string Sig, string? Version) ParseMediaUrl(string url)
    {
        var uri = new Uri("http://x" + url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return (Guid.Parse(uri.Segments[^1]), long.Parse(query["exp"]!, System.Globalization.CultureInfo.InvariantCulture), query["sig"]!, query["v"]);
    }

    [Fact]
    public async Task CreateAsync_WithStorage_UploadsThePhotoAndReturnsASignedUrl()
    {
        var userId = Guid.NewGuid();
        var motos = new FakeMotorcycleRepository();
        var storage = new InMemoryObjectStorage();
        var service = CreateService(userId, motos, new FakeKmHistoryRepository(), storage);

        var result = await service.CreateAsync(Dto(PhotoDataUrl));

        var stored = await motos.GetByIdAsync(result.Id);
        Assert.Equal("default.png", stored!.Image);
        Assert.Equal(PhotoBytes.Length, stored.ImageSizeBytes);
        Assert.Equal($"motorcycles/{userId:N}/{result.Id:N}/{stored.ImageVersion}.png", stored.ImageKey);
        Assert.Equal(new[] { stored.ImageKey! }, storage.GetKeys());
        Assert.StartsWith($"/api/media/motorcycles/{result.Id}?v={stored.ImageVersion}&exp=", result.Image);
    }

    [Fact]
    public async Task CreateAsync_WithoutStorage_KeepsTheDataUrlInTheDatabase()
    {
        var motos = new FakeMotorcycleRepository();
        var service = CreateService(Guid.NewGuid(), motos, new FakeKmHistoryRepository());

        var result = await service.CreateAsync(Dto(PhotoDataUrl));

        var stored = await motos.GetByIdAsync(result.Id);
        Assert.Equal(PhotoDataUrl, stored!.Image);
        Assert.Null(stored.ImageKey);
        Assert.StartsWith("/api/media/motorcycles/", result.Image);
        Assert.DoesNotContain("base64", result.Image);
    }

    [Fact]
    public async Task CreateAsync_WithoutPhoto_KeepsTheDefaultImage()
    {
        var service = CreateService(Guid.NewGuid(), new FakeMotorcycleRepository(), new FakeKmHistoryRepository());

        Assert.Equal("default.png", (await service.CreateAsync(Dto(null))).Image);
        Assert.Equal("default.png", (await service.CreateAsync(Dto("default.png"))).Image);
    }

    [Fact]
    public async Task CreateAsync_WithAMediaUrl_IsRejected()
    {
        var service = CreateService(Guid.NewGuid(), new FakeMotorcycleRepository(), new FakeKmHistoryRepository());

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(Dto($"/api/media/motorcycles/{Guid.NewGuid()}?v=1&exp=1&sig=x")));

        Assert.Equal("image_invalid_format", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenSavingFails_RemovesTheUploadedPhoto()
    {
        var userId = Guid.NewGuid();
        var storage = new InMemoryObjectStorage();
        var history = new FakeKmHistoryRepository { ThrowOnSave = true };
        var service = CreateService(userId, new FakeMotorcycleRepository(), history, storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(Dto(PhotoDataUrl)));

        Assert.Empty(storage.GetKeys());
    }

    [Fact]
    public async Task CreateAsync_BeyondTheQuota_IsRejectedBeforeUploading()
    {
        var userId = Guid.NewGuid();
        var storage = new InMemoryObjectStorage();
        // 995 bytes already used (attachments) + 8 incoming > 1000.
        var service = CreateService(userId, new FakeMotorcycleRepository(), new FakeKmHistoryRepository(), storage, maxBytesPerUser: 1000, attachmentBytes: 995);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(Dto(PhotoDataUrl)));

        Assert.Equal("storage_quota_exceeded", ex.Code);
        Assert.Empty(storage.GetKeys());
    }

    [Fact]
    public async Task UpdateAsync_ReplacingThePhoto_UploadsTheNewOneAndDeletesTheOldOneAfterSaving()
    {
        var userId = Guid.NewGuid();
        var motos = new FakeMotorcycleRepository();
        var storage = new InMemoryObjectStorage();
        var service = CreateService(userId, motos, new FakeKmHistoryRepository(), storage);
        var created = await service.CreateAsync(Dto(PhotoDataUrl));
        var moto = (await motos.GetByIdAsync(created.Id))!;
        var oldKey = moto.ImageKey!;

        await service.UpdateAsync(created.Id, Dto("data:image/webp;base64," + Convert.ToBase64String(new byte[] { 9, 9 })));

        Assert.NotEqual(oldKey, moto.ImageKey);
        Assert.EndsWith(".webp", moto.ImageKey);
        Assert.Equal(2, moto.ImageSizeBytes);
        Assert.Equal(new[] { moto.ImageKey! }, storage.GetKeys());
    }

    [Fact]
    public async Task UpdateAsync_EchoingTheOwnMediaUrl_KeepsThePhotoUntouched()
    {
        var userId = Guid.NewGuid();
        var motos = new FakeMotorcycleRepository();
        var storage = new InMemoryObjectStorage();
        var service = CreateService(userId, motos, new FakeKmHistoryRepository(), storage);
        var created = await service.CreateAsync(Dto(PhotoDataUrl));
        var moto = (await motos.GetByIdAsync(created.Id))!;
        var (key, version) = (moto.ImageKey, moto.ImageVersion);

        // The edit form sends back what the GET returned: a path or an absolute URL.
        await service.UpdateAsync(created.Id, Dto(created.Image));
        await service.UpdateAsync(created.Id, Dto("https://bikontrol.example" + created.Image));

        Assert.Equal(key, moto.ImageKey);
        Assert.Equal(version, moto.ImageVersion);
        Assert.Equal(new[] { key! }, storage.GetKeys());
    }

    [Fact]
    public async Task UpdateAsync_WithAnotherMotorcyclesMediaUrl_IsRejected()
    {
        var userId = Guid.NewGuid();
        var moto = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", userId) { Id = Guid.NewGuid() };
        var service = CreateService(userId, new FakeMotorcycleRepository(moto), new FakeKmHistoryRepository());

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(moto.Id, Dto($"/api/media/motorcycles/{Guid.NewGuid()}?v=1&exp=1&sig=x")));

        Assert.Equal("image_invalid_format", ex.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("default.png")]
    public async Task UpdateAsync_WithoutAPhoto_RemovesTheCurrentOne(string? image)
    {
        var userId = Guid.NewGuid();
        var motos = new FakeMotorcycleRepository();
        var storage = new InMemoryObjectStorage();
        var service = CreateService(userId, motos, new FakeKmHistoryRepository(), storage);
        var created = await service.CreateAsync(Dto(PhotoDataUrl));
        var moto = (await motos.GetByIdAsync(created.Id))!;

        await service.UpdateAsync(created.Id, Dto(image));

        Assert.Equal("default.png", moto.Image);
        Assert.Null(moto.ImageKey);
        Assert.Equal(0, moto.ImageSizeBytes);
        Assert.Empty(storage.GetKeys());
    }

    [Fact]
    public async Task UpdateAsync_WhenSavingFails_RemovesTheNewObjectAndKeepsTheOldOne()
    {
        var userId = Guid.NewGuid();
        var motos = new FakeMotorcycleRepository();
        var storage = new InMemoryObjectStorage();
        var service = CreateService(userId, motos, new FakeKmHistoryRepository(), storage);
        var created = await service.CreateAsync(Dto(PhotoDataUrl));
        var oldKey = (await motos.GetByIdAsync(created.Id))!.ImageKey!;
        motos.ThrowOnSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(created.Id, Dto("data:image/jpeg;base64," + Convert.ToBase64String(new byte[] { 7 }))));

        Assert.Equal(new[] { oldKey }, storage.GetKeys());
    }

    [Fact]
    public async Task UpdateAsync_ReplacingFreesTheOldPhotoInTheQuota()
    {
        var userId = Guid.NewGuid();
        var moto = new Motorcycle("YBR", "Yamaha", 2024, "N", 125, "ABC12D", userId) { Id = Guid.NewGuid(), ImageSizeBytes = 900, Image = "default.png" };
        // 900 used by this very photo; the new 8-byte one replaces it, so 8 <= 1000 even though 900 + 8 + 100 would not be.
        var service = CreateService(userId, new FakeMotorcycleRepository(moto), new FakeKmHistoryRepository(), maxBytesPerUser: 1000, attachmentBytes: 100);

        await service.UpdateAsync(moto.Id, Dto(PhotoDataUrl));

        Assert.Equal(PhotoBytes.Length, moto.ImageSizeBytes);
    }

    [Fact]
    public async Task SoftDeleteAsync_DropsThePhotoSoItNoLongerCountsAgainstTheQuota()
    {
        var userId = Guid.NewGuid();
        var motos = new FakeMotorcycleRepository();
        var storage = new InMemoryObjectStorage();
        var service = CreateService(userId, motos, new FakeKmHistoryRepository(), storage);
        var created = await service.CreateAsync(Dto(PhotoDataUrl));

        await service.SoftDeleteAsync(created.Id);

        Assert.Empty(storage.GetKeys());
        Assert.Equal(0, await motos.GetImageBytesByUserAsync(userId));
    }

    [Fact]
    public async Task GetByCurrentUserAsync_ExposesSignedUrlsAndKeepsTheDefault()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(userId, new FakeMotorcycleRepository(), new FakeKmHistoryRepository(), new InMemoryObjectStorage());
        var withPhoto = await service.CreateAsync(Dto(PhotoDataUrl));
        var without = await service.CreateAsync(Dto(null));

        var list = await service.GetByCurrentUserAsync();

        Assert.StartsWith("/api/media/motorcycles/", list.Single(m => m.Id == withPhoto.Id).Image);
        Assert.Equal("default.png", list.Single(m => m.Id == without.Id).Image);
    }

    [Fact]
    public async Task OpenImageAsync_ServesTheStoredPhotoForAValidSignature()
    {
        var motos = new FakeMotorcycleRepository();
        var service = CreateService(Guid.NewGuid(), motos, new FakeKmHistoryRepository(), new InMemoryObjectStorage());
        var created = await service.CreateAsync(Dto(PhotoDataUrl));
        var (id, exp, sig, _) = ParseMediaUrl(created.Image);

        using var media = await service.OpenImageAsync(id, exp, sig);
        using var buffer = new MemoryStream();
        await media.Content.CopyToAsync(buffer);

        Assert.Equal("image/png", media.ContentType);
        Assert.Equal(PhotoBytes, buffer.ToArray());
    }

    [Fact]
    public async Task OpenImageAsync_ForALegacyDatabasePhoto_ServesTheDecodedBytes()
    {
        var service = CreateService(Guid.NewGuid(), new FakeMotorcycleRepository(), new FakeKmHistoryRepository());
        var created = await service.CreateAsync(Dto(PhotoDataUrl));
        var (id, exp, sig, _) = ParseMediaUrl(created.Image);

        using var media = await service.OpenImageAsync(id, exp, sig);

        Assert.Equal(PhotoBytes.Length, media.Length);
        Assert.Equal("image/png", media.ContentType);
    }

    [Fact]
    public async Task OpenImageAsync_WithABadSignatureOrNoPhoto_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(userId, new FakeMotorcycleRepository(), new FakeKmHistoryRepository(), new InMemoryObjectStorage());
        var withPhoto = await service.CreateAsync(Dto(PhotoDataUrl));
        var without = await service.CreateAsync(Dto(null));
        var (id, exp, sig, _) = ParseMediaUrl(withPhoto.Image);
        var signer = Signer();
        var (noPhotoExp, noPhotoSig) = (long.Parse(System.Web.HttpUtility.ParseQueryString(new Uri("http://x" + signer.CreateMotorcycleUrl(without.Id, null)).Query)["exp"]!, System.Globalization.CultureInfo.InvariantCulture),
            System.Web.HttpUtility.ParseQueryString(new Uri("http://x" + signer.CreateMotorcycleUrl(without.Id, null)).Query)["sig"]!);

        await Assert.ThrowsAsync<NotFoundException>(() => service.OpenImageAsync(id, exp, sig + "x"));
        await Assert.ThrowsAsync<NotFoundException>(() => service.OpenImageAsync(Guid.NewGuid(), exp, sig));
        await Assert.ThrowsAsync<NotFoundException>(() => service.OpenImageAsync(without.Id, noPhotoExp, noPhotoSig));
    }

    private sealed class FakeTransactionManager : ITransactionManager
    {
        public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
            => action();

        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
            => action();
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId) { UserId = userId; }
        public Guid UserId { get; }
        public string Role => UserRole.User;
        public bool IsDemo => false;
    }

    private sealed class FakeMotorcycleRepository : IMotorcycleRepository
    {
        private readonly List<Motorcycle> _items = new();

        public FakeMotorcycleRepository(params Motorcycle[] seed) => _items.AddRange(seed);

        public bool ThrowOnSave { get; set; }

        public Task<long> GetImageBytesByUserAsync(Guid userId) =>
            Task.FromResult(_items.Where(m => m.UserId == userId).Sum(m => (long)m.ImageSizeBytes));

        public Task<Motorcycle> AddAsync(Motorcycle motorcycle)
        {
            if (motorcycle.Id == Guid.Empty) motorcycle.Id = Guid.NewGuid();
            _items.Add(motorcycle);
            return Task.FromResult(motorcycle);
        }

        public Task<Motorcycle?> GetByIdAsync(Guid id) =>
            Task.FromResult(_items.FirstOrDefault(m => m.Id == id && m.IsEnabled));

        public Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId) =>
            Task.FromResult(_items.Where(m => m.UserId == userId && m.IsEnabled).AsEnumerable());

        public Task SoftDeleteAsync(Guid id)
        {
            var entity = _items.FirstOrDefault(m => m.Id == id);
            if (entity is not null) entity.IsEnabled = false;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Motorcycle motorcycle) => Task.CompletedTask;

        public Task SaveChangesAsync() =>
            ThrowOnSave ? throw new InvalidOperationException("db down") : Task.CompletedTask;
    }

    /// <summary>Only the attachment byte total matters to the shared quota.</summary>
    private sealed class FakeAttachmentBytes : IMaintenanceRecordAttachmentRepository
    {
        private readonly long _bytes;
        public FakeAttachmentBytes(long bytes) => _bytes = bytes;
        public Task<long> GetTotalSizeBytesByUserAsync(Guid userId) => Task.FromResult(_bytes);
        public Task<MaintenanceRecordAttachment?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdAsync(Guid recordId) => throw new NotImplementedException();
        public Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdsAsync(IEnumerable<Guid> recordIds) => throw new NotImplementedException();
        public Task<IReadOnlyList<string>> GetStorageKeysByUserAsync(Guid userId) => throw new NotImplementedException();
        public Task<IReadOnlyList<MaintenanceRecordAttachment>> GetPendingStorageMigrationAsync(int take) => throw new NotImplementedException();
        public Task<Guid?> GetOwnerIdAsync(Guid attachmentId) => throw new NotImplementedException();
        public Task AddAsync(MaintenanceRecordAttachment entity) => throw new NotImplementedException();
        public Task RemoveAsync(MaintenanceRecordAttachment entity) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
    }

    private sealed class FakeKmHistoryRepository : IKmHistoryRepository
    {
        private readonly List<MotorcycleKmHistory> _items = new();

        public bool ThrowOnSave { get; set; }

        public void Seed(Guid motorcycleId, int km, DateTime recordedAt) =>
            _items.Add(new MotorcycleKmHistory { MotorcycleId = motorcycleId, Km = km, RecordedAt = recordedAt });

        public Task AddAsync(MotorcycleKmHistory entity)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public void Remove(MotorcycleKmHistory entity) => _items.Remove(entity);

        public Task<MotorcycleKmHistory?> GetLastByMotorcycleIdAsync(Guid motorcycleId) =>
            Task.FromResult(_items
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.RecordedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault());

        public Task<MotorcycleKmHistory?> GetFirstByMotorcycleIdAsync(Guid motorcycleId) =>
            Task.FromResult(_items
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderBy(x => x.RecordedAt)
                .FirstOrDefault());

        public Task<List<MotorcycleKmHistory>> GetByMotorcycleIdAsync(Guid motorcycleId) =>
            Task.FromResult(_items
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.RecordedAt)
                .ToList());

        public Task<Dictionary<Guid, int>> GetLatestKmByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds.Distinct().ToList();
            var result = _items
                .Where(x => ids.Contains(x.MotorcycleId))
                .GroupBy(x => x.MotorcycleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.RecordedAt).ThenByDescending(x => x.Id).First().Km);
            return Task.FromResult(result);
        }

        public Task<Dictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds.Distinct().ToList();
            var result = _items
                .Where(x => ids.Contains(x.MotorcycleId))
                .GroupBy(x => x.MotorcycleId)
                .ToDictionary(g => g.Key, g => (DateTime?)g.Min(x => x.RecordedAt));
            return Task.FromResult(result);
        }

        public Task LockMotorcycleAsync(Guid motorcycleId) => Task.CompletedTask;
        public Task SaveChangesAsync() =>
            ThrowOnSave ? throw new InvalidOperationException("db down") : Task.CompletedTask;
    }

    private sealed class FakeKmHistoryService : IKmHistoryService
    {
        public Task AddKmAsync(Guid motorcycleId, int km) => Task.CompletedTask;
        public Task<int> GetCurrentKmAsync(Guid motorcycleId) => Task.FromResult(0);
        public Task<DateTime?> GetInitialRecordedAtAsync(Guid motorcycleId) => Task.FromResult<DateTime?>(null);
        public Task<IReadOnlyDictionary<Guid, int>> GetCurrentKmByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) => Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());
        public Task<IReadOnlyDictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) => Task.FromResult<IReadOnlyDictionary<Guid, DateTime?>>(new Dictionary<Guid, DateTime?>());
        public Task RollbackLastKmAsync(Guid motorcycleId, int newKm) => Task.CompletedTask;
    }
}
