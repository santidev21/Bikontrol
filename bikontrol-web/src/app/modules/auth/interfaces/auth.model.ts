export interface AuthSession {
  id: string;
  email: string;
  fullName: string;
  role: string;
  token: string;
  refreshToken: string;
  expiresIn: number;
}

export type LoginResponse = AuthSession;

export interface RegisterResponse extends AuthSession {
  createdAt: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface ForgotPasswordResponse {
  message: string;
}

export interface ResetPasswordResponse {
  message: string;
}