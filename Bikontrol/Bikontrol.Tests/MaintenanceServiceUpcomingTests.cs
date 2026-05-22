using AutoMapper;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Services;

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
            new FakeCurrentUserService(userId));

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
            new FakeCurrentUserService(userId));

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
            new FakeCurrentUserService(userId));

        var result = (await service.GetUpcomingByMotorcycleAsync(motorcycleId)).ToList();
        Assert.Single(result);
        Assert.Equal(0, result[0].LifePercent);
        Assert.True(result[0].IsOverdue);
        Assert.Equal(initialDate, result[0].LastPerformedAt?.Date);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId) => UserId = userId;
        public Guid UserId { get; }
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
    }

    private sealed class FakeMotorcycleRepository : IMotorcycleRepository
    {
        private readonly Motorcycle _motorcycle;
        public FakeMotorcycleRepository(Motorcycle motorcycle) => _motorcycle = motorcycle;
        public Task<Motorcycle> AddAsync(Motorcycle motorcycle) => Task.FromResult(motorcycle);
        public Task<Motorcycle?> GetByIdAsync(Guid id) => Task.FromResult(id == _motorcycle.Id ? _motorcycle : null);
        public Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId) => Task.FromResult(Enumerable.Empty<Motorcycle>());
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task UpdateAsync(Motorcycle motorcycle) => Task.CompletedTask;
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
        public Task RollbackLastKmAsync(Guid motorcycleId, int newKm) => Task.CompletedTask;
    }

    private sealed class FakeRecordRepository : IMotorcycleMaintenanceRecordRepository
    {
        public Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity) => Task.FromResult(entity);
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId) =>
            Task.FromResult(Enumerable.Empty<MotorcycleMaintenanceRecord>());
        public Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId) =>
            Task.FromResult<MotorcycleMaintenanceRecord?>(null);
    }
}
