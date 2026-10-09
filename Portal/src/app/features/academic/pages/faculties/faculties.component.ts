import { ChangeDetectionStrategy, Component, DestroyRef, Injector, afterNextRender, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { readApiError } from '../../../../shared/http/read-api-error';
import { PAGE_LIMITS } from '../../../../shared/paging/page-limits';
import { PagerComponent } from '../../../../shared/paging/pager.component';
import { requiredTrimmed } from '../../../../shared/validators/required-trimmed';
import { AcademicApi } from '../../data/academic.api';
import { ACADEMIC_FIELD_LIMITS, Campus, Department, Faculty } from '../../data/academic.models';

/**
 * Faculties for one campus, and the departments under the selected faculty.
 * Faculty names are unique on that campus. Department names are unique inside the faculty.
 */
@Component({
  selector: 'app-faculties',
  imports: [ReactiveFormsModule, RouterLink, PagerComponent],
  templateUrl: './faculties.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FacultiesComponent {
  private readonly api = inject(AcademicApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private facultyGeneration = 0;
  private departmentGeneration = 0;
  private campusPageGeneration = 0;
  private campusChipsReady = false;
  private campusChoicesReady = false;

  /** Shared with `AcademicFieldLimits.Name`. */
  readonly limits = ACADEMIC_FIELD_LIMITS;

  /** Lookup page size for the faculty form's campus dropdown. The API caps a page at 100. */
  readonly lookupSize = PAGE_LIMITS.maxSize;

  readonly pageSize = PAGE_LIMITS.defaultSize;

  /** True until the chip page and the form lookup have both returned. */
  readonly loadingCampuses = signal(true);

  /** Campuses on the current filter-chip page. */
  readonly campuses = signal<Campus[]>([]);

  readonly campusPage = signal(1);
  readonly campusTotal = signal(0);

  /** Campuses offered when adding a faculty. A lookup, not the chip page. */
  readonly campusChoices = signal<Campus[]>([]);

  /** True when the faculty form's campus dropdown stopped at the lookup cap. */
  readonly campusChoicesTruncated = signal(false);

  /** Null lists faculties on every campus. */
  readonly campusFilter = signal<string | null>(null);

  /** Name of the filtered campus, kept when that chip is on another page. */
  readonly campusFilterName = signal<string | null>(null);

  /** Set when a campus chip page fails. */
  readonly campusPageError = signal<string | null>(null);

  /** Set when the faculty form's campus lookup fails. */
  readonly campusChoiceError = signal<string | null>(null);

  readonly faculties = signal<Faculty[]>([]);
  readonly facultyPage = signal(1);
  readonly facultyTotal = signal(0);
  readonly loadingFaculties = signal(true);
  readonly submittingFaculty = signal(false);
  readonly facultyError = signal<string | null>(null);
  readonly facultyLoadError = signal<string | null>(null);

  /** Faculty whose departments are listed beside it. */
  readonly selectedFacultyId = signal<string | null>(null);

  readonly departments = signal<Department[]>([]);
  readonly departmentPage = signal(1);
  readonly departmentTotal = signal(0);
  readonly loadingDepartments = signal(false);
  readonly submittingDepartment = signal(false);
  readonly departmentError = signal<string | null>(null);
  readonly departmentLoadError = signal<string | null>(null);

  readonly facultyForm = inject(FormBuilder).nonNullable.group({
    campusId: ['', [requiredTrimmed()]],
    name: ['', [requiredTrimmed(), Validators.maxLength(ACADEMIC_FIELD_LIMITS.name)]],
  });

  readonly departmentForm = inject(FormBuilder).nonNullable.group({
    name: ['', [requiredTrimmed(), Validators.maxLength(ACADEMIC_FIELD_LIMITS.name)]],
  });

  constructor() {
    this.loadCampusPage(1);
    this.loadCampusChoices();
    this.loadFaculties(1);
  }

  /** Faculty currently showing its departments. */
  selectedFaculty(): Faculty | null {
    const id = this.selectedFacultyId();
    return this.faculties().find((faculty) => faculty.id === id) ?? null;
  }

  /** Faculties matching the campus filter, including rows on other pages. */
  facultyCountLabel(): string {
    const count = this.facultyTotal();
    return count === 1 ? '1 faculty' : `${count} faculties`;
  }

  /** Departments in the selected faculty, including rows on other pages. */
  departmentCountLabel(): string {
    const count = this.departmentTotal();
    return count === 1 ? '1 department' : `${count} departments`;
  }

  /** Limits the faculty page to one campus, or clears that filter. */
  filterCampus(campusId: string | null): void {
    if (!campusId) {
      this.campusFilter.set(null);
      this.campusFilterName.set(null);
    } else {
      const match = this.campuses().find((campus) => campus.id === campusId);
      this.campusFilter.set(campusId);
      if (match) {
        this.campusFilterName.set(match.name);
      }
    }
    this.loadFaculties(1);
  }

  /**
   * The filtered campus when it is not on the chip page now showing.
   * The faculty list stays filtered, so the pressed chip has to stay in the group.
   */
  filteredCampusOffPage(): { id: string; name: string } | null {
    const id = this.campusFilter();
    const name = this.campusFilterName();
    if (!id || !name || this.campuses().some((campus) => campus.id === id)) {
      return null;
    }
    return { id, name };
  }

  /** Campus request that failed, if the page should say so. */
  campusProblem(): string | null {
    return this.campusPageError() ?? this.campusChoiceError();
  }

  /** Shows departments for this faculty. A department always displays its faculty. */
  selectFaculty(id: string): void {
    if (this.selectedFacultyId() === id) {
      return;
    }
    this.selectedFacultyId.set(id);
    this.departmentForm.reset();
    this.departmentError.set(null);
    // Drop the previous faculty's rows so the pane does not show them under the new name.
    this.departments.set([]);
    this.departmentTotal.set(0);
    this.loadDepartments(1);
    this.revealDepartments();
  }

  /**
   * On a phone the department editor sits under the faculty list.
   * Bring it up after a choice so the new faculty's departments are the thing on screen.
   */
  private revealDepartments(): void {
    if (typeof window.matchMedia !== 'function' || window.matchMedia('(min-width: 64rem)').matches) {
      return;
    }
    afterNextRender(
      () => {
        const pane = document.getElementById('faculty-departments');
        if (pane && typeof pane.scrollIntoView === 'function') {
          pane.scrollIntoView({ block: 'start' });
        }
      },
      { injector: this.injector },
    );
  }

  /** Loads one page of campuses for the filter chips. */
  loadCampusPage(page: number): void {
    const generation = ++this.campusPageGeneration;
    this.api
      .listCampuses(page, this.pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (generation !== this.campusPageGeneration) {
            return;
          }
          this.campuses.set(result.items);
          this.campusPage.set(result.page);
          this.campusTotal.set(result.totalCount);
          this.campusPageError.set(null);
          this.campusChipsReady = true;
          this.revealCampuses();
        },
        error: (error: unknown) => {
          if (generation !== this.campusPageGeneration) {
            return;
          }
          this.loadingCampuses.set(false);
          this.campusPageError.set(readApiError(error, 'Campuses could not be loaded.'));
        },
      });
  }

  /** Loads campuses for the faculty form. Further pages are not pulled into the browser. */
  loadCampusChoices(): void {
    this.api
      .listCampuses(1, this.lookupSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.campusChoices.set(result.items);
          this.campusChoicesTruncated.set(result.totalCount > result.items.length);
          const current = this.facultyForm.controls.campusId.value;
          if (!current && result.items.length > 0) {
            this.facultyForm.controls.campusId.setValue(result.items[0].id);
          }
          this.campusChoiceError.set(null);
          this.campusChoicesReady = true;
          this.revealCampuses();
        },
        error: (error: unknown) => {
          this.loadingCampuses.set(false);
          this.campusChoiceError.set(readApiError(error, 'Campuses could not be loaded.'));
        },
      });
  }

  /** Shows the page once both campus requests have returned. */
  private revealCampuses(): void {
    if (this.campusChipsReady && this.campusChoicesReady) {
      this.loadingCampuses.set(false);
    }
  }

  /** Loads one page of faculties for the selected campus filter. */
  loadFaculties(page: number): void {
    const generation = ++this.facultyGeneration;
    this.loadingFaculties.set(true);
    this.facultyLoadError.set(null);
    this.api
      .listFaculties(page, this.pageSize, this.campusFilter())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (generation !== this.facultyGeneration) {
            return;
          }
          this.faculties.set(result.items);
          this.facultyPage.set(result.page);
          this.facultyTotal.set(result.totalCount);
          this.loadingFaculties.set(false);
          const selected = this.selectedFacultyId();
          const stillVisible = result.items.some((faculty) => faculty.id === selected);
          if (!stillVisible) {
            this.selectedFacultyId.set(result.items[0]?.id ?? null);
            this.departmentForm.reset();
            this.departments.set([]);
            this.departmentTotal.set(0);
          }
          if (this.selectedFacultyId()) {
            this.loadDepartments(1);
          } else {
            this.departments.set([]);
            this.departmentTotal.set(0);
          }
        },
        error: (error: unknown) => {
          if (generation !== this.facultyGeneration) {
            return;
          }
          this.loadingFaculties.set(false);
          this.facultyLoadError.set(readApiError(error, 'Faculties could not be loaded.'));
        },
      });
  }

  /** Loads one page of departments for the selected faculty. */
  loadDepartments(page: number): void {
    const facultyId = this.selectedFacultyId();
    if (!facultyId) {
      this.departments.set([]);
      this.departmentTotal.set(0);
      return;
    }

    const generation = ++this.departmentGeneration;
    this.loadingDepartments.set(true);
    this.departmentLoadError.set(null);
    this.api
      .listDepartments(page, this.pageSize, facultyId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (generation !== this.departmentGeneration || facultyId !== this.selectedFacultyId()) {
            return;
          }
          this.departments.set(result.items);
          this.departmentPage.set(result.page);
          this.departmentTotal.set(result.totalCount);
          this.loadingDepartments.set(false);
        },
        error: (error: unknown) => {
          if (generation !== this.departmentGeneration) {
            return;
          }
          this.loadingDepartments.set(false);
          this.departmentLoadError.set(readApiError(error, 'Departments could not be loaded.'));
        },
      });
  }

  /** Creates a faculty on the chosen campus. */
  submitFaculty(): void {
    if (this.facultyForm.invalid || this.submittingFaculty()) {
      this.facultyForm.markAllAsTouched();
      return;
    }

    const campusId = this.facultyForm.controls.campusId.value;
    const name = this.facultyForm.controls.name.value.trim();
    this.submittingFaculty.set(true);
    this.facultyError.set(null);
    this.api
      .createFaculty({ campusId, name })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.submittingFaculty.set(false);
          this.facultyForm.controls.name.reset();
          this.selectedFacultyId.set(created.id);
          this.departmentForm.reset();
          this.departmentError.set(null);
          this.revealDepartments();
          // The new id is not on the current page yet. Drop the previous faculty's departments.
          this.departments.set([]);
          this.departmentTotal.set(0);
          this.campusFilter.set(campusId);
          this.campusFilterName.set(created.campusName);
          this.loadFaculties(1);
        },
        error: (error: unknown) => {
          this.submittingFaculty.set(false);
          this.facultyError.set(readApiError(error, 'The faculty could not be saved.'));
        },
      });
  }

  /** Creates a department under the selected faculty. */
  submitDepartment(): void {
    const facultyId = this.selectedFacultyId();
    if (!facultyId || this.departmentForm.invalid || this.submittingDepartment()) {
      this.departmentForm.markAllAsTouched();
      return;
    }

    const name = this.departmentForm.controls.name.value.trim();
    this.submittingDepartment.set(true);
    this.departmentError.set(null);
    this.api
      .createDepartment({ facultyId, name })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submittingDepartment.set(false);
          this.departmentForm.reset();
          this.loadDepartments(1);
        },
        error: (error: unknown) => {
          this.submittingDepartment.set(false);
          this.departmentError.set(readApiError(error, 'The department could not be saved.'));
        },
      });
  }
}
