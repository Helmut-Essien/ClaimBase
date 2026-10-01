import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

/** Sends an existing portal session to `/app`. Lecturers stay on the login screen. */
export const guestGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.token()) {
    return true;
  }
  const allowed = await auth.restore();
  return allowed ? router.createUrlTree(['/app']) : true;
};

/** Requires a portal JWT. Lecturers and missing tokens go back to `/login`. */
export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const allowed = await auth.restore();
  return allowed ? true : router.createUrlTree(['/login']);
};
