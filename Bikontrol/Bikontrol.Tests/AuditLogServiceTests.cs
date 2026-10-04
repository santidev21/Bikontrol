using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Services;

namespace Bikontrol.Tests;

public class AuditLogServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static AuditLog Entry(long id) => new()
    {
        Id = id,
        UserId = UserId,
        EntityName = "Motorcycle",
        EntityId = Guid.NewGuid().ToString(),
        Action = "Created",
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetMyActivityAsync_ShouldMapAndForwardUserId()
    {
        var repo = new FakeAuditLogRepository { Entries = new[] { Entry(2), Entry(1) } };
        var service = new AuditLogService(repo, new FakeCurrentUserService());

        var result = await service.GetMyActivityAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Motorcycle", result[0].EntityName);
        Assert.Equal(50, repo.LastLimit); // default
        Assert.Equal(UserId, repo.LastUserId);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(500, 200)]
    [InlineData(30, 30)]
    public async Task GetMyActivityAsync_ShouldClampLimit(int requested, int expected)
    {
        var repo = new FakeAuditLogRepository();
        var service = new AuditLogService(repo, new FakeCurrentUserService());

        await service.GetMyActivityAsync(requested);

        Assert.Equal(expected, repo.LastLimit);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid UserId => AuditLogServiceTests.UserId;
        public string Role => "User";
        public bool IsDemo => false;
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public IReadOnlyList<AuditLog> Entries { get; set; } = [];
        public Guid LastUserId { get; private set; }
        public int LastLimit { get; private set; }

        public Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, int limit)
        {
            LastUserId = userId;
            LastLimit = limit;
            return Task.FromResult(Entries);
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
