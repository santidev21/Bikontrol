import { Maintenance, MaintenanceAttachment, MaintenanceRecord } from './maintenance.interface';
import { Motorcycle } from './motorcycle.interface';

export interface Profile {
  id: string;
  email: string;
  fullName: string;
  role: string;
  createdAt: string;
  hasPassword: boolean;
  remindersEnabled: boolean;
}

export interface UpdateProfileRequest {
  fullName: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

/**
 * Full "download my data" export (see `GET /api/users/me/export`). The profile
 * page only downloads it; it is not rendered field by field.
 */
export interface UserDataExport {
  exportedAt: string;
  profile: Profile;
  motorcycles: Motorcycle[];
  maintenances: Maintenance[];
  maintenanceRecords: MaintenanceRecord[];
  attachments: MaintenanceAttachment[];
}
