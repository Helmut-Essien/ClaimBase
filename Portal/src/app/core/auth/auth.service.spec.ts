import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

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

    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');
    TestBed.inject(HttpClient)
      .get(`${environment.apiUrl}/api/auth/me`)
      .subscribe({ error: () => undefined });
    http.expectOne(`${environment.apiUrl}/api/auth/me`).flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(navigate).toHaveBeenCalledWith('/login');
    expect(auth.profile()).toBeNull();
    expect(auth.token()).toBeNull();
    expect(tenant.name()).toBe('');
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
    http.verify();
  });

  it('does not leave a reset link when the stored session is rejected', async () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');

    auth.storeToken('stale-token');
    const restored = auth.restore();
    http.expectOne(`${environment.apiUrl}/api/auth/me`).flush({}, { status: 401, statusText: 'Unauthorized' });

    await expect(restored).resolves.toBe(false);
    expect(auth.token()).toBeNull();
    expect(navigate).not.toHaveBeenCalled();
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

  it('leaves a stale token off forgot-password and reset-password', () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);

    auth.storeToken('stale-token');
    auth.forgotPassword('ada@claimbase.test').subscribe({ error: () => undefined });
    auth.resetPassword('ada@claimbase.test', 'raw-token', 'password123', 'password123').subscribe({
      error: () => undefined,
    });

    const forgot = http.expectOne(`${environment.apiUrl}/api/auth/forgot-password`);
    const reset = http.expectOne(`${environment.apiUrl}/api/auth/reset-password`);
    expect(forgot.request.headers.has('Authorization')).toBe(false);
    expect(reset.request.headers.has('Authorization')).toBe(false);

    forgot.flush({}, { status: 401, statusText: 'Unauthorized' });
    reset.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(auth.token()).toBe('stale-token');
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
