import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { environment } from '@env/environment';
import { SKIP_LOADING } from '../../../shared/interceptors/loading.interceptor';
import {
  ForgotPasswordResponse,
  LoginResponse,
  MessageResponse,
  RegisterRequest,
  RegisterResponse,
  ResetPasswordResponse,
} from '../interfaces/auth.model';

const TOKEN_KEY = 'token';
const ROLE_KEY = 'role';

/** The refresh token rides an HttpOnly cookie: auth calls need credentials so
 *  the browser stores and attaches it (they are cross-origin in development). */
const CREDENTIALS = { withCredentials: true };

/** Silent + credentialed: session calls must not raise the loading overlay. */
const silentContext = () => ({
  context: new HttpContext().set(SKIP_LOADING, true),
  withCredentials: true,
});

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);

  private apiUrl = `${environment.apiUrl}/auth`;

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/login`, { email, password }, CREDENTIALS)
      .pipe(tap((response) => this.storeSession(response)));
  }

  register(data: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.apiUrl}/register`, data, CREDENTIALS).pipe(
      tap((response) => {
        // When email confirmation is required there is no session yet.
        if (response.token) {
          this.storeSession(response);
        }
      }),
    );
  }

  confirmEmail(email: string, token: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.apiUrl}/confirm-email`, { email, token });
  }

  /** One-click unsubscribe from reminder emails; the signed token comes from the email link. */
  unsubscribeReminderEmails(token: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/reminders/email/unsubscribe`, null, {
      params: { token },
    });
  }

  resendConfirmation(email: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.apiUrl}/resend-confirmation`, { email });
  }

  googleLogin(idToken: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/google`, { idToken }, CREDENTIALS)
      .pipe(tap((response) => this.storeSession(response)));
  }

  demoLogin(): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/demo`, {}, CREDENTIALS)
      .pipe(tap((response) => this.storeSession(response)));
  }

  forgotPassword(email: string): Observable<ForgotPasswordResponse> {
    return this.http.post<ForgotPasswordResponse>(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(
    email: string,
    token: string,
    newPassword: string,
  ): Observable<ResetPasswordResponse> {
    return this.http.post<ResetPasswordResponse>(`${this.apiUrl}/reset-password`, {
      email,
      token,
      newPassword,
    });
  }

  refreshSession(): Observable<boolean> {
    // The refresh token rides the HttpOnly cookie; the body stays empty.
    return this.http.post<LoginResponse>(`${this.apiUrl}/refresh`, {}, silentContext()).pipe(
      tap((response) => this.storeSession(response)),
      map(() => true),
    );
  }

  logout(): void {
    // Best-effort revocation: the HttpOnly cookie (and the server-side token)
    // cannot be cleared from JS, so the server does it. Fire-and-forget: the
    // local session is dropped either way.
    this.http.post<MessageResponse>(`${this.apiUrl}/logout`, {}, silentContext()).subscribe({
      error: () => {},
    });
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(ROLE_KEY);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
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

  private storeSession(response: { token: string; role?: string }): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    const role = response.role ?? this.decodeRole(response.token) ?? 'User';
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
