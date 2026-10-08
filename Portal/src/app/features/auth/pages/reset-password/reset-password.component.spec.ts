import { Location } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';

import { environment } from '../../../../../environments/environment';
import { AUTH_TOKEN_KEY } from '../../../../core/auth/auth-session';
import { authInterceptor } from '../../../../core/auth/auth.interceptor';
import { AUTH_FIELD_LIMITS, PASSWORD_RESET_COPY } from '../../../../core/auth/auth.models';
import { LOGIN_DRAFT_KEY } from '../login/login-draft';
import { ResetPasswordComponent } from './reset-password.component';

describe('ResetPasswordComponent', () => {
  beforeEach(() => {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(LOGIN_DRAFT_KEY);
  });

  async function create(query: Record<string, string>): Promise<ReturnType<typeof TestBed.createComponent<ResetPasswordComponent>>> {
    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        provideRouter([
          { path: 'login', children: [] },
          { path: 'login/forgot-password', children: [] },
        ]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(query) } },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ResetPasswordComponent);
    await fixture.whenStable();
    return fixture;
  }

  it('hides the password fields when the link is missing', async () => {
    const fixture = await create({});
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain(PASSWORD_RESET_COPY.invalidLink);
    expect(compiled.querySelector('input')).toBeNull();
    expect(compiled.querySelector('a[href="/login/forgot-password"]')?.textContent).toContain('Request a new one');
  });

  it('hides the password fields when the token is too long', async () => {
    const fixture = await create({
      email: 'ada@claimbase.test',
      token: 'x'.repeat(AUTH_FIELD_LIMITS.resetToken + 1),
    });

    expect(fixture.nativeElement.textContent).toContain(PASSWORD_RESET_COPY.invalidLink);
    expect(fixture.nativeElement.querySelector('input')).toBeNull();
  });

  it('starts both passwords masked and does not post a mismatch', async () => {
    const fixture = await create({ email: 'Ada@ClaimBase.test', token: 'raw-token' });
    const compiled = fixture.nativeElement as HTMLElement;
    const inputs = compiled.querySelectorAll('input');
    const newToggle = compiled.querySelector('button[aria-label="Show new password"]');
    const confirmToggle = compiled.querySelector('button[aria-label="Show confirm password"]');

    expect(compiled.textContent).toContain('ada@claimbase.test');
    expect(compiled.textContent).not.toContain('raw-token');
    expect(TestBed.inject(Location).path()).toBe('/login/reset-password');
    expect(inputs.length).toBe(2);
    expect(inputs[0].type).toBe('password');
    expect(inputs[1].type).toBe('password');
    expect(newToggle?.getAttribute('aria-pressed')).toBe('false');
    expect(confirmToggle?.getAttribute('aria-pressed')).toBe('false');

    fixture.componentInstance.form.setValue({ newPassword: 'password1', confirmPassword: 'password2' });
    fixture.componentInstance.submit();
    fixture.detectChanges();

    TestBed.inject(HttpTestingController).expectNone(`${environment.apiUrl}/api/auth/reset-password`);
    expect(compiled.textContent).toContain('Passwords do not match.');
    expect(localStorage.getItem(LOGIN_DRAFT_KEY)).toBeNull();
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
  });

  it('shows success and does not sign in', async () => {
    localStorage.setItem(AUTH_TOKEN_KEY, 'stale-token');
    const fixture = await create({ email: 'ada@claimbase.test', token: 'raw-token' });
    fixture.componentInstance.form.setValue({ newPassword: 'password123', confirmPassword: 'password123' });
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    button.click();

    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne(`${environment.apiUrl}/api/auth/reset-password`);
    expect(request.request.body).toEqual({
      email: 'ada@claimbase.test',
      token: 'raw-token',
      newPassword: 'password123',
      confirmPassword: 'password123',
    });
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({ message: PASSWORD_RESET_COPY.reset });
    await fixture.whenStable();
    fixture.detectChanges();

    const status = fixture.nativeElement.querySelector('[role="status"]') as HTMLElement;
    expect(status.textContent).toContain(PASSWORD_RESET_COPY.reset);
    expect(document.activeElement).toBe(status);
    expect(fixture.nativeElement.querySelector('a[href="/login"]')?.textContent).toContain('Back to sign in');
    expect(fixture.nativeElement.querySelector('input')).toBeNull();
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBe('stale-token');
    http.verify();
  });

  it('offers a new link when the token is rejected', async () => {
    const fixture = await create({ email: 'ada@claimbase.test', token: 'raw-token' });
    fixture.componentInstance.form.setValue({ newPassword: 'password123', confirmPassword: 'password123' });
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).click();

    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`${environment.apiUrl}/api/auth/reset-password`).flush(
      { errors: [{ property: 'Token', message: PASSWORD_RESET_COPY.invalidToken }] },
      { status: 400, statusText: 'Bad Request' },
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain(PASSWORD_RESET_COPY.invalidToken);
    expect(fixture.nativeElement.querySelector('a[href="/login/forgot-password"]')?.textContent).toContain(
      'Request a new one',
    );
    expect(fixture.nativeElement.querySelector('input')).toBeNull();
    http.verify();
  });
});
