import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/auth/auth.guards';
import { setupGuard } from './core/auth/setup.guard';

/**
 * Portal routes. Feature pages are lazy.
 * Academic setup is limited to tenant admins and admins.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'login/forgot-password',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/pages/forgot-password/forgot-password.component').then((m) => m.ForgotPasswordComponent),
  },
  {
    path: 'login/reset-password',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/pages/reset-password/reset-password.component').then((m) => m.ResetPasswordComponent),
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
      {
        path: 'campuses',
        canActivate: [setupGuard],
        loadComponent: () =>
          import('./features/academic/pages/campuses/campuses.component').then((m) => m.CampusesComponent),
      },
      {
        path: 'faculties',
        canActivate: [setupGuard],
        loadComponent: () =>
          import('./features/academic/pages/faculties/faculties.component').then((m) => m.FacultiesComponent),
      },
      {
        path: 'semesters',
        canActivate: [setupGuard],
        loadComponent: () =>
          import('./features/academic/pages/semesters/semesters.component').then((m) => m.SemestersComponent),
      },
      {
        path: 'courses',
        canActivate: [setupGuard],
        loadComponent: () =>
          import('./features/academic/pages/courses/courses.component').then((m) => m.CoursesComponent),
      },
      {
        path: 'staff',
        canActivate: [setupGuard],
        loadComponent: () => import('./features/staff/pages/staff.component').then((m) => m.StaffComponent),
      },
      {
        path: 'rates',
        canActivate: [setupGuard],
        loadComponent: () => import('./features/rates/pages/rates/rates.component').then((m) => m.RatesComponent),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('./core/not-found/not-found.component').then((m) => m.NotFoundComponent),
  },
];
