import { HttpErrorResponse } from '@angular/common/http';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  Injector,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AUTH_FIELD_LIMITS, PASSWORD_RESET_COPY } from '../../../../core/auth/auth.models';
import { AuthService } from '../../../../core/auth/auth.service';
import { requiredTrimmed } from '../../../../shared/validators/required-trimmed';
import { AuthShellComponent } from '../auth-shell/auth-shell.component';
import { readLoginDraftEmail } from '../login/login-draft';

/**
 * Asks the API for a reset link. The confirmation is the same whether or not the address is on file.
 * This page reads the sign-in email draft and does not write `localStorage`.
 */
@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterLink, AuthShellComponent],
  templateUrl: './forgot-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForgotPasswordComponent {
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly outcome = viewChild<ElementRef<HTMLElement>>('outcome');

  /** Shared with the API email limit. */
  readonly limits = AUTH_FIELD_LIMITS;

  /** Disables the button while the request is in flight. */
  readonly submitting = signal(false);

  /** Shown when the request fails. A missing address is not an error. */
  readonly errorMessage = signal<string | null>(null);

  /** API confirmation. Null until the request succeeds. */
  readonly sentMessage = signal<string | null>(null);

  readonly form = inject(FormBuilder).nonNullable.group({
    email: [readLoginDraftEmail(), [requiredTrimmed(), trimmedEmail(), Validators.maxLength(AUTH_FIELD_LIMITS.email)]],
  });

  /** Trims and lowercases the email, then asks for a link. */
  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const email = this.form.controls.email.value.trim().toLowerCase();
    this.errorMessage.set(null);
    this.submitting.set(true);

    this.auth
      .forgotPassword(email)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.submitting.set(false);
          this.sentMessage.set(response.message || PASSWORD_RESET_COPY.linkSent);
          this.focusOutcome();
        },
        error: (error: HttpErrorResponse) => {
          this.submitting.set(false);
          this.errorMessage.set(
            error.status === 429 ? 'Too many attempts. Wait a minute and try again.' : 'The reset link could not be sent. Try again.',
          );
        },
      });
  }

  /** Moves focus to the confirmation that replaced the form, so keyboard focus is not dropped. */
  private focusOutcome(): void {
    afterNextRender(() => this.outcome()?.nativeElement.focus(), { injector: this.injector });
  }
}

/** Validates the trimmed address. Submit still lowercases it. */
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
