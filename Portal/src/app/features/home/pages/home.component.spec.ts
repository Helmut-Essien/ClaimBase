import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../../environments/environment';
import { MeResponse } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { HomeComponent } from './home.component';

describe('HomeComponent', () => {
  const profile = (role: MeResponse['role']): MeResponse => ({
    tenantId: 'tenant',
    tenantName: 'Development University',
    userId: 'user',
    email: 'person@claimbase.test',
    displayName: 'Person',
    role,
    currencyCode: 'GHS',
    timeZoneId: 'Africa/Accra',
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('does not ask for semesters when the role cannot open one', () => {
    TestBed.inject(AuthService).profile.set(profile('HeadOfDepartment'));
    const fixture = TestBed.createComponent(HomeComponent);
    fixture.detectChanges();

    TestBed.inject(HttpTestingController).expectNone((request) => request.url.includes('/api/semesters'));
    expect(fixture.nativeElement.textContent).toContain('An administrator opens the semester.');
  });

  it('names the open semester for an admin', () => {
    TestBed.inject(AuthService).profile.set(profile('Admin'));
    const fixture = TestBed.createComponent(HomeComponent);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne((request) => request.url === `${environment.apiUrl}/api/semesters`).flush({
      items: [
        {
          id: 'semester',
          name: '2026/27',
          startDate: '2026-08-01',
          endDate: '2026-12-20',
          status: 'Open',
        },
      ],
      page: 1,
      pageSize: 100,
      totalCount: 1,
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Open semester: 2026/27.');
    http.verify();
  });
});
