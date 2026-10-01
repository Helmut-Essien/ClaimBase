/** Roles the API returns. Lecturers are sent to the mobile app. */
export type PortalRole = 'TenantAdmin' | 'Admin' | 'HeadOfDepartment' | 'Finance' | 'Lecturer';

/** Login response. The portal stores `token` only for a non-lecturer role. */
export interface AuthResponse {
  token: string;
  expiresAt: string;
  tenantId: string;
  tenantName: string;
  userId: string;
  email: string;
  displayName: string;
  role: PortalRole;
  currencyCode: string;
}

/** Current user from `GET /api/auth/me`, including the tenant time zone. */
export interface MeResponse {
  tenantId: string;
  tenantName: string;
  userId: string;
  email: string;
  displayName: string;
  role: PortalRole;
  currencyCode: string;
  timeZoneId: string;
}

/**
 * Bounds shared with the API login contract.
 * Email 320, password 8–128.
 */
export const AUTH_FIELD_LIMITS = {
  email: 320,
  passwordMin: 8,
  passwordMax: 128,
  displayName: 200,
} as const;
