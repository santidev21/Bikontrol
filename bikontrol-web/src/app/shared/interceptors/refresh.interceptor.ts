import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, map, shareReplay, switchMap, throwError } from 'rxjs';
import { AuthService } from '../../modules/auth/services/auth.service';

let sharedRefresh$: Observable<true> | null = null;

/**
 * One in-flight refresh serves every concurrent 401. The session teardown
 * (logout + redirect) lives inside the shared stream so it runs once, not once
 * per failed request, and `refCount: false` keeps the POST alive even if the
 * request that started it is cancelled (e.g. its component is destroyed):
 * aborting it midway would leave the rotated cookie and the stored token out
 * of sync.
 */
function getSharedRefresh(authService: AuthService, router: Router): Observable<true> {
  if (!sharedRefresh$) {
    sharedRefresh$ = authService.refreshSession().pipe(
      map((ok) => {
        if (!ok) throw new Error('refresh_rejected');
        return true as const;
      }),
      catchError((error) => {
        authService.logout();
        router.navigate(['/login']);
        return throwError(() => error);
      }),
      finalize(() => {
        sharedRefresh$ = null;
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
  }
  return sharedRefresh$;
}

function isAuthUrl(url: string): boolean {
  return url.includes('/auth/');
}

export const refreshInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (isAuthUrl(req.url) || !authService.isAuthenticated()) {
    return next(req);
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      return getSharedRefresh(authService, router).pipe(
        // A failed refresh already ended the session; callers just see the 401.
        catchError(() => throwError(() => error)),
        // Only a successful refresh reaches the retry, so its own errors
        // (400/404/409/500...) propagate untouched instead of logging the user out.
        switchMap(() => next(req)),
      );
    }),
  );
};
