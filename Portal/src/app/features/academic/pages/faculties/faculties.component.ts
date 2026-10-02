import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
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
  private facultyGeneration = 0;
  private departmentGeneration = 0;

  /** Shared with `AcademicFieldLimits.Name`. */
  readonly limits = ACADEMIC_FIELD_LIMITS;

  /** Lookup page size for the campus chips. The API caps a page at 100. */
  readonly lookupSize = PAGE_LIMITS.maxSize;

  readonly pageSize = PAGE_LIMITS.defaultSize;

  /** True until the campus chip request returns. */
  readonly loadingCampuses = signal(true);

  /** Campuses used as the parent of a faculty and as filter chips. */
  readonly campuses = signal<Campus[]>([]);

  /** True when the tenant has more campuses than the chip row can show. */
  readonly campusesTruncated = signal(false);

  /** Null lists faculties on every campus. */
  readonly campusFilter = signal<string | null>(null);

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
    this.loadCampuses();
    this.loadFaculties(1);
  }

  /** Faculty currently showing its departments. */
  selectedFaculty(): Faculty | null {
    const id = this.selectedFacultyId();
    return this.faculties().find((faculty) => faculty.id === id) ?? null;
  }

  /** Limits the faculty page to one campus, or clears that filter. */
  filterCampus(campusId: string | null): void {
    this.campusFilter.set(campusId);
    this.loadFaculties(1);
  }

  /** Shows departments for this faculty. A department always displays its faculty. */
  selectFaculty(id: string): void {
    if (this.selectedFacultyId() === id) {
      return;
    }
    this.selectedFacultyId.set(id);
    this.departmentForm.reset();
    this.departmentError.set(null);
    this.loadDepartments(1);
  }

  /** Loads campuses for the chips and the faculty form. */
  loadCampuses(): void {
    this.api
      .listCampuses(1, this.lookupSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.campuses.set(result.items);
          this.campusesTruncated.set(result.totalCount > result.items.length);
          this.loadingCampuses.set(false);
          const current = this.facultyForm.controls.campusId.value;
          if (!current && result.items.length > 0) {
            this.facultyForm.controls.campusId.setValue(result.items[0].id);
          }
        },
        error: (error: unknown) => {
          this.loadingCampuses.set(false);
          this.facultyLoadError.set(readApiError(error, 'Campuses could not be loaded.'));
        },
      });
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
          this.campusFilter.set(campusId);
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
