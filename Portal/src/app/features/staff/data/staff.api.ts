import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  CreateStaffBody,
  CreateStaffPositionBody,
  StaffMember,
  StaffPosition,
  UpdateStaffBody,
} from '../../academic/data/academic.models';
import { PAGE_LIMITS, PagedResult } from '../../../shared/paging/page-limits';

/**
 * HTTP client for lecturers, their department assignments, and their position appointments.
 * Mirrors `StaffController`. A lecturer save always keeps at least one department.
 */
@Injectable({ providedIn: 'root' })
export class StaffApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  /** Lists one page of lecturers, including departments and appointments. */
  list(page: number, pageSize: number = PAGE_LIMITS.defaultSize): Observable<PagedResult<StaffMember>> {
    return this.http.get<PagedResult<StaffMember>>(`${this.base}/api/staff`, {
      params: { page, pageSize },
    });
  }

  /**
   * Creates a lecturer.
   * `departmentIds` must contain at least one department.
   */
  create(body: CreateStaffBody): Observable<StaffMember> {
    return this.http.post<StaffMember>(`${this.base}/api/staff`, body);
  }

  /** Updates staff fields. Department assignments stay on their own routes. */
  update(body: UpdateStaffBody): Observable<StaffMember> {
    return this.http.put<StaffMember>(`${this.base}/api/staff`, body);
  }

  /**
   * Assigns another department.
   * A lecturer may belong to more than one, including departments on different campuses.
   */
  assignDepartment(staffId: string, departmentId: string): Observable<StaffMember> {
    return this.http.post<StaffMember>(`${this.base}/api/staff/${staffId}/departments`, { departmentId });
  }

  /**
   * Removes one department assignment.
   * @throws The observable errors with 409 when this is the lecturer's last department.
   */
  removeDepartment(staffId: string, departmentId: string): Observable<StaffMember> {
    return this.http.delete<StaffMember>(`${this.base}/api/staff/${staffId}/departments/${departmentId}`);
  }

  /**
   * Adds a position appointment on this lecturer.
   * @throws The observable errors with 409 when the dates overlap another appointment.
   */
  createPosition(staffId: string, body: CreateStaffPositionBody): Observable<StaffPosition> {
    return this.http.post<StaffPosition>(`${this.base}/api/staff/${staffId}/positions`, body);
  }
}
