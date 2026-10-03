using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Email;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Persistence.Entities;
using Microsoft.Extensions.Configuration;

namespace Bikontrol.Tests;

/// <summary>
/// Reminder engine: which items are due, dedupe and the current-user view.
/// The countdown itself is shared with MaintenanceService (MaintenanceScheduleCalculator).
/// </summary>
public class ReminderServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid MotoId = Guid.NewGuid();

    private static UserMaintenance KmMaintenance(Guid id, int interval, Guid motoId)
    {
        return new UserMaintenance
        {
            Id = id,
            UserId = UserId,
            MotorcycleId = motoId,
            Name = "Aceite",
            TrackingType = "Km",
            KmInterval = interval,
            IsEnabled = true
        };
    }

    [Fact]
    public async Task GetDueForCurrentUserAsync_ShouldReturnOnlyDueItemsWorstFirst()
    {
        var due = KmMaintenance(Guid.NewGuid(), 1000, MotoId);       // overdue: 1500 km run
        var healthy = KmMaintenance(Guid.NewGuid(), 100000, MotoId); // plenty left

        var repo = new FakeUserMaintenanceRepository(new[] { due, healthy });
        var service = CreateService(repo, kmHistories: new Dictionary<Guid, int> { [MotoId] = 1500 });

        var result = await service.GetDueForCurrentUserAsync();

        Assert.Single(result);
        Assert.Equal(due.Id, result[0].UserMaintenanceId);
        Assert.True(result[0].IsOverdue);
    }

    [Fact]
    public async Task GenerateDueRemindersAsync_ShouldSkipItemsAlreadyRemindedWithinWindow()
    {
        var item = KmMaintenance(Guid.NewGuid(), 1000, MotoId);
        var repo = new FakeUserMaintenanceRepository(new[] { item });
        var logs = new FakeReminderLogRepository();
        // Pretend it was reminded today (inside the 3-day dedupe window).
        await logs.AddAsync(new ReminderLog
        {
            UserMaintenanceId = item.Id,
            Kind = ReminderKind.Due,
            CreatedAt = DateTime.UtcNow
        });

        var service = CreateService(repo, reminders: logs, kmHistories: new Dictionary<Guid, int> { [MotoId] = 1500 });

        var generated = await service.GenerateDueRemindersAsync();

        Assert.Equal(0, generated);
    }

    [Fact]
    public async Task GenerateDueRemindersAsync_ShouldRecordDueItemsNotRecentlyReminded()
    {
        var item = KmMaintenance(Guid.NewGuid(), 1000, MotoId);
        var repo = new FakeUserMaintenanceRepository(new[] { item });
        var logs = new FakeReminderLogRepository();

        var service = CreateService(repo, reminders: logs, kmHistories: new Dictionary<Guid, int> { [MotoId] = 1500 });

        var generated = await service.GenerateDueRemindersAsync();

        Assert.Equal(1, generated);
        var saved = Assert.Single(logs.Logs);
        Assert.Equal(item.Id, saved.UserMaintenanceId);
        Assert.Equal(ReminderKind.Due, saved.Kind);
        Assert.Equal(ReminderChannel.Pending, saved.Channel);
    }

    [Fact]
    public async Task SendPendingEmailsAsync_ShouldSendOneDigestPerUserAndMarkDelivered()
    {
        var user = new User("rider@bikontrol.com", "Rider", "hash");
        var item1 = KmMaintenance(Guid.NewGuid(), 1000, MotoId);
        var item2 = KmMaintenance(Guid.NewGuid(), 1000, MotoId);

        var logs = new FakeReminderLogRepository();
        await logs.AddAsync(new ReminderLog
        {
            UserId = user.Id,
            UserMaintenanceId = item1.Id,
            Channel = ReminderChannel.Pending,
            IsOverdue = true,
            User = user,
            UserMaintenance = item1
        });
        await logs.AddAsync(new ReminderLog
        {
            UserId = user.Id,
            UserMaintenanceId = item2.Id,
            Channel = ReminderChannel.Pending,
            IsOverdue = false,
            RemainingKm = 50,
            LifePercent = 10,
            User = user,
            UserMaintenance = item2
        });

        var email = new FakeEmailSender();
        var service = CreateService(new FakeUserMaintenanceRepository(new[] { item1 }), reminders: logs, emailSender: email);

        var sent = await service.SendPendingEmailsAsync();

        Assert.Equal(1, sent); // one digest, not two emails
        var message = Assert.Single(email.Sent);
        Assert.Equal(user.Email, message.To);
        Assert.Contains("2 mantenimientos", message.Subject);
        Assert.All(logs.Logs, l => Assert.Equal(ReminderChannel.Email, l.Channel));
        Assert.All(logs.Logs, l => Assert.NotNull(l.DeliveredAt));
    }

    [Fact]
    public async Task SendPendingEmailsAsync_WhenUserOptedOut_ShouldSkip()
    {
        var user = new User("rider@bikontrol.com", "Rider", "hash");
        user.SetRemindersEnabled(false);
        var item = KmMaintenance(Guid.NewGuid(), 1000, MotoId);

        var logs = new FakeReminderLogRepository();
        await logs.AddAsync(new ReminderLog
        {
            UserId = user.Id,
            UserMaintenanceId = item.Id,
            Channel = ReminderChannel.Pending,
            User = user,
            UserMaintenance = item
        });

        var email = new FakeEmailSender();
        var service = CreateService(new FakeUserMaintenanceRepository(new[] { item }), reminders: logs, emailSender: email);

        var sent = await service.SendPendingEmailsAsync();

        Assert.Equal(0, sent);
        Assert.Empty(email.Sent);
        Assert.Equal(ReminderChannel.Pending, logs.Logs[0].Channel);
    }

    private static ReminderService CreateService(
        FakeUserMaintenanceRepository userMaintenance,
        FakeReminderLogRepository? reminders = null,
        Dictionary<Guid, int>? kmHistories = null,
        FakeEmailSender? emailSender = null,
        FakeUserRepository? userRepository = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Reminders:DedupeDays"] = "3",
            ["Frontend:BaseUrl"] = "http://localhost:4200"
        }).Build();

        return new ReminderService(
            new FakeCurrentUserService(UserId),
            userRepository ?? new FakeUserRepository(new[] { new User("u@bikontrol.com", "User", "hash") }),
            userMaintenance,
            new FakeMotorcycleRepository(MotoId),
            new FakeKmHistoryService(kmHistories ?? new Dictionary<Guid, int>()),
            new FakeRecordRepository(),
            reminders ?? new FakeReminderLogRepository(),
            emailSender ?? new FakeEmailSender(),
            config);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId) => UserId = userId;
        public Guid UserId { get; }
        public string Role => UserRole.User;
        public bool IsDemo => false;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users;
        public FakeUserRepository(IEnumerable<User> users) => _users = users.ToList();
        public Task<User?> GetByEmailAsync(string email) => Task.FromResult(_users.FirstOrDefault());
        public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(_users.FirstOrDefault());
        public Task<bool> ExistsByEmailAsync(string email) => Task.FromResult(false);
        public Task<IReadOnlyList<User>> GetReminderRecipientsAsync() => Task.FromResult<IReadOnlyList<User>>(_users);
        public Task AddAsync(User user) => Task.CompletedTask;
        public Task UpdateAsync(User user) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeUserMaintenanceRepository : IUserMaintenanceRepository
    {
        private readonly List<UserMaintenance> _items;
        public FakeUserMaintenanceRepository(IEnumerable<UserMaintenance> items) => _items = items.ToList();
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAsync(Guid userId) =>
            Task.FromResult(_items.Where(x => x.UserId == userId).AsEnumerable());
        public Task<IEnumerable<UserMaintenance>> GetByUserIdAndMotorcycleIdAsync(Guid userId, Guid motorcycleId) =>
            Task.FromResult(_items.Where(x => x.UserId == userId && x.MotorcycleId == motorcycleId).AsEnumerable());
        public Task<UserMaintenance?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<UserMaintenance>> GetEnabledForUsersAsync(IEnumerable<Guid> userIds) =>
            Task.FromResult<IReadOnlyList<UserMaintenance>>(_items);
        public Task<UserMaintenance> AddAsync(UserMaintenance entity) => Task.FromResult(entity);
        public Task UpdateAsync(UserMaintenance entity) => Task.CompletedTask;
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task<UserMaintenance?> GetByBaseIdAsync(Guid userId, Guid motorcycleId, Guid baseId) => Task.FromResult<UserMaintenance?>(null);
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeMotorcycleRepository : IMotorcycleRepository
    {
        private readonly Guid _motoId;
        public FakeMotorcycleRepository(Guid motoId) => _motoId = motoId;
        public Task<Motorcycle?> GetByIdAsync(Guid id) => Task.FromResult<Motorcycle?>(null);
        public Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId) =>
            Task.FromResult<IEnumerable<Motorcycle>>(new[] { new Motorcycle("X", "Y", 2020, "Z", 150, "AAA", UserId) { Id = _motoId } });
        public Task<Motorcycle> AddAsync(Motorcycle motorcycle) => Task.FromResult(motorcycle);
        public Task UpdateAsync(Motorcycle motorcycle) => Task.CompletedTask;
        public Task SoftDeleteAsync(Guid id) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeKmHistoryService : IKmHistoryService
    {
        private readonly Dictionary<Guid, int> _kms;
        public FakeKmHistoryService(Dictionary<Guid, int> kms) => _kms = kms;
        public Task AddKmAsync(Guid motorcycleId, int km) => Task.CompletedTask;
        public Task<int> GetCurrentKmAsync(Guid motorcycleId) => Task.FromResult(_kms.TryGetValue(motorcycleId, out var km) ? km : 0);
        public Task<DateTime?> GetInitialRecordedAtAsync(Guid motorcycleId) => Task.FromResult<DateTime?>(null);
        public Task<IReadOnlyDictionary<Guid, int>> GetCurrentKmByMotorcycleIdsAsync(IEnumerable<Guid> ids) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(ids.ToDictionary(id => id, id => _kms.TryGetValue(id, out var km) ? km : 0));
        public Task<IReadOnlyDictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> ids) =>
            Task.FromResult<IReadOnlyDictionary<Guid, DateTime?>>(ids.ToDictionary(id => id, _ => (DateTime?)null));
        public Task RollbackLastKmAsync(Guid motorcycleId, int newKm) => Task.CompletedTask;
    }

    private sealed class FakeRecordRepository : IMotorcycleMaintenanceRecordRepository
    {
        public Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity) => Task.FromResult(entity);
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId) =>
            Task.FromResult(Enumerable.Empty<MotorcycleMaintenanceRecord>());
        public Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds) =>
            Task.FromResult(Enumerable.Empty<MotorcycleMaintenanceRecord>());
        public Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId) =>
            Task.FromResult<MotorcycleMaintenanceRecord?>(null);
        public Task<Dictionary<Guid, MotorcycleMaintenanceRecord>> GetLastByUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds) =>
            Task.FromResult(new Dictionary<Guid, MotorcycleMaintenanceRecord>());
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeReminderLogRepository : IReminderLogRepository
    {
        public List<ReminderLog> Logs { get; } = new();
        public Task<bool> ExistsSinceAsync(Guid userMaintenanceId, string kind, DateTime since) =>
            Task.FromResult(Logs.Any(l => l.UserMaintenanceId == userMaintenanceId && l.Kind == kind && l.CreatedAt >= since));
        public Task<IReadOnlySet<Guid>> GetRecentUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds, string kind, DateTime since)
        {
            var ids = userMaintenanceIds.ToList();
            var recent = Logs
                .Where(l => ids.Contains(l.UserMaintenanceId) && l.Kind == kind && l.CreatedAt >= since)
                .Select(l => l.UserMaintenanceId)
                .ToHashSet();
            return Task.FromResult<IReadOnlySet<Guid>>(recent);
        }
        public Task AddAsync(ReminderLog entity)
        {
            Logs.Add(entity);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<ReminderLog>> GetPendingWithDetailsAsync(string channel) =>
            Task.FromResult<IReadOnlyList<ReminderLog>>(
                Logs.Where(l => l.Channel == channel).ToList());
        public Task MarkDeliveredAsync(IEnumerable<Guid> ids, string channel, DateTime deliveredAt)
        {
            var idList = ids.ToHashSet();
            foreach (var log in Logs.Where(l => idList.Contains(l.Id)))
            {
                log.Channel = channel;
                log.DeliveredAt = deliveredAt;
            }
            return Task.CompletedTask;
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
}
