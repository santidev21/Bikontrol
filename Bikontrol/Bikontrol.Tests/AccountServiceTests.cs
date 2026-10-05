using AutoMapper;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Persistence.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Tests;

public class AccountServiceTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    [Fact]
    public async Task ExportMyDataAsync_ShouldReturnEverythingTheUserOwns()
    {
        var user = new User("rider@bikontrol.test", "Rider", "hashed");
        var userId = user.Id;
        var motorcycle = MotorcycleFor(userId, "XTZ");
        var maintenance = new UserMaintenance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MotorcycleId = motorcycle.Id,
            Name = "Aceite",
            TrackingType = "Km",
            KmInterval = 1500,
            IsEnabled = true
        };
        var record = new MotorcycleMaintenanceRecord
        {
            Id = Guid.NewGuid(),
            MotorcycleId = motorcycle.Id,
            UserMaintenanceId = maintenance.Id,
            PerformedAt = DateTime.UtcNow.AddDays(-1),
            PerformedKm = 1000,
            Cost = 25.5m
        };
        var attachment = new MaintenanceRecordAttachment
        {
            Id = Guid.NewGuid(),
            MotorcycleMaintenanceRecordId = record.Id,
            DataUrl = "data:image/jpeg;base64,abc",
            ContentType = "image/jpeg",
            CreatedAt = DateTime.UtcNow
        };

        var service = CreateService(userId, user,
            motorcycles: new[] { motorcycle },
            maintenances: new[] { maintenance },
            records: new[] { record },
            attachments: new[] { attachment },
            currentKms: new Dictionary<Guid, int> { [motorcycle.Id] = 1200 });

        var result = await service.ExportMyDataAsync();

        Assert.Equal(user.Id, result.Profile.Id);
        Assert.Equal("rider@bikontrol.test", result.Profile.Email);
        Assert.True(result.Profile.HasPassword);

        var moto = Assert.Single(result.Motorcycles);
        Assert.Equal("XTZ", moto.Name);
        Assert.Equal(1200, moto.Km); // resolved from the km history, not the entity

        var maint = Assert.Single(result.Maintenances);
        Assert.Equal("Aceite", maint.Name);
        Assert.Equal(1500, maint.KmInterval);

        var rec = Assert.Single(result.MaintenanceRecords);
        Assert.Equal(1000, rec.PerformedKm);
        Assert.Equal(25.5m, rec.Cost);
        Assert.Equal("Aceite", rec.MaintenanceName); // filled from the maintenance

        var att = Assert.Single(result.Attachments);
        Assert.Equal(record.Id, att.RecordId);
        Assert.Equal("data:image/jpeg;base64,abc", att.DataUrl);
    }

    [Fact]
    public async Task ExportMyDataAsync_UnknownUser_ShouldThrowNotFound()
    {
        var service = CreateService(Guid.NewGuid(), user: null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ExportMyDataAsync());
        Assert.Equal("Usuario no encontrado.", ex.Message);
    }

    [Fact]
    public async Task ExportMyDataAsync_NoMotorcycles_ShouldReturnEmptyCollections()
    {
        var user = new User("rider@bikontrol.test", "Rider", "hashed");
        var service = CreateService(user.Id, user);

        var result = await service.ExportMyDataAsync();

        Assert.Empty(result.Motorcycles);
        Assert.Empty(result.Maintenances);
        Assert.Empty(result.MaintenanceRecords);
        Assert.Empty(result.Attachments);
    }

    private static Motorcycle MotorcycleFor(Guid userId, string name)
        => new(name, "Yamaha", 2024, "Apodo", 150, "ABC123", userId) { Id = Guid.NewGuid() };

    private static AccountService CreateService(
        Guid userId,
        User? user,
        IEnumerable<Motorcycle>? motorcycles = null,
        IEnumerable<UserMaintenance>? maintenances = null,
        IEnumerable<MotorcycleMaintenanceRecord>? records = null,
        IEnumerable<MaintenanceRecordAttachment>? attachments = null,
        IReadOnlyDictionary<Guid, int>? currentKms = null)
    {
        return new AccountService(
            new FakeUserRepository(user),
            new FakeMotorcycleRepository(motorcycles ?? Array.Empty<Motorcycle>()),
            new FakeUserMaintenanceRepository(maintenances ?? Array.Empty<UserMaintenance>()),
            new FakeRecordRepository(records ?? Array.Empty<MotorcycleMaintenanceRecord>()),
            new FakeAttachmentRepository(attachments ?? Array.Empty<MaintenanceRecordAttachment>()),
            new FakeKmHistoryService(currentKms ?? new Dictionary<Guid, int>()),
            Mapper,
            new FakeCurrentUserService(userId));
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId) => UserId = userId;
        public Guid UserId { get; }
        public string Role => UserRole.User;
        public bool IsDemo => false;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User? _user;
        public FakeUserRepository(User? user) => _user = user;
        public Task<User?> GetByEmailAsync(string email) => Task.FromResult(_user);
        public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(_user is not null && _user.Id == id ? _user : null);
        public Task<bool> ExistsByEmailAsync(string email) => Task.FromResult(_user is not null);
        public Task<IReadOnlyList<User>> GetReminderRecipientsAsync() =>
            Task.FromResult<IReadOnlyList<User>>(_user is null ? Array.Empty<User>() : new[] { _user });
        public Task AddAsync(User user) => Task.CompletedTask;
        public Task UpdateAsync(User user) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeMotorcycleRepository : IMotorcycleRepository
    {
        private readonly List<Motorcycle> _items;
        public FakeMotorcycleRepository(IEnumerable<Motorcycle> items) => _items = items.ToList();
        public Task<Motorcycle?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(m => m.Id == id));
        public Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId) =>
            Task.FromResult(_items.Where(m => m.UserId == userId).AsEnumerable());
        public Task<Motorcycle> AddAsync(Motorcycle motorcycle) => Task.FromResult(motorcycle);
        public Task UpdateAsync(Motorcycle motorcycle) => Task.CompletedTask;
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeUserMaintenanceRepository : IUserMaintenanceRepository
    {
        private readonly List<UserMaintenance> _items;
        public FakeUserMaintenanceRepository(IEnumerable<UserMaintenance> items) => _items = items.ToList();
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAsync(Guid userId) =>
            Task.FromResult(_items.Where(m => m.UserId == userId).AsEnumerable());
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAndMotorcycleIdAsync(Guid userId, Guid motorcycleId) =>
            Task.FromResult(_items.Where(m => m.UserId == userId && m.MotorcycleId == motorcycleId).AsEnumerable());
        public Task<UserMaintenance?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(m => m.Id == id));
        public Task<IReadOnlyList<UserMaintenance>> GetEnabledForUsersAsync(IEnumerable<Guid> userIds) =>
            Task.FromResult<IReadOnlyList<UserMaintenance>>(_items);
        public Task<UserMaintenance> AddAsync(UserMaintenance entity) => Task.FromResult(entity);
        public Task UpdateAsync(UserMaintenance entity) => Task.CompletedTask;
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task<UserMaintenance?> GetByBaseIdAsync(Guid userId, Guid motorcycleId, Guid baseId) =>
            Task.FromResult<UserMaintenance?>(null);
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeRecordRepository : IMotorcycleMaintenanceRecordRepository
    {
        private readonly List<MotorcycleMaintenanceRecord> _items;
        public FakeRecordRepository(IEnumerable<MotorcycleMaintenanceRecord> items) => _items = items.ToList();
        public Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity) => Task.FromResult(entity);
        public Task<MotorcycleMaintenanceRecord?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(r => r.Id == id));
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId) =>
            Task.FromResult(_items.Where(r => r.MotorcycleId == motorcycleId).AsEnumerable());
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds.ToHashSet();
            return Task.FromResult(_items.Where(r => ids.Contains(r.MotorcycleId)).AsEnumerable());
        }
        public Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId) =>
            Task.FromResult(_items.LastOrDefault(r => r.UserMaintenanceId == userMaintenanceId));
        public Task<Dictionary<Guid, MotorcycleMaintenanceRecord>> GetLastByUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds) =>
            Task.FromResult(new Dictionary<Guid, MotorcycleMaintenanceRecord>());
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeAttachmentRepository : IMaintenanceRecordAttachmentRepository
    {
        private readonly List<MaintenanceRecordAttachment> _items;
        public FakeAttachmentRepository(IEnumerable<MaintenanceRecordAttachment> items) => _items = items.ToList();
        public Task<MaintenanceRecordAttachment?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(a => a.Id == id));
        public Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdAsync(Guid recordId) =>
            Task.FromResult<IReadOnlyList<MaintenanceRecordAttachment>>(_items.Where(a => a.MotorcycleMaintenanceRecordId == recordId).ToList());
        public Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdsAsync(IEnumerable<Guid> recordIds)
        {
            var ids = recordIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<MaintenanceRecordAttachment>>(
                _items.Where(a => ids.Contains(a.MotorcycleMaintenanceRecordId)).ToList());
        }
        public Task AddAsync(MaintenanceRecordAttachment entity) => Task.CompletedTask;
        public Task RemoveAsync(MaintenanceRecordAttachment entity) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeKmHistoryService : IKmHistoryService
    {
        private readonly IReadOnlyDictionary<Guid, int> _currentKms;
        public FakeKmHistoryService(IReadOnlyDictionary<Guid, int> currentKms) => _currentKms = currentKms;
        public Task AddKmAsync(Guid motorcycleId, int km) => Task.CompletedTask;
        public Task<int> GetCurrentKmAsync(Guid motorcycleId) => Task.FromResult(_currentKms.TryGetValue(motorcycleId, out var km) ? km : 0);
        public Task<DateTime?> GetInitialRecordedAtAsync(Guid motorcycleId) => Task.FromResult<DateTime?>(null);
        public Task<IReadOnlyDictionary<Guid, int>> GetCurrentKmByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) =>
            Task.FromResult(_currentKms);
        public Task<IReadOnlyDictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) =>
            Task.FromResult<IReadOnlyDictionary<Guid, DateTime?>>(new Dictionary<Guid, DateTime?>());
        public Task RollbackLastKmAsync(Guid motorcycleId, int newKm) => Task.CompletedTask;
    }
}
