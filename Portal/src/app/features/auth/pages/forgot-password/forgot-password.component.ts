import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthShellComponent } from '../auth-shell/auth-shell.component';

/**
 * Forgot-password entry. There is no reset email yet, so the page sends staff to a tenant admin.
 * It does not collect a new password and does not write the login draft.
 */
@Component({
  selector: 'app-forgot-password',
  imports: [RouterLink, AuthShellComponent],
  templateUrl: './forgot-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForgotPasswordComponent {}
