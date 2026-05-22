export interface Maintenance {
  id: string;
  motorcycleId: string;
  baseTypeId?: string | null;
  name: string;
  description?: string | null;
  kmInterval?: number | null;
  timeIntervalWeeks?: number | null;
  trackingType: 'Km' | 'Time';
  isEnabled: boolean;
  isSystem: boolean;
}

export interface SaveMaintenanceDTO {
  motorcycleId?: string;
  baseTypeId?: string;
  name: string;
  description?: string;
  kmInterval?: number;
  timeIntervalWeeks?: number;
  trackingType: 'Km' | 'Time';
}

export interface FollowMaintenancePayload {
  motorcycleId?: string;
  defaultId: string;
  trackingType: 'Km' | 'Time';
  kmInterval: number;
  timeIntervalWeeks: number;
}

export interface CreateMaintenanceRecordRequest {
  motorcycleId: string;
  userMaintenanceId: string;
  performedAt: string;
  performedKm?: number | null;
}

export interface MaintenanceRecord {
  id: string;
  motorcycleId: string;
  userMaintenanceId: string;
  performedAt: string;
  performedKm?: number | null;
  createdAt: string;
  maintenanceName: string;
}

export interface UpcomingMaintenance {
  userMaintenanceId: string;
  motorcycleId: string;
  name: string;
  description?: string | null;
  trackingType: 'Km' | 'Time';
  kmInterval?: number | null;
  timeIntervalWeeks?: number | null;
  remainingKm: number;
  remainingDays: number;
  lifePercent: number;
  isOverdue: boolean;
  lastPerformedAt?: string | null;
  lastPerformedKm?: number | null;
}
