/**
 * Client field limits for academic setup.
 * Must match `AcademicFieldLimits` and the Shared DTO `[MaxLength]` values.
 */
export const ACADEMIC_FIELD_LIMITS = {
  name: 200,
  shortName: 80,
  courseCode: 32,
  staffNumber: 32,
  biometricId: 64,
  email: 320,
} as const;

/** Campus row from `GET /api/campuses`. */
export interface Campus {
  /** Campus id. */
  id: string;
  /** Campus name, unique in the tenant. */
  name: string;
}

/** Faculty row, including its campus. */
export interface Faculty {
  /** Faculty id. */
  id: string;
  /** Parent campus id. */
  campusId: string;
  /** Parent campus name. */
  campusName: string;
  /** Faculty name, unique on that campus. */
  name: string;
}

/** Department row, including its campus and faculty. */
export interface Department {
  /** Department id. */
  id: string;
  /** Campus id. */
  campusId: string;
  /** Campus name. */
  campusName: string;
  /** Faculty id. */
  facultyId: string;
  /** Faculty name. */
  facultyName: string;
  /** Department name, unique inside the faculty. */
  name: string;
}

/** Semester lifecycle. Session writes are a later slice; this status is already the gate. */
export type SemesterStatus = 'Draft' | 'Open' | 'Closed';

/** Semester row. Dates are inclusive calendar days. */
export interface Semester {
  /** Semester id. */
  id: string;
  /** Semester name. */
  name: string;
  /** First day included (`yyyy-MM-dd`). */
  startDate: string;
  /** Last day included (`yyyy-MM-dd`). */
  endDate: string;
  /** Draft, open, or closed. */
  status: SemesterStatus;
}

/** Qualification lookup row. */
export interface Qualification {
  /** Qualification id. */
  id: string;
  /** Qualification name, unique in the tenant. */
  name: string;
}

/**
 * Shared position title.
 * Adding a title does not appoint any lecturer.
 */
export interface PositionTitle {
  /** Title id. */
  id: string;
  /** Title name, unique in the tenant. */
  name: string;
}

/** Course row. `code` is stored uppercase. */
export interface Course {
  /** Course id. */
  id: string;
  /** Uppercase course code, unique in the tenant. */
  code: string;
  /** Course name. */
  name: string;
  /** Qualification id. */
  qualificationId: string;
  /** Qualification name. */
  qualificationName: string;
}

/** A department assignment shown with its campus and faculty. */
export interface StaffDepartment {
  /** Department id. */
  departmentId: string;
  /** Department name. */
  departmentName: string;
  /** Campus id. */
  campusId: string;
  /** Campus name. */
  campusName: string;
  /** Faculty id. */
  facultyId: string;
  /** Faculty name. */
  facultyName: string;
}

/**
 * One position appointment on a lecturer.
 * `effectiveTo` is the first day excluded. Null means the appointment is still open.
 */
export interface StaffPosition {
  /** Appointment id. */
  id: string;
  /** Shared title id. */
  positionTitleId: string;
  /** Shared title name. */
  positionTitleName: string;
  /** First day included (`yyyy-MM-dd`). */
  effectiveFrom: string;
  /** First day excluded. Null while the appointment is open. */
  effectiveTo: string | null;
}

/** Part-time or full-time. Both are included on claims. */
export type EmploymentType = 'PartTime' | 'FullTime';

/** Staff row, including departments and appointments. */
export interface StaffMember {
  /** Staff id. */
  id: string;
  /** Staff number, unique in the tenant. */
  staffNumber: string;
  /** Display name. */
  displayName: string;
  /** Optional email, stored lowercase. */
  email: string | null;
  /** Part-time or full-time. */
  employmentType: EmploymentType;
  /** Optional biometric device id. Punches match this exact value. */
  biometricId: string | null;
  /** Department assignments. At least one. */
  departments: StaffDepartment[];
  /** Position appointments. */
  positions: StaffPosition[];
}

/** Body for `POST /api/campuses`. */
export interface CreateCampusBody {
  /** Campus name. */
  name: string;
}

/** Body for `POST /api/faculties`. */
export interface CreateFacultyBody {
  /** Parent campus. */
  campusId: string;
  /** Faculty name. */
  name: string;
}

/** Body for `POST /api/departments`. */
export interface CreateDepartmentBody {
  /** Parent faculty. */
  facultyId: string;
  /** Department name. */
  name: string;
}

/** Body for `POST /api/semesters`. */
export interface CreateSemesterBody {
  /** Semester name. */
  name: string;
  /** First day included. */
  startDate: string;
  /** Last day included. */
  endDate: string;
}

/** Body for `PUT /api/semesters`. Status changes only through open and close. */
export interface UpdateSemesterBody extends CreateSemesterBody {
  /** Semester id. */
  id: string;
}

/** Body for `POST /api/qualifications`. */
export interface CreateNamedBody {
  /** Lookup name. */
  name: string;
}

/** Body for `POST /api/courses` and, with `id`, `PUT /api/courses`. */
export interface SaveCourseBody {
  /** Course id. Present only on update. */
  id?: string;
  /** Course code. The client submits it in uppercase. */
  code: string;
  /** Course name. */
  name: string;
  /** Qualification id. */
  qualificationId: string;
}

/** Body for `POST /api/staff`. */
export interface CreateStaffBody {
  /** Staff number. */
  staffNumber: string;
  /** Display name. */
  displayName: string;
  /** Optional email. Blank is sent as null. */
  email: string | null;
  /** Part-time or full-time. */
  employmentType: EmploymentType;
  /** Optional device id. Blank is sent as null. */
  biometricId: string | null;
  /** At least one department. */
  departmentIds: string[];
}

/** Body for `PUT /api/staff`. Assignments are changed on their own routes. */
export interface UpdateStaffBody {
  /** Staff id. */
  id: string;
  /** Staff number. */
  staffNumber: string;
  /** Display name. */
  displayName: string;
  /** Optional email. */
  email: string | null;
  /** Part-time or full-time. */
  employmentType: EmploymentType;
  /** Optional device id. Blank clears it. */
  biometricId: string | null;
}

/** Body for `POST /api/staff/{id}/positions`. */
export interface CreateStaffPositionBody {
  /** Shared position title. */
  positionTitleId: string;
  /** First day included. */
  effectiveFrom: string;
  /** First day excluded. Null while the appointment is open. */
  effectiveTo: string | null;
}

/**
 * Prints the employment type. Part-time and full-time are both paid.
 * @param value API employment type.
 * @returns A label for the staff card and form.
 */
export function employmentLabel(value: EmploymentType | string): string {
  return value === 'FullTime' ? 'Full-time' : 'Part-time';
}
