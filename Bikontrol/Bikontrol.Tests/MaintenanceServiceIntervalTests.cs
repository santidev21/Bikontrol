using AutoMapper;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Tests;

/// <summary>
/// Cubre el cálculo de próximos mantenimientos: dado un intervalo cada X
/// (km o semanas), cuánto % de vida queda y cuántos km/días faltan.
/// LifePercent = % del intervalo RESTANTE (100 recién hecho → 0 vencido).
/// </summary>
public class MaintenanceServiceIntervalTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    // ---------- Km ----------

    [Fact]
    public async Task Km_FreshWithoutRecord_ShouldBe100PercentAndFullRemaining()
    {
        var result = await GetSingleUpcomingAsync(
            KmMaintenance(kmInterval: 5000), currentKm: 0, lastRecord: null);

        Assert.Equal(100, result.LifePercent);
        Assert.Equal(5000, result.RemainingKm);
        Assert.False(result.IsOverdue);
    }

    [Fact]
    public async Task Km_HalfUsed_ShouldBe50Percent()
    {
        var result = await GetSingleUpcomingAsync(
            KmMaintenance(kmInterval: 5000), currentKm: 2500, lastRecord: null);

        Assert.Equal(50, result.LifePercent);
        Assert.Equal(2500, result.RemainingKm);
        Assert.False(result.IsOverdue);
    }

    [Fact]
    public async Task Km_NinetyPercentUsed_ShouldBe10Percent()
    {
        var result = await GetSingleUpcomingAsync(
            KmMaintenance(kmInterval: 5000), currentKm: 4500, lastRecord: null);

        Assert.Equal(10, result.LifePercent);
        Assert.Equal(500, result.RemainingKm);
        Assert.False(result.IsOverdue);
    }

    [Fact]
    public async Task Km_ExactlyDue_ShouldBe0PercentAndOverdue()
    {
        var result = await GetSingleUpcomingAsync(
            KmMaintenance(kmInterval: 5000), currentKm: 5000, lastRecord: null);

        Assert.Equal(0, result.LifePercent);
        Assert.Equal(0, result.RemainingKm);
        Assert.True(result.IsOverdue);
    }

    [Fact]
    public async Task Km_Overdue_ShouldClampTo0AndReportNegativeRemaining()
    {
        var result = await GetSingleUpcomingAsync(
            KmMaintenance(kmInterval: 5000), currentKm: 6200, lastRecord: null);

        Assert.Equal(0, result.LifePercent);
        Assert.Equal(-1200, result.RemainingKm);
        Assert.True(result.IsOverdue);
    }

    [Fact]
    public async Task Km_WithLastRecord_ShouldUseItsKmAsBaseline()
    {
        var last = Record(performedKm: 4000, performedAt: DateTime.UtcNow.AddDays(-30));
        var result = await GetSingleUpcomingAsync(
            KmMaintenance(kmInterval: 5000), currentKm: 6500, lastRecord: last);

        // usados = 6500 - 4000 = 2500 → quedan 2500 → 50 %
        Assert.Equal(50, result.LifePercent);
        Assert.Equal(2500, result.RemainingKm);
        Assert.False(result.IsOverdue);
        Assert.Equal(4000, result.LastPerformedKm);
    }

    [Fact]
    public async Task Km_PartialLife_ShouldRoundDown()
    {
        // quedan 2000 de 3000 → 66.66… → floor = 66
        var result = await GetSingleUpcomingAsync(
            KmMaintenance(kmInterval: 3000), currentKm: 1000, lastRecord: null);

        Assert.Equal(66, result.LifePercent);
        Assert.Equal(2000, result.RemainingKm);
    }

    [Fact]
    public async Task Km_WithoutInterval_ShouldBeOverdue()
    {
        var maintenance = KmMaintenance(kmInterval: null);
        var result = await GetSingleUpcomingAsync(maintenance, currentKm: 1000, lastRecord: null);

        Assert.Equal(0, result.LifePercent);
        Assert.Equal(0, result.RemainingKm);
        Assert.True(result.IsOverdue);
    }

    [Fact]
    public async Task Km_CurrentKmBelowBaselineAfterRollback_ShouldClampTo100()
    {
        // Tras un rollback el odómetro puede quedar por debajo del baseline:
        // usados = 7000 - 8000 = -1000 → quedan 6000 de 5000 → clamp a 100.
        var last = Record(performedKm: 8000, performedAt: DateTime.UtcNow.AddDays(-10));
        var result = await GetSingleUpcomingAsync(
            KmMaintenance(kmInterval: 5000), currentKm: 7000, lastRecord: last);

        Assert.Equal(100, result.LifePercent);
        Assert.False(result.IsOverdue);
    }

    [Fact]
    public async Task Km_MultipleMaintenances_ShouldOrderMostUrgentFirst()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var fresh = KmMaintenance(kmInterval: 10000, name: "ZFresh");
        var urgent = KmMaintenance(kmInterval: 1000, name: "AUrgent");

        var service = CreateService(userId, motorcycleId,
            new List<UserMaintenance> { fresh, urgent },
            currentKm: 900, initialDate: null, lastRecords: new());

        var result = (await service.GetUpcomingByMotorcycleAsync(motorcycleId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("AUrgent", result[0].Name);
        Assert.Equal("ZFresh", result[1].Name);
    }

    [Fact]
    public async Task Km_MultipleMaintenances_SameLife_ShouldOrderByNameAscending()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        // Ambos al 50% de vida: el empate se rompe por nombre ascendente.
        var beta = KmMaintenance(kmInterval: 1000, name: "Beta");
        var alpha = KmMaintenance(kmInterval: 1000, name: "Alpha");

        var service = CreateService(userId, motorcycleId,
            new List<UserMaintenance> { beta, alpha },
            currentKm: 500, initialDate: null, lastRecords: new());

        var result = (await service.GetUpcomingByMotorcycleAsync(motorcycleId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
    }

    // ---------- Tiempo ----------

    [Fact]
    public async Task Time_PerformedToday_ShouldBe100Percent()
    {
        var last = Record(performedKm: null, performedAt: DateTime.UtcNow);
        var result = await GetSingleUpcomingAsync(
            TimeMaintenance(weeks: 4), currentKm: 1000, lastRecord: last);

        Assert.Equal(100, result.LifePercent);
        Assert.Equal(28, result.RemainingDays);
        Assert.False(result.IsOverdue);
    }

    [Fact]
    public async Task Time_HalfElapsed_ShouldBe50Percent()
    {
        var last = Record(performedKm: null, performedAt: DateTime.UtcNow.AddDays(-14));
        var result = await GetSingleUpcomingAsync(
            TimeMaintenance(weeks: 4), currentKm: 1000, lastRecord: last);

        Assert.Equal(50, result.LifePercent);
        Assert.Equal(14, result.RemainingDays);
        Assert.False(result.IsOverdue);
    }

    [Fact]
    public async Task Time_ExactlyDue_ShouldBe0PercentAndOverdue()
    {
        var last = Record(performedKm: null, performedAt: DateTime.UtcNow.AddDays(-28));
        var result = await GetSingleUpcomingAsync(
            TimeMaintenance(weeks: 4), currentKm: 1000, lastRecord: last);

        Assert.Equal(0, result.LifePercent);
        Assert.Equal(0, result.RemainingDays);
        Assert.True(result.IsOverdue);
    }

    [Fact]
    public async Task Time_Overdue_ShouldClampTo0AndReportNegativeRemaining()
    {
        var last = Record(performedKm: null, performedAt: DateTime.UtcNow.AddDays(-35));
        var result = await GetSingleUpcomingAsync(
            TimeMaintenance(weeks: 4), currentKm: 1000, lastRecord: last);

        Assert.Equal(0, result.LifePercent);
        Assert.Equal(-7, result.RemainingDays);
        Assert.True(result.IsOverdue);
    }

    [Fact]
    public async Task Time_WithoutRecord_ShouldUseInitialKmDateAsBaseline()
    {
        // Moto registrada hace 3 días, intervalo 2 semanas (14 días):
        // quedan 11 → floor(11*100/14) = 78.
        var initial = DateTime.UtcNow.Date.AddDays(-3);
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var service = CreateService(userId, motorcycleId,
            new List<UserMaintenance> { TimeMaintenance(weeks: 2) },
            currentKm: 1000, initialDate: initial, lastRecords: new());

        var result = (await service.GetUpcomingByMotorcycleAsync(motorcycleId)).ToList();

        Assert.Single(result);
        Assert.Equal(78, result[0].LifePercent);
        Assert.Equal(11, result[0].RemainingDays);
        Assert.False(result[0].IsOverdue);
        Assert.Equal(initial, result[0].LastPerformedAt?.Date);
    }

    [Fact]
    public async Task Time_WithoutRecordNorInitialDate_ShouldBeOverdueWithNegativeInterval()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var service = CreateService(userId, motorcycleId,
            new List<UserMaintenance> { TimeMaintenance(weeks: 3) },
            currentKm: 0, initialDate: null, lastRecords: new());

        var result = (await service.GetUpcomingByMotorcycleAsync(motorcycleId)).ToList();

        Assert.Single(result);
        Assert.Equal(0, result[0].LifePercent);
        Assert.Equal(-21, result[0].RemainingDays);
        Assert.True(result[0].IsOverdue);
    }

    [Fact]
    public async Task Time_WithoutInterval_ShouldBeOverdue()
    {
        var result = await GetSingleUpcomingAsync(
            TimeMaintenance(weeks: null), currentKm: 1000,
            lastRecord: Record(null, DateTime.UtcNow.AddDays(-5)));

        Assert.Equal(0, result.LifePercent);
        Assert.Equal(0, result.RemainingDays);
        Assert.True(result.IsOverdue);
    }

    [Fact]
    public async Task Time_TwoWeeksElapsedOneOfTwo_ShouldConvertWeeksToDays()
    {
        // 1 semana de 2 transcurrida → quedan 7 días → 50 %
        var last = Record(performedKm: null, performedAt: DateTime.UtcNow.AddDays(-7));
        var result = await GetSingleUpcomingAsync(
            TimeMaintenance(weeks: 2), currentKm: 1000, lastRecord: last);

        Assert.Equal(50, result.LifePercent);
        Assert.Equal(7, result.RemainingDays);
    }

    // ---------- Integridad al registrar ----------

    [Fact]
    public async Task Register_WithFutureDate_ShouldThrowValidation()
    {
        var ctx = RegisterContext(KmMaintenance(kmInterval: 5000));
        var ex = await Assert.ThrowsAsync<ValidationException>(() => ctx.Service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = ctx.MotorcycleId,
                UserMaintenanceId = ctx.MaintenanceId,
                PerformedAt = DateTime.UtcNow.AddDays(1),
                PerformedKm = 100
            }));
        Assert.Equal("No puedes agregar mantenimientos posteriores al dia de hoy", ex.Message);
    }

    [Fact]
    public async Task Register_KmTrackingWithoutKm_ShouldThrowValidation()
    {
        var ctx = RegisterContext(KmMaintenance(kmInterval: 5000));
        var ex = await Assert.ThrowsAsync<ValidationException>(() => ctx.Service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = ctx.MotorcycleId,
                UserMaintenanceId = ctx.MaintenanceId,
                PerformedAt = DateTime.UtcNow,
                PerformedKm = null
            }));
        Assert.Equal("Debes ingresar kilometraje para este mantenimiento.", ex.Message);
    }

    [Fact]
    public async Task Register_KmBelowLastRecord_ShouldThrowValidation()
    {
        var last = Record(performedKm: 5000, performedAt: DateTime.UtcNow.AddDays(-30));
        var ctx = RegisterContext(KmMaintenance(kmInterval: 5000), lastRecord: last, currentKm: 8000);
        var ex = await Assert.ThrowsAsync<ValidationException>(() => ctx.Service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = ctx.MotorcycleId,
                UserMaintenanceId = ctx.MaintenanceId,
                PerformedAt = DateTime.UtcNow,
                PerformedKm = 4000
            }));
        Assert.Equal("El kilometraje no puede ser menor al del registro anterior de este mantenimiento.", ex.Message);
    }

    [Fact]
    public async Task Register_KmAboveNextRecord_ShouldThrowValidation()
    {
        // Registro con fecha anterior pero km mayor que un registro posterior:
        // rompería la monotonía, así que debe rechazarse.
        var next = Record(performedKm: 4000, performedAt: DateTime.UtcNow.AddDays(-10));
        var ctx = RegisterContext(KmMaintenance(kmInterval: 5000), lastRecord: next, currentKm: 8000);
        var ex = await Assert.ThrowsAsync<ValidationException>(() => ctx.Service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = ctx.MotorcycleId,
                UserMaintenanceId = ctx.MaintenanceId,
                PerformedAt = DateTime.UtcNow.AddDays(-20),
                PerformedKm = 6000
            }));
        Assert.Equal("El kilometraje no puede ser mayor al del registro posterior de este mantenimiento.", ex.Message);
    }

    [Fact]
    public async Task Register_KmEqualToLastRecord_ShouldBeAllowed()
    {
        // Igual al último km registrado no es "anterior": debe permitirse.
        var last = Record(performedKm: 5000, performedAt: DateTime.UtcNow.AddDays(-30));
        var ctx = RegisterContext(KmMaintenance(kmInterval: 5000), lastRecord: last, currentKm: 8000);

        var result = await ctx.Service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = ctx.MotorcycleId,
                UserMaintenanceId = ctx.MaintenanceId,
                PerformedAt = DateTime.UtcNow,
                PerformedKm = 5000
            });

        Assert.Equal(5000, result.PerformedKm);
    }

    [Fact]
    public async Task Register_HistoricalKmBelowOdometer_ShouldSaveWithoutMovingOdometer()
    {
        // Registro tardío de un mantenimiento pasado: no debe mover el
        // odómetro ni fallar (antes dejaba escritura parcial).
        var last = Record(performedKm: 5000, performedAt: DateTime.UtcNow.AddDays(-30));
        var kmService = new FakeKmHistoryService(currentKm: 8000, initialRecordedAt: DateTime.UtcNow.AddDays(-60));
        var ctx = RegisterContext(KmMaintenance(kmInterval: 5000), lastRecord: last, kmService: kmService);

        var result = await ctx.Service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = ctx.MotorcycleId,
                UserMaintenanceId = ctx.MaintenanceId,
                PerformedAt = DateTime.UtcNow,
                PerformedKm = 6000,
                Cost = 123.45m
            });

        Assert.Equal("Aceite", result.MaintenanceName);
        // El registro persistido debe conservar todos sus campos (no solo el nombre).
        Assert.Equal(ctx.MotorcycleId, result.MotorcycleId);
        Assert.Equal(ctx.MaintenanceId, result.UserMaintenanceId);
        Assert.Equal(6000, result.PerformedKm);
        Assert.Equal(123.45m, result.Cost);
        Assert.Empty(kmService.AddedKms);
    }

    [Fact]
    public async Task Register_KmEqualToOdometer_ShouldNotAdvanceOdometer()
    {
        // Igual al odómetro actual no avanza (solo lo hace si es estrictamente mayor).
        var kmService = new FakeKmHistoryService(currentKm: 8000, initialRecordedAt: DateTime.UtcNow.AddDays(-60));
        var ctx = RegisterContext(KmMaintenance(kmInterval: 5000), lastRecord: null, kmService: kmService);

        await ctx.Service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = ctx.MotorcycleId,
                UserMaintenanceId = ctx.MaintenanceId,
                PerformedAt = DateTime.UtcNow,
                PerformedKm = 8000
            });

        Assert.Empty(kmService.AddedKms);
    }

    [Fact]
    public async Task Register_NotOwnedMotorcycle_ShouldThrowForbidden()
    {
        // El mantenimiento es del usuario, pero la moto pertenece a otro: la
        // verificación de propiedad del endpoint no debe saltarse.
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var maintenance = KmMaintenance(kmInterval: 5000);
        maintenance.UserId = userId;
        maintenance.MotorcycleId = motorcycleId;

        var foreignMotorcycle = new Motorcycle("Moto", "Brand", 2024, "N", 150, "ABC123", Guid.NewGuid())
        {
            Id = motorcycleId
        };

        var service = new MaintenanceService(
            new FakeMaintenanceRepository(),
            new FakeUserMaintenanceRepository(new List<UserMaintenance> { maintenance }),
            new FakeMotorcycleRepository(foreignMotorcycle),
            new FakeKmHistoryService(currentKm: 1000),
            new FakeRecordRepository(new()),
            Mapper,
            new FakeCurrentUserService(userId),
            new FakeTransactionManager());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = motorcycleId,
                UserMaintenanceId = maintenance.Id,
                PerformedAt = DateTime.UtcNow,
                PerformedKm = 1500
            }));
    }

    [Fact]
    public async Task Register_KmAboveOdometer_ShouldAdvanceOdometer()
    {
        var kmService = new FakeKmHistoryService(currentKm: 8000, initialRecordedAt: DateTime.UtcNow.AddDays(-60));
        var ctx = RegisterContext(KmMaintenance(kmInterval: 5000), lastRecord: null, kmService: kmService);

        await ctx.Service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = ctx.MotorcycleId,
                UserMaintenanceId = ctx.MaintenanceId,
                PerformedAt = DateTime.UtcNow,
                PerformedKm = 8500
            });

        Assert.Equal(new List<int> { 8500 }, kmService.AddedKms);
    }

    [Fact]
    public async Task Create_KmTrackingWithoutPositiveInterval_ShouldThrowValidation()
    {
        var ctx = RegisterContext(KmMaintenance(kmInterval: 5000));
        var ex = await Assert.ThrowsAsync<ValidationException>(() => ctx.Service.CreateUserMaintenanceAsync(
            new SaveMaintenanceDTO
            {
                MotorcycleId = ctx.MotorcycleId,
                Name = "X",
                TrackingType = "Km",
                KmInterval = 0
            }));
        Assert.Equal("Debes indicar un intervalo de kilometraje mayor a cero.", ex.Message);
    }

    [Fact]
    public async Task Create_TimeTrackingWithoutPositiveInterval_ShouldThrowValidation()
    {
        var ctx = RegisterContext(TimeMaintenance(weeks: 4));
        var ex = await Assert.ThrowsAsync<ValidationException>(() => ctx.Service.CreateUserMaintenanceAsync(
            new SaveMaintenanceDTO
            {
                MotorcycleId = ctx.MotorcycleId,
                Name = "X",
                TrackingType = "Time",
                TimeIntervalWeeks = null
            }));
        Assert.Equal("Debes indicar un intervalo de tiempo mayor a cero.", ex.Message);
    }

    [Fact]
    public async Task FollowDefault_TimeTrackingWithoutPositiveInterval_ShouldThrowValidation()
    {
        var ctx = RegisterContext(TimeMaintenance(weeks: 4));
        await Assert.ThrowsAsync<ValidationException>(() => ctx.Service.FollowDefaultAsync(
            ctx.MotorcycleId, Guid.NewGuid(), 1000, 0, "Time"));
    }

    [Fact]
    public async Task Register_MaintenanceNotFound_ShouldThrowNotFound()
    {
        var ctx = RegisterContext(KmMaintenance(kmInterval: 5000));
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => ctx.Service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = ctx.MotorcycleId,
                UserMaintenanceId = Guid.NewGuid(),
                PerformedAt = DateTime.UtcNow,
                PerformedKm = 100
            }));
        Assert.Equal("Mantenimiento no encontrado.", ex.Message);
    }

    [Fact]
    public async Task Register_MaintenanceNotOwned_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        var foreignMaintenance = KmMaintenance(kmInterval: 5000);
        foreignMaintenance.UserId = Guid.NewGuid();
        foreignMaintenance.MotorcycleId = motorcycleId;

        var service = new MaintenanceService(
            new FakeMaintenanceRepository(),
            new FakeUserMaintenanceRepository(new List<UserMaintenance> { foreignMaintenance }),
            new FakeMotorcycleRepository(new Motorcycle("Moto", "Brand", 2024, "N", 150, "ABC123", userId) { Id = motorcycleId }),
            new FakeKmHistoryService(currentKm: 1000),
            new FakeRecordRepository(new()),
            Mapper,
            new FakeCurrentUserService(userId),
            new FakeTransactionManager());

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = motorcycleId,
                UserMaintenanceId = foreignMaintenance.Id,
                PerformedAt = DateTime.UtcNow,
                PerformedKm = 1500
            }));
        Assert.Equal("No tienes permisos para registrar este mantenimiento.", ex.Message);
    }

    [Fact]
    public async Task Register_MotorcycleMismatch_ShouldThrowValidation()
    {
        var ctx = RegisterContext(KmMaintenance(kmInterval: 5000));
        var ex = await Assert.ThrowsAsync<ValidationException>(() => ctx.Service.RegisterMaintenanceRecordAsync(
            new CreateMaintenanceRecordRequest
            {
                MotorcycleId = Guid.NewGuid(),
                UserMaintenanceId = ctx.MaintenanceId,
                PerformedAt = DateTime.UtcNow,
                PerformedKm = 100
            }));
        Assert.Equal("El mantenimiento no pertenece a la motocicleta seleccionada.", ex.Message);
    }

    // ---------- Helpers ----------

    private static async Task<UpcomingMaintenanceDTO> GetSingleUpcomingAsync(
        UserMaintenance maintenance, int currentKm, MotorcycleMaintenanceRecord? lastRecord)
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        maintenance.UserId = userId;
        maintenance.MotorcycleId = motorcycleId;

        var lastRecords = new Dictionary<Guid, MotorcycleMaintenanceRecord?>();
        if (lastRecord is not null)
        {
            lastRecord.UserMaintenanceId = maintenance.Id;
            lastRecord.MotorcycleId = motorcycleId;
            lastRecords[maintenance.Id] = lastRecord;
        }

        var service = CreateService(userId, motorcycleId,
            new List<UserMaintenance> { maintenance },
            currentKm, initialDate: null, lastRecords: lastRecords);

        var result = (await service.GetUpcomingByMotorcycleAsync(motorcycleId)).ToList();
        Assert.Single(result);
        return result[0];
    }

    private sealed record RegisterFixture(
        MaintenanceService Service, Guid MotorcycleId, Guid MaintenanceId);

    private static RegisterFixture RegisterContext(
        UserMaintenance maintenance,
        MotorcycleMaintenanceRecord? lastRecord = null,
        int currentKm = 8000,
        FakeKmHistoryService? kmService = null)
    {
        var userId = Guid.NewGuid();
        var motorcycleId = Guid.NewGuid();
        maintenance.UserId = userId;
        maintenance.MotorcycleId = motorcycleId;

        var lastRecords = new Dictionary<Guid, MotorcycleMaintenanceRecord?>();
        if (lastRecord is not null)
        {
            lastRecord.UserMaintenanceId = maintenance.Id;
            lastRecord.MotorcycleId = motorcycleId;
            lastRecords[maintenance.Id] = lastRecord;
        }

        var service = CreateService(userId, motorcycleId,
            new List<UserMaintenance> { maintenance },
            currentKm, initialDate: DateTime.UtcNow.AddDays(-60),
            lastRecords: lastRecords, kmService: kmService);

        return new RegisterFixture(service, motorcycleId, maintenance.Id);
    }

    private static MaintenanceService CreateService(
        Guid userId,
        Guid motorcycleId,
        List<UserMaintenance> maintenances,
        int currentKm,
        DateTime? initialDate,
        Dictionary<Guid, MotorcycleMaintenanceRecord?> lastRecords,
        FakeKmHistoryService? kmService = null)
    {
        var motorcycle = new Motorcycle("Moto", "Brand", 2024, "N", 150, "ABC123", userId)
        {
            Id = motorcycleId
        };

        foreach (var m in maintenances)
        {
            m.UserId = userId;
            m.MotorcycleId = motorcycleId;
        }

        return new MaintenanceService(
            new FakeMaintenanceRepository(),
            new FakeUserMaintenanceRepository(maintenances),
            new FakeMotorcycleRepository(motorcycle),
            kmService ?? new FakeKmHistoryService(currentKm, initialDate),
            new FakeRecordRepository(lastRecords),
            Mapper,
            new FakeCurrentUserService(userId),
            new FakeTransactionManager());
    }

    private static UserMaintenance KmMaintenance(int? kmInterval, string name = "Aceite")
    {
        return new UserMaintenance
        {
            Id = Guid.NewGuid(),
            Name = name,
            TrackingType = "Km",
            KmInterval = kmInterval,
            IsEnabled = true
        };
    }

    private static UserMaintenance TimeMaintenance(int? weeks, string name = "Aceite")
    {
        return new UserMaintenance
        {
            Id = Guid.NewGuid(),
            Name = name,
            TrackingType = "Time",
            TimeIntervalWeeks = weeks,
            IsEnabled = true
        };
    }

    private static MotorcycleMaintenanceRecord Record(int? performedKm, DateTime performedAt)
    {
        return new MotorcycleMaintenanceRecord
        {
            Id = Guid.NewGuid(),
            PerformedKm = performedKm,
            PerformedAt = performedAt,
            CreatedAt = performedAt
        };
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
        public Task<IReadOnlyList<UserMaintenance>> GetEnabledForUsersAsync(IEnumerable<Guid> userIds)
        {
            var ids = userIds.ToList();
            return Task.FromResult<IReadOnlyList<UserMaintenance>>(_items.Where(x => ids.Contains(x.UserId) && x.IsEnabled).ToList());
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
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
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeKmHistoryService : IKmHistoryService
    {
        private readonly int _currentKm;
        private readonly DateTime? _initialRecordedAt;
        public List<int> AddedKms { get; } = new();
        public FakeKmHistoryService(int currentKm, DateTime? initialRecordedAt = null)
        {
            _currentKm = currentKm;
            _initialRecordedAt = initialRecordedAt;
        }
        public Task AddKmAsync(Guid motorcycleId, int km)
        {
            AddedKms.Add(km);
            return Task.CompletedTask;
        }
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
        private readonly Dictionary<Guid, MotorcycleMaintenanceRecord?> _lastByMaintenance;
        public FakeRecordRepository(Dictionary<Guid, MotorcycleMaintenanceRecord?> lastByMaintenance)
            => _lastByMaintenance = lastByMaintenance;
        public Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity) => Task.FromResult(entity);
        public Task<MotorcycleMaintenanceRecord?> GetByIdAsync(Guid id) => Task.FromResult<MotorcycleMaintenanceRecord?>(null);
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId) =>
            Task.FromResult(_lastByMaintenance.Values
                .Where(r => r is not null)
                .Cast<MotorcycleMaintenanceRecord>()
                .AsEnumerable());
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) =>
            Task.FromResult(Enumerable.Empty<MotorcycleMaintenanceRecord>());
        public Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId) =>
            Task.FromResult(_lastByMaintenance.TryGetValue(userMaintenanceId, out var r) ? r : null);
        public Task<Dictionary<Guid, MotorcycleMaintenanceRecord>> GetLastByUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds)
        {
            var result = new Dictionary<Guid, MotorcycleMaintenanceRecord>();
            foreach (var id in userMaintenanceIds.Distinct())
            {
                if (_lastByMaintenance.TryGetValue(id, out var record) && record is not null)
                    result[id] = record;
            }
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
