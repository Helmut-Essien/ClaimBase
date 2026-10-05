import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../../environments/environment';
import { RatesComponent } from './rates.component';

describe('RatesComponent', () => {
  const lecturer = { id: 'lecturer', name: 'Lecturer' };
  const senior = { id: 'senior', name: 'Senior Lecturer' };
  const diploma = { id: 'diploma', name: 'Diploma' };
  const degree = { id: 'degree', name: 'Degree' };
  const inForce = {
    id: 'rate-open',
    positionTitleId: 'senior',
    positionTitleName: 'Senior Lecturer',
    qualificationId: 'diploma',
    qualificationName: 'Diploma',
    amount: 10.5,
    effectiveFrom: '2020-01-01',
    effectiveTo: null,
  };
  const ended = {
    id: 'rate-ended',
    positionTitleId: 'lecturer',
    positionTitleName: 'Lecturer',
    qualificationId: 'degree',
    qualificationName: 'Degree',
    amount: 99,
    effectiveFrom: '2020-01-01',
    effectiveTo: '2020-06-01',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RatesComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('shows the amount in force and leaves a gap where the only row has ended', () => {
    const fixture = TestBed.createComponent(RatesComponent);
    const http = TestBed.inject(HttpTestingController);
    flushCatalog(http, [senior, lecturer], [diploma, degree], [inForce, ended]);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('GHS 10.50');
    expect(text).toContain('No rate');
    expect(text).not.toContain('GHS 99.00');
    expect(text).toContain('They are not stored as zero.');
    expect(text).toContain('It is not an hourly rate.');
    http.verify();
  });

  it('keeps the other cell when a teaching overlap is rejected', () => {
    const fixture = TestBed.createComponent(RatesComponent);
    const http = TestBed.inject(HttpTestingController);
    flushCatalog(http, [senior, lecturer], [diploma], [inForce]);
    fixture.detectChanges();

    fixture.componentInstance.selectCell('lecturer', 'diploma');
    http.expectOne((request) => request.params.get('positionTitleId') === 'lecturer').flush(emptyPage(20));
    fixture.componentInstance.teachingForm.patchValue({
      amount: '12',
      effectiveFrom: '2027-01-01',
      effectiveTo: '',
    });
    fixture.componentInstance.submitTeaching();

    const create = http.expectOne(
      (request) => request.method === 'POST' && request.url === `${environment.apiUrl}/api/rates/teaching`,
    );
    expect(create.request.body).toEqual({
      positionTitleId: 'lecturer',
      qualificationId: 'diploma',
      amount: 12,
      effectiveFrom: '2027-01-01',
      effectiveTo: null,
    });
    create.flush(
      { message: 'Those teaching dates overlap an existing rate for this position and qualification.' },
      { status: 409, statusText: 'Conflict' },
    );
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Those teaching dates overlap an existing rate for this position and qualification.');
    expect(text).toContain('GHS 10.50');
    http.verify();
  });

  it('loads the next page when an amount in force today is past the first hundred rows', () => {
    const fixture = TestBed.createComponent(RatesComponent);
    const http = TestBed.inject(HttpTestingController);
    const filler = Array.from({ length: 100 }, (_, index) => ({
      id: `filler-${index}`,
      positionTitleId: 'other',
      positionTitleName: 'Other',
      qualificationId: 'other-qual',
      qualificationName: 'Other',
      amount: 1,
      effectiveFrom: '2020-01-01',
      effectiveTo: null,
    }));

    http.expectOne((request) => request.url === `${environment.apiUrl}/api/position-titles`).flush({
      items: [senior],
      page: 1,
      pageSize: 100,
      totalCount: 1,
    });
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/qualifications`).flush({
      items: [diploma],
      page: 1,
      pageSize: 100,
      totalCount: 1,
    });
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/rates/transport`).flush(emptyPage(20));
    http
      .expectOne(
        (request) =>
          request.url === `${environment.apiUrl}/api/rates/teaching` &&
          request.params.get('page') === '1' &&
          request.params.get('on') !== null,
      )
      .flush({ items: filler, page: 1, pageSize: 100, totalCount: 101 });
    http
      .expectOne(
        (request) =>
          request.url === `${environment.apiUrl}/api/rates/teaching` && request.params.get('page') === '2',
      )
      .flush({ items: [inForce], page: 2, pageSize: 100, totalCount: 101 });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('GHS 10.50');
    http.verify();
  });

  it('does not show a gap while the amounts in force are still loading', () => {
    const fixture = TestBed.createComponent(RatesComponent);
    const http = TestBed.inject(HttpTestingController);

    http.expectOne((request) => request.url === `${environment.apiUrl}/api/position-titles`).flush({
      items: [senior],
      page: 1,
      pageSize: 100,
      totalCount: 1,
    });
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/qualifications`).flush({
      items: [diploma],
      page: 1,
      pageSize: 100,
      totalCount: 1,
    });
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/rates/transport`).flush(emptyPage(20));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Please wait…');
    expect(fixture.nativeElement.textContent).not.toContain('No rate');

    http
      .expectOne(
        (request) => request.url === `${environment.apiUrl}/api/rates/teaching` && request.params.get('page') === '1',
      )
      .flush({ items: [inForce], page: 1, pageSize: 100, totalCount: 1 });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('GHS 10.50');
    http.verify();
  });

  it('posts one open-ended transport amount', () => {
    const fixture = TestBed.createComponent(RatesComponent);
    const http = TestBed.inject(HttpTestingController);
    flushCatalog(http, [lecturer], [diploma], []);
    fixture.detectChanges();

    fixture.componentInstance.transportForm.setValue({
      amount: '20',
      effectiveFrom: '2026-01-01',
      effectiveTo: '',
    });
    fixture.componentInstance.submitTransport();

    const create = http.expectOne(
      (request) => request.method === 'POST' && request.url === `${environment.apiUrl}/api/rates/transport`,
    );
    expect(create.request.body).toEqual({
      amount: 20,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
    });
    create.flush({ id: 'transport', amount: 20, effectiveFrom: '2026-01-01', effectiveTo: null });
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/rates/transport`).flush(emptyPage(20));
    http.verify();
  });

  function flushCatalog(
    http: HttpTestingController,
    titles: { id: string; name: string }[],
    qualifications: { id: string; name: string }[],
    teaching: unknown[],
  ): void {
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/position-titles`).flush({
      items: titles,
      page: 1,
      pageSize: 100,
      totalCount: titles.length,
    });
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/qualifications`).flush({
      items: qualifications,
      page: 1,
      pageSize: 100,
      totalCount: qualifications.length,
    });
    http
      .expectOne(
        (request) => request.url === `${environment.apiUrl}/api/rates/teaching` && request.params.get('pageSize') === '100',
      )
      .flush({ items: teaching, page: 1, pageSize: 100, totalCount: teaching.length });
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/rates/transport`).flush(emptyPage(20));
  }

  function emptyPage(pageSize: number) {
    return { items: [], page: 1, pageSize, totalCount: 0 };
  }
});
