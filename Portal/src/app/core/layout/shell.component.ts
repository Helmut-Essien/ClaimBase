import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { canManageSetup } from '../auth/portal-role';
import { TenantStateService } from '../tenant/tenant-state.service';

/** A shell link. Exact matching keeps Home from staying active on setup routes. */
interface ShellLink {
  label: string;
  path: string;
  exact: boolean;
}

const homeLink: ShellLink = { label: 'Home', path: '/app', exact: true };

/** Setup routes that exist. Later slices add their own links. */
const setupLinks: ShellLink[] = [
  homeLink,
  { label: 'Campuses', path: '/app/campuses', exact: false },
  { label: 'Faculties', path: '/app/faculties', exact: false },
  { label: 'Semesters', path: '/app/semesters', exact: false },
  { label: 'Courses', path: '/app/courses', exact: false },
  { label: 'Staff', path: '/app/staff', exact: false },
];

/**
 * Authenticated portal frame.
 * Below `lg` the routes sit in a bottom nav. From `lg` they sit in a fixed sidebar.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {
  private readonly auth = inject(AuthService);

  /** Tenant name from `/api/auth/me`. */
  readonly tenantName = inject(TenantStateService).name;

  /** Links the signed-in role is allowed to call. */
  readonly links = computed(() => (canManageSetup(this.auth.profile()?.role) ? setupLinks : [homeLink]));

  /** Ends the session. */
  signOut(): void {
    this.auth.signOut();
  }
}
