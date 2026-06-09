using Bikontrol.API.Controllers;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bikontrol.Tests.Controllers;

public class MotorcyclesControllerTests
{
    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WithCreatedMotorcycle()
    {
        var dto = new SaveMotorcycleDTO
        {
            Name = "XTZ",
            Brand = "Yamaha",
            Year = 2024,
            Nickname = "La azul",
            Km = 1000,
            Displacement = 150,
            Plate = "ABC123"
        };
        var created = new MotorcycleDTO { Id = Guid.NewGuid(), Name = dto.Name, Brand = dto.Brand, Year = dto.Year, Nickname = dto.Nickname, Km = dto.Km, Displacement = dto.Displacement, Plate = dto.Plate, Image = "default.png" };
        var service = new FakeMotorcycleService { CreateResult = created };
        var controller = new MotorcyclesController(service);

        var result = await controller.Create(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(MotorcyclesController.GetById), createdResult.ActionName);
        Assert.Equal(created.Id, createdResult.RouteValues!["id"]);
        Assert.Same(created, createdResult.Value);
        Assert.Same(dto, service.LastCreateRequest);
    }

    [Fact]
    public async Task GetById_WhenMotorcycleExists_ShouldReturnOk()
    {
        var motorcycle = new MotorcycleDTO { Id = Guid.NewGuid(), Name = "XTZ", Brand = "Yamaha", Year = 2024, Nickname = "La azul", Km = 1000, Image = "default.png", Displacement = 150, Plate = "ABC123" };
        var service = new FakeMotorcycleService { GetByIdResult = motorcycle };
        var controller = new MotorcyclesController(service);

        var result = await controller.GetById(motorcycle.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(motorcycle, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenMotorcycleDoesNotExist_ShouldReturnNotFound()
    {
        var service = new FakeMotorcycleService { GetByIdResult = null };
        var controller = new MotorcyclesController(service);

        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetMyMotorcycles_ShouldReturnOkWithList()
    {
        var motorcycles = new List<MotorcycleDTO>
        {
            new() { Id = Guid.NewGuid(), Name = "XTZ", Brand = "Yamaha", Year = 2024, Nickname = "La azul", Km = 1000, Image = "default.png", Displacement = 150, Plate = "ABC123" }
        };
        var service = new FakeMotorcycleService { GetMineResult = motorcycles };
        var controller = new MotorcyclesController(service);

        var result = await controller.GetMyMotorcycles();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(motorcycles, ok.Value);
    }

    [Fact]
    public async Task GetCurrentKm_ShouldWrapTheKilometersInAnonymousObject()
    {
        var service = new FakeMotorcycleService { CurrentKm = 1520 };
        var controller = new MotorcyclesController(service);

        var result = await controller.GetCurrentKm(Guid.NewGuid());

        var ok = Assert.IsType<OkObjectResult>(result);
        var kmProperty = ok.Value!.GetType().GetProperty("km");
        Assert.NotNull(kmProperty);
        Assert.Equal(1520, kmProperty!.GetValue(ok.Value));
    }

    [Fact]
    public async Task AddKmHistory_ShouldCallServiceAndReturnNoContent()
    {
        var service = new FakeMotorcycleService();
        var controller = new MotorcyclesController(service);
        var request = new AddKmHistoryRequest { Km = 2000 };

        var result = await controller.AddKmHistory(Guid.NewGuid(), request);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(request.Km, service.LastKmToAdd);
    }

    [Fact]
    public async Task RollbackLastKm_ShouldCallServiceAndReturnNoContent()
    {
        var service = new FakeMotorcycleService();
        var controller = new MotorcyclesController(service);
        var request = new RollbackKmHistoryRequest { NewKm = 1800 };

        var result = await controller.RollbackLastKm(Guid.NewGuid(), request);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(request.NewKm, service.LastRollbackKm);
    }

    [Fact]
    public async Task Update_ShouldCallServiceAndReturnNoContent()
    {
        var service = new FakeMotorcycleService();
        var controller = new MotorcyclesController(service);
        var dto = new SaveMotorcycleDTO
        {
            Name = "XTZ",
            Brand = "Yamaha",
            Year = 2025,
            Nickname = "La azul",
            Km = 1200,
            Displacement = 150,
            Plate = "ABC123"
        };

        var result = await controller.Update(Guid.NewGuid(), dto);

        Assert.IsType<NoContentResult>(result);
        Assert.Same(dto, service.LastUpdateRequest);
    }

    [Fact]
    public async Task SoftDelete_ShouldCallServiceAndReturnNoContent()
    {
        var service = new FakeMotorcycleService();
        var controller = new MotorcyclesController(service);
        var id = Guid.NewGuid();

        var result = await controller.SoftDelete(id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(id, service.LastDeletedId);
    }

    private sealed class FakeMotorcycleService : IMotorcycleService
    {
        public SaveMotorcycleDTO? LastCreateRequest { get; private set; }
        public SaveMotorcycleDTO? LastUpdateRequest { get; private set; }
        public Guid LastDeletedId { get; private set; }
        public int? LastKmToAdd { get; private set; }
        public int? LastRollbackKm { get; private set; }
        public MotorcycleDTO? GetByIdResult { get; set; }
        public IList<MotorcycleDTO> GetMineResult { get; set; } = new List<MotorcycleDTO>();
        public int CurrentKm { get; set; }
        public MotorcycleDTO CreateResult { get; set; } = new();

        public Task<MotorcycleDTO> CreateAsync(SaveMotorcycleDTO dto)
        {
            LastCreateRequest = dto;
            return Task.FromResult(CreateResult);
        }

        public Task<MotorcycleDTO?> GetByIdAsync(Guid id) => Task.FromResult(GetByIdResult);
        public Task<IList<MotorcycleDTO>> GetByCurrentUserAsync() => Task.FromResult(GetMineResult);
        public Task<int> GetCurrentKmAsync(Guid id) => Task.FromResult(CurrentKm);
        public Task AddKmHistoryAsync(Guid id, int km)
        {
            LastKmToAdd = km;
            return Task.CompletedTask;
        }

        public Task RollbackLastKmAsync(Guid id, int newKm)
        {
            LastRollbackKm = newKm;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Guid id, SaveMotorcycleDTO dto)
        {
            LastUpdateRequest = dto;
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(Guid id)
        {
            LastDeletedId = id;
            return Task.CompletedTask;
        }
    }
}
