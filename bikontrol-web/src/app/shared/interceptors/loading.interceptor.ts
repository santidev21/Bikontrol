import { HttpContextToken, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';

/**
 * Set this context token to `true` on a request to keep it out of the global
 * loading overlay. Use it for silent/background calls (for example the token
 * refresh) that must not raise a blocking spinner.
 */
export const SKIP_LOADING = new HttpContextToken<boolean>(() => false);

/**
 * Feeds the global `LoadingService` from every blocking HTTP request. Safe reads
 * (GET) are left out: those render their own skeletons or inline spinners, so a
 * full-screen overlay on every navigation would be noise. `finalize` runs on
 * success, error and cancellation, so the counter is always balanced.
 */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method === 'GET' || req.context.get(SKIP_LOADING)) {
    return next(req);
  }

  const loading = inject(LoadingService);
  loading.begin();

  return next(req).pipe(finalize(() => loading.end()));
};
