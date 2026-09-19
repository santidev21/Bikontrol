using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Tests;

public class KmHistoryServiceTests
{
    [Fact]
    public async Task AddKmAsync_WhenKmIsGreaterThanCurrent_ShouldAddRecord()
    {
        var motorcycleId = Guid.NewGuid();
        var repository = new FakeKmHistoryRepository(new List<MotorcycleKmHistory>
        {
            new() { MotorcycleId = motorcycleId, Km = 1200, RecordedAt = DateTime.UtcNow.AddDays(-2) }
        });

        var service = new KmHistoryService(repository, new FakeTransactionManager());

        await service.AddKmAsync(motorcycleId, 1500);

        var history = await repository.GetByMotorcycleIdAsync(motorcycleId);
        Assert.Equal(2, history.Count);
        Assert.Equal(1500, history[0].Km);
    }

    [Fact]
    public async Task AddKmAsync_WhenKmIsLowerThanCurrent_ShouldThrowValidationException()
    {
        var motorcycleId = Guid.NewGuid();
        var repository = new FakeKmHistoryRepository(new List<MotorcycleKmHistory>
        {
            new() { MotorcycleId = motorcycleId, Km = 1800, RecordedAt = DateTime.UtcNow.AddDays(-2) }
        });

        var service = new KmHistoryService(repository, new FakeTransactionManager());

        await Assert.ThrowsAsync<ValidationException>(() => service.AddKmAsync(motorcycleId, 1600));
    }

    [Fact]
    public async Task RollbackLastKmAsync_WhenOnlyInitialRecord_ShouldThrowValidationException()
    {
        var repository = new FakeKmHistoryRepository(new List<MotorcycleKmHistory>
        {
            new() { MotorcycleId = Guid.NewGuid(), Km = 1200, RecordedAt = DateTime.UtcNow.AddDays(-2) }
        });

        var service = new KmHistoryService(repository, new FakeTransactionManager());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RollbackLastKmAsync(Guid.NewGuid(), 1000));
    }

    [Fact]
    public async Task RollbackLastKmAsync_WhenNewKmIsGreaterThanRemovedRecord_ShouldThrowValidationException()
    {
        var motorcycleId = Guid.NewGuid();
        var repository = new FakeKmHistoryRepository(new List<MotorcycleKmHistory>
        {
            new() { MotorcycleId = motorcycleId, Km = 2000, RecordedAt = DateTime.UtcNow.AddDays(-2) },
            new() { MotorcycleId = motorcycleId, Km = 2500, RecordedAt = DateTime.UtcNow.AddDays(-1) }
        });

        var service = new KmHistoryService(repository, new FakeTransactionManager());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RollbackLastKmAsync(motorcycleId, 2600));
    }

    [Fact]
    public async Task RollbackLastKmAsync_WhenNewKmEqualsPrevious_ShouldRemoveLastWithoutCreatingDuplicate()
    {
        var motorcycleId = Guid.NewGuid();
        var repository = new FakeKmHistoryRepository(new List<MotorcycleKmHistory>
        {
            new() { MotorcycleId = motorcycleId, Km = 2000, RecordedAt = DateTime.UtcNow.AddDays(-2) },
            new() { MotorcycleId = motorcycleId, Km = 2500, RecordedAt = DateTime.UtcNow.AddDays(-1) }
        });

        var service = new KmHistoryService(repository, new FakeTransactionManager());

        await service.RollbackLastKmAsync(motorcycleId, 2000);

        var history = await repository.GetByMotorcycleIdAsync(motorcycleId);
        Assert.Single(history);
        Assert.Equal(2000, history[0].Km);
    }

    [Fact]
    public async Task RollbackLastKmAsync_WhenNewKmIsBetweenPreviousAndLast_ShouldRollbackToPenultimateState()
    {
        var motorcycleId = Guid.NewGuid();
        var repository = new FakeKmHistoryRepository(new List<MotorcycleKmHistory>
        {
            new() { MotorcycleId = motorcycleId, Km = 2000, RecordedAt = DateTime.UtcNow.AddDays(-3) },
            new() { MotorcycleId = motorcycleId, Km = 2500, RecordedAt = DateTime.UtcNow.AddDays(-2) },
            new() { MotorcycleId = motorcycleId, Km = 2600, RecordedAt = DateTime.UtcNow.AddDays(-1) }
        });

        var service = new KmHistoryService(repository, new FakeTransactionManager());

        await service.RollbackLastKmAsync(motorcycleId, 2500);

        var history = await repository.GetByMotorcycleIdAsync(motorcycleId);
        Assert.Equal(2, history.Count);
        Assert.Equal(2500, history[0].Km);
        Assert.Equal(2000, history[1].Km);
    }

    [Fact]
    public async Task RollbackLastKmAsync_WhenNewKmEqualsLast_ShouldUndoLastChange()
    {
        var motorcycleId = Guid.NewGuid();
        var repository = new FakeKmHistoryRepository(new List<MotorcycleKmHistory>
        {
            new() { MotorcycleId = motorcycleId, Km = 2000, RecordedAt = DateTime.UtcNow.AddDays(-2) },
            new() { MotorcycleId = motorcycleId, Km = 2600, RecordedAt = DateTime.UtcNow.AddDays(-1) }
        });

        var service = new KmHistoryService(repository, new FakeTransactionManager());

        await service.RollbackLastKmAsync(motorcycleId, 2600);

        var history = await repository.GetByMotorcycleIdAsync(motorcycleId);
        Assert.Single(history);
        Assert.Equal(2000, history[0].Km);
    }

    private sealed class FakeKmHistoryRepository : IKmHistoryRepository
    {
        private readonly List<MotorcycleKmHistory> _items;

        public FakeKmHistoryRepository(List<MotorcycleKmHistory> items)
        {
            _items = items;
        }

        public Task AddAsync(MotorcycleKmHistory entity)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task<MotorcycleKmHistory?> GetLastByMotorcycleIdAsync(Guid motorcycleId)
        {
            var item = _items
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.RecordedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();
            return Task.FromResult(item);
        }

        public Task<MotorcycleKmHistory?> GetFirstByMotorcycleIdAsync(Guid motorcycleId)
        {
            var item = _items
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderBy(x => x.RecordedAt)
                .ThenBy(x => x.Id)
                .FirstOrDefault();
            return Task.FromResult(item);
        }

        public Task<List<MotorcycleKmHistory>> GetByMotorcycleIdAsync(Guid motorcycleId)
        {
            var list = _items
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.RecordedAt)
                .ThenByDescending(x => x.Id)
                .ToList();
            return Task.FromResult(list);
        }

        public void Remove(MotorcycleKmHistory entity)
        {
            _items.Remove(entity);
        }

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

    private sealed class FakeTransactionManager : ITransactionManager
    {
        public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
            => action();

        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
            => action();
    }
}
