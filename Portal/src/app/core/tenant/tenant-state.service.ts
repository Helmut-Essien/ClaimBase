import { Injectable, signal } from '@angular/core';

import { MeResponse } from '../auth/auth.models';

/**
 * Tenant name, currency, and time zone for the signed-in session.
 * Filled from `GET /api/auth/me`.
 */
@Injectable({ providedIn: 'root' })
export class TenantStateService {
  /** University name shown in the shell. */
  readonly name = signal('');

  /** ISO currency. Later money formatting reads this. */
  readonly currencyCode = signal('GHS');

  /** IANA zone used for calendar days. */
  readonly timeZoneId = signal('Africa/Accra');

  /** Copies the current-user payload into the shell. */
  apply(profile: MeResponse): void {
    this.name.set(profile.tenantName);
    this.currencyCode.set(profile.currencyCode);
    this.timeZoneId.set(profile.timeZoneId);
  }

  /** Resets the shell when the session ends. */
  clear(): void {
    this.name.set('');
    this.currencyCode.set('GHS');
    this.timeZoneId.set('Africa/Accra');
  }
}
