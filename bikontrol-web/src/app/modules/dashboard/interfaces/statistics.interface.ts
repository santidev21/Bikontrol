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

export interface StatisticsSummary {
  totalMotorcycles: number;
  totalKm: number;
  totalMaintenanceRecords: number;
  overdueCount: number;
  dueSoonCount: number;
  lastActivityAt?: string | null;
  totalCost: number;
  costPerKm?: number | null;
  health: HealthBucket[];
  kmByMotorcycle: MotorcycleKmStat[];
  recordsByType: MaintenanceCountStat[];
  last6Months: MonthlyActivity[];
  costByMotorcycle: MotorcycleCostStat[];
  costByYear: YearlyCost[];
}
