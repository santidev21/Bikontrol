using Bikontrol.API.Controllers;
using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bikontrol.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_ShouldReturnOkWithServiceResponse()
    {
        var response = new RegisterResponse
        {
            Id = Guid.NewGuid(),
            Email = "user@bikontrol.com",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow,
            Token = "token-123"
        };
        var service = new FakeAuthService { RegisterResult = response };
        var controller = new AuthController(service);
        var request = new RegisterRequest
        {
            Email = response.Email,
            FullName = response.FullName,
            Password = "Secret123!"
        };

        var result = await controller.Register(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        Assert.Same(request, service.LastRegisterRequest);
    }

    [Fact]
    public async Task Login_ShouldReturnOkWithServiceResponse()
    {
        var response = new LoginResponse
        {
            Id = Guid.NewGuid(),
            Email = "user@bikontrol.com",
            FullName = "Test User",
            Token = "token-456",
            RefreshToken = "refresh-456",
            ExpiresIn = 900
        };
        var service = new FakeAuthService { LoginResult = response };
        var controller = new AuthController(service);
        var request = new LoginRequest
        {
            Email = response.Email,
            Password = "Secret123!"
        };

        var result = await controller.Login(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        Assert.Same(request, service.LastLoginRequest);
    }

    [Fact]
    public async Task DemoLogin_ShouldReturnOkWithServiceResponse()
    {
        var response = new LoginResponse { Id = Guid.NewGuid(), Email = "demo@bikontrol.com", FullName = "Usuario Demo", Token = "demo-token", Role = "Demo", RefreshToken = "r", ExpiresIn = 900 };
        var service = new FakeAuthService { LoginResult = response };
        var controller = new AuthController(service);
        var result = await controller.DemoLogin();
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task GoogleLogin_ShouldReturnOk()
    {
        var response = new LoginResponse { Id = Guid.NewGuid(), Email = "user@bikontrol.com", FullName = "Test", Token = "t", RefreshToken = "r", ExpiresIn = 900 };
        var service = new FakeAuthService { LoginResult = response };
        var controller = new AuthController(service);
        var result = await controller.GoogleLogin(new GoogleLoginRequest { IdToken = "tok" });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Refresh_ShouldReturnOk()
    {
        var response = new LoginResponse { Id = Guid.NewGuid(), Email = "user@bikontrol.com", FullName = "Test", Token = "t", RefreshToken = "r", ExpiresIn = 900 };
        var service = new FakeAuthService { LoginResult = response };
        var controller = new AuthController(service);
        var result = await controller.Refresh(new RefreshTokenRequest { RefreshToken = "tok" });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnOk()
    {
        var service = new FakeAuthService();
        var controller = new AuthController(service);
        var result = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "a@b.com" });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnOk()
    {
        var service = new FakeAuthService();
        var controller = new AuthController(service);
        var result = await controller.ResetPassword(new ResetPasswordRequest { Email = "a@b.com", Token = "t", NewPassword = "Secret123!" });
        Assert.IsType<OkObjectResult>(result);
    }

    private sealed class FakeAuthService : IAuthService
    {
        public RegisterRequest? LastRegisterRequest { get; private set; }
        public LoginRequest? LastLoginRequest { get; private set; }
        public RegisterResponse RegisterResult { get; set; } = new();
        public LoginResponse LoginResult { get; set; } = new();

        public Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            LastRegisterRequest = request;
            return Task.FromResult(RegisterResult);
        }

        public Task<LoginResponse> LoginAsync(LoginRequest dto)
        {
            LastLoginRequest = dto;
            return Task.FromResult(LoginResult);
        }

        public Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request)
        {
            return Task.FromResult(LoginResult);
        }

        public Task<LoginResponse> RefreshAsync(RefreshTokenRequest request)
        {
            return Task.FromResult(LoginResult);
        }

        public Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            return Task.CompletedTask;
        }

        public Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            return Task.CompletedTask;
        }

        public Task<LoginResponse> DemoLoginAsync()
        {
            return Task.FromResult(LoginResult);
        }
    }
}
