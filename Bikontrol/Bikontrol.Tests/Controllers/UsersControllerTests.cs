using Bikontrol.API.Controllers;
using Bikontrol.Application.DTOs.Audit;
using Bikontrol.Application.DTOs.Users;
using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bikontrol.Tests.Controllers;

public class UsersControllerTests
{
    [Fact]
    public async Task GetMe_ShouldReturnOkWithProfile()
    {
        var profile = new ProfileDTO { Id = Guid.NewGuid(), Email = "a@b.c", FullName = "Santi", HasPassword = true };
        var service = new FakeUserService { Profile = profile };
        var controller = new UsersController(service, new FakeAuditLogService(), new FakeAccountService());

        var result = await controller.GetMe();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(profile, ok.Value);
    }

    [Fact]
    public async Task UpdateMe_ShouldForwardRequestAndReturnOk()
    {
        var profile = new ProfileDTO { Id = Guid.NewGuid(), FullName = "New" };
        var service = new FakeUserService { Profile = profile };
        var controller = new UsersController(service, new FakeAuditLogService(), new FakeAccountService());
        var request = new UpdateProfileRequest { FullName = "New" };

        var result = await controller.UpdateMe(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(profile, ok.Value);
        Assert.Same(request, service.LastUpdateRequest);
    }

    [Fact]
    public async Task ChangePassword_ShouldForwardRequestAndReturnOk()
    {
        var service = new FakeUserService();
        var controller = new UsersController(service, new FakeAuditLogService(), new FakeAccountService());
        var request = new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "newsecret" };

        var result = await controller.ChangePassword(request);

        Assert.IsType<OkObjectResult>(result);
        Assert.Same(request, service.LastPasswordRequest);
    }

    [Fact]
    public async Task GetMyActivity_ShouldReturnOkWithEntries()
    {
        var entries = new List<AuditLogDTO> { new() { Id = 1, EntityName = "Motorcycle", Action = "Created" } };
        var auditService = new FakeAuditLogService { Activity = entries };
        var controller = new UsersController(new FakeUserService(), auditService, new FakeAccountService());

        var result = await controller.GetMyActivity(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(entries, ok.Value);
        Assert.Equal(10, auditService.LastLimit);
    }

    [Fact]
    public async Task ExportMyData_ShouldReturnOkWithExport()
    {
        var export = new UserDataExportDTO { ExportedAt = DateTime.UtcNow, Profile = new ProfileDTO { Email = "a@b.c" } };
        var accountService = new FakeAccountService { Export = export };
        var controller = new UsersController(new FakeUserService(), new FakeAuditLogService(), accountService);

        var result = await controller.ExportMyData();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(export, ok.Value);
    }

    private sealed class FakeAccountService : IAccountService
    {
        public UserDataExportDTO Export { get; set; } = new();
        public Task<UserDataExportDTO> ExportMyDataAsync() => Task.FromResult(Export);
    }

    private sealed class FakeUserService : IUserService
    {
        public ProfileDTO Profile { get; set; } = new();
        public UpdateProfileRequest? LastUpdateRequest { get; private set; }
        public ChangePasswordRequest? LastPasswordRequest { get; private set; }

        public Task<ProfileDTO> GetMeAsync() => Task.FromResult(Profile);
        public Task<ProfileDTO> UpdateProfileAsync(UpdateProfileRequest request)
        {
            LastUpdateRequest = request;
            return Task.FromResult(Profile);
        }
        public Task ChangePasswordAsync(ChangePasswordRequest request)
        {
            LastPasswordRequest = request;
            return Task.CompletedTask;
        }
        public Task<ProfileDTO> UpdateRemindersAsync(UpdateRemindersRequest request) => Task.FromResult(Profile);
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public IReadOnlyList<AuditLogDTO> Activity { get; set; } = [];
        public int LastLimit { get; private set; }

        public Task<IReadOnlyList<AuditLogDTO>> GetMyActivityAsync(int limit = 50)
        {
            LastLimit = limit;
            return Task.FromResult(Activity);
        }
    }
}
