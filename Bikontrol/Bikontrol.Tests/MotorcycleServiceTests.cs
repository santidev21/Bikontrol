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

public class MotorcycleServiceTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

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

    private static MotorcycleService CreateService(Guid userId, FakeMotorcycleRepository motos, FakeKmHistoryRepository history)
        => new(
            motos,
            history,
            new FakeKmHistoryService(),
            Mapper,
            new FakeCurrentUserService(userId),
            new FakeTransactionManager());

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
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeKmHistoryRepository : IKmHistoryRepository
    {
        private readonly List<MotorcycleKmHistory> _items = new();

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

        public Task SaveChangesAsync() => Task.CompletedTask;
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
