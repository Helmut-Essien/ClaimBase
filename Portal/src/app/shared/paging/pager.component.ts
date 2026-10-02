import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/**
 * Previous and next controls for one API page.
 * Hidden when the filter already fits on a single page.
 */
@Component({
  selector: 'app-pager',
  template: `
    @if (totalCount() > pageSize()) {
      <div class="mt-4 flex items-center justify-between gap-3 text-sm">
        <button
          type="button"
          class="cb-button cb-button-secondary"
          [disabled]="page() <= 1"
          (click)="pageChange.emit(page() - 1)"
        >
          Previous
        </button>
        <p>Page {{ page() }} of {{ pageCount() }}</p>
        <button
          type="button"
          class="cb-button cb-button-secondary"
          [disabled]="page() >= pageCount()"
          (click)="pageChange.emit(page() + 1)"
        >
          Next
        </button>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PagerComponent {
  /** 1-based page currently shown. */
  readonly page = input.required<number>();

  /** Page size requested from the API. */
  readonly pageSize = input.required<number>();

  /** Total rows matching the filter. */
  readonly totalCount = input.required<number>();

  /** Emits the next 1-based page. */
  readonly pageChange = output<number>();

  /** Page count derived from the total. At least 1. */
  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
}
