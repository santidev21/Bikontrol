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

/// <summary>
/// Covers the CRUD/validation branches of <see cref="MaintenanceService"/> that
/// the interval-focused suites do not reach (reads, ownership guards, follow,
/// update, delete and record listing). These are the paths the reminder engine
/// and the API lean on, so a regression here is a real bug, not just a drop in a
/// mutation number.
/// </summary>
public class MaintenanceServiceCrudTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    // ---------- Reads ----------

    [Fact]
    public async Task GetDefaultsAsync_ShouldReturnMappedDefaults()
    {
        var userId = Guid.NewGuid();
        var defaults = new RecordingMaintenanceRepository(
            new Maintenance { Id = Guid.NewGuid(), Name = "Aceite", Description = "Cambio" });

        var service = CreateService(userId, defaults: defaults);

        var result = (await service.GetDefaultsAsync()).ToList();

        var dto = Assert.Single(result);
        Assert.Equal("Aceite", dto.Name);
        Assert.Equal("Cambio", dto.Description);
    }

    [Fact]
    public async Task GetUserMaintenanceAsync_ShouldReturnMappedUserMaintenances()
    {
        var userId = Guid.NewGuid();
        var userRepo = new RecordingUserMaintenanceRepository(
            new UserMaintenance { Id = Guid.NewGuid(), UserId = userId, Name = "Cadena", TrackingType = "Km", KmInterval = 1000 });

        var service = CreateService(userId, userMaintenances: userRepo);

        var result = (await service.GetUserMaintenanceAsync()).ToList();

        var dto = Assert.Single(result);
        Assert.Equal("Cadena", dto.Name);
        Assert.Equal(1000, dto.KmInterval);
    }

    [Fact]
    public async Task GetUserMaintenanceByMotorcycleAsync_ShouldEnsureOwnershipAndReturnMine()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var userRepo = new RecordingUserMaintenanceRepository(
            new UserMaintenance { Id = Guid.NewGuid(), UserId = userId, MotorcycleId = motorcycleId, Name = "Filtro", TrackingType = "Km", KmInterval = 2000 });

        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, userId)),
            userMaintenances: userRepo);

        var result = (await service.GetUserMaintenanceByMotorcycleAsync(motorcycleId)).ToList();

        Assert.Equal("Filtro", Assert.Single(result).Name);
    }

    [Fact]
    public async Task GetUserMaintenanceByMotorcycleAsync_NotOwned_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, Guid.NewGuid())));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetUserMaintenanceByMotorcycleAsync(motorcycleId));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ShouldThrow()
    {
        var service = CreateService(Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
        Assert.Equal("Mantenimiento no encontrado.", ex.Message);
    }

    [Fact]
    public async Task GetByIdAsync_NotOwned_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var other = new UserMaintenance { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Ajeno" };
        var service = CreateService(userId, userMaintenances: new RecordingUserMaintenanceRepository(other));

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetByIdAsync(other.Id));
        Assert.Equal("No tienes permisos para ver este mantenimiento.", ex.Message);
    }

    [Fact]
    public async Task GetByIdAsync_Owned_ShouldReturnMapped()
    {
        var userId = Guid.NewGuid();
        var mine = new UserMaintenance { Id = Guid.NewGuid(), UserId = userId, Name = "Mío", TrackingType = "Km", KmInterval = 500 };
        var service = CreateService(userId, userMaintenances: new RecordingUserMaintenanceRepository(mine));

        var dto = await service.GetByIdAsync(mine.Id);

        Assert.Equal("Mío", dto!.Name);
    }

    // ---------- Create ----------

    [Fact]
    public async Task CreateUserMaintenance_ValidKm_ShouldPersistAndReturn()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var userRepo = new RecordingUserMaintenanceRepository();
        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, userId)),
            userMaintenances: userRepo);

        var dto = await service.CreateUserMaintenanceAsync(new SaveMaintenanceDTO
        {
            MotorcycleId = motorcycleId,
            Name = "Aceite",
            Description = "Cambio",
            TrackingType = "Km",
            KmInterval = 5000
        });

        Assert.Equal("Aceite", dto.Name);
        Assert.Equal(5000, dto.KmInterval);
        Assert.Equal(motorcycleId, dto.MotorcycleId);
        var created = Assert.Single(userRepo.Items);
        Assert.Equal(userId, created.UserId);
        Assert.True(created.IsEnabled);
        Assert.Equal(1, userRepo.SaveChangesCount);
    }

    [Fact]
    public async Task CreateUserMaintenance_ValidTime_ShouldPersistAndReturn()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, userId)));

        var dto = await service.CreateUserMaintenanceAsync(new SaveMaintenanceDTO
        {
            MotorcycleId = motorcycleId,
            Name = "Refrigerante",
            Description = "Cambio",
            TrackingType = "Time",
            TimeIntervalWeeks = 8
        });

        Assert.Equal("Time", dto.TrackingType);
        Assert.Equal(8, dto.TimeIntervalWeeks);
    }

    [Fact]
    public async Task CreateUserMaintenance_InvalidTrackingType_ShouldThrow()
    {
        var service = CreateService(Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateUserMaintenanceAsync(new SaveMaintenanceDTO
        {
            MotorcycleId = Guid.NewGuid(),
            Name = "X",
            TrackingType = "Hours"
        }));

        Assert.Equal("El tipo de seguimiento debe ser 'Km' o 'Time'.", ex.Message);
    }

    [Fact]
    public async Task CreateUserMaintenance_NotOwnedMotorcycle_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, Guid.NewGuid())));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.CreateUserMaintenanceAsync(new SaveMaintenanceDTO
        {
            MotorcycleId = motorcycleId,
            Name = "X",
            TrackingType = "Km",
            KmInterval = 1000
        }));
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteUserMaintenance_NotFound_ShouldThrow()
    {
        var service = CreateService(Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteUserMaintenanceAsync(Guid.NewGuid()));
        Assert.Equal("Mantenimiento no encontrado.", ex.Message);
    }

    [Fact]
    public async Task DeleteUserMaintenance_NotOwned_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var other = new UserMaintenance { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Ajeno" };
        var service = CreateService(userId, userMaintenances: new RecordingUserMaintenanceRepository(other));

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.DeleteUserMaintenanceAsync(other.Id));
        Assert.Equal("No tienes permisos para borrar este mantenimiento.", ex.Message);
    }

    [Fact]
    public async Task DeleteUserMaintenance_Owned_ShouldSoftDeleteAndSave()
    {
        var userId = Guid.NewGuid();
        var mine = new UserMaintenance { Id = Guid.NewGuid(), UserId = userId, Name = "Mío" };
        var userRepo = new RecordingUserMaintenanceRepository(mine);
        var service = CreateService(userId, userMaintenances: userRepo);

        await service.DeleteUserMaintenanceAsync(mine.Id);

        Assert.Contains(mine.Id, userRepo.SoftDeleted);
        Assert.Equal(1, userRepo.SaveChangesCount);
    }

    // ---------- Follow default ----------

    [Fact]
    public async Task FollowDefault_DefaultNotFound_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, userId)));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.FollowDefaultAsync(
            motorcycleId, Guid.NewGuid(), 1000, 0, "Km"));
        Assert.Equal("No se encontró el mantenimiento predeterminado.", ex.Message);
    }

    [Fact]
    public async Task FollowDefault_AlreadyFollowing_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var defaultId = Guid.NewGuid();
        var defaults = new RecordingMaintenanceRepository(new Maintenance { Id = defaultId, Name = "Aceite" });
        var existing = new UserMaintenance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MotorcycleId = motorcycleId,
            BaseTypeId = defaultId,
            Name = "Aceite",
            IsEnabled = true
        };
        var service = CreateService(userId, defaults: defaults, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, userId)),
            userMaintenances: new RecordingUserMaintenanceRepository(existing));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.FollowDefaultAsync(
            motorcycleId, defaultId, 1000, 0, "Km"));
        Assert.Equal("Ya estás siguiendo este mantenimiento.", ex.Message);
    }

    [Fact]
    public async Task FollowDefault_DisabledExisting_ShouldReEnableAndUpdateIntervals()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var defaultId = Guid.NewGuid();
        var defaults = new RecordingMaintenanceRepository(new Maintenance { Id = defaultId, Name = "Aceite" });
        var existing = new UserMaintenance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MotorcycleId = motorcycleId,
            BaseTypeId = defaultId,
            Name = "Aceite",
            IsEnabled = false,
            TrackingType = "Km"
        };
        var userRepo = new RecordingUserMaintenanceRepository(existing);
        var service = CreateService(userId, defaults: defaults, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, userId)),
            userMaintenances: userRepo);

        var dto = await service.FollowDefaultAsync(motorcycleId, defaultId, 3000, 0, "Km");

        Assert.True(existing.IsEnabled);
        Assert.Equal(3000, existing.KmInterval);
        Assert.Equal("Km", existing.TrackingType);
        Assert.Contains(existing, userRepo.Updated);
        Assert.Equal(existing.Id, dto.Id);
        Assert.Equal(1, userRepo.SaveChangesCount);
    }

    [Fact]
    public async Task FollowDefault_NotOwnedMotorcycle_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var defaults = new RecordingMaintenanceRepository(new Maintenance { Id = Guid.NewGuid(), Name = "Aceite" });
        var service = CreateService(userId, defaults: defaults,
            motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, Guid.NewGuid())));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.FollowDefaultAsync(
            motorcycleId, Guid.NewGuid(), 1000, 0, "Km"));
    }

    [Fact]
    public async Task FollowDefault_New_ShouldCreateFromDefault()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var defaultId = Guid.NewGuid();
        var defaults = new RecordingMaintenanceRepository(new Maintenance
        {
            Id = defaultId,
            Name = "Aceite",
            Description = "Cambio de aceite"
        });
        var userRepo = new RecordingUserMaintenanceRepository();
        var service = CreateService(userId, defaults: defaults, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, userId)),
            userMaintenances: userRepo);

        var dto = await service.FollowDefaultAsync(motorcycleId, defaultId, 3000, 0, "Km");

        var created = Assert.Single(userRepo.Items);
        Assert.Equal(userId, created.UserId);
        Assert.Equal(motorcycleId, created.MotorcycleId);
        Assert.Equal(defaultId, created.BaseTypeId);
        Assert.Equal("Aceite", created.Name);
        Assert.Equal("Cambio de aceite", created.Description);
        Assert.Equal(3000, created.KmInterval);
        Assert.True(created.IsEnabled);
        Assert.Equal("Aceite", dto.Name);
        Assert.Equal(1, userRepo.SaveChangesCount);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_NotFound_ShouldThrow()
    {
        var service = CreateService(Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(Guid.NewGuid(), new SaveMaintenanceDTO
        {
            MotorcycleId = Guid.NewGuid(),
            Name = "X",
            TrackingType = "Km",
            KmInterval = 1000
        }));
        Assert.Equal("Mantenimiento no encontrado.", ex.Message);
    }

    [Fact]
    public async Task Update_NotOwned_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var other = new UserMaintenance { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Ajeno" };
        var service = CreateService(userId, userMaintenances: new RecordingUserMaintenanceRepository(other));

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.UpdateAsync(other.Id, new SaveMaintenanceDTO
        {
            MotorcycleId = Guid.NewGuid(),
            Name = "X",
            TrackingType = "Km",
            KmInterval = 1000
        }));
        Assert.Equal("No tienes permisos para editar este mantenimiento.", ex.Message);
    }

    [Fact]
    public async Task Update_NotOwnedMotorcycle_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var mine = new UserMaintenance { Id = Guid.NewGuid(), UserId = userId, MotorcycleId = motorcycleId, Name = "Mío", TrackingType = "Km", KmInterval = 1000 };
        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, Guid.NewGuid())),
            userMaintenances: new RecordingUserMaintenanceRepository(mine));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.UpdateAsync(mine.Id, new SaveMaintenanceDTO
        {
            MotorcycleId = motorcycleId,
            Name = "Mío",
            TrackingType = "Km",
            KmInterval = 2000
        }));
    }

    [Fact]
    public async Task Update_Owned_ShouldMapAndSave()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var mine = new UserMaintenance { Id = Guid.NewGuid(), UserId = userId, MotorcycleId = motorcycleId, Name = "Viejo", TrackingType = "Km", KmInterval = 1000 };
        var userRepo = new RecordingUserMaintenanceRepository(mine);
        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, userId)),
            userMaintenances: userRepo);

        await service.UpdateAsync(mine.Id, new SaveMaintenanceDTO
        {
            MotorcycleId = motorcycleId,
            Name = "Nuevo",
            Description = "Editado",
            TrackingType = "Km",
            KmInterval = 2500
        });

        Assert.Equal("Nuevo", mine.Name);
        Assert.Equal(2500, mine.KmInterval);
        Assert.Contains(mine, userRepo.Updated);
        Assert.Equal(1, userRepo.SaveChangesCount);
    }

    [Fact]
    public async Task Update_WhenDemo_ShouldThrowForbidden()
    {
        var service = CreateService(Guid.NewGuid(), isDemo: true);

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.UpdateAsync(Guid.NewGuid(), new SaveMaintenanceDTO
        {
            MotorcycleId = Guid.NewGuid(),
            Name = "X",
            TrackingType = "Km",
            KmInterval = 1000
        }));
        Assert.Equal("El usuario demo solo puede visualizar información.", ex.Message);
    }

    [Fact]
    public async Task Update_InvalidInterval_ShouldThrowValidation()
    {
        var userId = Guid.NewGuid();
        var mine = new UserMaintenance { Id = Guid.NewGuid(), UserId = userId, Name = "Mío", TrackingType = "Km", KmInterval = 1000 };
        var service = CreateService(userId, userMaintenances: new RecordingUserMaintenanceRepository(mine));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(mine.Id, new SaveMaintenanceDTO
        {
            MotorcycleId = Guid.NewGuid(),
            Name = "X",
            TrackingType = "Km",
            KmInterval = 0
        }));
        Assert.Equal("Debes indicar un intervalo de kilometraje mayor a cero.", ex.Message);
    }

    // ---------- Records listing ----------

    [Fact]
    public async Task GetMaintenanceRecordsByMotorcycleAsync_NotOwned_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, Guid.NewGuid())));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetMaintenanceRecordsByMotorcycleAsync(motorcycleId));
    }

    [Fact]
    public async Task GetMaintenanceRecordsByMotorcycleAsync_ShouldReturnMappedRecords()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var record = new MotorcycleMaintenanceRecord
        {
            Id = Guid.NewGuid(),
            MotorcycleId = motorcycleId,
            PerformedAt = DateTime.UtcNow.AddDays(-1),
            PerformedKm = 500
        };
        var records = new RecordingRecordRepository();
        records.Items.Add(record);
        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, userId)),
            records: records);

        var result = (await service.GetMaintenanceRecordsByMotorcycleAsync(motorcycleId)).ToList();

        var dto = Assert.Single(result);
        Assert.Equal(500, dto.PerformedKm);
    }

    [Fact]
    public async Task GetMaintenanceRecordsByMotorcyclesAsync_Empty_ShouldReturnEmpty()
    {
        var service = CreateService(Guid.NewGuid());

        var result = await service.GetMaintenanceRecordsByMotorcyclesAsync(Array.Empty<Guid>());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMaintenanceRecordsByMotorcyclesAsync_ShouldGroupPerMotorcycle()
    {
        var userId = Guid.NewGuid();
        var motoA = Guid.NewGuid();
        var motoB = Guid.NewGuid();
        var records = new RecordingRecordRepository();
        records.Items.Add(new MotorcycleMaintenanceRecord { Id = Guid.NewGuid(), MotorcycleId = motoA, PerformedAt = DateTime.UtcNow });
        records.Items.Add(new MotorcycleMaintenanceRecord { Id = Guid.NewGuid(), MotorcycleId = motoA, PerformedAt = DateTime.UtcNow });
        records.Items.Add(new MotorcycleMaintenanceRecord { Id = Guid.NewGuid(), MotorcycleId = motoB, PerformedAt = DateTime.UtcNow });
        var service = CreateService(userId,
            motorcycles: new RecordingMotorcycleRepository(Motorcycle(motoA, userId), Motorcycle(motoB, userId)),
            records: records);

        var result = await service.GetMaintenanceRecordsByMotorcyclesAsync(new[] { motoA, motoB });

        Assert.Equal(2, result[motoA].Count);
        Assert.Single(result[motoB]);
    }

    [Fact]
    public async Task GetMaintenanceRecordsByMotorcyclesAsync_NotOwned_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var owned = Guid.NewGuid();
        var foreign = Guid.NewGuid();
        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(owned, userId)));

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.GetMaintenanceRecordsByMotorcyclesAsync(new[] { owned, foreign }));
        Assert.Equal("No tienes permisos para ver estas motocicletas.", ex.Message);
    }

    // ---------- Ownership guards (via upcoming reads) ----------

    [Fact]
    public async Task GetUpcomingByMotorcycleAsync_UnknownMotorcycle_ShouldThrowNotFound()
    {
        var service = CreateService(Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetUpcomingByMotorcycleAsync(Guid.NewGuid()));
        Assert.Equal("Motocicleta no encontrada.", ex.Message);
    }

    [Fact]
    public async Task GetUpcomingByMotorcycleAsync_NotOwned_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var service = CreateService(userId, motorcycles: new RecordingMotorcycleRepository(Motorcycle(motorcycleId, Guid.NewGuid())));

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetUpcomingByMotorcycleAsync(motorcycleId));
        Assert.Equal("No tienes permisos para esta motocicleta.", ex.Message);
    }

    // ---------- Helpers ----------

    private static Motorcycle Motorcycle(Guid id, Guid userId)
        => new("Moto", "Brand", 2024, "N", 150, "ABC123", userId) { Id = id };

    private static MaintenanceService CreateService(
        Guid userId,
        bool isDemo = false,
        RecordingMaintenanceRepository? defaults = null,
        RecordingUserMaintenanceRepository? userMaintenances = null,
        RecordingMotorcycleRepository? motorcycles = null,
        RecordingRecordRepository? records = null,
        int currentKm = 0)
    {
        return new MaintenanceService(
            defaults ?? new RecordingMaintenanceRepository(),
            userMaintenances ?? new RecordingUserMaintenanceRepository(),
            motorcycles ?? new RecordingMotorcycleRepository(),
            new FakeKmHistoryService(currentKm),
            records ?? new RecordingRecordRepository(),
            Mapper,
            new FakeCurrentUserService(userId, isDemo ? UserRole.Demo : UserRole.User),
            new FakeTransactionManager());
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId, string role) { UserId = userId; Role = role; }
        public Guid UserId { get; }
        public string Role { get; }
        public bool IsDemo => Role == UserRole.Demo;
    }

    private sealed class FakeTransactionManager : ITransactionManager
    {
        public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default) => action();
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) => action();
    }

    private sealed class FakeKmHistoryService : IKmHistoryService
    {
        private readonly int _currentKm;
        public FakeKmHistoryService(int currentKm) => _currentKm = currentKm;
        public Task AddKmAsync(Guid motorcycleId, int km) => Task.CompletedTask;
        public Task<int> GetCurrentKmAsync(Guid motorcycleId) => Task.FromResult(_currentKm);
        public Task<DateTime?> GetInitialRecordedAtAsync(Guid motorcycleId) => Task.FromResult<DateTime?>(DateTime.UtcNow.AddDays(-10));
        public Task<IReadOnlyDictionary<Guid, int>> GetCurrentKmByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(motorcycleIds.Distinct().ToDictionary(id => id, _ => _currentKm));
        public Task<IReadOnlyDictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) =>
            Task.FromResult<IReadOnlyDictionary<Guid, DateTime?>>(motorcycleIds.Distinct().ToDictionary(id => id, _ => (DateTime?)DateTime.UtcNow.AddDays(-10)));
        public Task RollbackLastKmAsync(Guid motorcycleId, int newKm) => Task.CompletedTask;
    }

    private sealed class RecordingMaintenanceRepository : IMaintenanceRepository
    {
        private readonly List<Maintenance> _items;
        public RecordingMaintenanceRepository(params Maintenance[] items) => _items = items.ToList();
        public Task<IEnumerable<Maintenance>> GetAllAsync() => Task.FromResult<IEnumerable<Maintenance>>(_items);
        public Task<IEnumerable<Maintenance>> GetAllForUserAsync(Guid userId) => Task.FromResult<IEnumerable<Maintenance>>(_items);
        public Task<Maintenance?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
    }

    private sealed class RecordingUserMaintenanceRepository : IUserMaintenanceRepository
    {
        public List<UserMaintenance> Items { get; }
        public List<Guid> SoftDeleted { get; } = new();
        public List<UserMaintenance> Updated { get; } = new();
        public int SaveChangesCount { get; private set; }

        public RecordingUserMaintenanceRepository(params UserMaintenance[] items) => Items = items.ToList();

        public Task<UserMaintenance> AddAsync(UserMaintenance entity) { Items.Add(entity); return Task.FromResult(entity); }
        public Task<UserMaintenance?> GetByBaseIdAsync(Guid userId, Guid motorcycleId, Guid baseId) =>
            Task.FromResult(Items.FirstOrDefault(x => x.UserId == userId && x.MotorcycleId == motorcycleId && x.BaseTypeId == baseId));
        public Task<UserMaintenance?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAsync(Guid userId) => Task.FromResult(Items.Where(x => x.UserId == userId).AsEnumerable());
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAndMotorcycleIdAsync(Guid userId, Guid motorcycleId) =>
            Task.FromResult(Items.Where(x => x.UserId == userId && x.MotorcycleId == motorcycleId && x.IsEnabled).AsEnumerable());
        public Task SoftDeleteAsync(Guid id) { SoftDeleted.Add(id); return Task.CompletedTask; }
        public Task UpdateAsync(UserMaintenance entity) { Updated.Add(entity); return Task.CompletedTask; }
        public Task<IReadOnlyList<UserMaintenance>> GetEnabledForUsersAsync(IEnumerable<Guid> userIds)
        {
            var ids = userIds.ToList();
            return Task.FromResult<IReadOnlyList<UserMaintenance>>(Items.Where(x => ids.Contains(x.UserId) && x.IsEnabled).ToList());
        }
        public Task SaveChangesAsync() { SaveChangesCount++; return Task.CompletedTask; }
    }

    private sealed class RecordingMotorcycleRepository : IMotorcycleRepository
    {
        private readonly List<Motorcycle> _items;
        public RecordingMotorcycleRepository(params Motorcycle[] items) => _items = items.ToList();
        public Task<Motorcycle> AddAsync(Motorcycle motorcycle) { _items.Add(motorcycle); return Task.FromResult(motorcycle); }
        public Task<Motorcycle?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
        public Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId) => Task.FromResult(_items.Where(x => x.UserId == userId).AsEnumerable());
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task UpdateAsync(Motorcycle motorcycle) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class RecordingRecordRepository : IMotorcycleMaintenanceRecordRepository
    {
        public List<MotorcycleMaintenanceRecord> Items { get; } = new();
        public Dictionary<Guid, MotorcycleMaintenanceRecord> LastByMaintenance { get; } = new();

        public Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity) { Items.Add(entity); return Task.FromResult(entity); }
        public Task<MotorcycleMaintenanceRecord?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId) =>
            Task.FromResult(Items.Where(x => x.MotorcycleId == motorcycleId).AsEnumerable());
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds.ToHashSet();
            return Task.FromResult(Items.Where(x => ids.Contains(x.MotorcycleId)).AsEnumerable());
        }
        public Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId) =>
            Task.FromResult(LastByMaintenance.TryGetValue(userMaintenanceId, out var r) ? r : null);
        public Task<Dictionary<Guid, MotorcycleMaintenanceRecord>> GetLastByUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds)
        {
            var result = new Dictionary<Guid, MotorcycleMaintenanceRecord>();
            foreach (var id in userMaintenanceIds.Distinct())
            {
                if (LastByMaintenance.TryGetValue(id, out var record) && record is not null)
                    result[id] = record;
            }
            return Task.FromResult(result);
        }
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
