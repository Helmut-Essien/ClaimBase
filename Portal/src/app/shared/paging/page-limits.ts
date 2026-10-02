/**
 * Paging bounds for list screens.
 * Must match `PageLimits`: default 20, cap 100.
 */
export const PAGE_LIMITS = {
  defaultPage: 1,
  defaultSize: 20,
  maxSize: 100,
} as const;

/** One page from a list endpoint. */
export interface PagedResult<T> {
  /** Rows for this page. */
  items: T[];
  /** 1-based page number. */
  page: number;
  /** Requested page size. */
  pageSize: number;
  /** Total rows matching the filter, before paging. */
  totalCount: number;
}
