import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';
import { canManageSetup } from './portal-role';

/**
 * Allows tenant admins and admins into academic setup.
 * Other portal roles return to Home. The shell also hides these links.
 */
export const setupGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return canManageSetup(auth.profile()?.role) ? true : router.createUrlTree(['/app']);
};
