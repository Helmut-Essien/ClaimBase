import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../../../environments/environment';
import { AUTH_TOKEN_KEY } from '../../../../core/auth/auth-session';
import { authInterceptor } from '../../../../core/auth/auth.interceptor';
import { PASSWORD_RESET_COPY } from '../../../../core/auth/auth.models';
import { LOGIN_DRAFT_KEY } from '../login/login-draft';
import { ForgotPasswordComponent } from './forgot-password.component';

describe('ForgotPasswordComponent', () => {
  beforeEach(async () => {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(LOGIN_DRAFT_KEY);
    await TestBed.configureTestingModule({
      imports: [ForgotPasswordComponent],
      providers: [
        provideRouter([{ path: 'login', children: [] }]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
  });

  it('prefills the sign-in email and does not write storage', async () => {
    localStorage.setItem(LOGIN_DRAFT_KEY, JSON.stringify({ email: 'Ada@ClaimBase.test', password: 'secret' }));
    const fixture = TestBed.createComponent(ForgotPasswordComponent);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;

    expect((compiled.querySelector('#email') as HTMLInputElement).value).toBe('Ada@ClaimBase.test');
    expect(compiled.querySelector('h2')?.textContent).toContain('Forgot password');
    expect(compiled.querySelector('a[href="/login"]')?.textContent).toContain('Back to sign in');
    expect(compiled.querySelector('button[type="submit"]')?.textContent).toContain('Send reset link');

    fixture.componentInstance.form.controls.email.setValue('other@claimbase.test');
    fixture.detectChanges();

    expect(localStorage.getItem(LOGIN_DRAFT_KEY)).toBe(
      JSON.stringify({ email: 'Ada@ClaimBase.test', password: 'secret' }),
    );
  });

  it('posts a lowercase email and shows the same confirmation either way', async () => {
    localStorage.setItem(AUTH_TOKEN_KEY, 'stale-token');
    const fixture = TestBed.createComponent(ForgotPasswordComponent);
    fixture.componentInstance.form.controls.email.setValue(' Ada@ClaimBase.test ');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    button.click();

    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne(`${environment.apiUrl}/api/auth/forgot-password`);
    expect(request.request.body).toEqual({ email: 'ada@claimbase.test' });
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({ message: PASSWORD_RESET_COPY.linkSent });
    await fixture.whenStable();
    fixture.detectChanges();

    const status = fixture.nativeElement.querySelector('[role="status"]') as HTMLElement;
    expect(status.textContent).toContain(PASSWORD_RESET_COPY.linkSent);
    expect(document.activeElement).toBe(status);
    expect(fixture.nativeElement.querySelector('input')).toBeNull();
    expect(fixture.nativeElement.querySelector('a[href="/login"]')?.textContent).toContain('Back to sign in');
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBe('stale-token');
    expect(localStorage.getItem(LOGIN_DRAFT_KEY)).toBeNull();
    http.verify();
  });

  it('shows an error when the request fails', async () => {
    const fixture = TestBed.createComponent(ForgotPasswordComponent);
    fixture.componentInstance.form.controls.email.setValue('ada@claimbase.test');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    button.click();

    const http = TestBed.inject(HttpTestingController);
    http
      .expectOne(`${environment.apiUrl}/api/auth/forgot-password`)
      .flush({}, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain(
      'The reset link could not be sent. Try again.',
    );
    expect(fixture.nativeElement.textContent).not.toContain(PASSWORD_RESET_COPY.linkSent);
    expect(fixture.nativeElement.querySelector('#email')).not.toBeNull();
    http.verify();
  });
});
