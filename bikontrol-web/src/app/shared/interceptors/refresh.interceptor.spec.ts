import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { tap } from 'rxjs/operators';
import { AuthService } from '../../modules/auth/services/auth.service';
import { authInterceptor } from './auth.interceptor';
import { refreshInterceptor } from './refresh.interceptor';

describe('refreshInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: any;
  let router: any;

  beforeEach(() => {
    localStorage.setItem('token', 'access-token');
    localStorage.setItem('refreshToken', 'refresh-token');

    authService = {
      getToken: vi.fn(() => localStorage.getItem('token')),
      isAuthenticated: vi.fn().mockReturnValue(true),
      refreshSession: vi.fn().mockImplementation(() => {
        localStorage.setItem('token', 'new-access-token');
        localStorage.setItem('refreshToken', 'new-refresh-token');
        return of(true);
      }),
      logout: vi.fn(),
    };
    router = {
      navigate: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([refreshInterceptor, authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should refresh the session and retry the original request once on 401', () => {
    let response: any;

    http.get('/api/motorcycles').subscribe((res) => {
      response = res;
    });

    // First attempt: 401.
    const first = httpMock.expectOne('/api/motorcycles');
    expect(first.request.headers.get('Authorization')).toBe('Bearer access-token');
    first.flush({ error: 'unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(authService.refreshSession).toHaveBeenCalledTimes(1);

    // Retry after refresh must carry the NEW token (re-added by authInterceptor).
    const retry = httpMock.expectOne('/api/motorcycles');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-access-token');
    retry.flush([{ id: 1 }]);

    expect(response).toEqual([{ id: 1 }]);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should logout and redirect to login when the refresh fails', () => {
    authService.refreshSession.mockReturnValue(throwError(() => new Error('refresh failed')));

    http.get('/api/motorcycles').subscribe({ error: () => {} });

    const first = httpMock.expectOne('/api/motorcycles');
    first.flush({ error: 'unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    // No retry happens because refresh failed.
    expect(authService.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should share a single refresh across concurrent 401s', () => {
    // Deferred refresh: both 401s must attach before the refresh completes.
    const refreshSubject = new Subject<boolean>();
    authService.refreshSession.mockReturnValue(
      refreshSubject.asObservable().pipe(
        tap((ok) => {
          if (ok) {
            localStorage.setItem('token', 'new-access-token');
            localStorage.setItem('refreshToken', 'new-refresh-token');
          }
        }),
      ),
    );

    let firstResponse: any;
    let secondResponse: any;

    http.get('/api/motorcycles/mine').subscribe((res) => {
      firstResponse = res;
    });
    http.get('/api/maintenances/mine').subscribe((res) => {
      secondResponse = res;
    });

    const first = httpMock.expectOne('/api/motorcycles/mine');
    const second = httpMock.expectOne('/api/maintenances/mine');
    first.flush({ error: 'unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    second.flush({ error: 'unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    // A single shared refresh must serve both retries.
    expect(authService.refreshSession).toHaveBeenCalledTimes(1);

    refreshSubject.next(true);
    refreshSubject.complete();

    const retryFirst = httpMock.expectOne('/api/motorcycles/mine');
    retryFirst.flush([{ id: 1 }]);
    const retrySecond = httpMock.expectOne('/api/maintenances/mine');
    retrySecond.flush([{ id: 2 }]);

    expect(firstResponse).toEqual([{ id: 1 }]);
    expect(secondResponse).toEqual([{ id: 2 }]);
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
