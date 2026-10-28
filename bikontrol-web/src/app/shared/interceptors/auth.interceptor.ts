import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

/** The bearer token only travels to our own API, never to third-party URLs. */
function isApiRequest(url: string): boolean {
  return url.startsWith(environment.apiUrl) || /^\/api(\/|$)/.test(url);
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token');

  if (token && isApiRequest(req.url)) {
    const cloned = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
    return next(cloned);
  }

  return next(req);
};
