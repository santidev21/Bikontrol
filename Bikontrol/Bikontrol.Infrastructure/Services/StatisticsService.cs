using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Statistics;
using Bikontrol.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bikontrol.Infrastructure.Services
{
    /// <summary>
    /// Solo lectura. Reutiliza IMotorcycleService / IMaintenanceService para no
    /// duplicar la lógica de próximos mantenimientos (misma fuente de verdad).
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private const int DueSoonThresholdPercent = 25;
        private const int SoonThresholdPercent = 50;

        private readonly IMotorcycleService _motorcycles;
        private readonly IMaintenanceService _maintenances;

        public StatisticsService(IMotorcycleService motorcycles, IMaintenanceService maintenances)
        {
            _motorcycles = motorcycles;
            _maintenances = maintenances;
        }

        public async Task<StatisticsSummaryDTO> GetSummaryAsync()
        {
            var motorcycles = (await _motorcycles.GetByCurrentUserAsync()).ToList();
            var motorcycleIds = motorcycles.Select(m => m.Id).ToList();

            // Una sola pasada por mantenimientos/registros para toda la flota
            // (antes eran 2 consultas por moto).
            var upcomingByMotorcycle = await _maintenances.GetUpcomingByMotorcyclesAsync(motorcycleIds);
            var recordsByMotorcycle = await _maintenances.GetMaintenanceRecordsByMotorcyclesAsync(motorcycleIds);

            var summary = new StatisticsSummaryDTO
            {
                TotalMotorcycles = motorcycles.Count,
                TotalKm = motorcycles.Sum(m => m.Km)
            };

            var overdue = 0;
            var critical = 0;
            var soon = 0;
            var ok = 0;
            var allRecords = new List<MaintenanceRecordDTO>();

            foreach (var moto in motorcycles)
            {
                summary.KmByMotorcycle.Add(new MotorcycleKmStatDTO
                {
                    MotorcycleId = moto.Id,
                    Name = string.IsNullOrWhiteSpace(moto.Nickname) ? moto.Name : moto.Nickname,
                    Km = moto.Km
                });

                var upcoming = upcomingByMotorcycle.TryGetValue(moto.Id, out var up)
                    ? up
                    : Array.Empty<UpcomingMaintenanceDTO>();
                foreach (var u in upcoming)
                {
                    if (u.IsOverdue)
                    {
                        overdue++;
                    }
                    else if (u.LifePercent <= DueSoonThresholdPercent)
                    {
                        critical++;
                        summary.DueSoonCount++;
                    }
                    else if (u.LifePercent <= SoonThresholdPercent)
                    {
                        soon++;
                    }
                    else
                    {
                        ok++;
                    }
                }

                if (recordsByMotorcycle.TryGetValue(moto.Id, out var records))
                    allRecords.AddRange(records);
            }

            summary.OverdueCount = overdue;
            summary.Health = new List<HealthBucketDTO>
            {
                new() { Bucket = "Vencido", Count = overdue },
                new() { Bucket = "Crítico", Count = critical },
                new() { Bucket = "Próximo", Count = soon },
                new() { Bucket = "OK", Count = ok }
            };

            summary.TotalMaintenanceRecords = allRecords.Count;
            summary.LastActivityAt = allRecords
                .Select(r => (DateTime?)r.PerformedAt)
                .Max();

            summary.RecordsByType = allRecords
                .GroupBy(r => r.MaintenanceName)
                .Select(g => new MaintenanceCountStatDTO { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .ToList();

            var now = DateTime.UtcNow;
            for (var i = 5; i >= 0; i--)
            {
                var month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                summary.Last6Months.Add(new MonthlyActivityDTO
                {
                    YearMonth = month.ToString("yyyy-MM"),
                    Count = allRecords.Count(r => r.PerformedAt.Year == month.Year && r.PerformedAt.Month == month.Month)
                });
            }

            return summary;
        }
    }
}
