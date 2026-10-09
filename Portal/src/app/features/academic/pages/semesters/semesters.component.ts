import { ChangeDetectionStrategy, Component, DestroyRef, Injector, afterNextRender, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';

import { TenantStateService } from '../../../../core/tenant/tenant-state.service';
import { formatCalendarDate, zoneAbbreviation } from '../../../../shared/dates/calendar-date';
import { readApiError } from '../../../../shared/http/read-api-error';
import { PAGE_LIMITS } from '../../../../shared/paging/page-limits';
import { PagerComponent } from '../../../../shared/paging/pager.component';
import { requiredTrimmed } from '../../../../shared/validators/required-trimmed';
import { AcademicApi } from '../../data/academic.api';
import { ACADEMIC_FIELD_LIMITS, Semester } from '../../data/academic.models';

/**
 * Semester catalog. Draft can be opened, open can be closed, and closed stays visible and read-only.
 * Opening is refused when another open semester covers the same dates.
 */
@Component({
  selector: 'app-semesters',
  imports: [ReactiveFormsModule, PagerComponent],
  templateUrl: './semesters.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SemestersComponent {
  private readonly api = inject(AcademicApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly tenant = inject(TenantStateService);
  private readonly injector = inject(Injector);
  private loadGeneration = 0;

  /** Shared with `AcademicFieldLimits.Name`. */
  readonly limits = ACADEMIC_FIELD_LIMITS;

  readonly pageSize = PAGE_LIMITS.defaultSize;

  /** Rows on the current page. */
  readonly items = signal<Semester[]>([]);

  /** 1-based page. */
  readonly page = signal(1);

  /** Total semesters. */
  readonly totalCount = signal(0);

  /** True while the first list request is in flight. */
  readonly loading = signal(true);

  /** True while create or update is in flight. */
  readonly submitting = signal(false);

  /** Semester id whose open or close request is in flight. */
  readonly busyId = signal<string | null>(null);

  /** Id being edited. Null means the form creates a draft. */
  readonly editingId = signal<string | null>(null);

  /** Name captured when Edit is pressed. The live region must not follow each keystroke. */
  readonly editingName = signal<string | null>(null);

  /** Create, update, open, or close failure. A 409 overlap stays here. */
  readonly errorMessage = signal<string | null>(null);

  /** List failure. */
  readonly loadError = signal<string | null>(null);

  readonly form = inject(FormBuilder).nonNullable.group(
    {
      name: ['', [requiredTrimmed(), Validators.maxLength(ACADEMIC_FIELD_LIMITS.name)]],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
    },
    { validators: [endOnOrAfterStart()] },
  );

  constructor() {
    this.load(1);
  }

  /** Zone caption shown once. Calendar days are not instants. */
  zoneCaption(): string {
    const id = this.tenant.timeZoneId();
    return `${id} (${zoneAbbreviation(id)})`;
  }

  /** Formats a semester day. */
  formatDate(value: string): string {
    return formatCalendarDate(value);
  }

  /** True when this row is a closed semester and must not be edited. */
  isClosed(semester: Semester): boolean {
    return semester.status === 'Closed';
  }

  /** Loads one page of semesters. */
  load(page: number): void {
    const generation = ++this.loadGeneration;
    this.loading.set(true);
    this.loadError.set(null);
    this.api
      .listSemesters(page, this.pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (generation !== this.loadGeneration) {
            return;
          }
          this.items.set(result.items);
          this.page.set(result.page);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          if (generation !== this.loadGeneration) {
            return;
          }
          this.loading.set(false);
          this.loadError.set(readApiError(error, 'Semesters could not be loaded.'));
        },
      });
  }

  /** Fills the form for a draft or open semester. Closed rows have no edit action. */
  edit(semester: Semester): void {
    if (semester.status === 'Closed') {
      return;
    }
    this.editingId.set(semester.id);
    this.editingName.set(semester.name);
    this.errorMessage.set(null);
    this.form.setValue({
      name: semester.name,
      startDate: semester.startDate.slice(0, 10),
      endDate: semester.endDate.slice(0, 10),
    });
    // The list scrolls away. Keep the keyboard on the field that is now being edited.
    const name = document.getElementById('semester-name');
    if (name instanceof HTMLElement) {
      name.focus({ preventScroll: true });
    }
    this.revealForm();
  }

  /** The form sits above the list. Edit on a lower row would otherwise leave the filled fields off screen. */
  private revealForm(): void {
    afterNextRender(
      () => {
        const form = document.getElementById('semester-form');
        if (form && typeof form.scrollIntoView === 'function') {
          form.scrollIntoView({ block: 'start' });
        }
      },
      { injector: this.injector },
    );
  }

  /** Returns the form to creating a draft. */
  cancelEdit(): void {
    this.editingId.set(null);
    this.editingName.set(null);
    this.errorMessage.set(null);
    this.form.reset();
  }

  /** Creates a draft or updates a semester that is not closed. */
  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const name = this.form.controls.name.value.trim();
    const startDate = this.form.controls.startDate.value;
    const endDate = this.form.controls.endDate.value;
    const editingId = this.editingId();
    this.submitting.set(true);
    this.errorMessage.set(null);
    const request = editingId
      ? this.api.updateSemester({ id: editingId, name, startDate, endDate })
      : this.api.createSemester({ name, startDate, endDate });

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.cancelEdit();
        this.load(editingId ? this.page() : 1);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(readApiError(error, 'The semester could not be saved.'));
      },
    });
  }

  /** Opens a draft. An overlapping open semester stays on this screen as a 409. */
  open(semester: Semester): void {
    if (semester.status !== 'Draft' || this.busyId()) {
      return;
    }
    this.busyId.set(semester.id);
    this.errorMessage.set(null);
    this.api
      .openSemester(semester.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busyId.set(null);
          this.load(this.page());
        },
        error: (error: unknown) => {
          this.busyId.set(null);
          this.errorMessage.set(readApiError(error, 'The semester could not be opened.'));
        },
      });
  }

  /** Closes an open semester. It then stays on the list and cannot be edited. */
  close(semester: Semester): void {
    if (semester.status !== 'Open' || this.busyId()) {
      return;
    }
    this.busyId.set(semester.id);
    this.errorMessage.set(null);
    this.api
      .closeSemester(semester.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busyId.set(null);
          if (this.editingId() === semester.id) {
            this.cancelEdit();
          }
          this.load(this.page());
        },
        error: (error: unknown) => {
          this.busyId.set(null);
          this.errorMessage.set(readApiError(error, 'The semester could not be closed.'));
        },
      });
  }
}

/** Rejects an end date before the start. Both days are inclusive. */
function endOnOrAfterStart(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const start = control.get('startDate')?.value;
    const end = control.get('endDate')?.value;
    if (typeof start !== 'string' || typeof end !== 'string' || start.length === 0 || end.length === 0) {
      return null;
    }
    return end < start ? { dateOrder: true } : null;
  };
}
