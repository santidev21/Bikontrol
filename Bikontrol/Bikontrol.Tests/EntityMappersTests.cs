using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;

namespace Bikontrol.Tests;

/// <summary>
/// The mappings are source-generated with RequiredMappingStrategy.Target, so
/// a DTO property without a rule is a compile-time diagnostic; these tests
/// pin the values that actually reach each response.
/// </summary>
public class EntityMappersTests
{
    [Fact]
    public void Motorcycle_To_MotorcycleDTO_ShouldMapCoreFields()
    {
        var moto = new Motorcycle("YBR 125", "Yamaha", 2024, "Negra", 125, "ABC12D", Guid.NewGuid())
        {
            Id = Guid.NewGuid(),
            Image = "img.png"
        };

        var dto = moto.ToMotorcycleDTO();

        Assert.Equal(moto.Id, dto.Id);
        Assert.Equal("YBR 125", dto.Name);
        Assert.Equal("Yamaha", dto.Brand);
        Assert.Equal(2024, dto.Year);
        Assert.Equal("Negra", dto.Nickname);
        Assert.Equal(125, dto.Displacement);
        Assert.Equal("ABC12D", dto.Plate);
        Assert.Equal("img.png", dto.Image);
    }

    [Fact]
    public void Motorcycle_To_MotorcycleDTO_ShouldNotCarryKm()
    {
        // Km is resolved by the service from the km history, never from the
        // entity aggregate.
        var moto = new Motorcycle("YBR 125", "Yamaha", 2024, "Negra", 125, "ABC12D", Guid.NewGuid());

        var dto = moto.ToMotorcycleDTO();

        Assert.Equal(0, dto.Km);
    }

    [Fact]
    public void User_To_LoginResponse_ShouldNotCarryTokens()
    {
        var user = new User("test@bikontrol.com", "Test User", "hashed:pwd");

        var dto = user.ToLoginResponse();

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal("Test User", dto.FullName);
        Assert.Equal(UserRole.User, dto.Role);
        Assert.Equal(string.Empty, dto.Token);
        Assert.Equal(string.Empty, dto.RefreshToken);
        Assert.Equal(0, dto.ExpiresIn);
    }

    [Fact]
    public void User_To_RegisterResponse_ShouldNotCarryTokensOrFlag()
    {
        var user = new User("test@bikontrol.com", "Test User", "hashed:pwd");

        var dto = user.ToRegisterResponse();

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(string.Empty, dto.Token);
        Assert.False(dto.EmailConfirmationRequired);
    }

    [Fact]
    public void User_To_ProfileDTO_ShouldMapPreferencesAndBudgets()
    {
        var user = new User("test@bikontrol.com", "Test User", "hashed:pwd");
        user.SetMaintenanceBudget(10_000m, 120_000m);
        user.SetRemindersEnabled(false);

        var dto = user.ToProfileDTO();

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(10_000m, dto.MonthlyMaintenanceBudget);
        Assert.Equal(120_000m, dto.AnnualMaintenanceBudget);
        Assert.False(dto.RemindersEnabled);
    }

    [Fact]
    public void FuelUp_To_FuelUpDTO_ShouldCarryTheDerivedPrice()
    {
        var fuelUp = new FuelUp
        {
            Id = Guid.NewGuid(),
            MotorcycleId = Guid.NewGuid(),
            FueledAt = DateTime.UtcNow,
            OdometerKm = 1000,
            Litres = 4m,
            TotalCost = 8m,
            IsFullTank = true
        };

        var dto = fuelUp.ToFuelUpDTO();

        Assert.Equal(fuelUp.Id, dto.Id);
        Assert.Equal(4m, dto.Litres);
        Assert.Equal(8m, dto.TotalCost);
        Assert.Equal(2m, dto.PricePerLitre);
    }

    [Fact]
    public void Attachment_To_AttachmentDTO_ShouldResolveTheRecordId()
    {
        var recordId = Guid.NewGuid();
        var attachment = new MaintenanceRecordAttachment
        {
            MotorcycleMaintenanceRecordId = recordId,
            DataUrl = "data:image/png;base64,AAAA",
            FileName = "factura.png"
        };

        var dto = attachment.ToAttachmentDTO();

        Assert.Equal(recordId, dto.RecordId);
        Assert.Equal(attachment.DataUrl, dto.DataUrl);
    }

    [Fact]
    public void Record_To_RecordDTO_ShouldResolveTheMaintenanceName()
    {
        var maintenanceId = Guid.NewGuid();
        var record = new MotorcycleMaintenanceRecord
        {
            Id = Guid.NewGuid(),
            MotorcycleId = Guid.NewGuid(),
            UserMaintenanceId = maintenanceId,
            PerformedAt = DateTime.UtcNow,
            PerformedKm = 1000,
            UserMaintenance = new UserMaintenance { Name = "Aceite" }
        };

        var dto = record.ToMaintenanceRecordDTO();

        Assert.Equal("Aceite", dto.MaintenanceName);
        Assert.Equal(maintenanceId, dto.UserMaintenanceId);
    }
}
