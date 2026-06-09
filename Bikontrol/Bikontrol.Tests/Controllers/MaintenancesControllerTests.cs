using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;
using Microsoft.AspNetCore.Mvc;

namespace Bikontrol.Tests.Controllers;

public class MaintenancesControllerTests
{
    [Fact]
    public async Task GetDefaults_ShouldReturnOkWithList()
    {
        var defaults = new List<MaintenanceDTO> { new() { Id = Guid.NewGuid(), Name = "Oil", TrackingType = "Km" } };
        var service = new FakeMaintenanceService { Defaults = defaults };
        var controller = new global::MaintenancesController(service);

        var result = await controller.GetDefaults();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(defaults, ok.Value);
    }

    [Fact]
    public async Task GetMys_ShouldReturnOkWithList()
    {
        var userMaintenances = new List<MaintenanceDTO> { new() { Id = Guid.NewGuid(), Name = "Chain", TrackingType = "Km" } };
        var service = new FakeMaintenanceService { UserMaintenances = userMaintenances };
        var controller = new global::MaintenancesController(service);

        var result = await controller.GetMys();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(userMaintenances, ok.Value);
    }

    [Fact]
    public async Task GetMineByMotorcycle_ShouldForwardMotorcycleId()
    {
        var list = new List<MaintenanceDTO> { new() { Id = Guid.NewGuid(), Name = "Chain", TrackingType = "Km" } };
        var service = new FakeMaintenanceService { UserMaintenancesByMotorcycle = list };
        var controller = new global::MaintenancesController(service);
        var motorcycleId = Guid.NewGuid();

        var result = await controller.GetMineByMotorcycle(motorcycleId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(list, ok.Value);
        Assert.Equal(motorcycleId, service.LastMotorcycleIdQuery);
    }

    [Fact]
    public async Task GetById_ShouldReturnOkWithMaintenance()
    {
        var maintenance = new MaintenanceDTO { Id = Guid.NewGuid(), Name = "Oil", TrackingType = "Km" };
        var service = new FakeMaintenanceService { MaintenanceById = maintenance };
        var controller = new global::MaintenancesController(service);

        var result = await controller.GetById(maintenance.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(maintenance, ok.Value);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnOkWithCreatedMaintenance()
    {
        var dto = new SaveMaintenanceDTO { MotorcycleId = Guid.NewGuid(), Name = "Oil", TrackingType = "Km", KmInterval = 5000 };
        var created = new MaintenanceDTO { Id = Guid.NewGuid(), MotorcycleId = dto.MotorcycleId, Name = dto.Name, TrackingType = dto.TrackingType };
        var service = new FakeMaintenanceService { CreatedUserMaintenance = created };
        var controller = new global::MaintenancesController(service);

        var result = await controller.CreateUser(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(created, ok.Value);
        Assert.Same(dto, service.LastCreateUserMaintenanceRequest);
    }

    [Fact]
    public async Task DeleteUser_ShouldCallServiceAndReturnNoContent()
    {
        var service = new FakeMaintenanceService();
        var controller = new global::MaintenancesController(service);
        var id = Guid.NewGuid();

        var result = await controller.DeleteUser(id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(id, service.LastDeletedUserMaintenanceId);
    }

    [Fact]
    public async Task FollowDefault_ShouldForwardPayloadAndReturnOk()
    {
        var request = new FollowDefaultRequest
        {
            MotorcycleId = Guid.NewGuid(),
            DefaultId = Guid.NewGuid(),
            KmInterval = 7000,
            TimeIntervalWeeks = 12,
            TrackingType = "Km"
        };
        var resultMaintenance = new MaintenanceDTO { Id = Guid.NewGuid(), Name = "Brake pads", TrackingType = "Km" };
        var service = new FakeMaintenanceService { FollowDefaultResult = resultMaintenance };
        var controller = new global::MaintenancesController(service);

        var result = await controller.FollowDefault(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(resultMaintenance, ok.Value);
        Assert.Equal(request.MotorcycleId, service.LastFollowMotorcycleId);
        Assert.Equal(request.DefaultId, service.LastFollowDefaultId);
        Assert.Equal(request.KmInterval, service.LastFollowKmInterval);
        Assert.Equal(request.TimeIntervalWeeks, service.LastFollowTimeIntervalWeeks);
        Assert.Equal(request.TrackingType, service.LastFollowTrackingType);
    }

    [Fact]
    public async Task RegisterRecord_ShouldReturnOkWithCreatedRecord()
    {
        var request = new CreateMaintenanceRecordRequest
        {
            MotorcycleId = Guid.NewGuid(),
            UserMaintenanceId = Guid.NewGuid(),
            PerformedAt = DateTime.UtcNow,
            PerformedKm = 1000
        };
        var record = new MaintenanceRecordDTO
        {
            Id = Guid.NewGuid(),
            MotorcycleId = request.MotorcycleId,
            UserMaintenanceId = request.UserMaintenanceId,
            PerformedAt = request.PerformedAt,
            PerformedKm = request.PerformedKm,
            CreatedAt = DateTime.UtcNow,
            MaintenanceName = "Oil"
        };
        var service = new FakeMaintenanceService { CreatedRecord = record };
        var controller = new global::MaintenancesController(service);

        var result = await controller.RegisterRecord(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(record, ok.Value);
        Assert.Same(request, service.LastRecordRequest);
    }

    [Fact]
    public async Task GetMotorcycleRecords_ShouldReturnOkWithList()
    {
        var records = new List<MaintenanceRecordDTO> { new() { Id = Guid.NewGuid(), MaintenanceName = "Oil" } };
        var service = new FakeMaintenanceService { Records = records };
        var controller = new global::MaintenancesController(service);
        var motorcycleId = Guid.NewGuid();

        var result = await controller.GetMotorcycleRecords(motorcycleId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(records, ok.Value);
        Assert.Equal(motorcycleId, service.LastRecordsMotorcycleId);
    }

    [Fact]
    public async Task GetMotorcycleUpcoming_ShouldReturnOkWithList()
    {
        var upcoming = new List<UpcomingMaintenanceDTO> { new() { UserMaintenanceId = Guid.NewGuid(), Name = "Oil" } };
        var service = new FakeMaintenanceService { Upcoming = upcoming };
        var controller = new global::MaintenancesController(service);
        var motorcycleId = Guid.NewGuid();

        var result = await controller.GetMotorcycleUpcoming(motorcycleId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(upcoming, ok.Value);
        Assert.Equal(motorcycleId, service.LastUpcomingMotorcycleId);
    }

    [Fact]
    public async Task Update_ShouldCallServiceAndReturnNoContent()
    {
        var dto = new SaveMaintenanceDTO { MotorcycleId = Guid.NewGuid(), Name = "Oil", TrackingType = "Km", KmInterval = 5000 };
        var service = new FakeMaintenanceService();
        var controller = new global::MaintenancesController(service);
        var id = Guid.NewGuid();

        var result = await controller.Update(id, dto);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(id, service.LastUpdateId);
        Assert.Same(dto, service.LastUpdateRequest);
    }

    private sealed class FakeMaintenanceService : IMaintenanceService
    {
        public IEnumerable<MaintenanceDTO> Defaults { get; set; } = [];
        public IEnumerable<MaintenanceDTO> UserMaintenances { get; set; } = [];
        public IEnumerable<MaintenanceDTO> UserMaintenancesByMotorcycle { get; set; } = [];
        public MaintenanceDTO? MaintenanceById { get; set; }
        public MaintenanceDTO CreatedUserMaintenance { get; set; } = new();
        public MaintenanceDTO FollowDefaultResult { get; set; } = new();
        public MaintenanceRecordDTO CreatedRecord { get; set; } = new();
        public IEnumerable<MaintenanceRecordDTO> Records { get; set; } = [];
        public IEnumerable<UpcomingMaintenanceDTO> Upcoming { get; set; } = [];

        public Guid LastMotorcycleIdQuery { get; private set; }
        public SaveMaintenanceDTO? LastCreateUserMaintenanceRequest { get; private set; }
        public Guid LastDeletedUserMaintenanceId { get; private set; }
        public Guid LastFollowMotorcycleId { get; private set; }
        public Guid LastFollowDefaultId { get; private set; }
        public int? LastFollowKmInterval { get; private set; }
        public int? LastFollowTimeIntervalWeeks { get; private set; }
        public string? LastFollowTrackingType { get; private set; }
        public CreateMaintenanceRecordRequest? LastRecordRequest { get; private set; }
        public Guid LastRecordsMotorcycleId { get; private set; }
        public Guid LastUpcomingMotorcycleId { get; private set; }
        public Guid LastUpdateId { get; private set; }
        public SaveMaintenanceDTO? LastUpdateRequest { get; private set; }

        public Task<IEnumerable<MaintenanceDTO>> GetDefaultsAsync() => Task.FromResult(Defaults);
        public Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceAsync() => Task.FromResult(UserMaintenances);
        public Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceByMotorcycleAsync(Guid motorcycleId)
        {
            LastMotorcycleIdQuery = motorcycleId;
            return Task.FromResult(UserMaintenancesByMotorcycle);
        }

        public Task<MaintenanceDTO?> GetByIdAsync(Guid id) => Task.FromResult(MaintenanceById);

        public Task<MaintenanceDTO> CreateUserMaintenanceAsync(SaveMaintenanceDTO dto)
        {
            LastCreateUserMaintenanceRequest = dto;
            return Task.FromResult(CreatedUserMaintenance);
        }

        public Task DeleteUserMaintenanceAsync(Guid id)
        {
            LastDeletedUserMaintenanceId = id;
            return Task.CompletedTask;
        }

        public Task<MaintenanceDTO> FollowDefaultAsync(Guid motorcycleId, Guid defaultId, int? KmInterval, int? TimeIntervalWeeks, string TrackingType)
        {
            LastFollowMotorcycleId = motorcycleId;
            LastFollowDefaultId = defaultId;
            LastFollowKmInterval = KmInterval;
            LastFollowTimeIntervalWeeks = TimeIntervalWeeks;
            LastFollowTrackingType = TrackingType;
            return Task.FromResult(FollowDefaultResult);
        }

        public Task UpdateAsync(Guid id, SaveMaintenanceDTO dto)
        {
            LastUpdateId = id;
            LastUpdateRequest = dto;
            return Task.CompletedTask;
        }

        public Task<MaintenanceRecordDTO> RegisterMaintenanceRecordAsync(CreateMaintenanceRecordRequest request)
        {
            LastRecordRequest = request;
            return Task.FromResult(CreatedRecord);
        }

        public Task<IEnumerable<MaintenanceRecordDTO>> GetMaintenanceRecordsByMotorcycleAsync(Guid motorcycleId)
        {
            LastRecordsMotorcycleId = motorcycleId;
            return Task.FromResult(Records);
        }

        public Task<IEnumerable<UpcomingMaintenanceDTO>> GetUpcomingByMotorcycleAsync(Guid motorcycleId)
        {
            LastUpcomingMotorcycleId = motorcycleId;
            return Task.FromResult(Upcoming);
        }
    }
}
