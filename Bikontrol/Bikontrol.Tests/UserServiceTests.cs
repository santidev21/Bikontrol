using AutoMapper;
using Bikontrol.Application.DTOs.Users;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Persistence.Entities;
using Bikontrol.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Bikontrol.Tests;

public class UserServiceTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    [Fact]
    public async Task GetMeAsync_ShouldReturnProfileWithPasswordFlag()
    {
        var user = new User("user@bikontrol.com", "Santi", "hashed:secret");
        var service = CreateService(user, role: UserRole.User);

        var result = await service.GetMeAsync();

        Assert.Equal(user.Id, result.Id);
        Assert.Equal("user@bikontrol.com", result.Email);
        Assert.Equal("Santi", result.FullName);
        Assert.Equal(UserRole.User, result.Role);
        Assert.True(result.HasPassword);
    }

    [Fact]
    public async Task GetMeAsync_GoogleAccount_ShouldReportNoPassword()
    {
        var user = new User("g@bikontrol.com", "G User", "hashed:random");
        user.SetAuthProvider("Google");
        var service = CreateService(user, role: UserRole.User);

        var result = await service.GetMeAsync();

        Assert.False(result.HasPassword);
    }

    [Fact]
    public async Task GetMeAsync_UnknownUser_ShouldThrowNotFound()
    {
        var service = CreateService((User?)null, role: UserRole.User);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetMeAsync());
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldUpdateFullName()
    {
        var user = new User("user@bikontrol.com", "Old", "hashed:secret");
        var repository = new FakeUserRepository(user);
        var service = CreateService(repository, role: UserRole.User);

        var result = await service.UpdateProfileAsync(new UpdateProfileRequest { FullName = "  New Name " });

        Assert.Equal("New Name", result.FullName);
        Assert.Equal("New Name", user.FullName);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task UpdateProfileAsync_EmptyName_ShouldThrowValidation()
    {
        var user = new User("user@bikontrol.com", "Old", "hashed:secret");
        var service = CreateService(user, role: UserRole.User);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateProfileAsync(new UpdateProfileRequest { FullName = "   " }));
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenDemo_ShouldThrowForbidden()
    {
        var user = new User("demo@bikontrol.com", "Demo", "hashed:secret");
        var service = CreateService(user, role: UserRole.Demo);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.UpdateProfileAsync(new UpdateProfileRequest { FullName = "X" }));
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldUpdateHashAndClearResetToken()
    {
        var user = new User("user@bikontrol.com", "Santi", "hashed:old");
        user.SetResetPasswordToken("tokenhash", DateTime.UtcNow.AddHours(1));
        var repository = new FakeUserRepository(user);
        var service = CreateService(repository, role: UserRole.User);

        await service.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = "old",
            NewPassword = "newsecret"
        });

        Assert.Equal("hashed:newsecret", user.PasswordHash);
        Assert.Null(user.ResetPasswordTokenHash);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrent_ShouldThrowValidation()
    {
        var user = new User("user@bikontrol.com", "Santi", "hashed:old");
        var service = CreateService(user, role: UserRole.User);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ChangePasswordAsync(new ChangePasswordRequest
            {
                CurrentPassword = "wrong",
                NewPassword = "newsecret"
            }));
    }

    [Fact]
    public async Task ChangePasswordAsync_GoogleAccount_ShouldThrowValidation()
    {
        var user = new User("g@bikontrol.com", "G User", "hashed:random");
        user.SetAuthProvider("Google");
        var service = CreateService(user, role: UserRole.User);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ChangePasswordAsync(new ChangePasswordRequest
            {
                CurrentPassword = "random",
                NewPassword = "newsecret"
            }));
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenDemo_ShouldThrowForbidden()
    {
        var user = new User("demo@bikontrol.com", "Demo", "hashed:secret");
        var service = CreateService(user, role: UserRole.Demo);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.ChangePasswordAsync(new ChangePasswordRequest
            {
                CurrentPassword = "secret",
                NewPassword = "newsecret"
            }));
    }

    private static UserService CreateService(User? user, string role)
        => CreateService(new FakeUserRepository(user), role);

    private static UserService CreateService(FakeUserRepository repository, string role)
    {
        var userId = repository.Users.FirstOrDefault()?.Id ?? Guid.NewGuid();
        return new UserService(
            repository,
            new FakePasswordHasher(),
            Mapper,
            new FakeCurrentUserService(userId, role));
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users;
        public FakeUserRepository(User? user)
        {
            _users = user is null ? new List<User>() : new List<User> { user };
        }
        public List<User> Users => _users;
        public int SaveChangesCalls { get; private set; }
        public Task<User?> GetByEmailAsync(string email) =>
            Task.FromResult(_users.FirstOrDefault(x => x.Email == email));
        public Task<User?> GetByIdAsync(Guid id) =>
            Task.FromResult(_users.FirstOrDefault(x => x.Id == id));
        public Task<bool> ExistsByEmailAsync(string email) =>
            Task.FromResult(_users.Any(x => x.Email == email));
        public Task AddAsync(User user)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(User user) => Task.CompletedTask;
        public Task SaveChangesAsync()
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher<User>
    {
        public string HashPassword(User user, string password) => $"hashed:{password}";
        public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword) =>
            hashedPassword == $"hashed:{providedPassword}"
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId, string role)
        {
            UserId = userId;
            Role = role;
        }
        public Guid UserId { get; }
        public string Role { get; }
        public bool IsDemo => Role == UserRole.Demo;
    }
}
