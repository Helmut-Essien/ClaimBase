import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Unknown URLs. They stay on this page instead of bouncing to login or home. */
@Component({
  selector: 'app-not-found',
  template: `
    <main class="min-h-dvh bg-paper px-4 py-16 text-ink">
      <h1 class="text-3xl font-bold tracking-tight">Page not found</h1>
      <p class="mt-3 max-w-xl text-base text-slate">That address is not part of ClaimBase.</p>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundComponent {}
