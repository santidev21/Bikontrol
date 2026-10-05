import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../../modules/auth/services/auth.service';

export const rootRedirectGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Already signed in → straight to the dashboard. Guests get the landing page.
  return authService.isAuthenticated() ? router.parseUrl('/dashboard') : true;
};
