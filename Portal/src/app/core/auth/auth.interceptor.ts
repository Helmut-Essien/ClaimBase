import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthSession } from './auth-session';

/**
 * Marks `GET /api/auth/me` from a route guard.
 * A rejected probe clears the token and lets the guard stay on the current URL, so a reset link is not discarded.
 */
export const SESSION_PROBE = new HttpContextToken<boolean>(() => false);

/**
 * Attaches the stored JWT.
 * A 401 on an authenticated call clears the whole session and returns to login.
 * Login, forgot-password, and reset-password stay anonymous so a stale token is not sent, and a rejected reset is not treated as an expired session.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(AuthSession);
  const router = inject(Router);
  const token = session.token();
  const anonymous = isAnonymousAuth(req.url);
  const outgoing =
    token && !anonymous ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(outgoing).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && token && !anonymous) {
        session.clear();
        if (!req.context.get(SESSION_PROBE)) {
          void router.navigateByUrl('/login');
        }
      }
      return throwError(() => error);
    }),
  );
};

/** Password endpoints must not carry a JWT. An invalid bearer token would turn the reset into a 401. */
function isAnonymousAuth(url: string): boolean {
  return (
    url.endsWith('/api/auth/login') ||
    url.endsWith('/api/auth/forgot-password') ||
    url.endsWith('/api/auth/reset-password')
  );
}
