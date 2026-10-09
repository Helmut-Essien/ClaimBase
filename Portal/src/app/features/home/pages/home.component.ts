import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { canManageSetup } from '../../../core/auth/portal-role';
import { TenantStateService } from '../../../core/tenant/tenant-state.service';
import { formatCalendarDate, zoneAbbreviation } from '../../../shared/dates/calendar-date';
import { AcademicApi } from '../../academic/data/academic.api';
import { PAGE_LIMITS } from '../../../shared/paging/page-limits';

/**
 * What this role should do next.
 * Claim and omitted-session counts stay at zero until those slices exist.
 */
@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeComponent {
  private readonly auth = inject(AuthService);
  private readonly semesters = inject(AcademicApi);
  private readonly tenant = inject(TenantStateService);
  private readonly destroyRef = inject(DestroyRef);

  /** Tenant admins and admins can open a semester. Other roles cannot call that API. */
  readonly canSetup = canManageSetup(this.auth.profile()?.role);

  /** Signed-in name for the home greeting. */
  readonly displayName = computed(() => this.auth.profile()?.displayName ?? '');

  /** Name of the open semester, once the list returns. */
  readonly openSemester = signal<string | null>(null);

  /** Inclusive dates of that semester, shown once the name is known. */
  readonly openSemesterSpan = signal<string | null>(null);

  /** True when an admin's semester list has no Open row. */
  readonly noOpenSemester = signal(false);

  /** True while an admin's semester list is loading. */
  readonly loadingSemester = signal(this.canSetup);

  /** Shown when the semester list fails. */
  readonly semesterError = signal<string | null>(null);

  /** Zone caption for the open semester's dates. Calendar days are not instants. */
  zoneCaption(): string {
    const id = this.tenant.timeZoneId();
    return `${id} (${zoneAbbreviation(id)})`;
  }

  constructor() {
    if (!this.canSetup) {
      return;
    }

    this.semesters
      .listSemesters(1, PAGE_LIMITS.maxSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          const open = result.items.find((semester) => semester.status === 'Open');
          this.openSemester.set(open?.name ?? null);
          this.openSemesterSpan.set(
            open ? `${formatCalendarDate(open.startDate)} – ${formatCalendarDate(open.endDate)}` : null,
          );
          this.noOpenSemester.set(!open);
          this.loadingSemester.set(false);
        },
        error: () => {
          this.loadingSemester.set(false);
          this.semesterError.set('Semesters could not be loaded.');
        },
      });
  }
}
