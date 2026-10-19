export interface AuthSession {
  id: string;
  email: string;
  fullName: string;
  role: string;
  token: string;
  // The refresh token rides an HttpOnly cookie scoped to /api/auth; the body
  // field stays empty and exists only for script/tooling compatibility.
  refreshToken?: string;
  expiresIn: number;
}

export type LoginResponse = AuthSession;

export interface RegisterResponse extends AuthSession {
  createdAt: string;
  // True when the account must confirm its email before logging in; in that
  // case `token` is empty and no session is stored.
  emailConfirmationRequired: boolean;
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

export interface MessageResponse {
  message: string;
}
