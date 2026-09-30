import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Root shell. Sign-in and feature routes are added with the identity slice.
 */
@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
