import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { PAGE_LIMITS, PagedResult } from '../../../shared/paging/page-limits';
import {
  Campus,
  Course,
  CreateCampusBody,
  CreateDepartmentBody,
  CreateFacultyBody,
  CreateNamedBody,
  CreateSemesterBody,
  Department,
  Faculty,
  PositionTitle,
  Qualification,
  SaveCourseBody,
  Semester,
  UpdateSemesterBody,
} from './academic.models';

/**
 * HTTP client for academic setup.
 * Mirrors the campus, faculty, department, semester, qualification, course, and position-title controllers.
 * Each list call asks for one page. The portal does not download the whole tenant.
 */
@Injectable({ providedIn: 'root' })
export class AcademicApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  /** Lists one page of campuses. */
  listCampuses(page: number, pageSize: number = PAGE_LIMITS.defaultSize): Observable<PagedResult<Campus>> {
    return this.http.get<PagedResult<Campus>>(`${this.base}/api/campuses`, { params: this.pageParams(page, pageSize) });
  }

  /** Creates a campus. A duplicate name returns 409. */
  createCampus(body: CreateCampusBody): Observable<Campus> {
    return this.http.post<Campus>(`${this.base}/api/campuses`, body);
  }

  /**
   * Lists one page of faculties.
   * `campusId` limits the page to that campus.
   */
  listFaculties(
    page: number,
    pageSize: number = PAGE_LIMITS.defaultSize,
    campusId?: string | null,
  ): Observable<PagedResult<Faculty>> {
    return this.http.get<PagedResult<Faculty>>(`${this.base}/api/faculties`, {
      params: this.pageParams(page, pageSize, campusId ? { campusId } : undefined),
    });
  }

  /** Creates a faculty on one campus. A duplicate name on that campus returns 409. */
  createFaculty(body: CreateFacultyBody): Observable<Faculty> {
    return this.http.post<Faculty>(`${this.base}/api/faculties`, body);
  }

  /** Lists one page of departments, optionally for one faculty. */
  listDepartments(
    page: number,
    pageSize: number = PAGE_LIMITS.defaultSize,
    facultyId?: string | null,
  ): Observable<PagedResult<Department>> {
    return this.http.get<PagedResult<Department>>(`${this.base}/api/departments`, {
      params: this.pageParams(page, pageSize, facultyId ? { facultyId } : undefined),
    });
  }

  /** Creates a department under a faculty. A duplicate name in that faculty returns 409. */
  createDepartment(body: CreateDepartmentBody): Observable<Department> {
    return this.http.post<Department>(`${this.base}/api/departments`, body);
  }

  /** Lists one page of semesters, newest start date first. */
  listSemesters(page: number, pageSize: number = PAGE_LIMITS.defaultSize): Observable<PagedResult<Semester>> {
    return this.http.get<PagedResult<Semester>>(`${this.base}/api/semesters`, {
      params: this.pageParams(page, pageSize),
    });
  }

  /** Creates a draft semester. */
  createSemester(body: CreateSemesterBody): Observable<Semester> {
    return this.http.post<Semester>(`${this.base}/api/semesters`, body);
  }

  /** Updates a semester that is not closed. */
  updateSemester(body: UpdateSemesterBody): Observable<Semester> {
    return this.http.put<Semester>(`${this.base}/api/semesters`, body);
  }

  /**
   * Opens a draft semester.
   * @throws The observable errors with 409 when another open semester covers the same dates.
   */
  openSemester(id: string): Observable<Semester> {
    return this.http.post<Semester>(`${this.base}/api/semesters/${id}/open`, {});
  }

  /** Closes an open semester. */
  closeSemester(id: string): Observable<Semester> {
    return this.http.post<Semester>(`${this.base}/api/semesters/${id}/close`, {});
  }

  /** Lists one page of qualifications. */
  listQualifications(page: number, pageSize: number = PAGE_LIMITS.defaultSize): Observable<PagedResult<Qualification>> {
    return this.http.get<PagedResult<Qualification>>(`${this.base}/api/qualifications`, {
      params: this.pageParams(page, pageSize),
    });
  }

  /** Creates a qualification. A duplicate name returns 409. */
  createQualification(body: CreateNamedBody): Observable<Qualification> {
    return this.http.post<Qualification>(`${this.base}/api/qualifications`, body);
  }

  /** Lists one page of shared position titles. */
  listPositionTitles(page: number, pageSize: number = PAGE_LIMITS.defaultSize): Observable<PagedResult<PositionTitle>> {
    return this.http.get<PagedResult<PositionTitle>>(`${this.base}/api/position-titles`, {
      params: this.pageParams(page, pageSize),
    });
  }

  /**
   * Creates a shared title.
   * This does not appoint a lecturer. A duplicate name returns 409.
   */
  createPositionTitle(body: CreateNamedBody): Observable<PositionTitle> {
    return this.http.post<PositionTitle>(`${this.base}/api/position-titles`, body);
  }

  /** Lists one page of courses. */
  listCourses(page: number, pageSize: number = PAGE_LIMITS.defaultSize): Observable<PagedResult<Course>> {
    return this.http.get<PagedResult<Course>>(`${this.base}/api/courses`, { params: this.pageParams(page, pageSize) });
  }

  /** Creates a course. The code is stored uppercase. A duplicate code returns 409. */
  createCourse(body: SaveCourseBody): Observable<Course> {
    return this.http.post<Course>(`${this.base}/api/courses`, body);
  }

  /** Updates a course. The code is stored uppercase. A duplicate code returns 409. */
  updateCourse(body: SaveCourseBody): Observable<Course> {
    return this.http.put<Course>(`${this.base}/api/courses`, body);
  }

  private pageParams(page: number, pageSize: number, extra?: Record<string, string>): HttpParams {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (extra) {
      for (const [key, value] of Object.entries(extra)) {
        params = params.set(key, value);
      }
    }
    return params;
  }
}
