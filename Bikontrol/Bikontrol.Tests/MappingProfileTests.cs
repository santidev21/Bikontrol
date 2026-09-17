using AutoMapper;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;

namespace Bikontrol.Tests;

public class MappingProfileTests
{
    private static MapperConfiguration CreateConfig() =>
        new(cfg => cfg.AddProfile<MappingProfile>());

    [Fact]
    public void Configuration_ShouldBeValid()
    {
        CreateConfig().AssertConfigurationIsValid();
    }

    [Fact]
    public void Motorcycle_To_MotorcycleDTO_ShouldMapCoreFields()
    {
        var mapper = CreateConfig().CreateMapper();
        var moto = new Motorcycle("YBR 125", "Yamaha", 2024, "Negra", 125, "ABC12D", Guid.NewGuid())
        {
            Id = Guid.NewGuid(),
            Image = "img.png"
        };

        var dto = mapper.Map<MotorcycleDTO>(moto);

        Assert.Equal(moto.Id, dto.Id);
        Assert.Equal("YBR 125", dto.Name);
        Assert.Equal("Yamaha", dto.Brand);
        Assert.Equal(2024, dto.Year);
        Assert.Equal("Negra", dto.Nickname);
        Assert.Equal(125, dto.Displacement);
        Assert.Equal("ABC12D", dto.Plate);
        Assert.Equal("img.png", dto.Image);
    }
}
