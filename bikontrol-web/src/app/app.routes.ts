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
      {  path: 'motorcycles/add',  loadComponent: () => import('./modules/dashboard/pages/motorcycles/add-motorcycle/add-motorcycle.component').then((m) => m.AddMotorcycleComponent)}
    ]
  },

  { path: '**', redirectTo: 'login' }
];
