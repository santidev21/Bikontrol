using AutoMapper;
using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Authentication;
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
        Assert.Single(repository.Users);
        Assert.Equal(request.Email, repository.Users[0].Email);
        Assert.Equal("hashed:Secret123!", repository.Users[0].PasswordHash);
        Assert.Equal(1, repository.SaveChangesCalls);
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

    private AuthService CreateService(FakeUserRepository repository)
    {
        return new AuthService(repository, new FakePasswordHasher(), _tokenGenerator, _mapper);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
                ["Jwt:Issuer"] = "Bikontrol",
                ["Jwt:Audience"] = "Bikontrol.Tests"
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

        public Task SaveChangesAsync()
        {
            SaveChangesCalls++;
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
