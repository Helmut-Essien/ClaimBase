import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ForgotPasswordComponent } from './forgot-password.component';

describe('ForgotPasswordComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ForgotPasswordComponent],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('sends staff to a tenant admin and back to sign in', async () => {
    const fixture = TestBed.createComponent(ForgotPasswordComponent);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('h2')?.textContent).toContain('Forgot password');
    expect(compiled.textContent).toContain('A tenant admin sets the password');
    expect(compiled.querySelector('a[href="/login"]')?.textContent).toContain('Back to sign in');
    expect(compiled.querySelector('input')).toBeNull();
  });
});
