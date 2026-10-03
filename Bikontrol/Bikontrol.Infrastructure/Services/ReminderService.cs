using Bikontrol.Application.DTOs.Reminders;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Services;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Email;
using Bikontrol.Persistence.Entities;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Bikontrol.Infrastructure.Services
{
    public class ReminderService : IReminderService
    {
        private readonly ICurrentUserService _current;
        private readonly IUserRepository _userRepository;
        private readonly IUserMaintenanceRepository _userMaintenanceRepository;
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IKmHistoryService _kmHistoryService;
        private readonly IMotorcycleMaintenanceRecordRepository _recordRepository;
        private readonly IReminderLogRepository _reminderLogRepository;
        private readonly IEmailSender _emailSender;
        private readonly IPushSubscriptionRepository _pushSubscriptionRepository;
        private readonly IPushSender _pushSender;
        private readonly IConfiguration _configuration;

        public ReminderService(
            ICurrentUserService current,
            IUserRepository userRepository,
            IUserMaintenanceRepository userMaintenanceRepository,
            IMotorcycleRepository motorcycleRepository,
            IKmHistoryService kmHistoryService,
            IMotorcycleMaintenanceRecordRepository recordRepository,
            IReminderLogRepository reminderLogRepository,
            IEmailSender emailSender,
            IPushSubscriptionRepository pushSubscriptionRepository,
            IPushSender pushSender,
            IConfiguration configuration)
        {
            _pushSubscriptionRepository = pushSubscriptionRepository;
            _pushSender = pushSender;
            _current = current;
            _userRepository = userRepository;
            _userMaintenanceRepository = userMaintenanceRepository;
            _motorcycleRepository = motorcycleRepository;
            _kmHistoryService = kmHistoryService;
            _recordRepository = recordRepository;
            _reminderLogRepository = reminderLogRepository;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        /// <summary>Dedupe window: don't remind the same item twice within N days.</summary>
        private int DedupeDays =>
            int.TryParse(_configuration["Reminders:DedupeDays"], out var days) && days > 0 ? days : 3;

        public async Task<IReadOnlyList<DueReminderDTO>> GetDueForCurrentUserAsync()
        {
            var userId = _current.UserId;
            var maintenances = (await _userMaintenanceRepository.GetByUserIdAsync(userId)).ToList();
            if (maintenances.Count == 0)
                return Array.Empty<DueReminderDTO>();

            var motorcycleIds = maintenances.Select(m => m.MotorcycleId).Distinct().ToList();
            var motorcycles = (await _motorcycleRepository.GetByUserIdAsync(userId))
                .ToDictionary(m => m.Id);
            var currentKms = await _kmHistoryService.GetCurrentKmByMotorcycleIdsAsync(motorcycleIds);
            var initialDates = await _kmHistoryService.GetInitialRecordedAtByMotorcycleIdsAsync(motorcycleIds);
            var lastRecords = await _recordRepository.GetLastByUserMaintenanceIdsAsync(maintenances.Select(m => m.Id));

            var due = new List<DueReminderDTO>();
            foreach (var maintenance in maintenances)
            {
                var item = MaintenanceScheduleCalculator.Calculate(
                    maintenance,
                    lastRecords.TryGetValue(maintenance.Id, out var last) ? last : null,
                    currentKms.TryGetValue(maintenance.MotorcycleId, out var km) ? km : 0,
                    initialDates.TryGetValue(maintenance.MotorcycleId, out var initial) ? initial : null);

                if (!MaintenanceScheduleCalculator.IsDue(item))
                    continue;

                due.Add(new DueReminderDTO
                {
                    UserMaintenanceId = item.UserMaintenanceId,
                    MotorcycleId = item.MotorcycleId,
                    MotorcycleName = motorcycles.TryGetValue(item.MotorcycleId, out var moto) ? moto.Name : string.Empty,
                    Name = item.Name,
                    Description = item.Description,
                    TrackingType = item.TrackingType,
                    RemainingKm = item.RemainingKm,
                    RemainingDays = item.RemainingDays,
                    LifePercent = item.LifePercent,
                    IsOverdue = item.IsOverdue,
                    LastPerformedAt = item.LastPerformedAt,
                    LastPerformedKm = item.LastPerformedKm
                });
            }

            return due
                .OrderBy(r => r.LifePercent)
                .ThenBy(r => r.MotorcycleName)
                .ThenBy(r => r.Name)
                .ToList();
        }

        public async Task<int> GenerateDueRemindersAsync(CancellationToken cancellationToken = default)
        {
            var recipients = await _userRepository.GetReminderRecipientsAsync();
            if (recipients.Count == 0)
                return 0;

            var userIds = recipients.Select(u => u.Id).ToList();
            var maintenances = await _userMaintenanceRepository.GetEnabledForUsersAsync(userIds);
            if (maintenances.Count == 0)
                return 0;

            var motorcycleIds = maintenances.Select(m => m.MotorcycleId).Distinct().ToList();
            var currentKms = await _kmHistoryService.GetCurrentKmByMotorcycleIdsAsync(motorcycleIds);
            var initialDates = await _kmHistoryService.GetInitialRecordedAtByMotorcycleIdsAsync(motorcycleIds);
            var lastRecords = await _recordRepository.GetLastByUserMaintenanceIdsAsync(maintenances.Select(m => m.Id));

            // Items reminded within the dedupe window are skipped in one query.
            var since = DateTime.UtcNow.AddDays(-DedupeDays);
            var recentlyReminded = await _reminderLogRepository.GetRecentUserMaintenanceIdsAsync(
                maintenances.Select(m => m.Id), ReminderKind.Due, since);

            var generated = 0;
            foreach (var maintenance in maintenances)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (recentlyReminded.Contains(maintenance.Id))
                    continue;

                var item = MaintenanceScheduleCalculator.Calculate(
                    maintenance,
                    lastRecords.TryGetValue(maintenance.Id, out var last) ? last : null,
                    currentKms.TryGetValue(maintenance.MotorcycleId, out var km) ? km : 0,
                    initialDates.TryGetValue(maintenance.MotorcycleId, out var initial) ? initial : null);

                if (!MaintenanceScheduleCalculator.IsDue(item))
                    continue;

                await _reminderLogRepository.AddAsync(new ReminderLog
                {
                    UserId = maintenance.UserId,
                    UserMaintenanceId = maintenance.Id,
                    MotorcycleId = maintenance.MotorcycleId,
                    Kind = ReminderKind.Due,
                    IsOverdue = item.IsOverdue,
                    LifePercent = item.LifePercent,
                    RemainingKm = item.RemainingKm,
                    RemainingDays = item.RemainingDays,
                    Channel = ReminderChannel.Pending
                });
                generated++;
            }

            if (generated > 0)
                await _reminderLogRepository.SaveChangesAsync();

            return generated;
        }

        public async Task<int> SendPendingEmailsAsync(CancellationToken cancellationToken = default)
        {
            var pending = await _reminderLogRepository.GetPendingWithDetailsAsync(ReminderChannel.Pending);
            if (pending.Count == 0)
                return 0;

            var now = DateTime.UtcNow;
            var sent = 0;

            // One digest per user, so a user with several due items gets one email.
            foreach (var group in pending.GroupBy(r => r.UserId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var logs = group.ToList();
                var user = logs[0].User;
                if (user is null)
                    continue;
                // Respect the preference at send time (a user may have opted out
                // after the reminder was generated).
                if (!user.RemindersEnabled)
                    continue;

                var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
                var subject = logs.Count == 1
                    ? "Bikontrol: tenés un mantenimiento por vencer"
                    : $"Bikontrol: {logs.Count} mantenimientos por vencer";
                var body = BuildDigestBody(user, logs, baseUrl);

                await _emailSender.SendAsync(user.Email, subject, body);
                await _reminderLogRepository.MarkDeliveredAsync(logs.Select(l => l.Id), ReminderChannel.Email, now);
                sent++;
            }

            if (sent > 0)
                await _reminderLogRepository.SaveChangesAsync();

            return sent;
        }

        public async Task<int> SendPendingPushesAsync(CancellationToken cancellationToken = default)
        {
            var pending = await _reminderLogRepository.GetPendingWithDetailsAsync(ReminderChannel.Pending);
            if (pending.Count == 0)
                return 0;

            var now = DateTime.UtcNow;
            var delivered = 0;

            foreach (var group in pending.GroupBy(r => r.UserId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var logs = group.ToList();
                var user = logs[0].User;
                if (user is null || !user.RemindersEnabled)
                    continue;

                var subscriptions = await _pushSubscriptionRepository.GetByUserIdAsync(user.Id);
                if (subscriptions.Count == 0)
                    continue;

                var payload = new PushPayload
                {
                    Title = "Bikontrol: mantenimiento por vencer",
                    Body = logs.Count == 1
                        ? $"{logs[0].UserMaintenance?.Name ?? "Mantenimiento"} está por vencer."
                        : $"{logs.Count} mantenimientos están por vencer.",
                    Url = "/dashboard"
                };

                var anyDelivered = false;
                foreach (var subscription in subscriptions)
                {
                    var ok = await _pushSender.SendAsync(subscription, payload);
                    if (ok)
                    {
                        subscription.LastUsedAt = now;
                        anyDelivered = true;
                    }
                    else
                    {
                        // Gone (404/410): drop the dead subscription.
                        await _pushSubscriptionRepository.RemoveByEndpointAsync(subscription.Endpoint);
                    }
                }

                if (anyDelivered)
                {
                    await _reminderLogRepository.MarkDeliveredAsync(logs.Select(l => l.Id), ReminderChannel.Push, now);
                    delivered++;
                }
            }

            await _pushSubscriptionRepository.SaveChangesAsync();
            await _reminderLogRepository.SaveChangesAsync();
            return delivered;
        }

        private static string BuildDigestBody(User user, IReadOnlyList<ReminderLog> logs, string baseUrl)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Hola {user.FullName},");
            builder.AppendLine();
            builder.AppendLine("Estos mantenimientos de tus motos están por vencer o vencidos:");
            builder.AppendLine();

            foreach (var log in logs)
            {
                var name = log.UserMaintenance?.Name ?? "Mantenimiento";
                var detail = log.IsOverdue
                    ? "VENCIDO"
                    : log.RemainingKm > 0
                        ? $"faltan {log.RemainingKm} km"
                        : $"faltan {log.RemainingDays} días";
                builder.AppendLine($"- {name}: {detail} ({log.LifePercent}% de vida restante)");
            }

            builder.AppendLine();
            builder.AppendLine($"Entrá a Bikontrol para registrar lo que hiciste: {baseUrl}/dashboard");
            builder.AppendLine();
            builder.AppendLine("Si no querés recibir estos avisos, podés desactivarlos en tu perfil.");
            return builder.ToString();
        }
    }
}
