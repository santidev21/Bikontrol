import { HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { authInterceptor } from './auth.interceptor';
import { firstValueFrom, of } from 'rxjs';
import { environment } from '../../../environments/environment';

describe('authInterceptor', () => {
  afterEach(() => localStorage.clear());

  it('should add Authorization header when token exists', async () => {
    localStorage.setItem('token', 'my-token');
    const req = new HttpRequest('GET', '/api/test');
    const next: HttpHandlerFn = (r) => {
      expect(r.headers.get('Authorization')).toBe('Bearer my-token');
      return of(null as any);
    };

    await firstValueFrom(authInterceptor(req, next));
  });

  it('should not add header when no token', async () => {
    localStorage.clear();
    const req = new HttpRequest('GET', '/api/test');
    const next: HttpHandlerFn = (r) => {
      expect(r.headers.has('Authorization')).toBeFalsy();
      return of(null as any);
    };

    await firstValueFrom(authInterceptor(req, next));
  });

  it('should add the header for requests to the configured API url', async () => {
    localStorage.setItem('token', 'my-token');
    const req = new HttpRequest('GET', `${environment.apiUrl}/motorcycles`);
    const next: HttpHandlerFn = (r) => {
      expect(r.headers.get('Authorization')).toBe('Bearer my-token');
      return of(null as any);
    };

    await firstValueFrom(authInterceptor(req, next));
  });

  it('should never send the token to third-party urls', async () => {
    localStorage.setItem('token', 'my-token');
    const req = new HttpRequest('GET', 'https://example.com/api/payments');
    const next: HttpHandlerFn = (r) => {
      expect(r.headers.has('Authorization')).toBeFalsy();
      return of(null as any);
    };

    await firstValueFrom(authInterceptor(req, next));
  });
});
