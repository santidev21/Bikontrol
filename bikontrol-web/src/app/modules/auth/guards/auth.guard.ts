import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  const authService = inject(AuthService);

  const token = localStorage.getItem('token');

  if (token && !authService.isTokenExpired()) {
    return true;
  }

  authService.logout();
  return false;
};