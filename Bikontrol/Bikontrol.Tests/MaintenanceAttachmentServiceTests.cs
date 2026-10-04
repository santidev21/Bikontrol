using AutoMapper;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Services;
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
        bool isDemo = false)
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        return new MaintenanceAttachmentService(
            records ?? new FakeRecordRepository(Record()),
            attachments ?? new FakeAttachmentRepository(),
            new FakeMotorcycleRepository(moto ?? Moto()),
            new FakeCurrentUserService(isDemo),
            mapper);
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
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId) => Task.FromResult(Enumerable.Empty<MotorcycleMaintenanceRecord>());
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
            return Task.CompletedTask;
        }
    }
}
