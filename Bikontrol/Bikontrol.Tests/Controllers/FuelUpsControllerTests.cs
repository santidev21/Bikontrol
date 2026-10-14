using Bikontrol.API.Controllers;
using Bikontrol.Application.DTOs.Fuel;
using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bikontrol.Tests.Controllers;

public class FuelUpsControllerTests
{
    [Fact]
    public async Task GetByMotorcycle_ShouldReturnOkWithList()
    {
        var list = new List<FuelUpDTO> { new() { Id = Guid.NewGuid(), Litres = 4m } };
        var service = new FakeFuelUpService { ByMotorcycle = list };
        var controller = new FuelUpsController(service);
        var motorcycleId = Guid.NewGuid();

        var result = await controller.GetByMotorcycle(motorcycleId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(list, ok.Value);
        Assert.Equal(motorcycleId, service.LastMotorcycleId);
    }

    [Fact]
    public async Task GetById_ShouldReturnOkWithFuelUp()
    {
        var fuelUp = new FuelUpDTO { Id = Guid.NewGuid(), Litres = 4m };
        var service = new FakeFuelUpService { ById = fuelUp };
        var controller = new FuelUpsController(service);

        var result = await controller.GetById(fuelUp.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(fuelUp, ok.Value);
    }

    [Fact]
    public async Task Create_ShouldReturnOkWithCreated()
    {
        var request = new SaveFuelUpRequest { MotorcycleId = Guid.NewGuid(), OdometerKm = 1200, Litres = 4m, TotalCost = 8m };
        var created = new FuelUpDTO { Id = Guid.NewGuid(), OdometerKm = 1200, Litres = 4m };
        var service = new FakeFuelUpService { Created = created };
        var controller = new FuelUpsController(service);

        var result = await controller.Create(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(created, ok.Value);
        Assert.Same(request, service.LastSaveRequest);
    }

    [Fact]
    public async Task Update_ShouldReturnOkWithUpdated()
    {
        var request = new SaveFuelUpRequest { MotorcycleId = Guid.NewGuid(), Litres = 4m };
        var updated = new FuelUpDTO { Id = Guid.NewGuid(), Litres = 4m };
        var service = new FakeFuelUpService { Updated = updated };
        var controller = new FuelUpsController(service);
        var id = Guid.NewGuid();

        var result = await controller.Update(id, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(updated, ok.Value);
        Assert.Equal(id, service.LastUpdatedId);
    }

    [Fact]
    public async Task Delete_ShouldCallServiceAndReturnNoContent()
    {
        var service = new FakeFuelUpService();
        var controller = new FuelUpsController(service);
        var id = Guid.NewGuid();

        var result = await controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(id, service.LastDeletedId);
    }

    [Fact]
    public async Task GetConsumption_ShouldReturnOkWithMetrics()
    {
        var consumption = new FuelConsumptionDTO { MotorcycleId = Guid.NewGuid(), AverageKmPerLitre = 45m };
        var service = new FakeFuelUpService { Consumption = consumption };
        var controller = new FuelUpsController(service);
        var motorcycleId = consumption.MotorcycleId;

        var result = await controller.GetConsumption(motorcycleId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(consumption, ok.Value);
        Assert.Equal(motorcycleId, service.LastMotorcycleId);
    }

    private sealed class FakeFuelUpService : IFuelUpService
    {
        public IEnumerable<FuelUpDTO> ByMotorcycle { get; set; } = [];
        public FuelUpDTO? ById { get; set; }
        public FuelUpDTO Created { get; set; } = new();
        public FuelUpDTO Updated { get; set; } = new();
        public FuelConsumptionDTO Consumption { get; set; } = new();

        public Guid LastMotorcycleId { get; private set; }
        public Guid LastUpdatedId { get; private set; }
        public Guid LastDeletedId { get; private set; }
        public SaveFuelUpRequest? LastSaveRequest { get; private set; }

        public Task<IEnumerable<FuelUpDTO>> GetByMotorcycleAsync(Guid motorcycleId)
        {
            LastMotorcycleId = motorcycleId;
            return Task.FromResult(ByMotorcycle);
        }

        public Task<FuelUpDTO?> GetByIdAsync(Guid id) => Task.FromResult(ById);

        public Task<FuelUpDTO> CreateAsync(SaveFuelUpRequest request)
        {
            LastSaveRequest = request;
            return Task.FromResult(Created);
        }

        public Task<FuelUpDTO> UpdateAsync(Guid id, SaveFuelUpRequest request)
        {
            LastUpdatedId = id;
            LastSaveRequest = request;
            return Task.FromResult(Updated);
        }

        public Task DeleteAsync(Guid id)
        {
            LastDeletedId = id;
            return Task.CompletedTask;
        }

        public Task<FuelConsumptionDTO> GetConsumptionAsync(Guid motorcycleId)
        {
            LastMotorcycleId = motorcycleId;
            return Task.FromResult(Consumption);
        }

        public Task<IReadOnlyDictionary<Guid, FuelConsumptionDTO>> GetConsumptionByMotorcyclesAsync(IEnumerable<Guid> motorcycleIds)
            => throw new NotImplementedException();
    }
}
