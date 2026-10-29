using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Application.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bikontrol.Tests;

public class RetentionServiceTests
{
    [Fact]
    public async Task PurgeAsync_WithDefaults_ShouldPurgeTokensAndLogsButNotAudit()
    {
        var refreshTokenRepository = new FakeRefreshTokenRepository { Deleted = 3 };
        var reminderLogRepository = new FakeReminderLogRepository { Deleted = 5 };
        var auditLogRepository = new FakeAuditLogRepository { Deleted = 7 };
        var service = CreateService(refreshTokenRepository, reminderLogRepository, auditLogRepository);

        var result = await service.PurgeAsync();

        Assert.Equal(3, result.RefreshTokensDeleted);
        Assert.Equal(5, result.ReminderLogsDeleted);
        // Audit retention is off by default: the trail is append-only by design.
        Assert.Equal(0, result.AuditLogsDeleted);
        AssertCutoff(refreshTokenRepository.LastCutoffUtc, 30);
        AssertCutoff(reminderLogRepository.LastCutoffUtc, 90);
        Assert.Null(auditLogRepository.LastCutoffUtc);
    }

    [Fact]
    public async Task PurgeAsync_WhenAuditRetentionIsConfigured_ShouldAlsoPurgeAuditLogs()
    {
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var reminderLogRepository = new FakeReminderLogRepository();
        var auditLogRepository = new FakeAuditLogRepository { Deleted = 2 };
        var service = CreateService(
            refreshTokenRepository, reminderLogRepository, auditLogRepository,
            auditLogDays: "365");

        var result = await service.PurgeAsync();

        Assert.Equal(2, result.AuditLogsDeleted);
        AssertCutoff(auditLogRepository.LastCutoffUtc, 365);
    }

    [Fact]
    public async Task PurgeAsync_WhenDaysAreInvalid_ShouldFallBackToDefaults()
    {
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var reminderLogRepository = new FakeReminderLogRepository();
        var service = CreateService(
            refreshTokenRepository, reminderLogRepository, new FakeAuditLogRepository(),
            refreshTokenDays: "0",
            reminderLogDays: "-5");

        await service.PurgeAsync();

        // Invalid values (0 or negative) keep the safe defaults.
        AssertCutoff(refreshTokenRepository.LastCutoffUtc, 30);
        AssertCutoff(reminderLogRepository.LastCutoffUtc, 90);
    }

    private static RetentionService CreateService(
        FakeRefreshTokenRepository refreshTokenRepository,
        FakeReminderLogRepository reminderLogRepository,
        FakeAuditLogRepository auditLogRepository,
        string? refreshTokenDays = null,
        string? reminderLogDays = null,
        string? auditLogDays = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Retention:RefreshTokenDays"] = refreshTokenDays,
            ["Retention:ReminderLogDays"] = reminderLogDays,
            ["Retention:AuditLogDays"] = auditLogDays
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values.Where(v => v.Value is not null))
            .Build();

        return new RetentionService(refreshTokenRepository, reminderLogRepository, auditLogRepository, configuration);
    }

    /// <summary>The cutoff must be within a couple of seconds of the expected days ago.</summary>
    private static void AssertCutoff(DateTime? cutoffUtc, int expectedDaysAgo)
    {
        Assert.NotNull(cutoffUtc);
        var expected = DateTime.UtcNow.AddDays(-expectedDaysAgo);
        Assert.True(Math.Abs((cutoffUtc!.Value - expected).TotalSeconds) < 5,
            $"Expected a cutoff around {expected} but was {cutoffUtc}.");
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public int Deleted { get; set; }
        public DateTime? LastCutoffUtc { get; private set; }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash) => Task.FromResult<RefreshToken?>(null);

        public Task AddAsync(RefreshToken refreshToken) => Task.CompletedTask;

        public Task<int> RevokeAllForUserAsync(Guid userId) => Task.FromResult(0);
        public Task<int> RevokeFamilyAsync(Guid familyId) => Task.FromResult(0);

        public Task<int> DeleteInactiveBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            LastCutoffUtc = cutoffUtc;
            return Task.FromResult(Deleted);
        }

        public Task SaveChangesAsync() => Task.CompletedTask;

        public bool ConcurrentConflict { get; set; }
        public Task<bool> TrySaveAsync() => Task.FromResult(!ConcurrentConflict);
    }

    private sealed class FakeReminderLogRepository : IReminderLogRepository
    {
        public int Deleted { get; set; }
        public DateTime? LastCutoffUtc { get; private set; }

        public Task<bool> ExistsSinceAsync(Guid userMaintenanceId, string kind, DateTime since) =>
            Task.FromResult(false);

        public Task<IReadOnlySet<Guid>> GetRecentUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds, string kind, DateTime since) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task AddAsync(ReminderLog entity) => Task.CompletedTask;

        public Task<IReadOnlyList<ReminderLog>> GetPendingWithDetailsAsync(string channel, DateTime since) =>
            Task.FromResult<IReadOnlyList<ReminderLog>>(new List<ReminderLog>());

        public Task MarkDeliveredAsync(IEnumerable<Guid> ids, string channel, DateTime deliveredAt) =>
            Task.CompletedTask;

        public Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            LastCutoffUtc = cutoffUtc;
            return Task.FromResult(Deleted);
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public int Deleted { get; set; }
        public DateTime? LastCutoffUtc { get; private set; }

        public Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, int limit) =>
            Task.FromResult<IReadOnlyList<AuditLog>>(new List<AuditLog>());

        public Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            LastCutoffUtc = cutoffUtc;
            return Task.FromResult(Deleted);
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
