import { Routes } from '@angular/router';

export const routes: Routes = [
  // Auth
  { path: 'login', loadComponent: () => import('./modules/auth/pages/login/login.component').then(c => c.LoginComponent) },
  { path: 'register', loadComponent: () => import('./modules/auth/pages/register/register.component').then(c => c.RegisterComponent) },

  // Dashboard
  {
    path: 'dashboard',
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
    ]
  },

  { path: '**', redirectTo: 'login' }
];
