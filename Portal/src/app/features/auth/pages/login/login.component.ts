import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { AUTH_FIELD_LIMITS } from '../../../../core/auth/auth.models';
import { AuthService } from '../../../../core/auth/auth.service';
import { requiredTrimmed } from '../../../../shared/validators/required-trimmed';

/**
 * Staff sign-in. There is no signup. A lecturer response is cleared and the mobile-app message is shown.
 */
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Shared with the API password and email limits. */
  readonly limits = AUTH_FIELD_LIMITS;

  /** Disables the button while the request is in flight. */
  readonly submitting = signal(false);

  /** Shown when sign-in fails. */
  readonly errorMessage = signal<string | null>(null);

  /** True when the API returned a lecturer role. */
  readonly lecturerBlocked = this.auth.lecturerBlocked;

  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [requiredTrimmed(), trimmedEmail(), Validators.maxLength(AUTH_FIELD_LIMITS.email)]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(AUTH_FIELD_LIMITS.passwordMin),
        Validators.maxLength(AUTH_FIELD_LIMITS.passwordMax),
      ],
    ],
  });

  /** Trims and lowercases the email, then signs in. */
  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const email = this.form.controls.email.value.trim().toLowerCase();
    const password = this.form.controls.password.value;
    this.auth.beginLogin();
    this.errorMessage.set(null);
    this.submitting.set(true);

    this.auth
      .login(email, password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.submitting.set(false);
          // Lecturers belong in the mobile app. The portal must not keep their token.
          if (response.role === 'Lecturer') {
            this.auth.rejectLecturer();
            return;
          }
          this.auth.storeToken(response.token);
          void this.router.navigateByUrl('/app');
        },
        error: (error: HttpErrorResponse) => {
          this.submitting.set(false);
          this.errorMessage.set(
            error.status === 401 ? 'Email or password is incorrect.' : 'Sign-in failed. Try again.',
          );
        },
      });
  }
}

/** Validates the trimmed address so a trailing space does not block sign-in. Submit still lowercases it. */
function trimmedEmail(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (typeof control.value !== 'string') {
      return { email: true };
    }
    const trimmed = control.value.trim();
    if (trimmed.length === 0) {
      return null;
    }
    return Validators.email({ value: trimmed } as AbstractControl);
  };
}
