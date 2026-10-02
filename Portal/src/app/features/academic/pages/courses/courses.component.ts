import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { readApiError } from '../../../../shared/http/read-api-error';
import { PAGE_LIMITS } from '../../../../shared/paging/page-limits';
import { PagerComponent } from '../../../../shared/paging/pager.component';
import { requiredTrimmed } from '../../../../shared/validators/required-trimmed';
import { AcademicApi } from '../../data/academic.api';
import { ACADEMIC_FIELD_LIMITS, Course, Qualification } from '../../data/academic.models';

/**
 * Qualifications and the courses that use them.
 * Course codes are submitted in uppercase. Position titles are not edited here.
 */
@Component({
  selector: 'app-courses',
  imports: [ReactiveFormsModule, PagerComponent],
  templateUrl: './courses.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CoursesComponent {
  private readonly api = inject(AcademicApi);
  private readonly destroyRef = inject(DestroyRef);
  private qualificationGeneration = 0;
  private courseGeneration = 0;

  /** Shared with `AcademicFieldLimits`. */
  readonly limits = ACADEMIC_FIELD_LIMITS;

  readonly pageSize = PAGE_LIMITS.defaultSize;

  /** Qualification dropdown. Capped at the API page size. */
  readonly lookupSize = PAGE_LIMITS.maxSize;

  readonly qualifications = signal<Qualification[]>([]);
  readonly qualificationPage = signal(1);
  readonly qualificationTotal = signal(0);
  readonly loadingQualifications = signal(true);
  readonly submittingQualification = signal(false);
  readonly qualificationError = signal<string | null>(null);
  readonly qualificationLoadError = signal<string | null>(null);

  /** Dropdown catalog. Kept apart from the paged list so page 2 does not empty the course form. */
  readonly choices = signal<Qualification[]>([]);
  readonly choicesTruncated = signal(false);
  readonly loadingChoices = signal(true);

  readonly courses = signal<Course[]>([]);
  readonly coursePage = signal(1);
  readonly courseTotal = signal(0);
  readonly loadingCourses = signal(true);
  readonly submittingCourse = signal(false);
  readonly courseError = signal<string | null>(null);
  readonly courseLoadError = signal<string | null>(null);

  /** Course being updated. Null creates a new course. */
  readonly editingId = signal<string | null>(null);

  readonly qualificationForm = inject(FormBuilder).nonNullable.group({
    name: ['', [requiredTrimmed(), Validators.maxLength(ACADEMIC_FIELD_LIMITS.shortName)]],
  });

  readonly courseForm = inject(FormBuilder).nonNullable.group({
    code: ['', [requiredTrimmed(), Validators.maxLength(ACADEMIC_FIELD_LIMITS.courseCode)]],
    name: ['', [requiredTrimmed(), Validators.maxLength(ACADEMIC_FIELD_LIMITS.name)]],
    qualificationId: ['', [requiredTrimmed()]],
  });

  constructor() {
    this.loadQualifications(1);
    this.loadCourseChoices();
    this.loadCourses(1);
  }

  /** Loads one page of the qualification list. */
  loadQualifications(page: number): void {
    const generation = ++this.qualificationGeneration;
    this.loadingQualifications.set(true);
    this.qualificationLoadError.set(null);
    this.api
      .listQualifications(page, this.pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (generation !== this.qualificationGeneration) {
            return;
          }
          this.qualifications.set(result.items);
          this.qualificationPage.set(result.page);
          this.qualificationTotal.set(result.totalCount);
          this.loadingQualifications.set(false);
        },
        error: (error: unknown) => {
          if (generation !== this.qualificationGeneration) {
            return;
          }
          this.loadingQualifications.set(false);
          this.qualificationLoadError.set(readApiError(error, 'Qualifications could not be loaded.'));
        },
      });
  }

  /**
   * Loads the qualification dropdown.
   * A separate request from the paged list so page 2 of the list does not empty the course form.
   */
  loadCourseChoices(): void {
    this.loadingChoices.set(true);
    this.api
      .listQualifications(1, this.lookupSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.choices.set(result.items);
          this.choicesTruncated.set(result.totalCount > result.items.length);
          this.loadingChoices.set(false);
          const current = this.courseForm.controls.qualificationId.value;
          const stillThere = result.items.some((item) => item.id === current);
          if (!stillThere && result.items[0]) {
            this.courseForm.controls.qualificationId.setValue(result.items[0].id);
          }
        },
        error: (error: unknown) => {
          this.loadingChoices.set(false);
          this.courseLoadError.set(readApiError(error, 'Qualifications could not be loaded.'));
        },
      });
  }

  /** Loads one page of courses. */
  loadCourses(page: number): void {
    const generation = ++this.courseGeneration;
    this.loadingCourses.set(true);
    this.courseLoadError.set(null);
    this.api
      .listCourses(page, this.pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (generation !== this.courseGeneration) {
            return;
          }
          this.courses.set(result.items);
          this.coursePage.set(result.page);
          this.courseTotal.set(result.totalCount);
          this.loadingCourses.set(false);
        },
        error: (error: unknown) => {
          if (generation !== this.courseGeneration) {
            return;
          }
          this.loadingCourses.set(false);
          this.courseLoadError.set(readApiError(error, 'Courses could not be loaded.'));
        },
      });
  }

  /** Adds a qualification. Courses keep using the existing ones if this fails. */
  submitQualification(): void {
    if (this.qualificationForm.invalid || this.submittingQualification()) {
      this.qualificationForm.markAllAsTouched();
      return;
    }

    const name = this.qualificationForm.controls.name.value.trim();
    this.submittingQualification.set(true);
    this.qualificationError.set(null);
    this.api
      .createQualification({ name })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.submittingQualification.set(false);
          this.qualificationForm.reset();
          if (!this.courseForm.controls.qualificationId.value) {
            this.courseForm.controls.qualificationId.setValue(created.id);
          }
          this.loadQualifications(1);
          this.loadCourseChoices();
        },
        error: (error: unknown) => {
          this.submittingQualification.set(false);
          this.qualificationError.set(readApiError(error, 'The qualification could not be saved.'));
        },
      });
  }

  /** Fills the form from a course. The code stays uppercase. */
  edit(course: Course): void {
    this.editingId.set(course.id);
    this.courseError.set(null);
    if (!this.choices().some((item) => item.id === course.qualificationId)) {
      this.choices.update((items) => [
        ...items,
        { id: course.qualificationId, name: course.qualificationName },
      ]);
    }
    this.courseForm.setValue({
      code: course.code,
      name: course.name,
      qualificationId: course.qualificationId,
    });
  }

  /** Returns the form to creating a course. */
  cancelEdit(): void {
    this.editingId.set(null);
    this.courseError.set(null);
    const qualificationId = this.courseForm.controls.qualificationId.value;
    this.courseForm.reset({ code: '', name: '', qualificationId });
  }

  /** Creates or updates a course. The code is submitted in uppercase. */
  submitCourse(): void {
    if (this.courseForm.invalid || this.submittingCourse()) {
      this.courseForm.markAllAsTouched();
      return;
    }

    // Course codes are stored uppercase. The field may be typed in any case.
    const code = this.courseForm.controls.code.value.trim().toUpperCase();
    const name = this.courseForm.controls.name.value.trim();
    const qualificationId = this.courseForm.controls.qualificationId.value;
    const editingId = this.editingId();
    this.submittingCourse.set(true);
    this.courseError.set(null);
    const request = editingId
      ? this.api.updateCourse({ id: editingId, code, name, qualificationId })
      : this.api.createCourse({ code, name, qualificationId });

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submittingCourse.set(false);
        this.cancelEdit();
        this.loadCourses(editingId ? this.coursePage() : 1);
      },
      error: (error: unknown) => {
        this.submittingCourse.set(false);
        this.courseError.set(readApiError(error, 'The course could not be saved.'));
      },
    });
  }
}
