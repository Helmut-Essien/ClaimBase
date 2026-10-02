import { PortalRole } from './auth.models';

/**
 * Academic setup is limited to tenant admins and admins.
 * Heads of department and finance do not get those routes.
 */
export function canManageSetup(role: PortalRole | undefined): boolean {
  return role === 'TenantAdmin' || role === 'Admin';
}
