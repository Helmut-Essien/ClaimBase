import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { PAGE_LIMITS } from '../../../../shared/paging/page-limits';
import { PagerComponent } from '../../../../shared/paging/pager.component';
import { readApiError } from '../../../../shared/http/read-api-error';
import { requiredTrimmed } from '../../../../shared/validators/required-trimmed';
import { AcademicApi } from '../../data/academic.api';
import { ACADEMIC_FIELD_LIMITS, Campus } from '../../data/academic.models';

/**
 * Campus catalog. Names are unique in the tenant. Semesters and rates are not chosen here.
 */
@Component({
  selector: 'app-campuses',
  imports: [ReactiveFormsModule, PagerComponent],
  templateUrl: './campuses.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CampusesComponent {
  private readonly api = inject(AcademicApi);
  private readonly destroyRef = inject(DestroyRef);
  private loadGeneration = 0;

  /** Shared with `AcademicFieldLimits.Name`. */
  readonly limits = ACADEMIC_FIELD_LIMITS;

  /** Rows on the current page. */
  readonly items = signal<Campus[]>([]);

  /** 1-based page. */
  readonly page = signal(1);

  /** Page size sent to the API. */
  readonly pageSize = PAGE_LIMITS.defaultSize;

  /** Total campuses in the tenant. */
  readonly totalCount = signal(0);

  /** True while the list request is in flight and nothing is on screen yet. */
  readonly loading = signal(true);

  /** True while create is in flight. The button then reads "Please wait…". */
  readonly submitting = signal(false);

  /** API message for a failed create, including a duplicate name. */
  readonly errorMessage = signal<string | null>(null);

  /** API message when the list itself fails. */
  readonly loadError = signal<string | null>(null);

  readonly form = inject(FormBuilder).nonNullable.group({
    name: ['', [requiredTrimmed(), Validators.maxLength(ACADEMIC_FIELD_LIMITS.name)]],
  });

  constructor() {
    this.load(1);
  }

  /** Loads one page of campuses. */
  load(page: number): void {
    const generation = ++this.loadGeneration;
    this.loading.set(true);
    this.loadError.set(null);
    this.api
      .listCampuses(page, this.pageSize)
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
          this.loadError.set(readApiError(error, 'Campuses could not be loaded.'));
        },
      });
  }

  /** Creates a campus and reloads the first page so the new name appears in order. */
  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const name = this.form.controls.name.value.trim();
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.api
      .createCampus({ name })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.form.reset();
          this.load(1);
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.errorMessage.set(readApiError(error, 'The campus could not be saved.'));
        },
      });
  }
}
