import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';
import { TenantStateService } from '../tenant/tenant-state.service';
import { AUTH_TOKEN_KEY, AuthSession } from './auth-session';
import { authInterceptor } from './auth.interceptor';
import { MeResponse } from './auth.models';
import { AuthService } from './auth.service';

describe('AuthService session', () => {
  const me: MeResponse = {
    tenantId: 'tenant',
    tenantName: 'Development University',
    userId: 'user',
    email: 'admin@claimbase.test',
    displayName: 'Dev Admin',
    role: 'Admin',
    currencyCode: 'GHS',
    timeZoneId: 'Africa/Accra',
  };

  beforeEach(() => {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'login', children: [] }, { path: 'app', children: [] }]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
  });

  it('reuses a profile only for the token that loaded it', async () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);

    auth.storeToken('token-a');
    const first = auth.restore();
    http.expectOne(`${environment.apiUrl}/api/auth/me`).flush(me);
    await expect(first).resolves.toBe(true);

    await expect(auth.restore()).resolves.toBe(true);
    http.expectNone(`${environment.apiUrl}/api/auth/me`);

    auth.storeToken('token-b');
    expect(auth.profile()).toBeNull();
    expect(TestBed.inject(TenantStateService).name()).toBe('');

    const second = auth.restore();
    const request = http.expectOne(`${environment.apiUrl}/api/auth/me`);
    expect(request.request.headers.get('Authorization')).toBe('Bearer token-b');
    request.flush({ ...me, displayName: 'Other Admin', tenantName: 'Other University' });
    await expect(second).resolves.toBe(true);
    expect(auth.profile()?.tenantName).toBe('Other University');
    http.verify();
  });

  it('clears the profile and tenant when a call returns 401', async () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    const tenant = TestBed.inject(TenantStateService);

    auth.storeToken('token-a');
    const restored = auth.restore();
    http.expectOne(`${environment.apiUrl}/api/auth/me`).flush(me);
    await restored;
    expect(tenant.name()).toBe('Development University');

    TestBed.inject(HttpClient)
      .get(`${environment.apiUrl}/api/auth/me`)
      .subscribe({ error: () => undefined });
    http.expectOne(`${environment.apiUrl}/api/auth/me`).flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(auth.profile()).toBeNull();
    expect(auth.token()).toBeNull();
    expect(tenant.name()).toBe('');
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
    http.verify();
  });

  it('keeps the token when loading the profile fails temporarily', async () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);

    auth.storeToken('token-a');
    const restored = auth.restore();
    http.expectOne(`${environment.apiUrl}/api/auth/me`).flush({}, { status: 503, statusText: 'Unavailable' });

    await expect(restored).resolves.toBe(false);
    expect(auth.token()).toBe('token-a');
    expect(auth.profile()).toBeNull();
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBe('token-a');
    http.verify();
  });

  it('clears the shell when the token store is cleared', async () => {
    const auth = TestBed.inject(AuthService);
    const session = TestBed.inject(AuthSession);
    const http = TestBed.inject(HttpTestingController);

    auth.storeToken('token-a');
    const restored = auth.restore();
    http.expectOne(`${environment.apiUrl}/api/auth/me`).flush(me);
    await restored;

    session.clear();

    expect(auth.profile()).toBeNull();
    expect(TestBed.inject(TenantStateService).name()).toBe('');
    http.verify();
  });
});
