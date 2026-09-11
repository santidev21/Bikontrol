import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, switchMap, throwError } from 'rxjs';
import { AuthService } from '../../modules/auth/services/auth.service';

let sharedRefresh$: Observable<boolean> | null = null;

function getSharedRefresh(authService: AuthService): Observable<boolean> {
  if (!sharedRefresh$) {
    sharedRefresh$ = authService.refreshSession().pipe(
      finalize(() => {
        sharedRefresh$ = null;
      })
    );
  }
  return sharedRefresh$;
}

function isAuthUrl(url: string): boolean {
  return url.includes('/auth/login') || url.includes('/auth/refresh') || url.includes('/auth/google');
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