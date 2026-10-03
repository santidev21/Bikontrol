using Bikontrol.Application.DTOs.Reminders;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Services;
using Bikontrol.Domain.Entities;
using Bikontrol.Persistence.Entities;
using Microsoft.Extensions.Configuration;

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
        private readonly IConfiguration _configuration;

        public ReminderService(
            ICurrentUserService current,
            IUserRepository userRepository,
            IUserMaintenanceRepository userMaintenanceRepository,
            IMotorcycleRepository motorcycleRepository,
            IKmHistoryService kmHistoryService,
            IMotorcycleMaintenanceRecordRepository recordRepository,
            IReminderLogRepository reminderLogRepository,
            IConfiguration configuration)
        {
            _current = current;
            _userRepository = userRepository;
            _userMaintenanceRepository = userMaintenanceRepository;
            _motorcycleRepository = motorcycleRepository;
            _kmHistoryService = kmHistoryService;
            _recordRepository = recordRepository;
            _reminderLogRepository = reminderLogRepository;
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
    }
}
