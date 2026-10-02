import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../../environments/environment';
import { CampusesComponent } from './campuses.component';

describe('CampusesComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CampusesComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('trims the name and keeps a duplicate-name error on the form', () => {
    const fixture = TestBed.createComponent(CampusesComponent);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`${environment.apiUrl}/api/campuses?page=1&pageSize=20`).flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });

    fixture.componentInstance.form.controls.name.setValue('  North Campus  ');
    fixture.componentInstance.submit();

    const create = http.expectOne(`${environment.apiUrl}/api/campuses`);
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual({ name: 'North Campus' });
    create.flush({ message: 'A campus with that name already exists.' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('A campus with that name already exists.');
    http.verify();
  });
});
