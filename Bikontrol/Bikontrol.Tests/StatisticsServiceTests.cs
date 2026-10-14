using Bikontrol.Application.DTOs.Fuel;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.DTOs.Users;
using Bikontrol.Application.Interfaces;
using Bikontrol.Infrastructure.Services;

namespace Bikontrol.Tests;

public class StatisticsServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ShouldAggregateFleetStats()
    {
        var moto1Id = Guid.NewGuid();
        var moto2Id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        // Fechas ancladas al inicio de mes para que el test sea estable
        // sin importar el día en que se ejecute.
        var currentMonth = new DateTime(now.Year, now.Month, 1);
        var thisMonthA = currentMonth.AddDays(5);
        var thisMonthB = currentMonth.AddDays(6);
        var twoMonthsAgo = currentMonth.AddMonths(-2).AddDays(10);
        var sevenMonthsAgo = currentMonth.AddMonths(-7).AddDays(10);

        var motorcycles = new List<MotorcycleDTO>
        {
            new() { Id = moto1Id, Name = "YBR 125", Nickname = "Negra", Km = 8000 },
            new() { Id = moto2Id, Name = "CB125F", Nickname = "", Km = 12000 }
        };

        var upcoming = new Dictionary<Guid, List<UpcomingMaintenanceDTO>>
        {
            [moto1Id] = new()
            {
                new() { Name = "Aceite", IsOverdue = true, LifePercent = 0, RemainingKm = -200 },
                new() { Name = "Cadena", IsOverdue = false, LifePercent = 10, RemainingKm = 50 }
            },
            [moto2Id] = new()
            {
                new() { Name = "Bujía", IsOverdue = false, LifePercent = 40, RemainingDays = 10 },
                new() { Name = "Frenos", IsOverdue = false, LifePercent = 80, RemainingKm = 6400 }
            }
        };

        var records = new Dictionary<Guid, List<MaintenanceRecordDTO>>
        {
            [moto1Id] = new()
            {
                new() { MotorcycleId = moto1Id, MaintenanceName = "Aceite", PerformedAt = thisMonthA, Cost = 40m },
                new() { MotorcycleId = moto1Id, MaintenanceName = "Cadena", PerformedAt = thisMonthB, Cost = 20m },
                new() { MotorcycleId = moto1Id, MaintenanceName = "Aceite", PerformedAt = twoMonthsAgo, Cost = 100m }
            },
            [moto2Id] = new()
            {
                new() { MotorcycleId = moto2Id, MaintenanceName = "Aceite", PerformedAt = sevenMonthsAgo, Cost = 60m }
            }
        };

        var service = new StatisticsService(
            new FakeMotorcycleService(motorcycles),
            new FakeMaintenanceService(upcoming, records),
            new FakeFuelUpService(),
            new FakeUserService());

        var result = await service.GetSummaryAsync();

        Assert.Equal(2, result.TotalMotorcycles);
        Assert.Equal(20000, result.TotalKm);
        Assert.Equal(4, result.TotalMaintenanceRecords);
        Assert.Equal(1, result.OverdueCount);
        Assert.Equal(1, result.DueSoonCount);

        Assert.Equal(1, result.Health.Single(h => h.Bucket == "Vencido").Count);
        Assert.Equal(1, result.Health.Single(h => h.Bucket == "Crítico").Count);
        Assert.Equal(1, result.Health.Single(h => h.Bucket == "Próximo").Count);
        Assert.Equal(1, result.Health.Single(h => h.Bucket == "OK").Count);

        Assert.Equal(2, result.KmByMotorcycle.Count);
        Assert.Equal("Negra", result.KmByMotorcycle.Single(k => k.MotorcycleId == moto1Id).Name);
        Assert.Equal("CB125F", result.KmByMotorcycle.Single(k => k.MotorcycleId == moto2Id).Name);

        Assert.Equal("Aceite", result.RecordsByType[0].Name);
        Assert.Equal(3, result.RecordsByType[0].Count);
        Assert.Equal("Cadena", result.RecordsByType[1].Name);
        Assert.Equal(1, result.RecordsByType[1].Count);

        Assert.Equal(6, result.Last6Months.Count);
        Assert.Equal(3, result.Last6Months.Sum(m => m.Count));
        Assert.Equal(2, result.Last6Months[^1].Count);
        Assert.Equal(now.ToString("yyyy-MM"), result.Last6Months[^1].YearMonth);

        // Cost aggregates (only records with a cost are counted).
        Assert.Equal(220m, result.TotalCost);
        Assert.Equal(0.011m, result.CostPerKm); // 220 / 20000 km
        Assert.Equal(160m, result.Last6Months.Sum(m => m.Cost)); // excludes the 7-months-ago record
        Assert.Equal(220m, result.CostByYear.Sum(y => y.Cost));

        var moto1Cost = result.CostByMotorcycle.Single(c => c.MotorcycleId == moto1Id);
        Assert.Equal(160m, moto1Cost.Cost);
        Assert.Equal(0.02m, moto1Cost.CostPerKm); // 160 / 8000 km
        var moto2Cost = result.CostByMotorcycle.Single(c => c.MotorcycleId == moto2Id);
        Assert.Equal(60m, moto2Cost.Cost);
        Assert.Equal(0.005m, moto2Cost.CostPerKm); // 60 / 12000 km

        Assert.Equal(thisMonthB, result.LastActivityAt);
    }

    [Fact]
    public async Task GetSummaryAsync_WithoutCostsOrKm_ShouldReturnZeroAndNullCostPerKm()
    {
        var motoId = Guid.NewGuid();
        var motorcycles = new List<MotorcycleDTO> { new() { Id = motoId, Name = "Moto", Nickname = "M", Km = 0 } };
        var records = new Dictionary<Guid, List<MaintenanceRecordDTO>>
        {
            [motoId] = new() { new() { MotorcycleId = motoId, MaintenanceName = "Aceite", PerformedAt = DateTime.UtcNow } }
        };

        var service = new StatisticsService(
            new FakeMotorcycleService(motorcycles),
            new FakeMaintenanceService(new(), records),
            new FakeFuelUpService(),
            new FakeUserService());

        var result = await service.GetSummaryAsync();

        Assert.Equal(0m, result.TotalCost);
        Assert.Null(result.CostPerKm);
        Assert.Null(result.CostByMotorcycle.Single().CostPerKm); // km == 0
        Assert.Empty(result.CostByYear); // no record carried a cost
    }

    [Fact]
    public async Task GetSummaryAsync_WithoutMotorcycles_ShouldReturnZeros()
    {
        var service = new StatisticsService(
            new FakeMotorcycleService(new List<MotorcycleDTO>()),
            new FakeMaintenanceService(new(), new()),
            new FakeFuelUpService(),
            new FakeUserService());

        var result = await service.GetSummaryAsync();

        Assert.Equal(0, result.TotalMotorcycles);
        Assert.Equal(0, result.TotalKm);
        Assert.Equal(0, result.TotalMaintenanceRecords);
        Assert.Equal(0, result.OverdueCount);
        Assert.Equal(0, result.DueSoonCount);
        Assert.Null(result.LastActivityAt);
        Assert.Empty(result.RecordsByType);
        Assert.Equal(6, result.Last6Months.Count);
        Assert.All(result.Last6Months, m => Assert.Equal(0, m.Count));
        Assert.All(result.Health, h => Assert.Equal(0, h.Count));
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldAggregateFuelAndBudget()
    {
        var motoId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1).AddDays(3);
        var motorcycles = new List<MotorcycleDTO> { new() { Id = motoId, Name = "Moto", Nickname = "M", Km = 1000 } };
        var records = new Dictionary<Guid, List<MaintenanceRecordDTO>>
        {
            [motoId] = new() { new() { MotorcycleId = motoId, MaintenanceName = "Aceite", PerformedAt = thisMonth, Cost = 120m } }
        };
        var consumption = new Dictionary<Guid, FuelConsumptionDTO>
        {
            [motoId] = new()
            {
                MotorcycleId = motoId,
                TotalFuelCost = 80m,
                TotalMaintenanceCost = 120m,
                TotalCostOfOwnership = 200m,
                Points = new() { new() { DistanceKm = 400, Litres = 10m, KmPerLitre = 40m } }
            }
        };
        var profile = new ProfileDTO { MonthlyMaintenanceBudget = 100m, AnnualMaintenanceBudget = 1000m };

        var service = new StatisticsService(
            new FakeMotorcycleService(motorcycles),
            new FakeMaintenanceService(new(), records),
            new FakeFuelUpService(consumption),
            new FakeUserService(profile));

        var result = await service.GetSummaryAsync();

        Assert.Equal(80m, result.TotalFuelCost);
        Assert.Equal(200m, result.TotalCostOfOwnership);
        Assert.Equal(40m, result.AverageKmPerLitre);
        Assert.Equal(0.08m, result.FuelCostPerKm); // 80 / 1000
        Assert.Equal(100m, result.Budget.MonthlyBudget);
        Assert.Equal(120m, result.Budget.MonthlySpend);
        Assert.True(result.Budget.IsMonthlyOverBudget);  // 120 > 100
        Assert.False(result.Budget.IsAnnualOverBudget);  // 120 <= 1000
        Assert.False(result.HasConsumptionDrop);
    }

    private sealed class FakeMotorcycleService : IMotorcycleService
    {
        private readonly List<MotorcycleDTO> _motorcycles;
        public FakeMotorcycleService(List<MotorcycleDTO> motorcycles) => _motorcycles = motorcycles;
        public Task<MotorcycleDTO> CreateAsync(SaveMotorcycleDTO dto) => throw new NotImplementedException();
        public Task<MotorcycleDTO?> GetByIdAsync(Guid id) => Task.FromResult(_motorcycles.FirstOrDefault(m => m.Id == id));
        public Task<IList<MotorcycleDTO>> GetByCurrentUserAsync() => Task.FromResult<IList<MotorcycleDTO>>(_motorcycles);
        public Task<int> GetCurrentKmAsync(Guid id) => Task.FromResult(_motorcycles.First(m => m.Id == id).Km);
        public Task AddKmHistoryAsync(Guid id, int km) => Task.CompletedTask;
        public Task RollbackLastKmAsync(Guid id, int newKm) => Task.CompletedTask;
        public Task UpdateAsync(Guid id, SaveMotorcycleDTO dto) => Task.CompletedTask;
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
    }

    private sealed class FakeMaintenanceService : IMaintenanceService
    {
        private readonly Dictionary<Guid, List<UpcomingMaintenanceDTO>> _upcoming;
        private readonly Dictionary<Guid, List<MaintenanceRecordDTO>> _records;
        public FakeMaintenanceService(
            Dictionary<Guid, List<UpcomingMaintenanceDTO>> upcoming,
            Dictionary<Guid, List<MaintenanceRecordDTO>> records)
        {
            _upcoming = upcoming;
            _records = records;
        }
        public Task<IEnumerable<MaintenanceDTO>> GetDefaultsAsync() => Task.FromResult(Enumerable.Empty<MaintenanceDTO>());
        public Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceAsync() => Task.FromResult(Enumerable.Empty<MaintenanceDTO>());
        public Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceByMotorcycleAsync(Guid motorcycleId) => Task.FromResult(Enumerable.Empty<MaintenanceDTO>());
        public Task<MaintenanceDTO?> GetByIdAsync(Guid id) => Task.FromResult<MaintenanceDTO?>(null);
        public Task<MaintenanceDTO> CreateUserMaintenanceAsync(SaveMaintenanceDTO dto) => throw new NotImplementedException();
        public Task DeleteUserMaintenanceAsync(Guid id) => Task.CompletedTask;
        public Task<MaintenanceDTO> FollowDefaultAsync(Guid motorcycleId, Guid defaultId, int? KmInterval, int? TimeIntervalWeeks, string TrackingType) => throw new NotImplementedException();
        public Task UpdateAsync(Guid id, SaveMaintenanceDTO dto) => Task.CompletedTask;
        public Task<MaintenanceRecordDTO> RegisterMaintenanceRecordAsync(CreateMaintenanceRecordRequest request) => throw new NotImplementedException();
        public Task<IEnumerable<MaintenanceRecordDTO>> GetMaintenanceRecordsByMotorcycleAsync(Guid motorcycleId) =>
            Task.FromResult(_records.TryGetValue(motorcycleId, out var r) ? r.AsEnumerable() : Enumerable.Empty<MaintenanceRecordDTO>());
        public Task<IEnumerable<UpcomingMaintenanceDTO>> GetUpcomingByMotorcycleAsync(Guid motorcycleId) =>
            Task.FromResult(_upcoming.TryGetValue(motorcycleId, out var u) ? u.AsEnumerable() : Enumerable.Empty<UpcomingMaintenanceDTO>());

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<UpcomingMaintenanceDTO>>> GetUpcomingByMotorcyclesAsync(IEnumerable<Guid> motorcycleIds)
        {
            var result = new Dictionary<Guid, IReadOnlyList<UpcomingMaintenanceDTO>>();
            foreach (var id in motorcycleIds)
                result[id] = _upcoming.TryGetValue(id, out var u) ? u : new List<UpcomingMaintenanceDTO>();
            return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<UpcomingMaintenanceDTO>>>(result);
        }

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<MaintenanceRecordDTO>>> GetMaintenanceRecordsByMotorcyclesAsync(IEnumerable<Guid> motorcycleIds)
        {
            var result = new Dictionary<Guid, IReadOnlyList<MaintenanceRecordDTO>>();
            foreach (var id in motorcycleIds)
                result[id] = _records.TryGetValue(id, out var r) ? r : new List<MaintenanceRecordDTO>();
            return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<MaintenanceRecordDTO>>>(result);
        }
    }

    private sealed class FakeFuelUpService : IFuelUpService
    {
        private readonly Dictionary<Guid, FuelConsumptionDTO> _consumption;
        public FakeFuelUpService(Dictionary<Guid, FuelConsumptionDTO>? consumption = null) =>
            _consumption = consumption ?? new Dictionary<Guid, FuelConsumptionDTO>();

        public Task<IReadOnlyDictionary<Guid, FuelConsumptionDTO>> GetConsumptionByMotorcyclesAsync(IEnumerable<Guid> motorcycleIds)
        {
            var result = new Dictionary<Guid, FuelConsumptionDTO>();
            foreach (var id in motorcycleIds)
                result[id] = _consumption.TryGetValue(id, out var c) ? c : new FuelConsumptionDTO { MotorcycleId = id };
            return Task.FromResult<IReadOnlyDictionary<Guid, FuelConsumptionDTO>>(result);
        }

        public Task<IEnumerable<FuelUpDTO>> GetByMotorcycleAsync(Guid motorcycleId) => Task.FromResult(Enumerable.Empty<FuelUpDTO>());
        public Task<FuelUpDTO?> GetByIdAsync(Guid id) => Task.FromResult<FuelUpDTO?>(null);
        public Task<FuelUpDTO> CreateAsync(SaveFuelUpRequest request) => throw new NotImplementedException();
        public Task<FuelUpDTO> UpdateAsync(Guid id, SaveFuelUpRequest request) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<FuelConsumptionDTO> GetConsumptionAsync(Guid motorcycleId) => throw new NotImplementedException();
    }

    private sealed class FakeUserService : IUserService
    {
        private readonly ProfileDTO _profile;
        public FakeUserService(ProfileDTO? profile = null) => _profile = profile ?? new ProfileDTO();

        public Task<ProfileDTO> GetMeAsync() => Task.FromResult(_profile);
        public Task<ProfileDTO> UpdateProfileAsync(UpdateProfileRequest request) => Task.FromResult(_profile);
        public Task ChangePasswordAsync(ChangePasswordRequest request) => Task.CompletedTask;
        public Task<ProfileDTO> UpdateRemindersAsync(UpdateRemindersRequest request) => Task.FromResult(_profile);
        public Task<ProfileDTO> UpdateBudgetAsync(UpdateBudgetRequest request) => Task.FromResult(_profile);
    }
}
