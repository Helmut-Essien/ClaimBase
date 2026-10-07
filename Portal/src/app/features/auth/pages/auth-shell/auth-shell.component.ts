import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Centered sign-in card. The purple gradient stays on the brand panel.
 * Guest pages project their form into the gray half.
 */
@Component({
  selector: 'app-auth-shell',
  templateUrl: './auth-shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthShellComponent {}
