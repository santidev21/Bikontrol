using AutoMapper;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Tests;

public class MaintenanceServiceUpcomingTests
{
    [Fact]
    public async Task GetUpcomingByMotorcycleAsync_ShouldRecalculateWithNewFrequency_WithoutChangingRecords()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var maintenanceId = Guid.NewGuid();

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var service = new MaintenanceService(
            new FakeMaintenanceRepository(),
            new FakeUserMaintenanceRepository(new List<UserMaintenance>
            {
                new()
                {
                    Id = maintenanceId,
                    UserId = userId,
                    MotorcycleId = motorcycleId,
                    Name = "Chain",
                    TrackingType = "Km",
                    KmInterval = 7000,
                    IsEnabled = true
                }
            }),
            new FakeMotorcycleRepository(CreateMotorcycle(motorcycleId, userId)),
            new FakeKmHistoryService(8000),
            new FakeRecordRepository(),
            mapper,
            new FakeCurrentUserService(userId),
            new FakeTransactionManager());

        var firstResult = (await service.GetUpcomingByMotorcycleAsync(motorcycleId)).ToList();
        Assert.Single(firstResult);
        Assert.Equal(0, firstResult[0].LifePercent);
        Assert.True(firstResult[0].IsOverdue);

        var serviceWithNewFrequency = new MaintenanceService(
            new FakeMaintenanceRepository(),
            new FakeUserMaintenanceRepository(new List<UserMaintenance>
            {
                new()
                {
                    Id = maintenanceId,
                    UserId = userId,
                    MotorcycleId = motorcycleId,
                    Name = "Chain",
                    TrackingType = "Km",
                    KmInterval = 10000,
                    IsEnabled = true
                }
            }),
            new FakeMotorcycleRepository(CreateMotorcycle(motorcycleId, userId)),
            new FakeKmHistoryService(8000),
            new FakeRecordRepository(),
            mapper,
            new FakeCurrentUserService(userId),
            new FakeTransactionManager());

        var secondResult = (await serviceWithNewFrequency.GetUpcomingByMotorcycleAsync(motorcycleId)).ToList();
        Assert.Single(secondResult);
        Assert.Equal(20, secondResult[0].LifePercent);
        Assert.False(secondResult[0].IsOverdue);
    }

    [Fact]
    public async Task GetUpcomingByMotorcycleAsync_TimeMaintenanceWithoutRecord_ShouldUseInitialMotorcycleDateAsBaseline()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var maintenanceId = Guid.NewGuid();
        var initialDate = DateTime.UtcNow.Date.AddDays(-10);

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var service = new MaintenanceService(
            new FakeMaintenanceRepository(),
            new FakeUserMaintenanceRepository(new List<UserMaintenance>
            {
                new()
                {
                    Id = maintenanceId,
                    UserId = userId,
                    MotorcycleId = motorcycleId,
                    Name = "Coolant",
                    TrackingType = "Time",
                    TimeIntervalWeeks = 1,
                    IsEnabled = true
                }
            }),
            new FakeMotorcycleRepository(CreateMotorcycle(motorcycleId, userId)),
            new FakeKmHistoryService(5000, initialDate),
            new FakeRecordRepository(),
            mapper,
            new FakeCurrentUserService(userId),
            new FakeTransactionManager());

        var result = (await service.GetUpcomingByMotorcycleAsync(motorcycleId)).ToList();
        Assert.Single(result);
        Assert.Equal(0, result[0].LifePercent);
        Assert.True(result[0].IsOverdue);
        Assert.Equal(initialDate, result[0].LastPerformedAt?.Date);
    }

    [Fact]
    public async Task GetUpcomingByMotorcyclesAsync_ShouldGroupResultsPerMotorcycle()
    {
        var userId = Guid.NewGuid();
        var motoA = Guid.NewGuid();
        var motoB = Guid.NewGuid();

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var service = new MaintenanceService(
            new FakeMaintenanceRepository(),
            new FakeUserMaintenanceRepository(new List<UserMaintenance>
            {
                new() { Id = Guid.NewGuid(), UserId = userId, MotorcycleId = motoA, Name = "Aceite", TrackingType = "Km", KmInterval = 1000, IsEnabled = true },
                new() { Id = Guid.NewGuid(), UserId = userId, MotorcycleId = motoB, Name = "Cadena", TrackingType = "Km", KmInterval = 1000, IsEnabled = true }
            }),
            new FakeMotorcycleRepository(CreateMotorcycle(motoA, userId), CreateMotorcycle(motoB, userId)),
            new FakeKmHistoryService(500),
            new FakeRecordRepository(),
            mapper,
            new FakeCurrentUserService(userId),
            new FakeTransactionManager());

        var result = await service.GetUpcomingByMotorcyclesAsync(new[] { motoA, motoB });

        Assert.Equal(2, result.Count);
        Assert.Equal("Aceite", Assert.Single(result[motoA]).Name);
        Assert.Equal("Cadena", Assert.Single(result[motoB]).Name);
    }

    [Fact]
    public async Task GetUpcomingByMotorcyclesAsync_NotOwnedMotorcycle_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var owned = Guid.NewGuid();
        var foreign = Guid.NewGuid();

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var service = new MaintenanceService(
            new FakeMaintenanceRepository(),
            new FakeUserMaintenanceRepository(new List<UserMaintenance>()),
            new FakeMotorcycleRepository(CreateMotorcycle(owned, userId)),
            new FakeKmHistoryService(0),
            new FakeRecordRepository(),
            mapper,
            new FakeCurrentUserService(userId),
            new FakeTransactionManager());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.GetUpcomingByMotorcyclesAsync(new[] { owned, foreign }));
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId, string role = Persistence.Entities.UserRole.User)
        {
            UserId = userId;
            Role = role;
        }
        public Guid UserId { get; }
        public string Role { get; }
        public bool IsDemo => Role == Persistence.Entities.UserRole.Demo;
    }

    private static Motorcycle CreateMotorcycle(Guid motorcycleId, Guid userId)
    {
        var motorcycle = new Motorcycle("Moto", "Brand", 2024, "N", 150, "ABC123", userId)
        {
            Id = motorcycleId
        };

        return motorcycle;
    }

    private sealed class FakeMaintenanceRepository : IMaintenanceRepository
    {
        public Task<IEnumerable<Maintenance>> GetAllAsync() => Task.FromResult(Enumerable.Empty<Maintenance>());
        public Task<IEnumerable<Maintenance>> GetAllForUserAsync(Guid userId) => Task.FromResult(Enumerable.Empty<Maintenance>());
        public Task<Maintenance?> GetByIdAsync(Guid id) => Task.FromResult<Maintenance?>(null);
    }

    private sealed class FakeUserMaintenanceRepository : IUserMaintenanceRepository
    {
        private readonly List<UserMaintenance> _items;
        public FakeUserMaintenanceRepository(List<UserMaintenance> items) => _items = items;
        public Task<UserMaintenance> AddAsync(UserMaintenance entity) => Task.FromResult(entity);
        public Task<UserMaintenance?> GetByBaseIdAsync(Guid userId, Guid motorcycleId, Guid baseId) => Task.FromResult<UserMaintenance?>(null);
        public Task<UserMaintenance?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAsync(Guid userId) => Task.FromResult(_items.Where(x => x.UserId == userId).AsEnumerable());
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAndMotorcycleIdAsync(Guid userId, Guid motorcycleId) =>
            Task.FromResult(_items.Where(x => x.UserId == userId && x.MotorcycleId == motorcycleId && x.IsEnabled).AsEnumerable());
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task UpdateAsync(UserMaintenance entity) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeMotorcycleRepository : IMotorcycleRepository
    {
        private readonly List<Motorcycle> _motorcycles;
        public FakeMotorcycleRepository(params Motorcycle[] motorcycles) => _motorcycles = motorcycles.ToList();
        public Task<Motorcycle> AddAsync(Motorcycle motorcycle) => Task.FromResult(motorcycle);
        public Task<Motorcycle?> GetByIdAsync(Guid id) => Task.FromResult(_motorcycles.FirstOrDefault(m => m.Id == id));
        public Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId) => Task.FromResult(_motorcycles.Where(m => m.UserId == userId).AsEnumerable());
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task UpdateAsync(Motorcycle motorcycle) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeKmHistoryService : IKmHistoryService
    {
        private readonly int _currentKm;
        private readonly DateTime? _initialRecordedAt;
        public FakeKmHistoryService(int currentKm, DateTime? initialRecordedAt = null)
        {
            _currentKm = currentKm;
            _initialRecordedAt = initialRecordedAt ?? DateTime.UtcNow.AddDays(-10);
        }
        public Task AddKmAsync(Guid motorcycleId, int km) => Task.CompletedTask;
        public Task<int> GetCurrentKmAsync(Guid motorcycleId) => Task.FromResult(_currentKm);
        public Task<DateTime?> GetInitialRecordedAtAsync(Guid motorcycleId) => Task.FromResult(_initialRecordedAt);
        public Task<IReadOnlyDictionary<Guid, int>> GetCurrentKmByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(motorcycleIds.Distinct().ToDictionary(id => id, _ => _currentKm));
        public Task<IReadOnlyDictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) =>
            Task.FromResult<IReadOnlyDictionary<Guid, DateTime?>>(motorcycleIds.Distinct().ToDictionary(id => id, _ => _initialRecordedAt));
        public Task RollbackLastKmAsync(Guid motorcycleId, int newKm) => Task.CompletedTask;
    }

    private sealed class FakeRecordRepository : IMotorcycleMaintenanceRecordRepository
    {
        public Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity) => Task.FromResult(entity);
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId) =>
            Task.FromResult(Enumerable.Empty<MotorcycleMaintenanceRecord>());
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) =>
            Task.FromResult(Enumerable.Empty<MotorcycleMaintenanceRecord>());
        public Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId) =>
            Task.FromResult<MotorcycleMaintenanceRecord?>(null);
        public Task<Dictionary<Guid, MotorcycleMaintenanceRecord>> GetLastByUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds) =>
            Task.FromResult(new Dictionary<Guid, MotorcycleMaintenanceRecord>());
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeTransactionManager : ITransactionManager
    {
        public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
            => action();

        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
            => action();
    }
}
