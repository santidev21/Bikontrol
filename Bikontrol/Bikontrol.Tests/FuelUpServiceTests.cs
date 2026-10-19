using Bikontrol.Application.DTOs.Fuel;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Tests;

public class FuelUpServiceTests
{
    private static Motorcycle MotorcycleFor(Guid userId) =>
        new("YBR 125", "Yamaha", 2024, "Negra", 125, "ABC12D", userId) { Id = Guid.NewGuid() };

    private static FuelUpService CreateService(
        Guid userId,
        FakeFuelUpRepository? repo = null,
        FakeMotorcycleRepository? motorcycles = null,
        FakeRecordRepository? records = null,
        FakeKmHistoryService? kmHistory = null,
        string role = UserRole.User)
        => new(
            repo ?? new FakeFuelUpRepository(),
            motorcycles ?? new FakeMotorcycleRepository(Array.Empty<Motorcycle>()),
            records ?? new FakeRecordRepository(),
            kmHistory ?? new FakeKmHistoryService(),
            new FakeCurrentUserService(userId, role),
            new FakeTransactionManager());

    private static SaveFuelUpRequest Request(Guid motorcycleId, int odometerKm = 1200, decimal litres = 4m, decimal cost = 8m)
        => new()
        {
            MotorcycleId = motorcycleId,
            FueledAt = DateTime.UtcNow.AddDays(-1),
            OdometerKm = odometerKm,
            Litres = litres,
            TotalCost = cost
        };

    [Fact]
    public async Task CreateAsync_ValidRequest_AddsFuelUpAndAdvancesOdometer()
    {
        var userId = Guid.NewGuid();
        var moto = MotorcycleFor(userId);
        var repo = new FakeFuelUpRepository();
        var kmHistory = new FakeKmHistoryService { CurrentKm = 1000 };
        var service = CreateService(userId, repo, new FakeMotorcycleRepository(new[] { moto }), kmHistory: kmHistory);

        var dto = await service.CreateAsync(Request(moto.Id, odometerKm: 1200));

        Assert.Single(repo.Items);
        Assert.Equal(1200, dto.OdometerKm);
        Assert.Equal(2m, dto.PricePerLitre); // 8 / 4
        Assert.Equal(1200, kmHistory.LastAddedKm);
    }

    [Fact]
    public async Task CreateAsync_OdometerBelowCurrent_DoesNotAdvance()
    {
        var userId = Guid.NewGuid();
        var moto = MotorcycleFor(userId);
        var kmHistory = new FakeKmHistoryService { CurrentKm = 5000 };
        var service = CreateService(userId, motorcycles: new FakeMotorcycleRepository(new[] { moto }), kmHistory: kmHistory);

        await service.CreateAsync(Request(moto.Id, odometerKm: 1200));

        Assert.Null(kmHistory.LastAddedKm);
    }

    [Fact]
    public async Task CreateAsync_FutureDate_ThrowsValidation()
    {
        var userId = Guid.NewGuid();
        var moto = MotorcycleFor(userId);
        var service = CreateService(userId, motorcycles: new FakeMotorcycleRepository(new[] { moto }));
        var request = Request(moto.Id);
        request.FueledAt = DateTime.UtcNow.AddDays(2);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_ZeroLitres_ThrowsValidation()
    {
        var userId = Guid.NewGuid();
        var moto = MotorcycleFor(userId);
        var service = CreateService(userId, motorcycles: new FakeMotorcycleRepository(new[] { moto }));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(Request(moto.Id, litres: 0m)));
    }

    [Fact]
    public async Task CreateAsync_DemoUser_ThrowsForbidden()
    {
        var userId = Guid.NewGuid();
        var moto = MotorcycleFor(userId);
        var service = CreateService(userId, motorcycles: new FakeMotorcycleRepository(new[] { moto }), role: UserRole.Demo);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.CreateAsync(Request(moto.Id)));
    }

    [Fact]
    public async Task CreateAsync_MotorcycleNotOwned_ThrowsForbidden()
    {
        var moto = MotorcycleFor(Guid.NewGuid());
        var service = CreateService(Guid.NewGuid(), motorcycles: new FakeMotorcycleRepository(new[] { moto }));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.CreateAsync(Request(moto.Id)));
    }

    [Fact]
    public async Task GetByMotorcycleAsync_NotOwned_ThrowsForbidden()
    {
        var moto = MotorcycleFor(Guid.NewGuid());
        var service = CreateService(Guid.NewGuid(), motorcycles: new FakeMotorcycleRepository(new[] { moto }));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetByMotorcycleAsync(moto.Id));
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ThrowsNotFound()
    {
        var service = CreateService(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_ChangesFieldsAndPersists()
    {
        var userId = Guid.NewGuid();
        var moto = MotorcycleFor(userId);
        var existing = new FuelUp { Id = Guid.NewGuid(), MotorcycleId = moto.Id, FueledAt = DateTime.UtcNow.AddDays(-2), OdometerKm = 1000, Litres = 3m, TotalCost = 6m };
        var repo = new FakeFuelUpRepository(new[] { existing });
        var service = CreateService(userId, repo, new FakeMotorcycleRepository(new[] { moto }));

        var dto = await service.UpdateAsync(existing.Id, Request(moto.Id, odometerKm: 1500, litres: 5m, cost: 10m));

        Assert.Equal(1500, dto.OdometerKm);
        Assert.Equal(5m, existing.Litres);
        Assert.True(repo.UpdateCalled);
    }

    [Fact]
    public async Task UpdateAsync_HigherOdometer_AdvancesKmHistory()
    {
        var userId = Guid.NewGuid();
        var moto = MotorcycleFor(userId);
        var existing = new FuelUp { Id = Guid.NewGuid(), MotorcycleId = moto.Id, OdometerKm = 1000, Litres = 3m, TotalCost = 6m };
        var repo = new FakeFuelUpRepository(new[] { existing });
        var kmHistory = new FakeKmHistoryService { CurrentKm = 1000 };
        var service = CreateService(userId, repo, new FakeMotorcycleRepository(new[] { moto }), kmHistory: kmHistory);

        await service.UpdateAsync(existing.Id, Request(moto.Id, odometerKm: 1500));

        Assert.Equal(1500, kmHistory.LastAddedKm);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        var userId = Guid.NewGuid();
        var moto = MotorcycleFor(userId);
        var existing = new FuelUp { Id = Guid.NewGuid(), MotorcycleId = moto.Id };
        var repo = new FakeFuelUpRepository(new[] { existing });
        var service = CreateService(userId, repo, new FakeMotorcycleRepository(new[] { moto }));

        await service.DeleteAsync(existing.Id);

        Assert.True(repo.RemoveCalled);
    }

    [Fact]
    public async Task GetConsumptionAsync_IncludesMaintenanceCost()
    {
        var userId = Guid.NewGuid();
        var moto = MotorcycleFor(userId);
        var repo = new FakeFuelUpRepository(new[]
        {
            new FuelUp { Id = Guid.NewGuid(), MotorcycleId = moto.Id, OdometerKm = 1000, Litres = 5m, TotalCost = 10m, IsFullTank = true },
            new FuelUp { Id = Guid.NewGuid(), MotorcycleId = moto.Id, OdometerKm = 1200, Litres = 4m, TotalCost = 8m, IsFullTank = true }
        });
        var records = new FakeRecordRepository(new[]
        {
            new MotorcycleMaintenanceRecord { Id = Guid.NewGuid(), MotorcycleId = moto.Id, Cost = 30m }
        });
        var service = CreateService(userId, repo, new FakeMotorcycleRepository(new[] { moto }), records);

        var result = await service.GetConsumptionAsync(moto.Id);

        Assert.Equal(18m, result.TotalFuelCost);
        Assert.Equal(30m, result.TotalMaintenanceCost);
        Assert.Equal(48m, result.TotalCostOfOwnership);
        Assert.Equal(50m, result.AverageKmPerLitre);
    }

    [Fact]
    public async Task GetConsumptionByMotorcyclesAsync_NotOwned_ThrowsForbidden()
    {
        var moto = MotorcycleFor(Guid.NewGuid());
        var service = CreateService(Guid.NewGuid(), motorcycles: new FakeMotorcycleRepository(new[] { moto }));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.GetConsumptionByMotorcyclesAsync(new[] { moto.Id }));
    }

    private sealed class FakeFuelUpRepository : IFuelUpRepository
    {
        private readonly List<FuelUp> _items;
        public FakeFuelUpRepository(IEnumerable<FuelUp>? items = null) => _items = items?.ToList() ?? new List<FuelUp>();
        public IReadOnlyList<FuelUp> Items => _items;
        public bool UpdateCalled { get; private set; }
        public bool RemoveCalled { get; private set; }

        public Task<FuelUp> AddAsync(FuelUp entity)
        {
            _items.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<FuelUp?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<FuelUp>> GetByMotorcycleIdAsync(Guid motorcycleId, int? limit = null) =>
            Task.FromResult<IReadOnlyList<FuelUp>>(_items.Where(x => x.MotorcycleId == motorcycleId).ToList());
        public Task<IReadOnlyList<FuelUp>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<FuelUp>>(_items.Where(x => ids.Contains(x.MotorcycleId)).ToList());
        }

        public Task UpdateAsync(FuelUp entity)
        {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public void Remove(FuelUp entity) => RemoveCalled = true;
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

    private sealed class FakeRecordRepository : IMotorcycleMaintenanceRecordRepository
    {
        private readonly List<MotorcycleMaintenanceRecord> _items;
        public FakeRecordRepository(IEnumerable<MotorcycleMaintenanceRecord>? items = null) => _items = items?.ToList() ?? new List<MotorcycleMaintenanceRecord>();
        public Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity) => Task.FromResult(entity);
        public Task<MotorcycleMaintenanceRecord?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(r => r.Id == id));
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId, int? limit = null) =>
            Task.FromResult(_items.Where(r => r.MotorcycleId == motorcycleId).AsEnumerable());
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds.ToHashSet();
            return Task.FromResult(_items.Where(r => ids.Contains(r.MotorcycleId)).AsEnumerable());
        }
        public Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId) => Task.FromResult<MotorcycleMaintenanceRecord?>(null);
        public Task<Dictionary<Guid, MotorcycleMaintenanceRecord>> GetLastByUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds) => Task.FromResult(new Dictionary<Guid, MotorcycleMaintenanceRecord>());
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeKmHistoryService : IKmHistoryService
    {
        public int CurrentKm { get; set; }
        public int? LastAddedKm { get; private set; }

        public Task AddKmAsync(Guid motorcycleId, int km)
        {
            LastAddedKm = km;
            CurrentKm = km;
            return Task.CompletedTask;
        }

        public Task<int> GetCurrentKmAsync(Guid motorcycleId) => Task.FromResult(CurrentKm);
        public Task<DateTime?> GetInitialRecordedAtAsync(Guid motorcycleId) => Task.FromResult<DateTime?>(null);
        public Task<IReadOnlyDictionary<Guid, int>> GetCurrentKmByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(motorcycleIds.ToDictionary(id => id, _ => CurrentKm));
        public Task<IReadOnlyDictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) =>
            Task.FromResult<IReadOnlyDictionary<Guid, DateTime?>>(motorcycleIds.ToDictionary(id => id, _ => (DateTime?)null));
        public Task RollbackLastKmAsync(Guid motorcycleId, int newKm) => Task.CompletedTask;
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId, string role)
        {
            UserId = userId;
            Role = role;
        }
        public Guid UserId { get; }
        public string Role { get; }
        public bool IsDemo => Role == UserRole.Demo;
    }

    private sealed class FakeTransactionManager : ITransactionManager
    {
        public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default) => action();
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) => action();
    }
}
