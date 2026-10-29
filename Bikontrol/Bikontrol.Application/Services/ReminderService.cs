using Bikontrol.Shared.Exceptions;
using Bikontrol.Application.Email;
using Bikontrol.Application.DTOs.Reminders;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Options;
using Bikontrol.Application.Services;
using Bikontrol.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Bikontrol.Application.Services
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
        private readonly IUnsubscribeTokenService _unsubscribeTokens;
        private readonly IConfiguration _configuration;
        private readonly ReminderOptions _options;
        private readonly ILogger<ReminderService> _logger;

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
            IUnsubscribeTokenService unsubscribeTokens,
            IConfiguration configuration,
            IOptions<ReminderOptions> options,
            ILogger<ReminderService> logger)
        {
            _pushSubscriptionRepository = pushSubscriptionRepository;
            _pushSender = pushSender;
            _unsubscribeTokens = unsubscribeTokens;
            _current = current;
            _userRepository = userRepository;
            _userMaintenanceRepository = userMaintenanceRepository;
            _motorcycleRepository = motorcycleRepository;
            _kmHistoryService = kmHistoryService;
            _recordRepository = recordRepository;
            _reminderLogRepository = reminderLogRepository;
            _emailSender = emailSender;
            _configuration = configuration;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>Dedupe window: don't remind the same item twice within N days.</summary>
        private int DedupeDays => _options.DedupeDays;

        /// <summary>
        /// How far back an undelivered reminder is still worth sending. Capped at
        /// two days so a stale log never goes out as a late "new" reminder and
        /// never overlaps the next dedupe window.
        /// </summary>
        private DateTime DeliverySince => DateTime.UtcNow.AddDays(-Math.Min(DedupeDays, 2));

        public async Task<IReadOnlyList<DueReminderDTO>> GetDueForCurrentUserAsync()
        {
            var userId = _current.UserId;
            var maintenances = (await _userMaintenanceRepository.GetByUserIdAsync(userId)).ToList();
            if (maintenances.Count == 0)
                return Array.Empty<DueReminderDTO>();

            var motorcycleIds = maintenances.Select(m => m.MotorcycleId).Distinct().ToList();
            var motorcycleNames = await _motorcycleRepository.GetNamesByUserIdAsync(userId);
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
                    MotorcycleName = motorcycleNames.TryGetValue(item.MotorcycleId, out var motoName) ? motoName : string.Empty,
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
            var pending = await _reminderLogRepository.GetPendingWithDetailsAsync(ReminderChannel.Email, DeliverySince);
            if (pending.Count == 0)
                return 0;

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

                try
                {
                    var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
                    var subject = logs.Count == 1
                        ? "Bikontrol: tenés un mantenimiento por vencer"
                        : $"Bikontrol: {logs.Count} mantenimientos por vencer";
                    var token = _unsubscribeTokens.Create(user.Id);
                    var unsubscribePage = $"{baseUrl}/unsubscribe?token={Uri.EscapeDataString(token)}";
                    var oneClickUrl = $"{baseUrl}/api/reminders/email/unsubscribe?token={Uri.EscapeDataString(token)}";

                    await _emailSender.SendAsync(new EmailMessage(
                        user.Email,
                        subject,
                        BuildDigestBody(user, logs, baseUrl, unsubscribePage),
                        BuildDigestHtml(user, logs, baseUrl, unsubscribePage),
                        oneClickUrl));

                    // Persist right after each send: if a later user fails, this
                    // one is not emailed again on the retry.
                    await _reminderLogRepository.MarkDeliveredAsync(logs.Select(l => l.Id), ReminderChannel.Email, DateTime.UtcNow);
                    await _reminderLogRepository.SaveChangesAsync();
                    sent++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Reminder digest email failed for user {UserId}; will retry on the next run.", user.Id);
                }
            }

            return sent;
        }

        public async Task<int> SendPendingPushesAsync(CancellationToken cancellationToken = default)
        {
            var pending = await _reminderLogRepository.GetPendingWithDetailsAsync(ReminderChannel.Push, DeliverySince);
            if (pending.Count == 0)
                return 0;

            var delivered = 0;

            foreach (var group in pending.GroupBy(r => r.UserId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var logs = group.ToList();
                var user = logs[0].User;
                if (user is null || !user.RemindersEnabled)
                    continue;

                try
                {
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

                    var now = DateTime.UtcNow;
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

                    await _pushSubscriptionRepository.SaveChangesAsync();
                    await _reminderLogRepository.SaveChangesAsync();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Reminder push failed for user {UserId}; will retry on the next run.", user.Id);
                }
            }

            return delivered;
        }

        public async Task UnsubscribeAsync(string? token)
        {
            if (!_unsubscribeTokens.TryValidate(token, out var userId))
                throw new ValidationException("El enlace para darte de baja no es válido.", "invalid_unsubscribe_token");

            var user = await _userRepository.GetByIdAsync(userId);
            // Unknown or deleted account: nothing left to unsubscribe, and
            // answering the same way avoids revealing which accounts exist.
            if (user is null || !user.RemindersEnabled)
                return;

            user.SetRemindersEnabled(false);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        private static List<string> DigestLines(IReadOnlyList<ReminderLog> logs)
        {
            var lines = new List<string>();
            foreach (var log in logs)
            {
                var name = log.UserMaintenance?.Name ?? "Mantenimiento";
                var detail = log.IsOverdue
                    ? "VENCIDO"
                    : log.RemainingKm > 0
                        ? $"faltan {log.RemainingKm} km"
                        : $"faltan {log.RemainingDays} días";
                lines.Add($"{name}: {detail} ({log.LifePercent}% de vida restante)");
            }

            return lines;
        }

        private static string BuildDigestBody(User user, IReadOnlyList<ReminderLog> logs, string baseUrl, string unsubscribeUrl)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Hola {user.FullName},");
            builder.AppendLine();
            builder.AppendLine("Estos mantenimientos de tus motos están por vencer o vencidos:");
            builder.AppendLine();

            foreach (var line in DigestLines(logs))
            {
                builder.AppendLine($"- {line}");
            }

            builder.AppendLine();
            builder.AppendLine($"Entrá a Bikontrol para registrar lo que hiciste: {baseUrl}/dashboard");
            builder.AppendLine();
            builder.AppendLine($"Si no querés recibir estos avisos, date de baja acá: {unsubscribeUrl}");
            return builder.ToString();
        }

        private static string BuildDigestHtml(User user, IReadOnlyList<ReminderLog> logs, string baseUrl, string unsubscribeUrl) =>
            EmailTemplate.Render(
                heading: logs.Count == 1 ? "Tenés un mantenimiento por vencer" : $"Tenés {logs.Count} mantenimientos por vencer",
                paragraphs: new[] { $"Hola {user.FullName}, estos mantenimientos de tus motos están por vencer o vencidos:" },
                bullets: DigestLines(logs),
                buttonLabel: "Abrir Bikontrol",
                buttonUrl: $"{baseUrl}/dashboard",
                footerLine: "Recibís este aviso porque activaste los recordatorios.",
                footerLinkLabel: "Darme de baja",
                footerLinkUrl: unsubscribeUrl);
    }
}
