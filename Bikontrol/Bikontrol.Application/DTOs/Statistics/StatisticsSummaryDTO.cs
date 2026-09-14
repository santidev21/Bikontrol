using System;
using System.Collections.Generic;

namespace Bikontrol.Application.DTOs.Statistics
{
    public class StatisticsSummaryDTO
    {
        public int TotalMotorcycles { get; set; }
        public int TotalKm { get; set; }
        public int TotalMaintenanceRecords { get; set; }
        public int OverdueCount { get; set; }
        public int DueSoonCount { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public List<HealthBucketDTO> Health { get; set; } = new();
        public List<MotorcycleKmStatDTO> KmByMotorcycle { get; set; } = new();
        public List<MaintenanceCountStatDTO> RecordsByType { get; set; } = new();
        public List<MonthlyActivityDTO> Last6Months { get; set; } = new();
    }

    public class HealthBucketDTO
    {
        /// <summary>Vencido | Crítico | Próximo | OK</summary>
        public string Bucket { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MotorcycleKmStatDTO
    {
        public Guid MotorcycleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Km { get; set; }
    }

    public class MaintenanceCountStatDTO
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MonthlyActivityDTO
    {
        /// <summary>Formato "yyyy-MM".</summary>
        public string YearMonth { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
