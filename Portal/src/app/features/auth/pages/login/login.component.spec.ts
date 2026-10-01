import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AUTH_TOKEN_KEY } from '../../../../core/auth/auth-session';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  beforeEach(async () => {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([{ path: 'app', children: [] }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
  });

  it('shows the product headline', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Lecturer claims from the sessions they log.');
    expect(compiled.textContent).toContain('University staff only.');
  });

  it('clears a lecturer token and shows the mobile app message', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.componentInstance.form.setValue({
      email: ' Lecturer@ClaimBase.test ',
      password: 'password123',
    });
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    button.click();

    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne('http://localhost:5190/api/auth/login');
    expect(request.request.body).toEqual({ email: 'lecturer@claimbase.test', password: 'password123' });
    request.flush({
      token: 'lecturer-token',
      expiresAt: '2026-09-30T20:00:00Z',
      tenantId: 'tenant',
      tenantName: 'Development University',
      userId: 'user',
      email: 'lecturer@claimbase.test',
      displayName: 'Dev Lecturer',
      role: 'Lecturer',
      currencyCode: 'GHS',
    });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Use the ClaimBase mobile app');
    http.verify();
  });

  it('stores a portal token', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.componentInstance.form.setValue({
      email: 'admin@claimbase.test',
      password: 'password123',
    });
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    button.click();

    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne('http://localhost:5190/api/auth/login');
    request.flush({
      token: 'portal-token',
      expiresAt: '2026-09-30T20:00:00Z',
      tenantId: 'tenant',
      tenantName: 'Development University',
      userId: 'user',
      email: 'admin@claimbase.test',
      displayName: 'Dev Admin',
      role: 'Admin',
      currencyCode: 'GHS',
    });
    await fixture.whenStable();

    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBe('portal-token');
    http.verify();
  });
});
