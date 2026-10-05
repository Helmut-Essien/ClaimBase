import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { PAGE_LIMITS, PagedResult } from '../../../shared/paging/page-limits';
import { CreateTeachingRateBody, CreateTransportRateBody, TeachingRate, TransportRate } from './rates.models';

/**
 * HTTP client for the university rate schedule.
 * Mirrors `TeachingRatesController` and `TransportRatesController`.
 * Each list call asks for one page.
 */
@Injectable({ providedIn: 'root' })
export class RatesApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  /**
   * Lists one page of teaching rates.
   * The optional ids limit the page to one matrix cell.
   * `on` keeps only rows in force on that calendar day (`yyyy-MM-dd`).
   */
  listTeaching(
    page: number,
    pageSize: number = PAGE_LIMITS.defaultSize,
    positionTitleId?: string | null,
    qualificationId?: string | null,
    on?: string | null,
  ): Observable<PagedResult<TeachingRate>> {
    const extra: Record<string, string> = {};
    if (positionTitleId) {
      extra['positionTitleId'] = positionTitleId;
    }
    if (qualificationId) {
      extra['qualificationId'] = qualificationId;
    }
    if (on) {
      extra['on'] = on;
    }
    return this.http.get<PagedResult<TeachingRate>>(`${this.base}/api/rates/teaching`, {
      params: pageParams(page, pageSize, extra),
    });
  }

  /**
   * Posts one teaching-rate row.
   * @throws The observable errors with 409 when the dates overlap a row that already has an end.
   */
  createTeaching(body: CreateTeachingRateBody): Observable<TeachingRate> {
    return this.http.post<TeachingRate>(`${this.base}/api/rates/teaching`, body);
  }

  /** Lists one page of the tenant transport timeline, oldest start date first. */
  listTransport(page: number, pageSize: number = PAGE_LIMITS.defaultSize): Observable<PagedResult<TransportRate>> {
    return this.http.get<PagedResult<TransportRate>>(`${this.base}/api/rates/transport`, {
      params: pageParams(page, pageSize),
    });
  }

  /**
   * Posts one transport-rate row.
   * @throws The observable errors with 409 when the dates overlap a row that already has an end.
   */
  createTransport(body: CreateTransportRateBody): Observable<TransportRate> {
    return this.http.post<TransportRate>(`${this.base}/api/rates/transport`, body);
  }
}

function pageParams(page: number, pageSize: number, extra?: Record<string, string>): HttpParams {
  let params = new HttpParams().set('page', page).set('pageSize', pageSize);
  if (extra) {
    for (const [key, value] of Object.entries(extra)) {
      params = params.set(key, value);
    }
  }
  return params;
}
