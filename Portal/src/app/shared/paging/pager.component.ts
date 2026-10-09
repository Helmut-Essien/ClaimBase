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
        <p class="text-center text-muted tabular-nums">{{ rangeStart() }}–{{ rangeEnd() }} of {{ totalCount() }}</p>
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

  /** First row number on this page. A page past the end does not start after the total. */
  readonly rangeStart = computed(() => {
    const start = (this.page() - 1) * this.pageSize() + 1;
    return Math.min(Math.max(start, 1), Math.max(this.totalCount(), 1));
  });

  /** Last row number on this page, capped at the total. */
  readonly rangeEnd = computed(() => {
    const end = Math.min(this.page() * this.pageSize(), this.totalCount());
    return Math.max(end, this.rangeStart());
  });
}
