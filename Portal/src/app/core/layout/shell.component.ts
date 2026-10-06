import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
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
  { label: 'Rates', path: '/app/rates', exact: false },
];

/**
 * Authenticated portal frame.
 * Below `lg` the sidebar is an off-canvas drawer. From `lg` it stays open.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {
  private readonly auth = inject(AuthService);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  /** Tenant name from `/api/auth/me`. */
  readonly tenantName = inject(TenantStateService).name;

  /** Links the signed-in role is allowed to call. */
  readonly links = computed(() => (canManageSetup(this.auth.profile()?.role) ? setupLinks : [homeLink]));

  /** True while the phone drawer is open. The desktop sidebar ignores this. */
  readonly navOpen = signal(false);

  /** True from the `lg` breakpoint, where the sidebar is always visible. */
  readonly wide = signal(false);

  /** Element that had focus before the drawer opened. */
  private restoreFocus: HTMLElement | null = null;

  constructor() {
    this.watchBreakpoint();
    effect(() => {
      document.body.style.overflow = this.navOpen() && !this.wide() ? 'hidden' : '';
    });
    this.destroyRef.onDestroy(() => {
      document.body.style.overflow = '';
    });
  }

  /** Ends the session. */
  signOut(): void {
    this.auth.signOut();
  }

  /** Opens or closes the phone drawer. */
  toggleNav(): void {
    if (this.navOpen()) {
      this.closeNav();
      return;
    }
    this.openNav();
  }

  /** Slides the drawer in and moves focus to its close control. */
  openNav(): void {
    this.restoreFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    this.navOpen.set(true);
    afterNextRender(
      () => {
        const close = this.host.nativeElement.querySelector('#portal-nav-close');
        if (close instanceof HTMLElement) {
          close.focus();
        }
      },
      { injector: this.injector },
    );
  }

  /** Slides the drawer away and returns focus to the menu button. */
  closeNav(): void {
    if (!this.navOpen()) {
      return;
    }
    this.navOpen.set(false);
    const restore = this.restoreFocus;
    this.restoreFocus = null;
    afterNextRender(
      () => {
        restore?.focus();
      },
      { injector: this.injector },
    );
  }

  /** Closes the drawer from the keyboard without affecting the desktop sidebar. */
  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeNav();
  }

  /**
   * Keeps Tab inside the drawer while it covers the page.
   * The desktop sidebar is not a dialog, so it does not trap focus.
   */
  trapFocus(event: KeyboardEvent): void {
    if (event.key !== 'Tab' || !this.navOpen() || this.wide()) {
      return;
    }
    const root = event.currentTarget;
    if (!(root instanceof HTMLElement)) {
      return;
    }
    const focusable = [...root.querySelectorAll<HTMLElement>('a[href], button:not([disabled])')];
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (!first || !last) {
      return;
    }
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  /** Tracks `lg` so a resize does not leave the page marked as a dialog. */
  private watchBreakpoint(): void {
    if (typeof window.matchMedia !== 'function') {
      return;
    }
    const query = window.matchMedia('(min-width: 64rem)');
    const sync = () => {
      this.wide.set(query.matches);
      if (query.matches) {
        this.navOpen.set(false);
      }
    };
    sync();
    query.addEventListener('change', sync);
    this.destroyRef.onDestroy(() => query.removeEventListener('change', sync));
  }
}
