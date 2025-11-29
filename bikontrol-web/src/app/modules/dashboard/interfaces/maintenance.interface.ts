export interface MaintenanceType {
  id: string;
  baseTypeId?: string | null;
  name: string;
  description?: string | null;
  kmInterval?: number | null;
  timeIntervalWeeks?: number | null;
  isEnabled: boolean;
  isSystem: boolean;
}