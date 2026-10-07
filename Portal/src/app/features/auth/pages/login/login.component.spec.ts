import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AUTH_TOKEN_KEY } from '../../../../core/auth/auth-session';
import { LOGIN_DRAFT_KEY, LoginComponent } from './login.component';

describe('LoginComponent', () => {
  beforeEach(async () => {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(LOGIN_DRAFT_KEY);
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
    expect(compiled.textContent).toContain('Excellence • Morality • Service');
    expect(compiled.querySelector('a[href="/login/forgot-password"]')?.textContent).toContain('Forgot password?');
    expect(compiled.querySelector('#password-hint')?.textContent).toContain('not stored');
    const password = compiled.querySelector('#password') as HTMLInputElement;
    const toggle = compiled.querySelector('button[aria-label="Show password"]') as HTMLButtonElement;
    expect(password.type).toBe('password');
    expect(toggle.getAttribute('aria-pressed')).toBe('false');
  });

  it('restores the email and drops a password saved by an older visit', async () => {
    localStorage.setItem(
      LOGIN_DRAFT_KEY,
      JSON.stringify({ email: 'admin@claimbase.test', password: 'password123' }),
    );
    const fixture = TestBed.createComponent(LoginComponent);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect((compiled.querySelector('#email') as HTMLInputElement).value).toBe('admin@claimbase.test');
    expect((compiled.querySelector('#password') as HTMLInputElement).value).toBe('');
    expect(JSON.parse(localStorage.getItem(LOGIN_DRAFT_KEY) ?? '{}')).toEqual({
      email: 'admin@claimbase.test',
    });
  });

  it('keeps the latest email and does not store the password', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    await fixture.whenStable();
    fixture.componentInstance.form.setValue({
      email: 'finance@claimbase.test',
      password: 'password123',
    });

    expect(JSON.parse(localStorage.getItem(LOGIN_DRAFT_KEY) ?? '{}')).toEqual({
      email: 'finance@claimbase.test',
    });
    expect(localStorage.getItem(LOGIN_DRAFT_KEY)).not.toContain('password123');
  });

  it('points assistive tech at the email error after the field is touched', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    await fixture.whenStable();
    fixture.componentInstance.form.controls.email.markAsTouched();
    fixture.detectChanges();
    const email = fixture.nativeElement.querySelector('#email') as HTMLInputElement;

    expect(email.getAttribute('aria-invalid')).toBe('true');
    expect(email.getAttribute('aria-describedby')).toBe('email-hint email-error');
    expect(fixture.nativeElement.querySelector('#email-error')?.textContent).toContain('Enter a valid email.');
  });

  it('reveals the password when the closed eye is pressed', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    const toggle = compiled.querySelector('button[aria-label="Show password"]') as HTMLButtonElement;
    toggle.click();
    fixture.detectChanges();

    const password = compiled.querySelector('#password') as HTMLInputElement;
    expect(password.type).toBe('text');
    expect(compiled.querySelector('button[aria-label="Hide password"]')).not.toBeNull();
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
    const notice = fixture.nativeElement.querySelector('.cb-note') as HTMLElement;
    expect(notice.textContent).toContain('Use the ClaimBase mobile app');
    expect(fixture.nativeElement.querySelector('.cb-banner')).toBeNull();
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
