export interface LoginResponse {
  id: string;
  email: string;
  fullName: string;
  token: string;
}

export interface RegisterResponse {
  id: string;
  email: string;
  fullName: string;
  createdAt: string;
  token: string;
}