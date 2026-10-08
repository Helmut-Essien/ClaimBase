import { Location } from '@angular/common';
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
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AUTH_FIELD_LIMITS, PASSWORD_RESET_COPY } from '../../../../core/auth/auth.models';
import { AuthService } from '../../../../core/auth/auth.service';
import { readApiError } from '../../../../shared/http/read-api-error';
import { AuthShellComponent } from '../auth-shell/auth-shell.component';

/**
 * Sets a new password from the emailed link. The query carries the email and token.
 * Nothing on this page is written to `localStorage`, and a success does not sign the user in.
 */
@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink, AuthShellComponent],
  templateUrl: './reset-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordComponent {
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly location = inject(Location);
  private readonly injector = inject(Injector);
  private readonly query = inject(ActivatedRoute).snapshot.queryParamMap;
  private readonly outcome = viewChild<ElementRef<HTMLElement>>('outcome');

  constructor() {
    // The emailed link is read once. Leaving it in the address bar keeps the raw token in history.
    if (this.query.keys.length > 0) {
      this.location.replaceState('/login/reset-password');
    }
  }

  /** Shared with the API password limits. */
  readonly limits = AUTH_FIELD_LIMITS;

  /** Lowercased email from the link. */
  readonly email = (this.query.get('email') ?? '').trim().toLowerCase();

  /** Raw token from the link. It is posted once and never shown. */
  readonly token = (this.query.get('token') ?? '').trim();

  /** A link without both values cannot choose a password. */
  readonly linkValid =
    this.email.includes('@') &&
    this.token.length > 0 &&
    this.token.length <= AUTH_FIELD_LIMITS.resetToken;

  /** Disables the button while the request is in flight. */
  readonly submitting = signal(false);

  /** Shown for a rejected link. The password fields stay hidden. */
  readonly linkError = signal<string | null>(this.linkValid ? null : PASSWORD_RESET_COPY.invalidLink);

  /** Shown when the request fails for a reason other than the link. */
  readonly errorMessage = signal<string | null>(null);

  /** API confirmation. Null until the password is changed. */
  readonly doneMessage = signal<string | null>(null);

  /** False keeps each password masked and shows the closed eye. */
  readonly passwordVisible = signal(false);
  readonly confirmVisible = signal(false);

  readonly form = inject(FormBuilder).nonNullable.group(
    {
      newPassword: [
        '',
        [
          Validators.required,
          Validators.minLength(AUTH_FIELD_LIMITS.passwordMin),
          Validators.maxLength(AUTH_FIELD_LIMITS.passwordMax),
        ],
      ],
      confirmPassword: ['', [Validators.required, Validators.maxLength(AUTH_FIELD_LIMITS.passwordMax)]],
    },
    { validators: passwordsMatch },
  );

  /** Switches the new-password field between masked and visible. */
  togglePassword(): void {
    this.passwordVisible.update((visible) => !visible);
  }

  /** Switches the confirm field between masked and visible. */
  toggleConfirm(): void {
    this.confirmVisible.update((visible) => !visible);
  }

  /** Moves focus to the message that replaced the form, so keyboard focus is not dropped. */
  private focusOutcome(): void {
    afterNextRender(() => this.outcome()?.nativeElement.focus(), { injector: this.injector });
  }

  /** Posts the link and the new password. A mismatch never leaves the browser. */
  submit(): void {
    if (!this.linkValid || this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.submitting.set(true);
    this.auth
      .resetPassword(
        this.email,
        this.token,
        this.form.controls.newPassword.value,
        this.form.controls.confirmPassword.value,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.submitting.set(false);
          this.doneMessage.set(response.message || PASSWORD_RESET_COPY.reset);
          this.focusOutcome();
        },
        error: (error: HttpErrorResponse) => {
          this.submitting.set(false);
          if (error.status === 429) {
            this.errorMessage.set('Too many attempts. Wait a minute and try again.');
            return;
          }
          const message = readApiError(error, '');
          if (error.status === 400 && message.includes(PASSWORD_RESET_COPY.invalidToken)) {
            this.linkError.set(PASSWORD_RESET_COPY.invalidToken);
            this.focusOutcome();
            return;
          }
          this.errorMessage.set(message || 'The password could not be reset. Try again.');
        },
      });
  }
}

/** Requires the two password fields to be equal. Empty confirm is handled by its own required rule. */
function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  const password = control.get('newPassword')?.value;
  const confirm = control.get('confirmPassword')?.value;
  if (typeof confirm !== 'string' || confirm.length === 0) {
    return null;
  }
  return password === confirm ? null : { mismatch: true };
}
