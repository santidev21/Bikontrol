using Bikontrol.Application.DTOs.Maintenance;
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

public class MaintenanceAttachmentServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid MotoId = Guid.NewGuid();
    private static readonly Guid RecordId = Guid.NewGuid();

    // 1x1 transparent PNG.
    private const string PngDataUrl =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private static Motorcycle Moto(Guid? owner = null) => new("XTZ 150", "Yamaha", 2021, "La Negra", 150, "ABC123", owner ?? UserId) { Id = MotoId };

    private static MotorcycleMaintenanceRecord Record() => new()
    {
        Id = RecordId,
        MotorcycleId = MotoId,
        UserMaintenanceId = Guid.NewGuid()
    };

    private static MaintenanceAttachmentService CreateService(
        FakeRecordRepository? records = null,
        FakeAttachmentRepository? attachments = null,
        Motorcycle? moto = null,
        bool isDemo = false,
        IObjectStorage? storage = null,
        long maxBytesPerUser = 0)
    {
        return new MaintenanceAttachmentService(
            records ?? new FakeRecordRepository(Record()),
            attachments ?? new FakeAttachmentRepository(),
            new FakeMotorcycleRepository(moto ?? Moto()),
            new FakeCurrentUserService(isDemo),
            storage ?? new NoopObjectStorage(),
            Signer(),
            Options.Create(new StorageOptions { MaxBytesPerUser = maxBytesPerUser }),
            NullLogger<MaintenanceAttachmentService>.Instance);
    }

    private static MediaUrlSigner Signer() => new(Options.Create(new JwtOptions { Key = new string('k', 40) }));

    private static (Guid Id, long Exp, string Sig) ParseMediaUrl(string url)
    {
        var uri = new Uri("http://x" + url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return (Guid.Parse(uri.Segments[^1]), long.Parse(query["exp"]!, System.Globalization.CultureInfo.InvariantCulture), query["sig"]!);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistValidatedAttachment()
    {
        var repo = new FakeAttachmentRepository();
        var service = CreateService(attachments: repo);

        var dto = await service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = PngDataUrl, FileName = "  factura.png  " });

        Assert.Equal(RecordId, dto.RecordId);
        Assert.Equal("factura.png", dto.FileName);
        Assert.Equal("image/png", dto.ContentType);
        Assert.Single(repo.Saved);
        Assert.Equal(1, repo.SaveCalls);
    }

    [Fact]
    public async Task AddAsync_ShouldRejectNonImageDataUrl()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = "data:text/plain;base64,aGk=" }));
    }

    [Fact]
    public async Task AddAsync_ShouldRejectEmptyImage()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = "" }));
    }

    [Fact]
    public async Task GetByRecordAsync_ShouldReturnMappedList()
    {
        var attachments = new FakeAttachmentRepository();
        await attachments.AddAsync(new MaintenanceRecordAttachment { MotorcycleMaintenanceRecordId = RecordId, DataUrl = PngDataUrl, ContentType = "image/png" });
        var service = CreateService(attachments: attachments);

        var result = await service.GetByRecordAsync(RecordId);

        var only = Assert.Single(result);
        Assert.Equal(RecordId, only.RecordId);
    }

    [Fact]
    public async Task GetByRecordAsync_WhenRecordMissing_ShouldThrowNotFound()
    {
        var service = CreateService(
            records: new FakeRecordRepository(null),
            attachments: null);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByRecordAsync(RecordId));
    }

    [Fact]
    public async Task AddAsync_WhenNotOwner_ShouldThrowForbidden()
    {
        var service = CreateService(moto: Moto(Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = PngDataUrl }));
    }

    [Fact]
    public async Task AddAsync_WhenDemo_ShouldThrowForbidden()
    {
        var service = CreateService(isDemo: true);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = PngDataUrl }));
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveOwnedAttachment()
    {
        var attachment = new MaintenanceRecordAttachment { Id = Guid.NewGuid(), MotorcycleMaintenanceRecordId = RecordId, DataUrl = PngDataUrl };
        var repo = new FakeAttachmentRepository();
        await repo.AddAsync(attachment);
        var service = CreateService(attachments: repo);

        await service.DeleteAsync(RecordId, attachment.Id);

        Assert.Empty(repo.Items);
        Assert.Equal(1, repo.SaveCalls);
    }

    [Fact]
    public async Task DeleteAsync_WhenAttachmentBelongsToAnotherRecord_ShouldThrowNotFound()
    {
        var attachment = new MaintenanceRecordAttachment { Id = Guid.NewGuid(), MotorcycleMaintenanceRecordId = Guid.NewGuid(), DataUrl = PngDataUrl };
        var repo = new FakeAttachmentRepository();
        await repo.AddAsync(attachment);
        var service = CreateService(attachments: repo);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(RecordId, attachment.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenDemo_ShouldThrowForbidden()
    {
        var service = CreateService(isDemo: true);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.DeleteAsync(RecordId, Guid.NewGuid()));
    }

    [Fact]
    public async Task AddAsync_WithoutStorage_KeepsTheImageInTheDatabaseAndReturnsASignedUrl()
    {
        var repo = new FakeAttachmentRepository();
        var service = CreateService(attachments: repo);

        var dto = await service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = PngDataUrl });

        var saved = Assert.Single(repo.Saved);
        Assert.Equal(PngDataUrl, saved.DataUrl);
        Assert.Null(saved.StorageKey);
        Assert.True(saved.SizeBytes > 0);
        Assert.StartsWith($"/api/media/attachments/{saved.Id}?exp=", dto.DataUrl);
    }

    [Fact]
    public async Task AddAsync_WithStorage_UploadsTheBytesAndClearsTheDataUrl()
    {
        var repo = new FakeAttachmentRepository();
        var storage = new InMemoryObjectStorage();
        var service = CreateService(attachments: repo, storage: storage);

        var dto = await service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = PngDataUrl });

        var saved = Assert.Single(repo.Saved);
        Assert.Equal(string.Empty, saved.DataUrl);
        Assert.Equal($"attachments/{UserId:N}/{saved.Id:N}.png", saved.StorageKey);
        Assert.Equal(new[] { saved.StorageKey! }, storage.GetKeys());
        Assert.StartsWith("/api/media/attachments/", dto.DataUrl);
    }

    [Fact]
    public async Task AddAsync_WhenTheQuotaWouldBeExceeded_ShouldRejectAndStoreNothing()
    {
        var repo = new FakeAttachmentRepository();
        await repo.AddAsync(new MaintenanceRecordAttachment { MotorcycleMaintenanceRecordId = RecordId, SizeBytes = 990 });
        repo.Saved.Clear();
        var storage = new InMemoryObjectStorage();
        var service = CreateService(attachments: repo, storage: storage, maxBytesPerUser: 1000);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = PngDataUrl }));

        Assert.Equal("storage_quota_exceeded", ex.Code);
        Assert.Empty(storage.GetKeys());
        Assert.Empty(repo.Saved);
    }

    [Fact]
    public async Task AddAsync_WhenSavingFails_ShouldRemoveTheUploadedObject()
    {
        var repo = new FakeAttachmentRepository { ThrowOnSave = true };
        var storage = new InMemoryObjectStorage();
        var service = CreateService(attachments: repo, storage: storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = PngDataUrl }));

        Assert.Empty(storage.GetKeys());
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTheStoredObject()
    {
        var storage = new InMemoryObjectStorage();
        await storage.PutAsync("attachments/k.png", new byte[] { 1 }, "image/png");
        var attachment = new MaintenanceRecordAttachment { Id = Guid.NewGuid(), MotorcycleMaintenanceRecordId = RecordId, StorageKey = "attachments/k.png" };
        var repo = new FakeAttachmentRepository();
        await repo.AddAsync(attachment);
        var service = CreateService(attachments: repo, storage: storage);

        await service.DeleteAsync(RecordId, attachment.Id);

        Assert.Empty(storage.GetKeys());
    }

    [Fact]
    public async Task OpenMediaAsync_WithAValidSignature_StreamsTheStoredObject()
    {
        var storage = new InMemoryObjectStorage();
        var repo = new FakeAttachmentRepository();
        var service = CreateService(attachments: repo, storage: storage);
        var dto = await service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = PngDataUrl });
        var (id, exp, sig) = ParseMediaUrl(dto.DataUrl);

        using var media = await service.OpenMediaAsync(id, exp, sig);
        using var buffer = new MemoryStream();
        await media.Content.CopyToAsync(buffer);

        Assert.Equal("image/png", media.ContentType);
        Assert.Equal(ImageDataUrlValidator.Decode(PngDataUrl), buffer.ToArray());
    }

    [Fact]
    public async Task OpenMediaAsync_ForALegacyRow_ServesTheDecodedDatabaseImage()
    {
        var repo = new FakeAttachmentRepository();
        var service = CreateService(attachments: repo);
        var dto = await service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = PngDataUrl });
        var (id, exp, sig) = ParseMediaUrl(dto.DataUrl);

        using var media = await service.OpenMediaAsync(id, exp, sig);

        Assert.Equal(ImageDataUrlValidator.Decode(PngDataUrl).Length, media.Length);
    }

    [Fact]
    public async Task OpenMediaAsync_WithATamperedOrWrongSignature_ShouldThrowNotFound()
    {
        var repo = new FakeAttachmentRepository();
        var service = CreateService(attachments: repo);
        var dto = await service.AddAsync(RecordId, new AddAttachmentRequest { DataUrl = PngDataUrl });
        var (id, exp, sig) = ParseMediaUrl(dto.DataUrl);

        await Assert.ThrowsAsync<NotFoundException>(() => service.OpenMediaAsync(id, exp, sig + "x"));
        await Assert.ThrowsAsync<NotFoundException>(() => service.OpenMediaAsync(Guid.NewGuid(), exp, sig));
        await Assert.ThrowsAsync<NotFoundException>(() => service.OpenMediaAsync(id, exp + 1, sig));
        await Assert.ThrowsAsync<NotFoundException>(() => service.OpenMediaAsync(id, exp, null));
    }

    [Fact]
    public async Task OpenMediaAsync_WhenTheObjectIsMissing_ShouldThrowNotFound()
    {
        var attachment = new MaintenanceRecordAttachment { Id = Guid.NewGuid(), MotorcycleMaintenanceRecordId = RecordId, StorageKey = "attachments/gone.png" };
        var repo = new FakeAttachmentRepository();
        await repo.AddAsync(attachment);
        var service = CreateService(attachments: repo, storage: new InMemoryObjectStorage());
        var url = Signer().CreateAttachmentUrl(attachment.Id);
        var (id, exp, sig) = ParseMediaUrl(url);

        await Assert.ThrowsAsync<NotFoundException>(() => service.OpenMediaAsync(id, exp, sig));
    }

    [Fact]
    public async Task GetByRecordAsync_ShouldReturnASignedUrlInsteadOfTheDataUrl()
    {
        var attachments = new FakeAttachmentRepository();
        await attachments.AddAsync(new MaintenanceRecordAttachment { MotorcycleMaintenanceRecordId = RecordId, DataUrl = PngDataUrl, ContentType = "image/png" });
        var service = CreateService(attachments: attachments);

        var only = Assert.Single(await service.GetByRecordAsync(RecordId));

        Assert.StartsWith("/api/media/attachments/", only.DataUrl);
        Assert.DoesNotContain("base64", only.DataUrl);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(bool isDemo) => IsDemo = isDemo;
        public Guid UserId => MaintenanceAttachmentServiceTests.UserId;
        public string Role => "User";
        public bool IsDemo { get; }
    }

    private sealed class FakeMotorcycleRepository : IMotorcycleRepository
    {
        private readonly Motorcycle _moto;
        public FakeMotorcycleRepository(Motorcycle moto) => _moto = moto;
        public Task<Motorcycle?> GetByIdAsync(Guid id) => Task.FromResult<Motorcycle?>(_moto);
        public Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId) => Task.FromResult<IEnumerable<Motorcycle>>(new[] { _moto });
        public Task<Motorcycle> AddAsync(Motorcycle motorcycle) => Task.FromResult(motorcycle);
        public Task UpdateAsync(Motorcycle motorcycle) => Task.CompletedTask;
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeRecordRepository : IMotorcycleMaintenanceRecordRepository
    {
        private readonly MotorcycleMaintenanceRecord? _record;
        public FakeRecordRepository(MotorcycleMaintenanceRecord? record) => _record = record;
        public Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity) => Task.FromResult(entity);
        public Task<MotorcycleMaintenanceRecord?> GetByIdAsync(Guid id) => Task.FromResult(_record);
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId, int? limit = null) => Task.FromResult(Enumerable.Empty<MotorcycleMaintenanceRecord>());
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) => Task.FromResult(Enumerable.Empty<MotorcycleMaintenanceRecord>());
        public Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId) => Task.FromResult<MotorcycleMaintenanceRecord?>(null);
        public Task<Dictionary<Guid, MotorcycleMaintenanceRecord>> GetLastByUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds) => Task.FromResult(new Dictionary<Guid, MotorcycleMaintenanceRecord>());
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeAttachmentRepository : IMaintenanceRecordAttachmentRepository
    {
        private readonly List<MaintenanceRecordAttachment> _items = new();
        public IReadOnlyList<MaintenanceRecordAttachment> Items => _items;
        public List<MaintenanceRecordAttachment> Saved { get; } = new();
        public int SaveCalls { get; private set; }
        public bool ThrowOnSave { get; set; }

        public Task<long> GetTotalSizeBytesByUserAsync(Guid userId) => Task.FromResult(_items.Sum(a => (long)a.SizeBytes));
        public Task<IReadOnlyList<string>> GetStorageKeysByUserAsync(Guid userId) =>
            Task.FromResult<IReadOnlyList<string>>(_items.Where(a => a.StorageKey is not null).Select(a => a.StorageKey!).ToList());
        public Task<IReadOnlyList<MaintenanceRecordAttachment>> GetPendingStorageMigrationAsync(int take) =>
            Task.FromResult<IReadOnlyList<MaintenanceRecordAttachment>>(_items.Where(a => a.StorageKey is null && a.DataUrl != "").Take(take).ToList());
        public Task<Guid?> GetOwnerIdAsync(Guid attachmentId) => Task.FromResult<Guid?>(MaintenanceAttachmentServiceTests.UserId);

        public Task<MaintenanceRecordAttachment?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(a => a.Id == id));
        public Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdAsync(Guid recordId)
            => Task.FromResult<IReadOnlyList<MaintenanceRecordAttachment>>(_items.Where(a => a.MotorcycleMaintenanceRecordId == recordId).ToList());
        public Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdsAsync(IEnumerable<Guid> recordIds)
        {
            var ids = recordIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<MaintenanceRecordAttachment>>(_items.Where(a => ids.Contains(a.MotorcycleMaintenanceRecordId)).ToList());
        }
        public Task AddAsync(MaintenanceRecordAttachment entity)
        {
            _items.Add(entity);
            Saved.Add(entity);
            return Task.CompletedTask;
        }
        public Task RemoveAsync(MaintenanceRecordAttachment entity)
        {
            _items.Remove(entity);
            return Task.CompletedTask;
        }
        public Task SaveChangesAsync()
        {
            SaveCalls++;
            if (ThrowOnSave)
                throw new InvalidOperationException("db down");
            return Task.CompletedTask;
        }
    }
}
