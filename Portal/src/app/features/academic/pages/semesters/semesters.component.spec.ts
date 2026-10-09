import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../../environments/environment';
import { Semester } from '../../data/academic.models';
import { SemestersComponent } from './semesters.component';

describe('SemestersComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SemestersComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('leaves a closed semester without open or edit actions', () => {
    const fixture = TestBed.createComponent(SemestersComponent);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`${environment.apiUrl}/api/semesters?page=1&pageSize=20`).flush({
      items: [
        {
          id: 'semester',
          name: '2024/25',
          startDate: '2024-08-01',
          endDate: '2024-12-20',
          status: 'Closed',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    const labels = [...fixture.nativeElement.querySelectorAll('button')].map((button) =>
      (button as HTMLButtonElement).textContent?.trim(),
    );
    expect(labels).not.toContain('Open');
    expect(labels).not.toContain('Edit');
    expect(fixture.nativeElement.textContent).toContain('Closed');
    http.verify();
  });

  it('shows an overlapping open semester on the form', () => {
    const fixture = TestBed.createComponent(SemestersComponent);
    const http = TestBed.inject(HttpTestingController);
    const draft: Semester = {
      id: 'semester',
      name: '2026/27',
      startDate: '2026-08-01',
      endDate: '2026-12-20',
      status: 'Draft',
    };
    http.expectOne(`${environment.apiUrl}/api/semesters?page=1&pageSize=20`).flush({
      items: [draft],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    fixture.componentInstance.open(draft);
    const open = http.expectOne(`${environment.apiUrl}/api/semesters/semester/open`);
    open.flush(
      { message: 'Another open semester already covers these dates.' },
      { status: 409, statusText: 'Conflict' },
    );
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Another open semester already covers these dates.');
    http.verify();
  });

  it('keeps the edit banner on the original name while the field changes', () => {
    const fixture = TestBed.createComponent(SemestersComponent);
    const http = TestBed.inject(HttpTestingController);
    const draft: Semester = {
      id: 'semester',
      name: '2026/27',
      startDate: '2026-08-01',
      endDate: '2026-12-20',
      status: 'Draft',
    };
    http.expectOne(`${environment.apiUrl}/api/semesters?page=1&pageSize=20`).flush({
      items: [draft],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
    fixture.detectChanges();

    fixture.componentInstance.edit(draft);
    fixture.detectChanges();
    fixture.componentInstance.form.controls.name.setValue('Renamed');
    fixture.detectChanges();

    const status = fixture.nativeElement.querySelector('[role="status"]') as HTMLElement;
    expect(status.textContent).toContain('Editing 2026/27.');
    expect(status.textContent).not.toContain('Renamed');
    expect(document.activeElement).toBe(fixture.nativeElement.querySelector('#semester-name'));
    http.verify();
  });
});
