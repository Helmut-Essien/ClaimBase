import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot } from '@angular/router';

import { MeResponse } from './auth.models';
import { AuthService } from './auth.service';
import { setupGuard } from './setup.guard';

describe('setupGuard', () => {
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

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('sends finance back to home', () => {
    TestBed.inject(AuthService).profile.set(profile('Finance'));

    const result = TestBed.runInInjectionContext(() =>
      setupGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );

    expect(result).toEqual(TestBed.inject(Router).createUrlTree(['/app']));
  });

  it('allows an admin into academic setup', () => {
    TestBed.inject(AuthService).profile.set(profile('TenantAdmin'));

    const result = TestBed.runInInjectionContext(() =>
      setupGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );

    expect(result).toBe(true);
  });
});
