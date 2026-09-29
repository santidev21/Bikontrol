import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, shareReplay, switchMap, throwError } from 'rxjs';
import { AuthService } from '../../modules/auth/services/auth.service';

let sharedRefresh$: Observable<boolean> | null = null;

function getSharedRefresh(authService: AuthService): Observable<boolean> {
  if (!sharedRefresh$) {
    // shareReplay keeps concurrent 401s on a single POST /auth/refresh.
    // Without it every subscriber re-executes refreshSession() and mints
    // competing refresh tokens.
    sharedRefresh$ = authService.refreshSession().pipe(
      shareReplay({ bufferSize: 1, refCount: true }),
      finalize(() => {
        sharedRefresh$ = null;
      })
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

      return getSharedRefresh(authService).pipe(
        switchMap(ok => (ok ? next(req) : throwError(() => error))),
        catchError(() => {
          authService.logout();
          router.navigate(['/login']);
          return throwError(() => error);
        })
      );
    })
  );
};