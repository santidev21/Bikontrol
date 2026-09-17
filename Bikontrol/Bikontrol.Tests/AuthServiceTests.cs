using AutoMapper;
using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Authentication;
using Bikontrol.Infrastructure.Email;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Persistence.Entities;
using Bikontrol.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Bikontrol.Tests;

public class AuthServiceTests
{
    private readonly IMapper _mapper;
    private readonly JwtTokenGenerator _tokenGenerator;
    private FakeRefreshTokenRepository _refreshRepository = new();
    private FakeEmailSender _emailSender = new();

    public AuthServiceTests()
    {
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        _tokenGenerator = new JwtTokenGenerator(BuildConfiguration());
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
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowAuthExceptionWith409()
    {
        var repository = new FakeUserRepository(existingUsers: [new User("test@bikontrol.com", "Existing User", "hashed:Secret123!")]);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.RegisterAsync(new RegisterRequest
        {
            Email = "test@bikontrol.com",
            FullName = "Test User",
            Password = "Secret123!"
        }));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("El usuario ya existe.", exception.Message);
        Assert.Empty(repository.UsersCreated);
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
    public async Task GoogleLoginAsync_WhenClientIdIsMissing_ShouldThrow503()
    {
        var repository = new FakeUserRepository();
        var service = new AuthService(
            repository,
            new FakeRefreshTokenRepository(),
            new FakePasswordHasher(),
            _tokenGenerator,
            new FakeEmailSender(),
            _mapper,
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
    public async Task RegisterAsync_ShouldSetRoleToUserByDefault()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);
        var response = await service.RegisterAsync(new RegisterRequest { Email = "new@bikontrol.com", FullName = "New User", Password = "Secret123!" });
        Assert.Equal("User", response.Role);
        Assert.Equal("User", repository.Users.Single().Role);
    }

    private static string ExtractToken(string body)
    {
        var marker = "token=";
        var start = body.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = body.IndexOf('&', start);
        return Uri.UnescapeDataString(body.Substring(start, end - start));
    }

    private AuthService CreateService(FakeUserRepository repository)
    {
        _refreshRepository = new FakeRefreshTokenRepository(repository.Users);
        _emailSender = new FakeEmailSender();
        return new AuthService(
            repository,
            _refreshRepository,
            new FakePasswordHasher(),
            _tokenGenerator,
            _emailSender,
            _mapper,
            BuildConfiguration());
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
                ["Jwt:Issuer"] = "Bikontrol",
                ["Jwt:Audience"] = "Bikontrol.Tests",
                ["Jwt:ExpireMinutes"] = "15",
                ["Jwt:RefreshExpireDays"] = "30",
                ["Frontend:BaseUrl"] = "http://localhost:4200"
            })
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

        public Task<int> RevokeAllForUserAsync(Guid userId)
        {
            var active = Tokens.Where(t => t.UserId == userId && t.RevokedAt is null).ToList();
            foreach (var token in active)
                token.Revoke();
            return Task.FromResult(active.Count);
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = new();

        public Task SendAsync(string toEmail, string subject, string body)
        {
            Sent.Add((toEmail, subject, body));
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
