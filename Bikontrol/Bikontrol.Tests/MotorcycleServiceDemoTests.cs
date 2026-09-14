using AutoMapper;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Persistence.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Tests;

public class MotorcycleServiceDemoTests
{
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    [Fact]
    public async Task CreateAsync_WhenDemo_ShouldThrowForbidden()
    {
        var service = CreateService(isDemo: true);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.CreateAsync(new SaveMotorcycleDTO { Name = "Moto", Brand = "Honda", Year = 2024, Nickname = "N", Displacement = 150, Plate = "ABC123", Km = 100 }));
    }

    [Theory]
    [InlineData(nameof(MotorcycleService.AddKmHistoryAsync))]
    [InlineData(nameof(MotorcycleService.RollbackLastKmAsync))]
    [InlineData(nameof(MotorcycleService.UpdateAsync))]
    [InlineData(nameof(MotorcycleService.SoftDeleteAsync))]
    public async Task WriteOperations_WhenDemo_ShouldThrowForbidden(string method)
    {
        var userId = Guid.NewGuid();
        var motoId = Guid.NewGuid();
        var service = CreateService(isDemo: true, motorcycleId: motoId, userId: userId);
        var ex = method switch
        {
            nameof(MotorcycleService.AddKmHistoryAsync) => await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.AddKmHistoryAsync(motoId, 500)),
            nameof(MotorcycleService.RollbackLastKmAsync) => await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.RollbackLastKmAsync(motoId, 400)),
            nameof(MotorcycleService.UpdateAsync) => await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.UpdateAsync(motoId, new SaveMotorcycleDTO { Name = "Moto", Brand = "Honda", Year = 2024, Nickname = "N", Displacement = 150, Plate = "ABC123", Km = 100 })),
            nameof(MotorcycleService.SoftDeleteAsync) => await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.SoftDeleteAsync(motoId)),
            _ => throw new InvalidOperationException()
        };
        Assert.Contains("demo", ex.Message.ToLower());
    }

    [Fact]
    public async Task GetByIdAsync_WhenDemo_ShouldStillAllowRead()
    {
        var userId = Guid.NewGuid();
        var motoId = Guid.NewGuid();
        var service = CreateService(isDemo: true, motorcycleId: motoId, userId: userId);
        // GetById is read-only, should not be blocked by demo guard (only write ops are blocked)
        // It will still enforce ownership; with same userId it should succeed
        var result = await service.GetByIdAsync(motoId);
        Assert.NotNull(result);
    }

    private MotorcycleService CreateService(bool isDemo, Guid? motorcycleId = null, Guid? userId = null)
    {
        var uid = userId ?? Guid.NewGuid();
        var mid = motorcycleId ?? Guid.NewGuid();
        var motorcycle = new Motorcycle("Moto", "Honda", 2024, "N", 150, "ABC123", uid) { Id = mid };
        var current = new FakeCurrentUserService(uid, isDemo ? UserRole.Demo : UserRole.User);
        return new MotorcycleService(
            new FakeMotorcycleRepository(motorcycle),
            new FakeKmHistoryRepository(),
            new FakeKmHistoryService(),
            _mapper,
            current,
            new FakeTransactionManager());
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
        public FakeCurrentUserService(Guid userId, string role) { UserId = userId; Role = role; }
        public Guid UserId { get; }
        public string Role { get; }
        public bool IsDemo => Role == UserRole.Demo;
    }

    private sealed class FakeMotorcycleRepository : IMotorcycleRepository
    {
        private readonly Motorcycle _motorcycle;
        public FakeMotorcycleRepository(Motorcycle motorcycle) => _motorcycle = motorcycle;
        public Task<Motorcycle> AddAsync(Motorcycle motorcycle) => Task.FromResult(motorcycle);
        public Task<Motorcycle?> GetByIdAsync(Guid id) => Task.FromResult(id == _motorcycle.Id ? _motorcycle : null);
        public Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId) => Task.FromResult(Enumerable.Empty<Motorcycle>().AsEnumerable());
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task UpdateAsync(Motorcycle motorcycle) => Task.CompletedTask;
    }

    private sealed class FakeKmHistoryRepository : IKmHistoryRepository
    {
        public Task AddAsync(MotorcycleKmHistory entity) => Task.CompletedTask;
        public Task<List<MotorcycleKmHistory>> GetByMotorcycleIdAsync(Guid motorcycleId) => Task.FromResult(new List<MotorcycleKmHistory> { new() { MotorcycleId = motorcycleId, Km = 100, RecordedAt = DateTime.UtcNow } });
        public Task<MotorcycleKmHistory?> GetFirstByMotorcycleIdAsync(Guid motorcycleId) => Task.FromResult<MotorcycleKmHistory?>(null);
        public Task<MotorcycleKmHistory?> GetLastByMotorcycleIdAsync(Guid motorcycleId) => Task.FromResult<MotorcycleKmHistory?>(new MotorcycleKmHistory { MotorcycleId = motorcycleId, Km = 100, RecordedAt = DateTime.UtcNow });
        public void Remove(MotorcycleKmHistory entity) { }
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeKmHistoryService : IKmHistoryService
    {
        public Task AddKmAsync(Guid motorcycleId, int km) => Task.CompletedTask;
        public Task<int> GetCurrentKmAsync(Guid motorcycleId) => Task.FromResult(100);
        public Task<DateTime?> GetInitialRecordedAtAsync(Guid motorcycleId) => Task.FromResult<DateTime?>(DateTime.UtcNow.AddDays(-5));
        public Task RollbackLastKmAsync(Guid motorcycleId, int newKm) => Task.CompletedTask;
    }
}
