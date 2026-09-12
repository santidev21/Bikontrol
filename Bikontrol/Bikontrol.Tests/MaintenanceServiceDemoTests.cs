using AutoMapper;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Persistence.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Tests;

public class MaintenanceServiceDemoTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    [Fact]
    public async Task CreateUserMaintenanceAsync_WhenDemo_ShouldThrowForbidden()
    {
        var service = CreateService(isDemo: true);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.CreateUserMaintenanceAsync(new SaveMaintenanceDTO { MotorcycleId = Guid.NewGuid(), Name = "Test", Description = "Desc", KmInterval = 5000 }));
    }

    [Fact]
    public async Task FollowDefaultAsync_WhenDemo_ShouldThrowForbidden()
    {
        var service = CreateService(isDemo: true);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.FollowDefaultAsync(Guid.NewGuid(), Guid.NewGuid(), 1000, 0, "Km"));
    }

    [Fact]
    public async Task RegisterMaintenanceRecordAsync_WhenDemo_ShouldThrowForbidden()
    {
        var service = CreateService(isDemo: true);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.RegisterMaintenanceRecordAsync(new CreateMaintenanceRecordRequest { MotorcycleId = Guid.NewGuid(), UserMaintenanceId = Guid.NewGuid(), PerformedAt = DateTime.UtcNow, PerformedKm = 100 }));
    }

    [Fact]
    public async Task DeleteUserMaintenanceAsync_WhenDemo_ShouldThrowForbidden()
    {
        var service = CreateService(isDemo: true);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.DeleteUserMaintenanceAsync(Guid.NewGuid()));
    }

    private MaintenanceService CreateService(bool isDemo)
    {
        var userId = Guid.NewGuid();
        var motoId = Guid.NewGuid();
        var current = new FakeCurrentUserService(userId, isDemo ? UserRole.Demo : UserRole.User);
        var moto = new Motorcycle("Moto", "Brand", 2024, "N", 150, "ABC", userId) { Id = motoId };
        return new MaintenanceService(
            new FakeMaintenanceRepository(),
            new FakeUserMaintenanceRepository(),
            new FakeMotorcycleRepository(moto),
            new FakeKmHistoryService(),
            new FakeRecordRepository(),
            _mapper,
            current);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId, string role) { UserId = userId; Role = role; }
        public Guid UserId { get; }
        public string Role { get; }
        public bool IsDemo => Role == UserRole.Demo;
    }

    private sealed class FakeMaintenanceRepository : IMaintenanceRepository
    {
        public Task<IEnumerable<Maintenance>> GetAllAsync() => Task.FromResult(Enumerable.Empty<Maintenance>());
        public Task<IEnumerable<Maintenance>> GetAllForUserAsync(Guid userId) => Task.FromResult(Enumerable.Empty<Maintenance>());
        public Task<Maintenance?> GetByIdAsync(Guid id) => Task.FromResult<Maintenance?>(new Maintenance { Id = id, Name = "Default", Description = "Desc" });
    }

    private sealed class FakeUserMaintenanceRepository : IUserMaintenanceRepository
    {
        public Task<UserMaintenance> AddAsync(UserMaintenance entity) => Task.FromResult(entity);
        public Task<UserMaintenance?> GetByBaseIdAsync(Guid userId, Guid motorcycleId, Guid baseId) => Task.FromResult<UserMaintenance?>(null);
        public Task<UserMaintenance?> GetByIdAsync(Guid id) => Task.FromResult<UserMaintenance?>(new UserMaintenance { Id = id, UserId = Guid.Empty, Name = "Test", MotorcycleId = Guid.NewGuid() });
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAsync(Guid userId) => Task.FromResult(Enumerable.Empty<UserMaintenance>());
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAndMotorcycleIdAsync(Guid userId, Guid motorcycleId) => Task.FromResult(Enumerable.Empty<UserMaintenance>());
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task UpdateAsync(UserMaintenance entity) => Task.CompletedTask;
    }

    private sealed class FakeMotorcycleRepository : IMotorcycleRepository
    {
        private readonly Motorcycle _m;
        public FakeMotorcycleRepository(Motorcycle m) => _m = m;
        public Task<Motorcycle> AddAsync(Motorcycle motorcycle) => Task.FromResult(motorcycle);
        public Task<Motorcycle?> GetByIdAsync(Guid id) => Task.FromResult(id == _m.Id ? _m : null);
        public Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId) => Task.FromResult(Enumerable.Empty<Motorcycle>());
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task UpdateAsync(Motorcycle motorcycle) => Task.CompletedTask;
    }

    private sealed class FakeKmHistoryService : IKmHistoryService
    {
        public Task AddKmAsync(Guid motorcycleId, int km) => Task.CompletedTask;
        public Task<int> GetCurrentKmAsync(Guid motorcycleId) => Task.FromResult(0);
        public Task<DateTime?> GetInitialRecordedAtAsync(Guid motorcycleId) => Task.FromResult<DateTime?>(DateTime.UtcNow);
        public Task RollbackLastKmAsync(Guid motorcycleId, int newKm) => Task.CompletedTask;
    }

    private sealed class FakeRecordRepository : IMotorcycleMaintenanceRecordRepository
    {
        public Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity) => Task.FromResult(entity);
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId) => Task.FromResult(Enumerable.Empty<MotorcycleMaintenanceRecord>());
        public Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId) => Task.FromResult<MotorcycleMaintenanceRecord?>(null);
    }
}
