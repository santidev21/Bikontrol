using Bikontrol.API.Controllers;
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
        var controller = new UsersController(service);

        var result = await controller.GetMe();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(profile, ok.Value);
    }

    [Fact]
    public async Task UpdateMe_ShouldForwardRequestAndReturnOk()
    {
        var profile = new ProfileDTO { Id = Guid.NewGuid(), FullName = "New" };
        var service = new FakeUserService { Profile = profile };
        var controller = new UsersController(service);
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
        var controller = new UsersController(service);
        var request = new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "newsecret" };

        var result = await controller.ChangePassword(request);

        Assert.IsType<OkObjectResult>(result);
        Assert.Same(request, service.LastPasswordRequest);
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
    }
}
