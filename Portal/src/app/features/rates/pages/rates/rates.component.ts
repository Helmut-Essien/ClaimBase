import { ChangeDetectionStrategy, Component, DestroyRef, Injector, afterNextRender, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AcademicApi } from '../../../academic/data/academic.api';
import { PositionTitle, Qualification } from '../../../academic/data/academic.models';
import { TenantStateService } from '../../../../core/tenant/tenant-state.service';
import { formatCalendarDate, zoneAbbreviation } from '../../../../shared/dates/calendar-date';
import { readApiError } from '../../../../shared/http/read-api-error';
import { PAGE_LIMITS } from '../../../../shared/paging/page-limits';
import { PagerComponent } from '../../../../shared/paging/pager.component';
import { requiredTrimmed } from '../../../../shared/validators/required-trimmed';
import { RatesApi } from '../../data/rates.api';
import { calendarToday, cedisAmount, formatMoney, rateInForce } from '../../data/rate-schedule';
import { TeachingRate, TransportRate } from '../../data/rates.models';

/** One matrix cell: the qualification and the amount in force today, if any. */
interface MatrixCell {
  qualification: Qualification;
  rate: TeachingRate | null;
}

/** One matrix row: a position title and a cell for each qualification. */
interface MatrixRow {
  title: PositionTitle;
  cells: MatrixCell[];
}

/**
 * University teaching matrix and transport timeline.
 * Saving posts one new row. An open end keeps that amount in later semesters.
 * A 409 overlap stays on the form and leaves the other cells in place.
 */
@Component({
  selector: 'app-rates',
  imports: [ReactiveFormsModule, RouterLink, PagerComponent],
  templateUrl: './rates.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RatesComponent {
  private readonly ratesApi = inject(RatesApi);
  private readonly academicApi = inject(AcademicApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly tenant = inject(TenantStateService);
  private readonly injector = inject(Injector);
  private matrixGeneration = 0;
  private historyGeneration = 0;
  private transportGeneration = 0;

  /** Stops a runaway follow of in-force pages. One page already covers a normal matrix. */
  private readonly maxInForcePages = 10;

  readonly pageSize = PAGE_LIMITS.defaultSize;
  readonly lookupSize = PAGE_LIMITS.maxSize;

  readonly titles = signal<PositionTitle[]>([]);
  readonly qualifications = signal<Qualification[]>([]);
  readonly titlesTruncated = signal(false);
  readonly qualificationsTruncated = signal(false);
  readonly loadingCatalog = signal(true);
  readonly catalogError = signal<string | null>(null);
  private catalogPending = 2;

  /** Teaching rows in force today. History before today is not part of a cell. */
  readonly matrixRates = signal<TeachingRate[]>([]);
  readonly matrixTruncated = signal(false);
  readonly loadingMatrix = signal(true);
  readonly matrixError = signal<string | null>(null);

  readonly history = signal<TeachingRate[]>([]);
  readonly historyPage = signal(1);
  readonly historyTotal = signal(0);
  readonly loadingHistory = signal(false);
  readonly historyError = signal<string | null>(null);

  readonly transport = signal<TransportRate[]>([]);
  readonly transportPage = signal(1);
  readonly transportTotal = signal(0);
  readonly loadingTransport = signal(true);
  readonly transportLoadError = signal<string | null>(null);

  readonly submittingTeaching = signal(false);
  readonly submittingTransport = signal(false);
  readonly teachingError = signal<string | null>(null);
  readonly transportError = signal<string | null>(null);

  readonly teachingForm = inject(FormBuilder).nonNullable.group(
    {
      positionTitleId: ['', [requiredTrimmed()]],
      qualificationId: ['', [requiredTrimmed()]],
      amount: ['', [cedisAmount]],
      effectiveFrom: ['', [requiredTrimmed()]],
      effectiveTo: [''],
    },
    { validators: [endAfterStart()] },
  );

  readonly transportForm = inject(FormBuilder).nonNullable.group(
    {
      amount: ['', [cedisAmount]],
      effectiveFrom: ['', [requiredTrimmed()]],
      effectiveTo: [''],
    },
    { validators: [endAfterStart()] },
  );

  /** Positions down the side, qualifications across, with the amount in force today. */
  readonly matrix = computed<MatrixRow[]>(() => {
    const day = calendarToday(this.tenant.timeZoneId());
    const rates = this.matrixRates();
    return this.titles().map((title) => ({
      title,
      cells: this.qualifications().map((qualification) => ({
        qualification,
        rate: rateInForce(
          rates.filter((rate) => rate.positionTitleId === title.id && rate.qualificationId === qualification.id),
          day,
        ),
      })),
    }));
  });

  constructor() {
    this.loadCatalog();
    this.loadMatrix();
    this.loadTransport(1);
  }

  /** Zone caption shown once. Rate days are calendar dates in this zone. */
  zoneCaption(): string {
    const id = this.tenant.timeZoneId();
    return `${id} (${zoneAbbreviation(id)})`;
  }

  /** Formats an amount with the tenant currency code. */
  money(amount: number): string {
    return formatMoney(amount, this.tenant.currencyCode());
  }

  /** Teaching amount label. GHS is cedis; any other tenant keeps its currency code. */
  hourlyLabel(): string {
    return this.tenant.currencyCode() === 'GHS' ? 'Cedis per hour' : `${this.tenant.currencyCode()} per hour`;
  }

  /** Transport amount label. Paid once per teaching day, not per hour. */
  dailyLabel(): string {
    return this.tenant.currencyCode() === 'GHS' ? 'Cedis per teaching day' : `${this.tenant.currencyCode()} per teaching day`;
  }

  /** Names the cell the teaching form will save. Null until both sides are chosen. */
  pairCaption(): string | null {
    const title = this.titles().find((item) => item.id === this.teachingForm.controls.positionTitleId.value);
    const qualification = this.qualifications().find(
      (item) => item.id === this.teachingForm.controls.qualificationId.value,
    );
    if (!title || !qualification) {
      return null;
    }
    return `${title.name} × ${qualification.name}`;
  }

  /** Shows the half-open span. A null end has not been replaced. */
  rangeLabel(rate: { effectiveFrom: string; effectiveTo: string | null }): string {
    const start = formatCalendarDate(rate.effectiveFrom);
    if (rate.effectiveTo === null) {
      return `${start} onward`;
    }
    return `${start} until ${formatCalendarDate(rate.effectiveTo)}`;
  }

  /** True when this cell is the one the teaching form will post. */
  isSelected(positionTitleId: string, qualificationId: string): boolean {
    return (
      this.teachingForm.controls.positionTitleId.value === positionTitleId &&
      this.teachingForm.controls.qualificationId.value === qualificationId
    );
  }

  /**
   * Points the teaching form at one matrix cell and loads that cell's rows.
   * The rest of the matrix stays as it was.
   */
  selectCell(positionTitleId: string, qualificationId: string): void {
    this.teachingForm.patchValue({ positionTitleId, qualificationId });
    this.teachingError.set(null);
    this.loadHistory(1);
    this.revealTeachingForm();
  }

  /** On a phone the editor sits under a sideways matrix. Bring that cell's form into view. */
  private revealTeachingForm(): void {
    if (typeof window.matchMedia !== 'function' || window.matchMedia('(min-width: 64rem)').matches) {
      return;
    }
    afterNextRender(
      () => {
        const form = document.getElementById('teaching-rate');
        if (form && typeof form.scrollIntoView === 'function') {
          form.scrollIntoView({ block: 'start' });
        }
      },
      { injector: this.injector },
    );
  }

  /** Reloads the selected cell after the pair dropdowns change. */
  onPairChange(): void {
    const positionTitleId = this.teachingForm.controls.positionTitleId.value;
    const qualificationId = this.teachingForm.controls.qualificationId.value;
    if (!positionTitleId || !qualificationId) {
      this.history.set([]);
      this.historyTotal.set(0);
      return;
    }
    this.loadHistory(1);
  }

  /** Loads one page of the selected cell. */
  loadHistory(page: number): void {
    const positionTitleId = this.teachingForm.controls.positionTitleId.value;
    const qualificationId = this.teachingForm.controls.qualificationId.value;
    if (!positionTitleId || !qualificationId) {
      return;
    }

    const generation = ++this.historyGeneration;
    this.loadingHistory.set(true);
    this.historyError.set(null);
    this.ratesApi
      .listTeaching(page, this.pageSize, positionTitleId, qualificationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (generation !== this.historyGeneration) {
            return;
          }
          this.history.set(result.items);
          this.historyPage.set(result.page);
          this.historyTotal.set(result.totalCount);
          this.loadingHistory.set(false);
        },
        error: (error: unknown) => {
          if (generation !== this.historyGeneration) {
            return;
          }
          this.loadingHistory.set(false);
          this.historyError.set(readApiError(error, 'Teaching rates could not be loaded.'));
        },
      });
  }

  /** Loads one page of the transport timeline. */
  loadTransport(page: number): void {
    const generation = ++this.transportGeneration;
    this.loadingTransport.set(true);
    this.transportLoadError.set(null);
    this.ratesApi
      .listTransport(page, this.pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (generation !== this.transportGeneration) {
            return;
          }
          this.transport.set(result.items);
          this.transportPage.set(result.page);
          this.transportTotal.set(result.totalCount);
          this.loadingTransport.set(false);
        },
        error: (error: unknown) => {
          if (generation !== this.transportGeneration) {
            return;
          }
          this.loadingTransport.set(false);
          this.transportLoadError.set(readApiError(error, 'Transport rates could not be loaded.'));
        },
      });
  }

  /**
   * Posts one teaching row for the selected position and qualification.
   * An overlap leaves every other cell unchanged.
   */
  submitTeaching(): void {
    if (this.teachingForm.invalid || this.submittingTeaching()) {
      this.teachingForm.markAllAsTouched();
      return;
    }

    const value = this.teachingForm.getRawValue();
    this.submittingTeaching.set(true);
    this.teachingError.set(null);
    this.ratesApi
      .createTeaching({
        positionTitleId: value.positionTitleId,
        qualificationId: value.qualificationId,
        amount: Number(value.amount),
        effectiveFrom: value.effectiveFrom,
        effectiveTo: value.effectiveTo === '' ? null : value.effectiveTo,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submittingTeaching.set(false);
          this.teachingForm.patchValue({ amount: '', effectiveFrom: '', effectiveTo: '' });
          this.teachingForm.markAsUntouched();
          this.loadMatrix();
          this.loadHistory(this.historyPage());
        },
        error: (error: unknown) => {
          // A 409 names the overlap. The matrix is left as loaded so the other cells stay visible.
          this.submittingTeaching.set(false);
          this.teachingError.set(readApiError(error, 'The teaching rate could not be saved.'));
        },
      });
  }

  /** Posts one transport row on the tenant timeline. */
  submitTransport(): void {
    if (this.transportForm.invalid || this.submittingTransport()) {
      this.transportForm.markAllAsTouched();
      return;
    }

    const value = this.transportForm.getRawValue();
    this.submittingTransport.set(true);
    this.transportError.set(null);
    this.ratesApi
      .createTransport({
        amount: Number(value.amount),
        effectiveFrom: value.effectiveFrom,
        effectiveTo: value.effectiveTo === '' ? null : value.effectiveTo,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submittingTransport.set(false);
          this.transportForm.reset();
          this.loadTransport(1);
        },
        error: (error: unknown) => {
          this.submittingTransport.set(false);
          this.transportError.set(readApiError(error, 'The transport rate could not be saved.'));
        },
      });
  }

  private loadCatalog(): void {
    this.academicApi
      .listPositionTitles(1, this.lookupSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.titles.set(result.items);
          this.titlesTruncated.set(result.totalCount > result.items.length);
          this.finishCatalog();
        },
        error: (error: unknown) => {
          this.catalogError.set(readApiError(error, 'Position titles could not be loaded.'));
          this.finishCatalog();
        },
      });

    this.academicApi
      .listQualifications(1, this.lookupSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.qualifications.set(result.items);
          this.qualificationsTruncated.set(result.totalCount > result.items.length);
          this.finishCatalog();
        },
        error: (error: unknown) => {
          this.catalogError.set(readApiError(error, 'Qualifications could not be loaded.'));
          this.finishCatalog();
        },
      });
  }

  /** Both lookups must finish before an empty list is treated as a missing catalog. */
  private finishCatalog(): void {
    this.catalogPending -= 1;
    if (this.catalogPending === 0) {
      this.loadingCatalog.set(false);
    }
  }

  private loadMatrix(): void {
    const generation = ++this.matrixGeneration;
    this.loadingMatrix.set(true);
    this.matrixError.set(null);
    this.matrixTruncated.set(false);
    // The cell shows the amount in force today. Rows that have already ended are not part of that answer.
    this.loadInForce(calendarToday(this.tenant.timeZoneId()), 1, [], generation);
  }

  private loadInForce(day: string, page: number, accumulated: TeachingRate[], generation: number): void {
    this.ratesApi
      .listTeaching(page, this.lookupSize, null, null, day)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (generation !== this.matrixGeneration) {
            return;
          }
          const items = accumulated.concat(result.items);
          const more = items.length < result.totalCount && result.items.length === this.lookupSize;
          if (more && page < this.maxInForcePages) {
            this.loadInForce(day, page + 1, items, generation);
            return;
          }
          this.matrixRates.set(items);
          this.matrixTruncated.set(items.length < result.totalCount);
          this.loadingMatrix.set(false);
        },
        error: (error: unknown) => {
          if (generation !== this.matrixGeneration) {
            return;
          }
          this.loadingMatrix.set(false);
          this.matrixError.set(readApiError(error, 'Teaching rates could not be loaded.'));
        },
      });
  }
}

/** An empty end stays open. A filled end must be after the start, because that day is excluded. */
function endAfterStart(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const start = control.get('effectiveFrom')?.value;
    const end = control.get('effectiveTo')?.value;
    if (typeof start !== 'string' || typeof end !== 'string' || start.length === 0 || end.length === 0) {
      return null;
    }
    return end <= start ? { dateOrder: true } : null;
  };
}
