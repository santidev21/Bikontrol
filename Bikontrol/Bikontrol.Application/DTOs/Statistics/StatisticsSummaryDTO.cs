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

        /// <summary>Sum of all recorded maintenance costs (only records with a cost).</summary>
        public decimal TotalCost { get; set; }

        /// <summary>Fleet-average cost per km; null when total km is 0.</summary>
        public decimal? CostPerKm { get; set; }

        /// <summary>Sum of all recorded fuel-up costs.</summary>
        public decimal TotalFuelCost { get; set; }

        /// <summary>Total cost of ownership: fuel + maintenance.</summary>
        public decimal TotalCostOfOwnership { get; set; }

        /// <summary>Fleet-average fuel cost per km; null when total km is 0.</summary>
        public decimal? FuelCostPerKm { get; set; }

        /// <summary>Fleet-average consumption (km/l, full-to-full); null without data.</summary>
        public decimal? AverageKmPerLitre { get; set; }

        /// <summary>True when any motorcycle shows a sharp consumption drop.</summary>
        public bool HasConsumptionDrop { get; set; }

        /// <summary>Maintenance budget vs spend, with overspend flags.</summary>
        public BudgetStatusDTO Budget { get; set; } = new();

        public List<HealthBucketDTO> Health { get; set; } = new();
        public List<MotorcycleKmStatDTO> KmByMotorcycle { get; set; } = new();
        public List<MaintenanceCountStatDTO> RecordsByType { get; set; } = new();
        public List<MonthlyActivityDTO> Last6Months { get; set; } = new();
        public List<MotorcycleCostStatDTO> CostByMotorcycle { get; set; } = new();
        public List<YearlyCostDTO> CostByYear { get; set; } = new();
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

        /// <summary>Spend recorded in this month (only records with a cost).</summary>
        public decimal Cost { get; set; }
    }

    public class MotorcycleCostStatDTO
    {
        public Guid MotorcycleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Cost { get; set; }

        /// <summary>Cost per km for this motorcycle; null when its km is 0.</summary>
        public decimal? CostPerKm { get; set; }
    }

    public class YearlyCostDTO
    {
        public int Year { get; set; }
        public decimal Cost { get; set; }
    }

    /// <summary>
    /// Maintenance budget (monthly/annual) compared with the spend recorded in
    /// the current period. Budgets are optional; <c>null</c> disables the check.
    /// </summary>
    public class BudgetStatusDTO
    {
        public decimal? MonthlyBudget { get; set; }
        public decimal? AnnualBudget { get; set; }
        public decimal MonthlySpend { get; set; }
        public decimal AnnualSpend { get; set; }
        public bool IsMonthlyOverBudget { get; set; }
        public bool IsAnnualOverBudget { get; set; }
    }
}
