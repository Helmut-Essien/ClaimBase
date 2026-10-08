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
 * Bounds shared with `AuthFieldLimits` and the password-reset token column.
 * Email 320, password 8–128, reset token 128 on the wire.
 */
export const AUTH_FIELD_LIMITS = {
  email: 320,
  passwordMin: 8,
  passwordMax: 128,
  resetToken: 128,
  displayName: 200,
} as const;

/** Copy returned by the password-reset API. The forgot message does not say whether the email exists. */
export const PASSWORD_RESET_COPY = {
  linkSent: 'If that email belongs to a staff account, a reset link is on its way.',
  reset: 'Password has been reset successfully.',
  invalidToken: 'Invalid reset token.',
  invalidLink: 'This reset link is invalid. Request a new one.',
} as const;

/** Body returned by `POST /api/auth/forgot-password` and `POST /api/auth/reset-password`. */
export interface PasswordResetMessage {
  message: string;
}
