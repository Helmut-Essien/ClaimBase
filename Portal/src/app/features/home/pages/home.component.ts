import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Honest empty home. Counts stay at zero until claims and sessions exist. */
@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeComponent {}
