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
