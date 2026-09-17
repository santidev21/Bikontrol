using System.ComponentModel.DataAnnotations;
using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.DTOs.Users;

namespace Bikontrol.Tests;

public class DtoValidationTests
{
    private static IReadOnlyList<ValidationResult> Validate(object dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void SaveMaintenanceDTO_ValidKmMaintenance_Passes()
    {
        var errors = Validate(new SaveMaintenanceDTO
        {
            MotorcycleId = Guid.NewGuid(),
            Name = "Cambio de aceite",
            TrackingType = "Km",
            KmInterval = 1500
        });

        Assert.Empty(errors);
    }

    [Fact]
    public void SaveMaintenanceDTO_UnknownTrackingType_Fails()
    {
        var errors = Validate(new SaveMaintenanceDTO
        {
            Name = "X",
            TrackingType = "Foo",
            KmInterval = 100
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(SaveMaintenanceDTO.TrackingType)));
    }

    [Fact]
    public void SaveMaintenanceDTO_NameTooLong_Fails()
    {
        var errors = Validate(new SaveMaintenanceDTO
        {
            Name = new string('a', 151),
            TrackingType = "Km",
            KmInterval = 100
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(SaveMaintenanceDTO.Name)));
    }

    [Fact]
    public void SaveMaintenanceDTO_EmptyName_Fails()
    {
        var errors = Validate(new SaveMaintenanceDTO
        {
            Name = "",
            TrackingType = "Km",
            KmInterval = 100
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(SaveMaintenanceDTO.Name)));
    }

    [Fact]
    public void SaveMaintenanceDTO_NonPositiveKmInterval_Fails()
    {
        var errors = Validate(new SaveMaintenanceDTO
        {
            Name = "X",
            TrackingType = "Km",
            KmInterval = 0
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(SaveMaintenanceDTO.KmInterval)));
    }

    [Fact]
    public void FollowDefaultRequest_UnknownTrackingType_Fails()
    {
        var errors = Validate(new FollowDefaultRequest
        {
            TrackingType = "Foo",
            KmInterval = 100
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(FollowDefaultRequest.TrackingType)));
    }

    [Fact]
    public void AddKmHistoryRequest_NegativeKm_Fails()
    {
        var errors = Validate(new AddKmHistoryRequest { Km = -5 });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(AddKmHistoryRequest.Km)));
    }

    [Fact]
    public void RollbackKmHistoryRequest_NegativeKm_Fails()
    {
        var errors = Validate(new RollbackKmHistoryRequest { NewKm = -1 });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RollbackKmHistoryRequest.NewKm)));
    }

    [Fact]
    public void RegisterRequest_PasswordTooLong_Fails()
    {
        var errors = Validate(new RegisterRequest
        {
            Email = "a@bikontrol.com",
            FullName = "Name",
            Password = new string('a', 129)
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RegisterRequest.Password)));
    }

    [Fact]
    public void ChangePasswordRequest_NewPasswordTooLong_Fails()
    {
        var errors = Validate(new ChangePasswordRequest
        {
            CurrentPassword = "old",
            NewPassword = new string('a', 129)
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ChangePasswordRequest.NewPassword)));
    }

    [Fact]
    public void ResetPasswordRequest_PasswordTooLong_Fails()
    {
        var errors = Validate(new ResetPasswordRequest
        {
            Email = "a@bikontrol.com",
            Token = "t",
            NewPassword = new string('a', 129)
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ResetPasswordRequest.NewPassword)));
    }

    [Fact]
    public void CreateMaintenanceRecordRequest_NegativePerformedKm_Fails()
    {
        var errors = Validate(new CreateMaintenanceRecordRequest
        {
            MotorcycleId = Guid.NewGuid(),
            UserMaintenanceId = Guid.NewGuid(),
            PerformedAt = DateTime.UtcNow,
            PerformedKm = -10
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateMaintenanceRecordRequest.PerformedKm)));
    }
}
