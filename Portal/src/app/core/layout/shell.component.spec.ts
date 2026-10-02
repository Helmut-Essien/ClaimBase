import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { MeResponse } from '../auth/auth.models';
import { AuthService } from '../auth/auth.service';
import { ShellComponent } from './shell.component';

describe('ShellComponent', () => {
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
      imports: [ShellComponent],
      providers: [
        provideRouter([
          { path: 'app', children: [] },
          { path: 'app/campuses', children: [] },
          { path: 'app/faculties', children: [] },
          { path: 'app/semesters', children: [] },
          { path: 'app/courses', children: [] },
          { path: 'app/staff', children: [] },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
  });

  it('shows academic setup to an admin and hides it from finance', () => {
    const auth = TestBed.inject(AuthService);
    auth.profile.set(profile('Admin'));
    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Campuses');
    expect(fixture.nativeElement.textContent).toContain('Staff');

    auth.profile.set(profile('Finance'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Campuses');
    expect(fixture.nativeElement.textContent).toContain('Home');
  });
});
