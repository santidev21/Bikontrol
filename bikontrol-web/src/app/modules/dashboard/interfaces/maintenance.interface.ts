export interface Maintenance {
  id: string;
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
  baseTypeId?: string;
  name: string;
  description?: string;
  kmInterval?: number;
  timeIntervalWeeks?: number;
  trackingType: 'Km' | 'Time';
}

export interface FollowMaintenancePayload {
  defaultId: string;
  trackingType: 'Km' | 'Time';
  kmInterval: number;
  timeIntervalWeeks: number;
}
