import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, tap, throwError } from 'rxjs';
import { environment } from '@env/environment';
import { ForgotPasswordResponse, LoginResponse, RegisterResponse, ResetPasswordResponse } from '../interfaces/auth.model';

const TOKEN_KEY = 'token';
const REFRESH_TOKEN_KEY = 'refreshToken';
const ROLE_KEY = 'role';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { email, password }).pipe(
      tap(response => this.storeSession(response))
    );
  }

  register(data: any): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.apiUrl}/register`, data).pipe(
      tap(response => this.storeSession(response))
    );
  }

  googleLogin(idToken: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/google`, { idToken }).pipe(
      tap(response => this.storeSession(response))
    );
  }

  demoLogin(): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/demo`, {}).pipe(
      tap(response => this.storeSession(response))
    );
  }

  forgotPassword(email: string): Observable<ForgotPasswordResponse> {
    return this.http.post<ForgotPasswordResponse>(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<ResetPasswordResponse> {
    return this.http.post<ResetPasswordResponse>(`${this.apiUrl}/reset-password`, { email, token, newPassword });
  }

  refreshSession(): Observable<boolean> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }
    return this.http.post<LoginResponse>(`${this.apiUrl}/refresh`, { refreshToken }).pipe(
      tap(response => this.storeSession(response)),
      map(() => true)
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(ROLE_KEY);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getRole(): string {
    const stored = localStorage.getItem(ROLE_KEY);
    if (stored) return stored;
    const token = this.getToken();
    if (!token) return 'User';
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.role ?? payload.Role ?? 'User';
    } catch {
      return 'User';
    }
  }

  isDemo(): boolean {
    return this.getRole() === 'Demo';
  }

  private storeSession(response: { token: string; refreshToken: string; role?: string }): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    const role = (response as any).role ?? this.decodeRole(response.token) ?? 'User';
    localStorage.setItem(ROLE_KEY, role);
  }

  private decodeRole(token: string): string | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.role ?? payload.Role ?? null;
    } catch {
      return null;
    }
  }
}