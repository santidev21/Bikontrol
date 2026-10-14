export interface HealthBucket {
  bucket: string;
  count: number;
}

export interface MotorcycleKmStat {
  motorcycleId: string;
  name: string;
  km: number;
}

export interface MaintenanceCountStat {
  name: string;
  count: number;
}

export interface MonthlyActivity {
  yearMonth: string;
  count: number;
  cost: number;
}

export interface MotorcycleCostStat {
  motorcycleId: string;
  name: string;
  cost: number;
  costPerKm?: number | null;
}

export interface YearlyCost {
  year: number;
  cost: number;
}

/** Monthly/annual maintenance budget vs. actual spend for the current user. */
export interface BudgetStatus {
  monthlyBudget?: number | null;
  annualBudget?: number | null;
  monthlySpend: number;
  annualSpend: number;
  isMonthlyOverBudget: boolean;
  isAnnualOverBudget: boolean;
}

export interface StatisticsSummary {
  totalMotorcycles: number;
  totalKm: number;
  totalMaintenanceRecords: number;
  overdueCount: number;
  dueSoonCount: number;
  lastActivityAt?: string | null;
  totalCost: number;
  costPerKm?: number | null;
  totalFuelCost: number;
  totalCostOfOwnership: number;
  fuelCostPerKm?: number | null;
  averageKmPerLitre?: number | null;
  hasConsumptionDrop: boolean;
  budget: BudgetStatus;
  health: HealthBucket[];
  kmByMotorcycle: MotorcycleKmStat[];
  recordsByType: MaintenanceCountStat[];
  last6Months: MonthlyActivity[];
  costByMotorcycle: MotorcycleCostStat[];
  costByYear: YearlyCost[];
}
