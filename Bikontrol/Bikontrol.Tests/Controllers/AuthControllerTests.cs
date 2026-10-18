using Bikontrol.API.Controllers;
using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

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
        var controller = CreateController(service);
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
        var controller = CreateController(service);
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
        var controller = CreateController(service);
        var result = await controller.DemoLogin();
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task GoogleLogin_ShouldReturnOk()
    {
        var response = new LoginResponse { Id = Guid.NewGuid(), Email = "user@bikontrol.com", FullName = "Test", Token = "t", RefreshToken = "r", ExpiresIn = 900 };
        var service = new FakeAuthService { LoginResult = response };
        var controller = CreateController(service);
        var result = await controller.GoogleLogin(new GoogleLoginRequest { IdToken = "tok" });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Refresh_ShouldReturnOk()
    {
        var response = new LoginResponse { Id = Guid.NewGuid(), Email = "user@bikontrol.com", FullName = "Test", Token = "t", RefreshToken = "new-refresh", ExpiresIn = 900 };
        var service = new FakeAuthService { LoginResult = response };
        var controller = CreateController(service);

        var result = await controller.Refresh(new RefreshTokenRequest { RefreshToken = "body-token" });

        // No cookie: the body token is the fallback for scripts and tooling.
        Assert.Equal("body-token", service.LastRefreshToken);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Refresh_ShouldPreferTheCookieOverTheBody()
    {
        var response = new LoginResponse { Id = Guid.NewGuid(), Email = "user@bikontrol.com", FullName = "Test", Token = "new-token", RefreshToken = "new-refresh", ExpiresIn = 900 };
        var service = new FakeAuthService { LoginResult = response };
        var controller = CreateController(service);
        controller.Request.Headers.Cookie = $"{AuthController.RefreshTokenCookieName}=cookie-token";

        var result = await controller.Refresh(new RefreshTokenRequest { RefreshToken = "body-token" });

        Assert.Equal("cookie-token", service.LastRefreshToken);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_ShouldMoveTheRefreshTokenIntoAnHttpOnlyCookie()
    {
        var response = new LoginResponse { Id = Guid.NewGuid(), Email = "user@bikontrol.com", FullName = "Test User", Token = "token-456", RefreshToken = "refresh-456", ExpiresIn = 900 };
        var service = new FakeAuthService { LoginResult = response };
        var controller = CreateController(service);

        var result = await controller.Login(new LoginRequest { Email = "user@bikontrol.com", Password = "Secret123!" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<LoginResponse>(ok.Value);
        // The token only rides the Set-Cookie header; the body stays empty.
        Assert.Equal(string.Empty, body.RefreshToken);
        var setCookie = controller.Response.Headers["Set-Cookie"].ToString().ToLowerInvariant();
        Assert.Contains("bikontrol_rt=refresh-456", setCookie);
        Assert.Contains("httponly", setCookie);
        Assert.Contains("samesite=strict", setCookie);
        Assert.Contains("path=/api/auth", setCookie);
    }

    [Fact]
    public async Task Login_InProduction_ShouldMarkTheCookieSecure()
    {
        var response = new LoginResponse { Id = Guid.NewGuid(), Email = "user@bikontrol.com", FullName = "Test", Token = "t", RefreshToken = "refresh-456", ExpiresIn = 900 };
        var service = new FakeAuthService { LoginResult = response };
        var controller = CreateController(service, environmentName: "Production");

        await controller.Login(new LoginRequest { Email = "user@bikontrol.com", Password = "Secret123!" });

        Assert.Contains("secure", controller.Response.Headers["Set-Cookie"].ToString().ToLowerInvariant());
    }

    [Fact]
    public async Task Logout_ShouldRevokeTheCookieTokenAndClearTheCookie()
    {
        var service = new FakeAuthService();
        var controller = CreateController(service);
        controller.Request.Headers.Cookie = $"{AuthController.RefreshTokenCookieName}=cookie-token";

        var result = await controller.Logout(null);

        Assert.Equal("cookie-token", service.LastLogoutToken);
        Assert.IsType<OkObjectResult>(result);
        // The cookie is cleared (an expired Set-Cookie header), not just dropped.
        Assert.Contains("bikontrol_rt=", controller.Response.Headers["Set-Cookie"].ToString());
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnOk()
    {
        var service = new FakeAuthService();
        var controller = CreateController(service);
        var result = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "a@b.com" });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmEmail_ShouldReturnOk()
    {
        var service = new FakeAuthService();
        var controller = CreateController(service);
        var result = await controller.ConfirmEmail(new ConfirmEmailRequest { Email = "a@b.com", Token = "t" });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ResendConfirmation_ShouldReturnOk()
    {
        var service = new FakeAuthService();
        var controller = CreateController(service);
        var result = await controller.ResendConfirmation(new ResendConfirmationRequest { Email = "a@b.com" });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnOk()
    {
        var service = new FakeAuthService();
        var controller = CreateController(service);
        var result = await controller.ResetPassword(new ResetPasswordRequest { Email = "a@b.com", Token = "t", NewPassword = "Secret123!" });
        Assert.IsType<OkObjectResult>(result);
    }

    private static AuthController CreateController(FakeAuthService service, string environmentName = "Development")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:RefreshExpireDays"] = "30" })
            .Build();
        var controller = new AuthController(service, new FakeWebHostEnvironment { EnvironmentName = environmentName }, configuration);
        controller.ControllerContext.HttpContext = new DefaultHttpContext();
        return controller;
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Bikontrol.Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string? WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeAuthService : IAuthService
    {
        public RegisterRequest? LastRegisterRequest { get; private set; }
        public LoginRequest? LastLoginRequest { get; private set; }
        public string? LastRefreshToken { get; private set; }
        public string? LastLogoutToken { get; private set; }
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
            LastRefreshToken = request.RefreshToken;
            return Task.FromResult(LoginResult);
        }

        public Task LogoutAsync(string? refreshToken)
        {
            LastLogoutToken = refreshToken;
            return Task.CompletedTask;
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

        public Task ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            return Task.CompletedTask;
        }

        public Task ResendConfirmationAsync(ResendConfirmationRequest request)
        {
            return Task.CompletedTask;
        }
    }
}
