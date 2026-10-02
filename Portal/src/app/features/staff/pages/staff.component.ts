import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { TenantStateService } from '../../../core/tenant/tenant-state.service';
import { AcademicApi } from '../../academic/data/academic.api';
import {
  ACADEMIC_FIELD_LIMITS,
  Campus,
  Department,
  EmploymentType,
  Faculty,
  PositionTitle,
  StaffDepartment,
  StaffMember,
  StaffPosition,
  employmentLabel,
} from '../../academic/data/academic.models';
import { formatCalendarDate, zoneAbbreviation } from '../../../shared/dates/calendar-date';
import { readApiError } from '../../../shared/http/read-api-error';
import { PAGE_LIMITS } from '../../../shared/paging/page-limits';
import { PagerComponent } from '../../../shared/paging/pager.component';
import { optionalEmail } from '../../../shared/validators/optional-email';
import { requiredTrimmed } from '../../../shared/validators/required-trimmed';
import { StaffApi } from '../data/staff.api';

/** List, create, or the record of one lecturer. */
type StaffPanel = 'list' | 'create' | 'edit';

/**
 * Lecturers, their departments, and their position appointments.
 * A title in the shared catalog is not an appointment until it is added on a lecturer.
 * Part-time and full-time staff are both included.
 */
@Component({
  selector: 'app-staff',
  imports: [ReactiveFormsModule, RouterLink, PagerComponent],
  templateUrl: './staff.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StaffComponent {
  private readonly staffApi = inject(StaffApi);
  private readonly academicApi = inject(AcademicApi);
  private readonly tenant = inject(TenantStateService);
  private readonly destroyRef = inject(DestroyRef);
  private loadGeneration = 0;

  /** Shared with `AcademicFieldLimits`. */
  readonly limits = ACADEMIC_FIELD_LIMITS;

  readonly pageSize = PAGE_LIMITS.defaultSize;
  readonly lookupSize = PAGE_LIMITS.maxSize;

  /** Which panel is on screen. */
  readonly panel = signal<StaffPanel>('list');

  /** Lecturer being edited. Null while creating. */
  readonly current = signal<StaffMember | null>(null);

  /** Departments chosen before the lecturer exists. */
  readonly draftDepartments = signal<StaffDepartment[]>([]);

  readonly items = signal<StaffMember[]>([]);
  readonly page = signal(1);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly loadError = signal<string | null>(null);
  readonly departmentError = signal<string | null>(null);

  readonly titles = signal<PositionTitle[]>([]);
  readonly titlesTruncated = signal(false);
  readonly submittingTitle = signal(false);
  readonly titleError = signal<string | null>(null);

  readonly campuses = signal<Campus[]>([]);
  readonly faculties = signal<Faculty[]>([]);
  readonly departments = signal<Department[]>([]);

  readonly submittingPosition = signal(false);
  readonly positionError = signal<string | null>(null);

  /** Departments on the draft or on the saved lecturer. */
  readonly assignedDepartments = computed(() =>
    this.panel() === 'create' ? this.draftDepartments() : (this.current()?.departments ?? []),
  );

  /** Appointments, newest start date first. */
  readonly positions = computed(() => {
    const rows = this.current()?.positions ?? [];
    return [...rows].sort((left, right) => right.effectiveFrom.localeCompare(left.effectiveFrom));
  });

  readonly form = inject(FormBuilder).nonNullable.group({
    staffNumber: ['', [requiredTrimmed(), Validators.maxLength(ACADEMIC_FIELD_LIMITS.staffNumber)]],
    displayName: ['', [requiredTrimmed(), Validators.maxLength(ACADEMIC_FIELD_LIMITS.name)]],
    email: ['', [optionalEmail(), Validators.maxLength(ACADEMIC_FIELD_LIMITS.email)]],
    employmentType: ['PartTime' as EmploymentType, Validators.required],
    biometricId: ['', Validators.maxLength(ACADEMIC_FIELD_LIMITS.biometricId)],
    campusId: [''],
    facultyId: [''],
    departmentId: [''],
  });

  readonly titleForm = inject(FormBuilder).nonNullable.group({
    name: ['', [requiredTrimmed(), Validators.maxLength(ACADEMIC_FIELD_LIMITS.shortName)]],
  });

  readonly positionForm = inject(FormBuilder).nonNullable.group(
    {
      positionTitleId: ['', requiredTrimmed()],
      effectiveFrom: ['', Validators.required],
      effectiveTo: [''],
    },
    { validators: [exclusiveEndAfterStart()] },
  );

  constructor() {
    this.load(1);
    this.loadTitles();
    this.form.controls.campusId.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((campusId) => {
      this.loadFaculties(campusId);
    });
    this.form.controls.facultyId.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((facultyId) => {
      this.loadPickerDepartments(facultyId);
    });
  }

  /** Zone caption for appointment dates. */
  zoneCaption(): string {
    const id = this.tenant.timeZoneId();
    return `${id} (${zoneAbbreviation(id)})`;
  }

  /** Formats a calendar day from the API. */
  formatDate(value: string): string {
    return formatCalendarDate(value);
  }

  /** Employment label. Both types stay on the form. */
  employment(value: string): string {
    return employmentLabel(value);
  }

  /** Loads one page of lecturers. */
  load(page: number): void {
    const generation = ++this.loadGeneration;
    this.loading.set(true);
    this.loadError.set(null);
    this.staffApi
      .list(page, this.pageSize)
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
          const current = this.current();
          if (current) {
            const refreshed = result.items.find((staff) => staff.id === current.id);
            if (refreshed) {
              this.current.set(refreshed);
            }
          }
        },
        error: (error: unknown) => {
          if (generation !== this.loadGeneration) {
            return;
          }
          this.loading.set(false);
          this.loadError.set(readApiError(error, 'Staff could not be loaded.'));
        },
      });
  }

  /** Loads the shared title catalog. A title here is not an appointment. */
  loadTitles(): void {
    this.academicApi
      .listPositionTitles(1, this.lookupSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.titles.set(result.items);
          this.titlesTruncated.set(result.totalCount > result.items.length);
          if (!this.positionForm.controls.positionTitleId.value && result.items[0]) {
            this.positionForm.controls.positionTitleId.setValue(result.items[0].id);
          }
        },
        error: (error: unknown) => {
          this.titleError.set(readApiError(error, 'Position titles could not be loaded.'));
        },
      });
  }

  /** Opens a blank lecturer. Positions can be added after the person is saved. */
  startCreate(): void {
    this.panel.set('create');
    this.current.set(null);
    this.draftDepartments.set([]);
    this.errorMessage.set(null);
    this.departmentError.set(null);
    this.form.reset({
      staffNumber: '',
      displayName: '',
      email: '',
      employmentType: 'PartTime',
      biometricId: '',
      campusId: '',
      facultyId: '',
      departmentId: '',
    });
    this.ensureCampuses();
  }

  /** Opens one lecturer's departments and appointments. */
  startEdit(staff: StaffMember): void {
    this.panel.set('edit');
    this.current.set(staff);
    this.errorMessage.set(null);
    this.departmentError.set(null);
    this.positionError.set(null);
    this.patchForm(staff);
    this.ensureCampuses();
  }

  /** Returns to the list. */
  cancel(): void {
    this.panel.set('list');
    this.current.set(null);
    this.draftDepartments.set([]);
    this.errorMessage.set(null);
    this.load(this.page());
  }

  /** Creates a shared title. It does not appoint the lecturer on screen. */
  submitTitle(): void {
    if (this.titleForm.invalid || this.submittingTitle()) {
      this.titleForm.markAllAsTouched();
      return;
    }

    const name = this.titleForm.controls.name.value.trim();
    this.submittingTitle.set(true);
    this.titleError.set(null);
    this.academicApi
      .createPositionTitle({ name })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submittingTitle.set(false);
          this.titleForm.reset();
          this.loadTitles();
        },
        error: (error: unknown) => {
          this.submittingTitle.set(false);
          this.titleError.set(readApiError(error, 'The position title could not be saved.'));
        },
      });
  }

  /** Adds the picked department to the draft, or assigns it on a saved lecturer. */
  addDepartment(): void {
    const department = this.departments().find((item) => item.id === this.form.controls.departmentId.value);
    if (!department) {
      this.departmentError.set('Choose a department.');
      return;
    }

    if (this.assignedDepartments().some((item) => item.departmentId === department.id)) {
      this.departmentError.set('That department is already assigned.');
      return;
    }

    this.departmentError.set(null);
    if (this.panel() === 'create') {
      this.draftDepartments.update((rows) => [...rows, toAssignment(department)]);
      return;
    }

    const staff = this.current();
    if (!staff) {
      return;
    }

    this.staffApi
      .assignDepartment(staff.id, department.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => this.current.set(updated),
        error: (error: unknown) => {
          this.departmentError.set(readApiError(error, 'The department could not be assigned.'));
        },
      });
  }

  /**
   * Removes one assignment.
   * The last department stays: a lecturer is always assigned to at least one.
   */
  removeDepartment(assignment: StaffDepartment): void {
    if (this.assignedDepartments().length <= 1) {
      return;
    }

    this.departmentError.set(null);
    if (this.panel() === 'create') {
      this.draftDepartments.update((rows) => rows.filter((item) => item.departmentId !== assignment.departmentId));
      return;
    }

    const staff = this.current();
    if (!staff) {
      return;
    }

    this.staffApi
      .removeDepartment(staff.id, assignment.departmentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => this.current.set(updated),
        error: (error: unknown) => {
          this.departmentError.set(readApiError(error, 'The department could not be removed.'));
        },
      });
  }

  /** Saves the lecturer. Email is submitted lowercase. Create requires a department. */
  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.panel() === 'create' && this.draftDepartments().length === 0) {
      this.errorMessage.set('Choose at least one department.');
      return;
    }

    // Email is stored lowercase. A blank address is cleared rather than saved as empty text.
    const email = emptyToNull(this.form.controls.email.value)?.toLowerCase() ?? null;
    const biometricId = emptyToNull(this.form.controls.biometricId.value);
    const staffNumber = this.form.controls.staffNumber.value.trim();
    const displayName = this.form.controls.displayName.value.trim();
    const employmentType = this.form.controls.employmentType.value;
    this.submitting.set(true);
    this.errorMessage.set(null);

    if (this.panel() === 'create') {
      this.staffApi
        .create({
          staffNumber,
          displayName,
          email,
          employmentType,
          biometricId,
          departmentIds: this.draftDepartments().map((item) => item.departmentId),
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (created) => {
            this.submitting.set(false);
            this.draftDepartments.set([]);
            this.panel.set('edit');
            this.current.set(created);
            this.patchForm(created);
          },
          error: (error: unknown) => {
            this.submitting.set(false);
            this.errorMessage.set(readApiError(error, 'The lecturer could not be saved.'));
          },
        });
      return;
    }

    const current = this.current();
    if (!current) {
      this.submitting.set(false);
      return;
    }

    this.staffApi
      .update({ id: current.id, staffNumber, displayName, email, employmentType, biometricId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.submitting.set(false);
          this.current.set(updated);
          this.patchForm(updated);
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.errorMessage.set(readApiError(error, 'The lecturer could not be saved.'));
        },
      });
  }

  /**
   * Adds an appointment on this lecturer.
   * Overlapping ranges stay on this form as a 409. A null end stays in force.
   */
  submitPosition(): void {
    const staff = this.current();
    if (!staff || this.positionForm.invalid || this.submittingPosition()) {
      this.positionForm.markAllAsTouched();
      return;
    }

    const effectiveFrom = this.positionForm.controls.effectiveFrom.value;
    const effectiveTo = emptyToNull(this.positionForm.controls.effectiveTo.value);
    this.submittingPosition.set(true);
    this.positionError.set(null);
    this.staffApi
      .createPosition(staff.id, {
        positionTitleId: this.positionForm.controls.positionTitleId.value,
        effectiveFrom,
        effectiveTo,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.submittingPosition.set(false);
          this.current.update((row) => (row ? { ...row, positions: [...row.positions, created] } : row));
          this.positionForm.patchValue({ effectiveFrom: '', effectiveTo: '' });
        },
        error: (error: unknown) => {
          this.submittingPosition.set(false);
          this.positionError.set(readApiError(error, 'The appointment could not be saved.'));
        },
      });
  }

  private patchForm(staff: StaffMember): void {
    this.form.setValue({
      staffNumber: staff.staffNumber,
      displayName: staff.displayName,
      email: staff.email ?? '',
      employmentType: staff.employmentType,
      biometricId: staff.biometricId ?? '',
      campusId: this.form.controls.campusId.value,
      facultyId: this.form.controls.facultyId.value,
      departmentId: this.form.controls.departmentId.value,
    });
  }

  private ensureCampuses(): void {
    if (this.campuses().length > 0) {
      if (!this.form.controls.campusId.value && this.campuses()[0]) {
        this.form.controls.campusId.setValue(this.campuses()[0].id);
      }
      return;
    }
    this.academicApi
      .listCampuses(1, this.lookupSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.campuses.set(result.items);
          const first = result.items[0];
          if (first && !this.form.controls.campusId.value) {
            this.form.controls.campusId.setValue(first.id);
          }
        },
        error: (error: unknown) => {
          this.departmentError.set(readApiError(error, 'Campuses could not be loaded.'));
        },
      });
  }

  private loadFaculties(campusId: string): void {
    this.faculties.set([]);
    this.departments.set([]);
    this.form.controls.facultyId.setValue('', { emitEvent: false });
    this.form.controls.departmentId.setValue('', { emitEvent: false });
    if (!campusId) {
      return;
    }

    this.academicApi
      .listFaculties(1, this.lookupSize, campusId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (this.form.controls.campusId.value !== campusId) {
            return;
          }
          this.faculties.set(result.items);
          const first = result.items[0];
          if (first) {
            this.form.controls.facultyId.setValue(first.id);
          }
        },
        error: (error: unknown) => {
          this.departmentError.set(readApiError(error, 'Faculties could not be loaded.'));
        },
      });
  }

  private loadPickerDepartments(facultyId: string): void {
    this.departments.set([]);
    this.form.controls.departmentId.setValue('', { emitEvent: false });
    if (!facultyId) {
      return;
    }

    this.academicApi
      .listDepartments(1, this.lookupSize, facultyId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (this.form.controls.facultyId.value !== facultyId) {
            return;
          }
          this.departments.set(result.items);
          const first = result.items[0];
          if (first) {
            this.form.controls.departmentId.setValue(first.id, { emitEvent: false });
          }
        },
        error: (error: unknown) => {
          this.departmentError.set(readApiError(error, 'Departments could not be loaded.'));
        },
      });
  }
}

/** The end date is the first day excluded, so it must fall after the start. Blank means open-ended. */
function exclusiveEndAfterStart(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const start = control.get('effectiveFrom')?.value;
    const end = control.get('effectiveTo')?.value;
    if (typeof start !== 'string' || typeof end !== 'string' || start.length === 0 || end.length === 0) {
      return null;
    }
    return end <= start ? { dateOrder: true } : null;
  };
}

function emptyToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}

function toAssignment(department: Department): StaffDepartment {
  return {
    departmentId: department.id,
    departmentName: department.name,
    campusId: department.campusId,
    campusName: department.campusName,
    facultyId: department.facultyId,
    facultyName: department.facultyName,
  };
}
