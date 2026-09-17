import { Routes } from '@angular/router';
import { authGuard } from './shared/guards/auth.guard';
import { guestGuard } from './shared/guards/guest.guard';
import { rootRedirectGuard } from './shared/guards/root-redirect.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', canActivate: [rootRedirectGuard], loadComponent: () => import('./modules/auth/pages/login/login.component').then(c => c.LoginComponent) },

  // Auth
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./modules/auth/pages/login/login.component').then(c => c.LoginComponent) },
  { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./modules/auth/pages/register/register.component').then(c => c.RegisterComponent) },
  { path: 'forgot-password', loadComponent: () => import('./modules/auth/pages/forgot-password/forgot-password.component').then(c => c.ForgotPasswordComponent) },
  { path: 'reset-password', loadComponent: () => import('./modules/auth/pages/reset-password/reset-password.component').then(c => c.ResetPasswordComponent) },

  // Dashboard
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./modules/dashboard/components/dashboard-layout/dashboard-layout.component').then(c => c.DashboardLayoutComponent),
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', loadComponent: () => import('./modules/dashboard/pages/home/home.component').then(c => c.HomeComponent) },
      {  path: 'motorcycles/summary',  loadComponent: () => import('./modules/dashboard/pages/motorcycles/motorcycle-summary/motorcycle-summary.component')
        .then((m) => m.MotorcycleSummaryComponent)},
      {  path: 'motorcycles/:motorcycleId/register-maintenance',  loadComponent: () => import('./modules/dashboard/pages/motorcycles/register-maintenance-record/register-maintenance-record.component')
        .then((m) => m.RegisterMaintenanceRecordComponent)},
      {  path: 'motorcycles/add',  loadComponent: () => import('./modules/dashboard/pages/motorcycles/save-motorcycle/save-motorcycle.component')
        .then((m) => m.SaveMotorcycleComponent)},
      { path: 'motorcycles/edit/:id', loadComponent: () => import('./modules/dashboard/pages/motorcycles/save-motorcycle/save-motorcycle.component')
        .then((m) => m.SaveMotorcycleComponent)},
      { path: 'motorcycles/:motorcycleId/maintenance', loadComponent: () => import('./modules/dashboard/pages/maintenance/maintenance-page/maintenance-page.component')
        .then(c => c.MaintenancePageComponent) },
      {  path: 'motorcycles/:motorcycleId/maintenance/add',  loadComponent: () => import('./modules/dashboard/pages/maintenance/save-maintenance/save-maintenance.component')
        .then((m) => m.SaveMaintenanceComponent)},
      { path: 'motorcycles/:motorcycleId/maintenance/edit/:id', loadComponent: () => import('./modules/dashboard/pages/maintenance/save-maintenance/save-maintenance.component')
        .then((m) => m.SaveMaintenanceComponent)},
      { path: 'statistics', loadComponent: () => import('./modules/dashboard/pages/statistics/statistics.component')
        .then((c) => c.StatisticsComponent)},
      { path: 'profile', loadComponent: () => import('./modules/dashboard/pages/profile/profile.component')
        .then((c) => c.ProfileComponent)},
      // Defensive fallback: unknown dashboard URLs go to home, never to login
      { path: '**', redirectTo: 'home' },
    ]
  },

  { path: '**', redirectTo: 'login' }
];
