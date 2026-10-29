using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Authentication;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Mapping;
using Bikontrol.Application.Options;
using Bikontrol.Application.Services;
using Bikontrol.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Bikontrol.Tests;

public class AuthServiceTests
{
    private readonly JwtTokenGenerator _tokenGenerator;
    private FakeRefreshTokenRepository _refreshRepository = new();
    private FakeEmailSender _emailSender = new();
    private FakeGoogleTokenValidator _googleValidator = new();

    public AuthServiceTests()
    {
        _tokenGenerator = new JwtTokenGenerator(TestOptions.From<JwtOptions>(BuildConfiguration(), "Jwt"));
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailIsNew_ShouldCreateUserAndReturnToken()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);
        var request = new RegisterRequest
        {
            Email = "test@bikontrol.com",
            FullName = "Test User",
            Password = "Secret123!"
        };

        var response = await service.RegisterAsync(request);

        Assert.Equal(request.Email, response.Email);
        Assert.Equal(request.FullName, response.FullName);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.True(response.ExpiresIn > 0);
        Assert.Single(repository.Users);
        Assert.Equal(request.Email, repository.Users[0].Email);
        Assert.Equal("hashed:Secret123!", repository.Users[0].PasswordHash);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Single(_refreshRepository.Tokens);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldNotRevealItAndEmailTheOwner()
    {
        var user = new User("test@bikontrol.com", "Existing User", "hashed:Secret123!");
        user.MarkEmailConfirmed();
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository);

        var response = await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@bikontrol.com",
            FullName = "Someone Else",
            Password = "Secret123!"
        });

        // Indistinguishable from a fresh registration that needs confirmation:
        // no session minted, no stored account data leaked (random Id,
        // requested name, User role), and an email goes to the real owner.
        Assert.True(response.EmailConfirmationRequired);
        Assert.Equal(string.Empty, response.Token);
        Assert.Equal(string.Empty, response.RefreshToken);
        Assert.Equal("Someone Else", response.FullName);
        Assert.Equal(UserRole.User, response.Role);
        Assert.NotEqual(user.Id, response.Id);
        Assert.Empty(repository.UsersCreated);
        Assert.Single(_emailSender.Sent);
        Assert.Equal("test@bikontrol.com", _emailSender.Sent[0].To);
        Assert.Equal("Ya tenés una cuenta en Bikontrol", _emailSender.Sent[0].Subject);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExistsButIsUnconfirmed_ShouldResendConfirmationInstead()
    {
        var repository = new FakeUserRepository(existingUsers: [new User("test@bikontrol.com", "Existing User", "hashed:Secret123!")]);
        var service = CreateService(repository);

        var response = await service.RegisterAsync(new RegisterRequest
        {
            Email = "test@bikontrol.com",
            FullName = "Someone Else",
            Password = "Secret123!"
        });

        // Unconfirmed accounts cannot log in, so a register probe resends the
        // confirmation link rather than an "account exists" note.
        Assert.True(response.EmailConfirmationRequired);
        Assert.Equal(string.Empty, response.Token);
        Assert.Empty(repository.UsersCreated);
        Assert.Single(_emailSender.Sent);
        Assert.Equal("Confirmá tu correo de Bikontrol", _emailSender.Sent[0].Subject);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ShouldReturnTokenAndUserData()
    {
        var user = new User("test@bikontrol.com", "Test User", "hashed:Secret123!");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository);

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "test@bikontrol.com",
            Password = "Secret123!"
        });

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.FullName, response.FullName);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ShouldThrowAuthException()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "missing@bikontrol.com",
            Password = "Secret123!"
        }));

        Assert.Equal("El correo o contraseña son inválidos.", exception.Message);
        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ShouldThrowAuthException()
    {
        var user = new User("test@bikontrol.com", "Test User", "hashed:Secret123!");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "test@bikontrol.com",
            Password = "WrongPassword"
        }));

        Assert.Equal("El correo o contraseña son inválidos.", exception.Message);
        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenIsValid_ShouldRotateAndReturnNewTokens()
    {
        var user = new User("test@bikontrol.com", "Test User", "hashed:Secret123!");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository);
        var login = await service.LoginAsync(new LoginRequest { Email = user.Email, Password = "Secret123!" });

        var refreshed = await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = login.RefreshToken });

        Assert.False(string.IsNullOrWhiteSpace(refreshed.Token));
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        // The old token must be revoked and a new one issued.
        Assert.Equal(2, _refreshRepository.Tokens.Count);
        Assert.Single(_refreshRepository.Tokens, t => t.RevokedAt is null);
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenIsUnknown_ShouldThrowAuthException()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => service.RefreshAsync(new RefreshTokenRequest { RefreshToken = "does-not-exist" }));

        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenUserExists_ShouldSendEmailWithResetLink()
    {
        var user = new User("test@bikontrol.com", "Test User", "hashed:Secret123!");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository);

        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email });

        Assert.NotNull(user.ResetPasswordTokenHash);
        Assert.NotNull(user.ResetPasswordTokenExpires);
        var email = Assert.Single(_emailSender.Sent);
        Assert.Equal(user.Email, email.To);
        Assert.Contains("/reset-password?token=", email.Body);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenUserDoesNotExist_ShouldNotSendEmail()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "missing@bikontrol.com" });

        Assert.Empty(_emailSender.Sent);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldUpdatePasswordAndClearToken()
    {
        var user = new User("test@bikontrol.com", "Test User", "hashed:Secret123!");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository);
        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email });
        var token = ExtractToken(_emailSender.Sent.Single().Body);

        await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
            Token = token,
            NewPassword = "NewSecret123!"
        });

        Assert.Equal("hashed:NewSecret123!", user.PasswordHash);
        Assert.Null(user.ResetPasswordTokenHash);
        Assert.Null(user.ResetPasswordTokenExpires);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldThrowAuthException()
    {
        var user = new User("test@bikontrol.com", "Test User", "hashed:Secret123!");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository);
        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email });

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
            Token = "wrong-token",
            NewPassword = "NewSecret123!"
        }));

        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("hashed:Secret123!", user.PasswordHash);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldRevokeExistingRefreshTokens()
    {
        var user = new User("test@bikontrol.com", "Test User", "hashed:Secret123!");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository);
        await service.LoginAsync(new LoginRequest { Email = user.Email, Password = "Secret123!" });
        Assert.Single(_refreshRepository.Tokens, t => t.RevokedAt is null);

        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email });
        var token = ExtractToken(_emailSender.Sent.Single().Body);

        await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
            Token = token,
            NewPassword = "NewSecret123!"
        });

        Assert.All(_refreshRepository.Tokens, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task GoogleLoginAsync_WhenTokenIsInvalid_ShouldThrow401()
    {
        var service = CreateService(new FakeUserRepository(), GoogleConfiguration());
        _googleValidator.Identity = null;

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => service.GoogleLoginAsync(new GoogleLoginRequest { IdToken = "bad" }));

        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("google_invalid_token", exception.Code);
    }

    [Fact]
    public async Task GoogleLoginAsync_WhenEmailIsNotVerified_ShouldThrow401()
    {
        var service = CreateService(new FakeUserRepository(), GoogleConfiguration());
        _googleValidator.Identity = new GoogleIdentity("rider@bikontrol.com", false, "Rider");

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => service.GoogleLoginAsync(new GoogleLoginRequest { IdToken = "token" }));

        Assert.Equal("google_email_unverified", exception.Code);
    }

    [Fact]
    public async Task GoogleLoginAsync_WhenUserIsNew_ShouldCreateAConfirmedGoogleAccountAndIssueASession()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, GoogleConfiguration());
        _googleValidator.Identity = new GoogleIdentity("rider@bikontrol.com", true, "Rider");

        var response = await service.GoogleLoginAsync(new GoogleLoginRequest { IdToken = "token" });

        var created = Assert.Single(repository.UsersCreated);
        Assert.Equal("rider@bikontrol.com", created.Email);
        Assert.Equal("Rider", created.FullName);
        Assert.True(created.IsEmailConfirmed);
        Assert.False(string.IsNullOrEmpty(response.Token));
        Assert.Single(_refreshRepository.Tokens);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RefreshAsync_WhenTheCookieIsAbsent_ShouldReportAnExpiredSession(string? token)
    {
        var service = CreateService(new FakeUserRepository());

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => service.RefreshAsync(new RefreshTokenRequest { RefreshToken = token! }));

        Assert.Equal("session_expired", exception.Code);
    }

    [Fact]
    public async Task RefreshAsync_WhenTheTokenWasRevoked_ShouldReportAnExpiredSession()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);
        var user = new User("rider@bikontrol.com", "Rider", "hash");
        repository.Users.Add(user);
        var stored = new RefreshToken(user.Id, AuthServiceTestsHash("old"), DateTime.UtcNow.AddDays(7));
        stored.Revoke();
        _refreshRepository.Tokens.Add(stored);

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => service.RefreshAsync(new RefreshTokenRequest { RefreshToken = "old" }));

        Assert.Equal("session_expired", exception.Code);
    }

    [Fact]
    public async Task LogoutAsync_WhenTheTokenIsActive_ShouldRevokeIt()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);
        var user = new User("rider@bikontrol.com", "Rider", "hash");
        repository.Users.Add(user);
        var stored = new RefreshToken(user.Id, AuthServiceTestsHash("live"), DateTime.UtcNow.AddDays(7));
        _refreshRepository.Tokens.Add(stored);

        await service.LogoutAsync("live");

        Assert.NotNull(stored.RevokedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-token")]
    public async Task LogoutAsync_WhenThereIsNothingToRevoke_ShouldNotFail(string? token)
    {
        var service = CreateService(new FakeUserRepository());

        await service.LogoutAsync(token);

        Assert.Empty(_refreshRepository.Tokens);
    }

    [Fact]
    public async Task GoogleLoginAsync_WhenTheAccountIsUnconfirmed_ShouldConfirmItBecauseGoogleVerifiedTheEmail()
    {
        var user = new User("rider@bikontrol.com", "Rider", "hash");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository, GoogleConfiguration());
        _googleValidator.Identity = new GoogleIdentity("rider@bikontrol.com", true, "Rider");

        await service.GoogleLoginAsync(new GoogleLoginRequest { IdToken = "token" });

        Assert.True(user.IsEmailConfirmed);
        Assert.Empty(repository.UsersCreated);
    }

    private static string AuthServiceTestsHash(string token) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private static IConfiguration GoogleConfiguration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
            ["Jwt:Issuer"] = "Bikontrol",
            ["Jwt:Audience"] = "Bikontrol.Tests",
            ["Google:ClientId"] = "client-id"
        }).Build();

    [Fact]
    public async Task RefreshAsync_WhenARotatedTokenIsReplayed_ShouldRevokeTheWholeFamily()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);
        var user = new User("rider@bikontrol.com", "Rider", "hash");
        user.MarkEmailConfirmed();
        repository.Users.Add(user);
        var raw = "raw-refresh-token";
        _refreshRepository.Tokens.Add(new RefreshToken(user.Id, AuthServiceTestsHash(raw), DateTime.UtcNow.AddDays(7)));

        var rotated = await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = raw });
        var original = _refreshRepository.Tokens[0];
        typeof(RefreshToken).GetProperty(nameof(RefreshToken.RevokedAt))!
            .SetValue(original, DateTime.UtcNow.AddMinutes(-5));

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => service.RefreshAsync(new RefreshTokenRequest { RefreshToken = raw }));

        Assert.Equal("session_expired", exception.Code);
        // The token minted by the legitimate rotation dies too.
        Assert.All(_refreshRepository.Tokens, t => Assert.NotNull(t.RevokedAt));
        Assert.Single(_refreshRepository.Tokens.Select(t => t.FamilyId).Distinct());
        Assert.NotNull(rotated.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_WhenTheTokenWasRotatedMomentsAgo_ShouldNotRevokeTheFamily()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);
        var user = new User("rider@bikontrol.com", "Rider", "hash");
        user.MarkEmailConfirmed();
        repository.Users.Add(user);
        var raw = "raw-refresh-token";
        _refreshRepository.Tokens.Add(new RefreshToken(user.Id, AuthServiceTestsHash(raw), DateTime.UtcNow.AddDays(7)));

        await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = raw });
        await Assert.ThrowsAsync<AuthException>(
            () => service.RefreshAsync(new RefreshTokenRequest { RefreshToken = raw }));

        Assert.Contains(_refreshRepository.Tokens, t => t.IsActive);
    }

    [Fact]
    public async Task RefreshAsync_WhenTheAccountIsLockedOut_ShouldReportAnExpiredSession()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);
        var user = new User("rider@bikontrol.com", "Rider", "hash");
        user.MarkEmailConfirmed();
        user.RegisterFailedLogin(1, TimeSpan.FromMinutes(10));
        repository.Users.Add(user);
        var raw = "raw-refresh-token";
        _refreshRepository.Tokens.Add(new RefreshToken(user.Id, AuthServiceTestsHash(raw), DateTime.UtcNow.AddDays(7)));

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => service.RefreshAsync(new RefreshTokenRequest { RefreshToken = raw }));

        Assert.Equal("session_expired", exception.Code);
    }

    [Fact]
    public async Task RefreshAsync_WhenAnotherRefreshRotatedTheTokenFirst_ShouldReportAnExpiredSession()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);
        var user = new User("rider@bikontrol.com", "Rider", "hash");
        user.MarkEmailConfirmed();
        repository.Users.Add(user);
        var raw = "raw-refresh-token";
        _refreshRepository.Tokens.Add(new RefreshToken(user.Id, AuthServiceTestsHash(raw), DateTime.UtcNow.AddDays(7)));
        _refreshRepository.ConcurrentConflict = true;

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => service.RefreshAsync(new RefreshTokenRequest { RefreshToken = raw }));

        Assert.Equal("session_expired", exception.Code);
    }

    [Fact]
    public async Task GoogleLoginAsync_WhenClientIdIsMissing_ShouldThrow503()
    {
        var repository = new FakeUserRepository();
        var service = new AuthService(
            repository,
            new FakeRefreshTokenRepository(),
            new FakePasswordHasher(),
            _tokenGenerator,
            new FakeGoogleTokenValidator(),
            new FakeEmailSender(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
                ["Jwt:Issuer"] = "Bikontrol",
                ["Jwt:Audience"] = "Bikontrol.Tests"
            }).Build());

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => service.GoogleLoginAsync(new GoogleLoginRequest { IdToken = "whatever" }));

        Assert.Equal(503, exception.StatusCode);
    }

    [Fact]
    public async Task DemoLoginAsync_ShouldCreateDemoUserAndReturnTokenWithRoleDemo()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var response = await service.DemoLoginAsync();

        Assert.Equal("demo@bikontrol.com", response.Email);
        Assert.Equal("Demo", response.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Single(repository.Users);
        Assert.Equal("Demo", repository.Users.Single().Role);
        // token should contain role claim
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.Token);
        Assert.Equal("Demo", jwt.Claims.First(c => c.Type == "role").Value);
    }

    [Fact]
    public async Task DemoLoginAsync_WhenDemoUserAlreadyExists_ShouldReuseExisting()
    {
        var existingDemo = new User("demo@bikontrol.com", "Usuario Demo", "hashed:random", "Demo");
        var repository = new FakeUserRepository(existingUsers: [existingDemo]);
        var service = CreateService(repository);

        var response = await service.DemoLoginAsync();

        Assert.Equal(existingDemo.Id, response.Id);
        Assert.Single(repository.Users);
    }

    [Fact]
    public async Task DemoLoginAsync_WhenEmailBelongsToRealUser_ShouldThrow403()
    {
        var realUser = new User("demo@bikontrol.com", "Real User", "hashed:random", "User");
        var repository = new FakeUserRepository(existingUsers: [realUser]);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.DemoLoginAsync());

        Assert.Equal(403, exception.StatusCode);
    }

    [Theory]
    [InlineData("false")]
    [InlineData(null)]
    public async Task DemoLoginAsync_WhenDemoIsNotEnabled_ShouldThrow404AndSeedNothing(string? demoEnabled)
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, BuildConfiguration(demoEnabled));

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.DemoLoginAsync());

        Assert.Equal(404, exception.StatusCode);
        Assert.Empty(repository.Users);
        Assert.Empty(_refreshRepository.Tokens);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSetRoleToUserByDefault()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);
        var response = await service.RegisterAsync(new RegisterRequest { Email = "new@bikontrol.com", FullName = "New User", Password = "Secret123!" });
        Assert.Equal("User", response.Role);
        Assert.Equal("User", repository.Users.Single().Role);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailConfirmationRequired_ShouldNotIssueSessionUntilConfirmed()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, BuildConfiguration(emailConfirmationRequired: true));

        var response = await service.RegisterAsync(new RegisterRequest
        {
            Email = "confirm@bikontrol.com",
            FullName = "Confirm User",
            Password = "Secret123!"
        });

        // No session until the email is confirmed.
        Assert.True(response.EmailConfirmationRequired);
        Assert.Empty(response.Token);
        Assert.Empty(response.RefreshToken);
        Assert.Empty(_refreshRepository.Tokens);

        var user = repository.Users.Single();
        Assert.False(user.IsEmailConfirmed);
        Assert.NotNull(user.EmailConfirmationTokenHash);

        var email = Assert.Single(_emailSender.Sent);
        Assert.Contains("/confirm-email?token=", email.Body);
        var token = ExtractToken(email.Body);

        var blocked = await Assert.ThrowsAsync<AuthException>(
            () => service.LoginAsync(new LoginRequest { Email = user.Email, Password = "Secret123!" }));
        Assert.Equal(403, blocked.StatusCode);

        await service.ConfirmEmailAsync(new ConfirmEmailRequest { Email = user.Email, Token = token });
        Assert.True(user.IsEmailConfirmed);
        Assert.Null(user.EmailConfirmationTokenHash);

        var login = await service.LoginAsync(new LoginRequest { Email = user.Email, Password = "Secret123!" });
        Assert.False(string.IsNullOrWhiteSpace(login.Token));
    }

    [Fact]
    public async Task LoginAsync_WhenEmailNotConfirmed_ShouldThrow403()
    {
        var user = new User("unconfirmed@bikontrol.com", "Unconfirmed", "hashed:Secret123!");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository, BuildConfiguration(emailConfirmationRequired: true));

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => service.LoginAsync(new LoginRequest { Email = user.Email, Password = "Secret123!" }));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithInvalidToken_ShouldThrowAndKeepAccountUnconfirmed()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, BuildConfiguration(emailConfirmationRequired: true));
        await service.RegisterAsync(new RegisterRequest { Email = "user@bikontrol.com", FullName = "User", Password = "Secret123!" });

        var exception = await Assert.ThrowsAsync<AuthException>(
            () => service.ConfirmEmailAsync(new ConfirmEmailRequest { Email = "user@bikontrol.com", Token = "wrong-token" }));

        Assert.Equal(401, exception.StatusCode);
        Assert.False(repository.Users.Single().IsEmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenAlreadyConfirmed_ShouldBeIdempotent()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, BuildConfiguration(emailConfirmationRequired: true));
        await service.RegisterAsync(new RegisterRequest { Email = "user@bikontrol.com", FullName = "User", Password = "Secret123!" });
        var token = ExtractToken(_emailSender.Sent.Single().Body);

        await service.ConfirmEmailAsync(new ConfirmEmailRequest { Email = "user@bikontrol.com", Token = token });
        await service.ConfirmEmailAsync(new ConfirmEmailRequest { Email = "user@bikontrol.com", Token = token });

        Assert.True(repository.Users.Single().IsEmailConfirmed);
    }

    [Fact]
    public async Task ResendConfirmationAsync_WhenUserIsUnconfirmed_ShouldSendANewLink()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, BuildConfiguration(emailConfirmationRequired: true));
        await service.RegisterAsync(new RegisterRequest { Email = "user@bikontrol.com", FullName = "User", Password = "Secret123!" });

        await service.ResendConfirmationAsync(new ResendConfirmationRequest { Email = "user@bikontrol.com" });

        Assert.Equal(2, _emailSender.Sent.Count);
        Assert.Contains("/confirm-email?token=", _emailSender.Sent[1].Body);
    }

    [Fact]
    public async Task ResendConfirmationAsync_WhenUserDoesNotExist_ShouldNotSendEmail()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, BuildConfiguration(emailConfirmationRequired: true));

        await service.ResendConfirmationAsync(new ResendConfirmationRequest { Email = "missing@bikontrol.com" });

        Assert.Empty(_emailSender.Sent);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailConfirmationDisabled_ShouldConfirmImmediatelyAndIssueSession()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, BuildConfiguration(emailConfirmationRequired: false));

        var response = await service.RegisterAsync(new RegisterRequest
        {
            Email = "open@bikontrol.com",
            FullName = "Open User",
            Password = "Secret123!"
        });

        Assert.False(response.EmailConfirmationRequired);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.True(repository.Users.Single().IsEmailConfirmed);
    }

    [Fact]
    public async Task LoginAsync_ShouldLockAccountAfterMaxFailedAttempts()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, BuildConfiguration());
        await service.RegisterAsync(new RegisterRequest { Email = "user@bikontrol.com", FullName = "User", Password = "Secret123!" });
        var user = repository.Users.Single();

        // 5 wrong attempts (default max) lock the account.
        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<AuthException>(
                () => service.LoginAsync(new LoginRequest { Email = user.Email, Password = "Wrong123!" }));
        }

        Assert.True(user.IsLockedOut);

        // Even the correct password is rejected while locked out.
        var locked = await Assert.ThrowsAsync<AuthException>(
            () => service.LoginAsync(new LoginRequest { Email = user.Email, Password = "Secret123!" }));
        Assert.Equal(429, locked.StatusCode);
        Assert.Contains("bloqueada", locked.Message);
    }

    [Fact]
    public async Task LoginAsync_WhenSuccessful_ShouldClearFailedAttempts()
    {
        var user = new User("user@bikontrol.com", "User", "hashed:Secret123!");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository, BuildConfiguration());

        for (var i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<AuthException>(
                () => service.LoginAsync(new LoginRequest { Email = user.Email, Password = "Wrong123!" }));
        }

        Assert.Equal(4, user.AccessFailedCount);

        await service.LoginAsync(new LoginRequest { Email = user.Email, Password = "Secret123!" });

        Assert.Equal(0, user.AccessFailedCount);
        Assert.False(user.IsLockedOut);
    }

    [Fact]
    public async Task LoginAsync_WhenLockoutDisabled_ShouldNotLock()
    {
        var user = new User("user@bikontrol.com", "User", "hashed:Secret123!");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository, BuildConfiguration(lockoutEnabled: false));

        for (var i = 0; i < 10; i++)
        {
            await Assert.ThrowsAsync<AuthException>(
                () => service.LoginAsync(new LoginRequest { Email = user.Email, Password = "Wrong123!" }));
        }

        Assert.False(user.IsLockedOut);
        Assert.Equal(0, user.AccessFailedCount);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldUnlockAccount()
    {
        var user = new User("user@bikontrol.com", "User", "hashed:Secret123!");
        var repository = new FakeUserRepository(existingUsers: [user]);
        var service = CreateService(repository, BuildConfiguration());
        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email });
        // Lock the account before using the link (reset still clears it).
        for (var i = 0; i < 5; i++)
            user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15));
        Assert.True(user.IsLockedOut);
        var token = ExtractToken(_emailSender.Sent.Single().Body);

        await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = user.Email,
            Token = token,
            NewPassword = "NewSecret123!"
        });

        Assert.False(user.IsLockedOut);
        Assert.Equal(0, user.AccessFailedCount);
    }

    private static string ExtractToken(string body)
    {
        var marker = "token=";
        var start = body.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = body.IndexOf('&', start);
        return Uri.UnescapeDataString(body.Substring(start, end - start));
    }

    private AuthService CreateService(FakeUserRepository repository, IConfiguration? configuration = null)
    {
        _refreshRepository = new FakeRefreshTokenRepository(repository.Users);
        _emailSender = new FakeEmailSender();
        _googleValidator = new FakeGoogleTokenValidator();
        return new AuthService(
            repository,
            _refreshRepository,
            new FakePasswordHasher(),
            _tokenGenerator,
            _googleValidator,
            _emailSender,
            configuration ?? BuildConfiguration());
    }

    // Demo is opt-in; tests default it to enabled (like Development) and can
    // pass null to reproduce an environment where the flag is absent. Email
    // confirmation defaults to off so the pre-existing tests keep issuing
    // sessions; individual tests turn it on.
    private static IConfiguration BuildConfiguration(
        string? demoEnabled = "true",
        bool emailConfirmationRequired = false,
        bool lockoutEnabled = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
            ["Jwt:Issuer"] = "Bikontrol",
            ["Jwt:Audience"] = "Bikontrol.Tests",
            ["Jwt:ExpireMinutes"] = "15",
            ["Jwt:RefreshExpireDays"] = "30",
            ["Frontend:BaseUrl"] = "http://localhost:4200",
            ["EmailConfirmation:Required"] = emailConfirmationRequired ? "true" : "false",
            ["Lockout:Enabled"] = lockoutEnabled ? "true" : "false",
            ["Lockout:MaxFailedAttempts"] = "5",
            ["Lockout:Minutes"] = "15",
            ["Auth:ForgotPasswordMinMs"] = "0"
        };
        if (demoEnabled is not null)
            values["Demo:Enabled"] = demoEnabled;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _seed;

        public FakeUserRepository(List<User>? existingUsers = null)
        {
            _seed = existingUsers ?? new List<User>();
        }

        public List<User> Users => _seed;
        public List<User> UsersCreated { get; } = new();
        public int SaveChangesCalls { get; private set; }

        public Task<User?> GetByEmailAsync(string email)
        {
            var user = _seed.FirstOrDefault(x => x.Email == email);
            return Task.FromResult(user);
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            var user = _seed.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(user);
        }

        public Task<bool> ExistsByEmailAsync(string email)
        {
            var exists = _seed.Any(x => x.Email == email);
            return Task.FromResult(exists);
        }

        public Task AddAsync(User user)
        {
            _seed.Add(user);
            UsersCreated.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<User>> GetReminderRecipientsAsync()
        {
            return Task.FromResult<IReadOnlyList<User>>(_seed);
        }

        public Task SaveChangesAsync()
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly List<User> _users;

        public FakeRefreshTokenRepository(List<User>? users = null)
        {
            _users = users ?? new List<User>();
        }

        public List<RefreshToken> Tokens { get; } = new();

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            var token = Tokens.FirstOrDefault(t => t.TokenHash == tokenHash);
            if (token is not null)
            {
                token.User = _users.FirstOrDefault(u => u.Id == token.UserId);
            }
            return Task.FromResult(token);
        }

        public Task AddAsync(RefreshToken refreshToken)
        {
            Tokens.Add(refreshToken);
            return Task.CompletedTask;
        }

        public Task<int> RevokeFamilyAsync(Guid familyId)
        {
            var active = Tokens.Where(t => t.FamilyId == familyId && t.RevokedAt is null).ToList();
            foreach (var token in active)
                token.Revoke();
            return Task.FromResult(active.Count);
        }

        public Task<int> RevokeAllForUserAsync(Guid userId)
        {
            var active = Tokens.Where(t => t.UserId == userId && t.RevokedAt is null).ToList();
            foreach (var token in active)
                token.Revoke();
            return Task.FromResult(active.Count);
        }

        public Task<int> DeleteInactiveBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task SaveChangesAsync() => Task.CompletedTask;

        public bool ConcurrentConflict { get; set; }
        public Task<bool> TrySaveAsync() => Task.FromResult(!ConcurrentConflict);
    }

    private sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
    {
        public GoogleIdentity? Identity { get; set; }

        public Task<GoogleIdentity?> ValidateAsync(string idToken, string clientId) =>
            Task.FromResult(Identity);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = new();
        public List<EmailMessage> Messages { get; } = new();

        public Task SendAsync(EmailMessage message)
        {
            Sent.Add((message.To, message.Subject, message.TextBody));
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher<User>
    {
        public string HashPassword(User user, string password) => $"hashed:{password}";

        public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
        {
            return hashedPassword == HashPassword(user, providedPassword)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }
}
