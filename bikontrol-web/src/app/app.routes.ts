import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: 'login', loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule) },
  { path: '**', redirectTo: 'login' }
];
