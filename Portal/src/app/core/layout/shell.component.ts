import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { TenantStateService } from '../tenant/tenant-state.service';

/**
 * Authenticated portal frame. Only Home is linked until later slices add routes.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {
  private readonly auth = inject(AuthService);

  /** Tenant name from `/api/auth/me`. */
  readonly tenantName = inject(TenantStateService).name;

  /** Ends the session. */
  signOut(): void {
    this.auth.signOut();
  }
}
