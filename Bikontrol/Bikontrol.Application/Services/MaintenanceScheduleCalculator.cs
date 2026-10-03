using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Services
{
    /// <summary>
    /// Single source of truth for the maintenance countdown. Both the per-user
    /// dashboard (<c>MaintenanceService</c>) and the reminder engine use it, so a
    /// reminder can never disagree with what the app shows.
    /// </summary>
    public static class MaintenanceScheduleCalculator
    {
        /// <summary>
        /// A maintenance is "due" for a reminder when it is overdue or has at most
        /// this percentage of its life left (matches the dashboard's "Próximo").
        /// </summary>
        public const int DueLifeThresholdPercent = 20;

        public static UpcomingMaintenanceDTO Calculate(
            UserMaintenance maintenance,
            MotorcycleMaintenanceRecord? lastRecord,
            int currentKm,
            DateTime? initialRecordedAt)
        {
            var dto = new UpcomingMaintenanceDTO
            {
                UserMaintenanceId = maintenance.Id,
                MotorcycleId = maintenance.MotorcycleId,
                Name = maintenance.Name,
                Description = maintenance.Description,
                TrackingType = maintenance.TrackingType,
                KmInterval = maintenance.KmInterval,
                TimeIntervalWeeks = maintenance.TimeIntervalWeeks,
                LastPerformedAt = lastRecord?.PerformedAt,
                LastPerformedKm = lastRecord?.PerformedKm
            };

            if (maintenance.TrackingType == "Km")
            {
                var interval = maintenance.KmInterval ?? 0;
                if (interval <= 0)
                {
                    dto.LifePercent = 0;
                    dto.RemainingKm = 0;
                    dto.IsOverdue = true;
                    return dto;
                }

                var baselineKm = lastRecord?.PerformedKm ?? 0;
                var used = currentKm - baselineKm;
                var remaining = interval - used;
                var life = (int)Math.Floor((double)remaining * 100 / interval);

                dto.RemainingKm = remaining;
                dto.RemainingDays = 0;
                dto.IsOverdue = remaining <= 0;
                dto.LifePercent = Math.Clamp(life, 0, 100);
                return dto;
            }

            var intervalDays = (maintenance.TimeIntervalWeeks ?? 0) * 7;
            if (intervalDays <= 0)
            {
                dto.LifePercent = 0;
                dto.RemainingDays = 0;
                dto.IsOverdue = true;
                return dto;
            }

            if (lastRecord is null && initialRecordedAt is null)
            {
                dto.LifePercent = 0;
                dto.RemainingDays = -intervalDays;
                dto.IsOverdue = true;
                return dto;
            }

            var baselineDate = lastRecord?.PerformedAt.Date ?? initialRecordedAt!.Value.Date;
            dto.LastPerformedAt = baselineDate;
            var daysUsed = (DateTime.UtcNow.Date - baselineDate).Days;
            var remainingDays = intervalDays - daysUsed;
            var timeLife = (int)Math.Floor((double)remainingDays * 100 / intervalDays);

            dto.RemainingDays = remainingDays;
            dto.RemainingKm = 0;
            dto.IsOverdue = remainingDays <= 0;
            dto.LifePercent = Math.Clamp(timeLife, 0, 100);
            return dto;
        }

        /// <summary>
        /// Whether the item should produce a reminder: overdue or at/below the
        /// due threshold.
        /// </summary>
        public static bool IsDue(UpcomingMaintenanceDTO item)
        {
            return item.IsOverdue || item.LifePercent <= DueLifeThresholdPercent;
        }
    }
}
