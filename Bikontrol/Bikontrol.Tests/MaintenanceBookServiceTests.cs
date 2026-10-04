using System.Text;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Persistence.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Tests;

public class MaintenanceBookServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid MotoId = Guid.NewGuid();

    private static Motorcycle Moto() => new("XTZ 150", "Yamaha", 2021, "La Negra", 150, "ABC123", UserId) { Id = MotoId };

    private static MotorcycleMaintenanceRecord Record(string name, DateTime at, int? km) => new()
    {
        Id = Guid.NewGuid(),
        MotorcycleId = MotoId,
        UserMaintenanceId = Guid.NewGuid(),
        PerformedAt = at,
        PerformedKm = km,
        UserMaintenance = new UserMaintenance { Name = name, Description = "d", TrackingType = "Km", KmInterval = 1000 }
    };

    [Fact]
    public async Task GetBookAsync_ShouldOrderNewestFirstAndExposeSummary()
    {
        var records = new[]
        {
            Record("Aceite", new DateTime(2026, 1, 1), 1000),
            Record("Cadena", new DateTime(2026, 3, 1), 3000),
        };
        var service = CreateService(records, currentKm: 3500);

        var book = await service.GetBookAsync(MotoId);

        Assert.Equal(2, book.TotalMaintenances);
        Assert.Equal("Cadena", book.Entries[0].Name); // newest first
        Assert.Equal(1000, book.FirstKm);
        Assert.Equal(3500, book.CurrentKm);
    }

    [Fact]
    public async Task GetBookAsync_WhenNotOwner_ShouldThrowForbidden()
    {
        var repo = new FakeMotorcycleRepository(Moto()) { OtherOwner = true };
        var service = new MaintenanceBookService(
            new FakeCurrentUserService(UserId),
            repo,
            new FakeUserMaintenanceRepository(),
            new FakeRecordRepository(Array.Empty<MotorcycleMaintenanceRecord>()),
            new FakeKmHistoryService());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetBookAsync(MotoId));
    }

    [Fact]
    public async Task GetCsvAsync_ShouldEscapeCommasAndIncludeBomHeader()
    {
        var records = new[] { Record("Filtro, aire", new DateTime(2026, 2, 2), 2000) };
        var service = CreateService(records, currentKm: 2000);

        var bytes = await service.GetCsvAsync(MotoId);
        var text = Encoding.UTF8.GetString(bytes);

        Assert.StartsWith("Fecha,Odometro", text.Substring(1)); // skip BOM char
        Assert.Contains("\"Filtro, aire\"", text); // escaped
        Assert.Contains("2026-02-02", text);
    }

    [Fact]
    public async Task GetPdfAsync_ShouldProducePdfBytes()
    {
        var service = CreateService(new[] { Record("Aceite", new DateTime(2026, 1, 1), 1000) }, currentKm: 1000);

        var bytes = await service.GetPdfAsync(MotoId);

        Assert.True(bytes.Length > 0);
        // Every PDF starts with the "%PDF" magic bytes.
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private static MaintenanceBookService CreateService(IEnumerable<MotorcycleMaintenanceRecord> records, int currentKm)
    {
        return new MaintenanceBookService(
            new FakeCurrentUserService(UserId),
            new FakeMotorcycleRepository(Moto()),
            new FakeUserMaintenanceRepository(),
            new FakeRecordRepository(records),
            new FakeKmHistoryService { CurrentKm = currentKm });
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId) => UserId = userId;
        public Guid UserId { get; }
        public string Role => UserRole.User;
        public bool IsDemo => false;
    }

    private sealed class FakeMotorcycleRepository : IMotorcycleRepository
    {
        private readonly Motorcycle _moto;
        public FakeMotorcycleRepository(Motorcycle moto) => _moto = moto;
        public bool OtherOwner { get; set; }
        public Task<Motorcycle?> GetByIdAsync(Guid id)
        {
            if (OtherOwner)
                _moto.UserId = Guid.NewGuid();
            return Task.FromResult<Motorcycle?>(_moto);
        }
        public Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId) => Task.FromResult<IEnumerable<Motorcycle>>(new[] { _moto });
        public Task<Motorcycle> AddAsync(Motorcycle motorcycle) => Task.FromResult(motorcycle);
        public Task UpdateAsync(Motorcycle motorcycle) => Task.CompletedTask;
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeUserMaintenanceRepository : IUserMaintenanceRepository
    {
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAsync(Guid userId) => Task.FromResult(Enumerable.Empty<UserMaintenance>());
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAndMotorcycleIdAsync(Guid userId, Guid motorcycleId) => Task.FromResult(Enumerable.Empty<UserMaintenance>());
        public Task<UserMaintenance?> GetByIdAsync(Guid id) => Task.FromResult<UserMaintenance?>(null);
        public Task<IReadOnlyList<UserMaintenance>> GetEnabledForUsersAsync(IEnumerable<Guid> userIds) => Task.FromResult<IReadOnlyList<UserMaintenance>>(Array.Empty<UserMaintenance>());
        public Task<UserMaintenance> AddAsync(UserMaintenance entity) => Task.FromResult(entity);
        public Task UpdateAsync(UserMaintenance entity) => Task.CompletedTask;
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task<UserMaintenance?> GetByBaseIdAsync(Guid userId, Guid motorcycleId, Guid baseId) => Task.FromResult<UserMaintenance?>(null);
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeRecordRepository : IMotorcycleMaintenanceRecordRepository
    {
        private readonly List<MotorcycleMaintenanceRecord> _records;
        public FakeRecordRepository(IEnumerable<MotorcycleMaintenanceRecord> records) => _records = records.ToList();
        public Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity) => Task.FromResult(entity);
        public Task<MotorcycleMaintenanceRecord?> GetByIdAsync(Guid id) => Task.FromResult<MotorcycleMaintenanceRecord?>(null);
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId) => Task.FromResult<IEnumerable<MotorcycleMaintenanceRecord>>(_records);
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) => Task.FromResult<IEnumerable<MotorcycleMaintenanceRecord>>(_records);
        public Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId) => Task.FromResult<MotorcycleMaintenanceRecord?>(null);
        public Task<Dictionary<Guid, MotorcycleMaintenanceRecord>> GetLastByUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds) => Task.FromResult(new Dictionary<Guid, MotorcycleMaintenanceRecord>());
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeKmHistoryService : IKmHistoryService
    {
        public int CurrentKm { get; set; }
        public Task AddKmAsync(Guid motorcycleId, int km) => Task.CompletedTask;
        public Task<int> GetCurrentKmAsync(Guid motorcycleId) => Task.FromResult(CurrentKm);
        public Task<DateTime?> GetInitialRecordedAtAsync(Guid motorcycleId) => Task.FromResult<DateTime?>(new DateTime(2020, 1, 1));
        public Task<IReadOnlyDictionary<Guid, int>> GetCurrentKmByMotorcycleIdsAsync(IEnumerable<Guid> ids) => Task.FromResult<IReadOnlyDictionary<Guid, int>>(ids.ToDictionary(id => id, _ => CurrentKm));
        public Task<IReadOnlyDictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> ids) => Task.FromResult<IReadOnlyDictionary<Guid, DateTime?>>(ids.ToDictionary(id => id, _ => (DateTime?)new DateTime(2020, 1, 1)));
        public Task RollbackLastKmAsync(Guid motorcycleId, int newKm) => Task.CompletedTask;
    }
}
