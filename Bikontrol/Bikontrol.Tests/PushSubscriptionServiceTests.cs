using Bikontrol.Application.DTOs.Reminders;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Persistence.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Tests;

public class PushSubscriptionServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task RegisterAsync_ShouldUpsertWithCurrentUser()
    {
        var repo = new FakePushSubscriptionRepository();
        var service = new PushSubscriptionService(repo, new FakeCurrentUserService(UserId, UserRole.User));

        await service.RegisterAsync(new RegisterPushSubscriptionRequest
        {
            Endpoint = "https://push/1",
            Keys = new PushSubscriptionKeys { P256dh = "key", Auth = "auth" }
        });

        Assert.True(repo.UpsertedCalled);
        Assert.Equal(UserId, repo.LastUpsert!.UserId);
        Assert.Equal("https://push/1", repo.LastUpsert.Endpoint);
    }

    [Theory]
    [InlineData("", "k", "a")]
    [InlineData("https://push/1", "", "a")]
    [InlineData("https://push/1", "k", "")]
    public async Task RegisterAsync_WhenIncomplete_ShouldThrowValidation(string endpoint, string p256dh, string auth)
    {
        var service = new PushSubscriptionService(new FakePushSubscriptionRepository(), new FakeCurrentUserService(UserId, UserRole.User));

        await Assert.ThrowsAsync<ValidationException>(() => service.RegisterAsync(new RegisterPushSubscriptionRequest
        {
            Endpoint = endpoint,
            Keys = new PushSubscriptionKeys { P256dh = p256dh, Auth = auth }
        }));
    }

    [Fact]
    public async Task RegisterAsync_WhenDemo_ShouldThrowForbidden()
    {
        var service = new PushSubscriptionService(new FakePushSubscriptionRepository(), new FakeCurrentUserService(UserId, UserRole.Demo));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.RegisterAsync(new RegisterPushSubscriptionRequest
        {
            Endpoint = "https://push/1",
            Keys = new PushSubscriptionKeys { P256dh = "k", Auth = "a" }
        }));
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId, string role) { UserId = userId; Role = role; }
        public Guid UserId { get; }
        public string Role { get; }
        public bool IsDemo => Role == UserRole.Demo;
    }

    private sealed class FakePushSubscriptionRepository : IPushSubscriptionRepository
    {
        public bool UpsertedCalled { get; private set; }
        public PushSubscription? LastUpsert { get; private set; }
        public Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(Guid userId) => Task.FromResult<IReadOnlyList<PushSubscription>>(Array.Empty<PushSubscription>());
        public Task<IReadOnlyList<PushSubscription>> GetByUserIdsAsync(IEnumerable<Guid> userIds) => Task.FromResult<IReadOnlyList<PushSubscription>>(Array.Empty<PushSubscription>());
        public Task UpsertAsync(PushSubscription subscription)
        {
            UpsertedCalled = true;
            LastUpsert = subscription;
            return Task.CompletedTask;
        }
        public Task RemoveByEndpointAsync(string endpoint) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
