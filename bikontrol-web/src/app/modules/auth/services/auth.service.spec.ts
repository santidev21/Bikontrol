import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, of } from 'rxjs';
import { AuthService } from './auth.service';

describe('AuthService (unit, mocked HttpClient)', () => {
  let service: AuthService;
  let mockHttp: any;

  beforeEach(() => {
    mockHttp = {
      post: vi.fn(),
    };
    // Default observable so fire-and-forget calls (logout) never explode.
    mockHttp.post.mockReturnValue(of({ message: 'ok' }));
    localStorage.clear();
    // The service uses inject(), so it must be created inside an injection context.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [{ provide: HttpClient, useValue: mockHttp }],
    });
    service = TestBed.runInInjectionContext(() => new AuthService());
  });

  afterEach(() => {
    vi.resetAllMocks();
    mockHttp.post.mockReturnValue(of({ message: 'ok' }));
    localStorage.clear();
  });

  it('login should call POST with credentials and store the access token', async () => {
    const mock: any = { token: 'abc123', refreshToken: '' };
    mockHttp.post.mockReturnValue(of(mock));

    const res = await firstValueFrom(service.login('a@b.com', 'pwd'));

    expect(res).toEqual(mock);
    expect(localStorage.getItem('token')).toBe('abc123');
    // The refresh token rides the HttpOnly cookie, never localStorage.
    expect(localStorage.getItem('refreshToken')).toBeNull();
    expect(service.isAuthenticated()).toBeTruthy();
    expect(mockHttp.post).toHaveBeenCalledWith(
      `${service['apiUrl']}/login`,
      { email: 'a@b.com', password: 'pwd' },
      expect.objectContaining({ withCredentials: true }),
    );
  });

  it('login should overwrite an existing session', async () => {
    localStorage.setItem('token', 'old-token');
    const mock: any = { token: 'new-token', refreshToken: '' };
    mockHttp.post.mockReturnValue(of(mock));

    await firstValueFrom(service.login('a@b.com', 'pwd'));

    expect(localStorage.getItem('token')).toBe('new-token');
  });

  it('register should call POST with credentials and store the access token', async () => {
    const mock: any = { token: 'reg-token', refreshToken: '' };
    mockHttp.post.mockReturnValue(of(mock));

    const data = { fullName: 'Test User', email: 'x@y.com', password: 'pw' };
    const res = await firstValueFrom(service.register(data));

    expect(res).toEqual(mock);
    expect(localStorage.getItem('token')).toBe('reg-token');
    expect(localStorage.getItem('refreshToken')).toBeNull();
    expect(mockHttp.post).toHaveBeenCalledWith(
      `${service['apiUrl']}/register`,
      data,
      expect.objectContaining({ withCredentials: true }),
    );
  });

  it('register should not store a session when email confirmation is required', async () => {
    const mock: any = {
      email: 'x@y.com',
      emailConfirmationRequired: true,
      token: '',
      refreshToken: '',
    };
    mockHttp.post.mockReturnValue(of(mock));

    await firstValueFrom(
      service.register({ fullName: 'Test User', email: 'x@y.com', password: 'pw' }),
    );

    expect(localStorage.getItem('token')).toBeNull();
    expect(service.isAuthenticated()).toBeFalsy();
  });

  it('confirmEmail should call POST /confirm-email', async () => {
    mockHttp.post.mockReturnValue(of({ message: 'ok' }));

    const res = await firstValueFrom(service.confirmEmail('a@b.com', 'tok'));

    expect(res.message).toBe('ok');
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}/confirm-email`, {
      email: 'a@b.com',
      token: 'tok',
    });
  });

  it('resendConfirmation should call POST /resend-confirmation', async () => {
    mockHttp.post.mockReturnValue(of({ message: 'ok' }));

    const res = await firstValueFrom(service.resendConfirmation('a@b.com'));

    expect(res.message).toBe('ok');
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}/resend-confirmation`, {
      email: 'a@b.com',
    });
  });

  it('googleLogin should call POST /google and store the session', async () => {
    const mock: any = { token: 'google-token', refreshToken: '' };
    mockHttp.post.mockReturnValue(of(mock));

    await firstValueFrom(service.googleLogin('id-token-123'));

    expect(localStorage.getItem('token')).toBe('google-token');
    expect(mockHttp.post).toHaveBeenCalledWith(
      `${service['apiUrl']}/google`,
      { idToken: 'id-token-123' },
      expect.objectContaining({ withCredentials: true }),
    );
  });

  it('forgotPassword should call POST /forgot-password', async () => {
    mockHttp.post.mockReturnValue(of({ message: 'ok' }));

    const res = await firstValueFrom(service.forgotPassword('a@b.com'));

    expect(res.message).toBe('ok');
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}/forgot-password`, {
      email: 'a@b.com',
    });
  });

  it('resetPassword should call POST /reset-password', async () => {
    mockHttp.post.mockReturnValue(of({ message: 'ok' }));

    const res = await firstValueFrom(service.resetPassword('a@b.com', 'tok', 'new-pass'));

    expect(res.message).toBe('ok');
    expect(mockHttp.post).toHaveBeenCalledWith(`${service['apiUrl']}/reset-password`, {
      email: 'a@b.com',
      token: 'tok',
      newPassword: 'new-pass',
    });
  });

  it('refreshSession should POST /refresh with the cookie and store the new access token', async () => {
    const mock: any = { token: 'new-token', refreshToken: '' };
    mockHttp.post.mockReturnValue(of(mock));

    const ok = await firstValueFrom(service.refreshSession());

    expect(ok).toBe(true);
    expect(localStorage.getItem('token')).toBe('new-token');
    expect(mockHttp.post).toHaveBeenCalledWith(
      `${service['apiUrl']}/refresh`,
      {},
      // The refresh rides the HttpOnly cookie and stays out of the loading overlay.
      expect.objectContaining({ withCredentials: true, context: expect.any(Object) }),
    );
  });

  it('logout should revoke server-side and clear the local session', () => {
    mockHttp.post.mockReturnValue(of({ message: 'Sesión cerrada.' }));
    localStorage.setItem('token', 't');
    localStorage.setItem('role', 'Demo');

    service.logout();

    expect(mockHttp.post).toHaveBeenCalledWith(
      `${service['apiUrl']}/logout`,
      {},
      expect.objectContaining({ withCredentials: true }),
    );
    expect(localStorage.getItem('token')).toBeNull();
    expect(localStorage.getItem('role')).toBeNull();
  });

  it('getToken and isAuthenticated', () => {
    expect(service.getToken()).toBeNull();
    expect(service.isAuthenticated()).toBeFalsy();
    localStorage.setItem('token', 'tok');
    expect(service.getToken()).toBe('tok');
    expect(service.isAuthenticated()).toBeTruthy();
  });

  it('isAuthenticated should return false when storage is empty', () => {
    expect(service.isAuthenticated()).toBeFalsy();
  });

  it('demoLogin should call POST /demo and store session with Demo role', async () => {
    const mock: any = {
      token: btoa('h') + '.' + btoa(JSON.stringify({ role: 'Demo' })) + '.' + btoa('s'),
      refreshToken: '',
      role: 'Demo',
    };
    mockHttp.post.mockReturnValue(of(mock));

    await firstValueFrom(service.demoLogin());

    expect(localStorage.getItem('token')).toBe(mock.token);
    expect(localStorage.getItem('role')).toBe('Demo');
    expect(service.isDemo()).toBeTruthy();
    expect(service.getRole()).toBe('Demo');
    expect(mockHttp.post).toHaveBeenCalledWith(
      `${service['apiUrl']}/demo`,
      {},
      expect.objectContaining({ withCredentials: true }),
    );
  });

  it('isDemo should decode role from JWT when localStorage empty', () => {
    const payload = btoa(JSON.stringify({ role: 'Demo' }));
    const token = `header.${payload}.sig`;
    localStorage.setItem('token', token);
    expect(service.isDemo()).toBeTruthy();
    expect(service.getRole()).toBe('Demo');
  });

  it('logout should clear the stored role', () => {
    localStorage.setItem('role', 'Demo');
    service.logout();
    expect(localStorage.getItem('role')).toBeNull();
  });
});
