import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/auth/auth.guards';

/** Portal routes. Later slices add children under `/app` only when those screens exist. */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'app',
    canActivate: [authGuard],
    loadComponent: () => import('./core/layout/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/home/pages/home.component').then((m) => m.HomeComponent),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('./core/not-found/not-found.component').then((m) => m.NotFoundComponent),
  },
];
