# Rates slice review

## Iteration 4

Bugbot, security review, and the senior pass were run on the uncommitted slice 3 rates work (backend schedule plus Portal `/app/rates`). Two Portal correctness bugs were fixed, then the suite was run again.

### Findings fixed
- Medium: the teaching matrix used the first 100 teaching rows ordered by title, qualification, and start date. A cell whose in-force row was off that page showed “No rate”. `GET /api/rates/teaching` now accepts `on` (half-open `[from, to)`). The matrix requests that date in the tenant zone and follows pages, capped at 10. Covered by `TeachingList_OnADate_ReturnsOnlyTheRowInForce` and the Portal page-2 spec.
- Medium: the grid rendered as soon as the catalog finished, while in-force pages were still loading, so every cell flashed “No rate”. The template waits on `loadingCatalog() || loadingMatrix()`. Covered by `does not show a gap while the amounts in force are still loading`.

### Findings dismissed
- Database exclusion constraint. The API writer is serialized by `pg_advisory_xact_lock`, and a range exclusion needs `btree_gist`, which the rest of the schema does not use. Direct SQL can still insert overlaps. Residual, not an API defect.
- Catalog existence checks sit outside the lock. Same check-then-insert pattern as courses and staff. A title deleted in that window fails the foreign key. It does not attach another tenant’s row. Verified by `MissingCatalogAndOtherTenant_AreNotFound`.
- `DateOnly.MaxValue` stands in for an open end. Shared `DateRange`, same as staff appointments. It only matters on 9999-12-31.
- Finance receives the academic-setup forbidden message. Shared `SetupAccess`. The status is 403. Verified by `HeadOfDepartmentFinanceAndLecturer_CannotSetRates`.
- `hashtextextended` collisions. A collision only makes two timelines wait on each other. Lock keys include the tenant id.
- A bounded replacement leaves a gap after its own end. The previous open row ends on the new start and is not resumed. That matches `ReplaceOpenEnded`.
- No tenant foreign key on `TransportRates`. Isolation is the tenant query filter, same as the other tenant tables.
- Extra `GetTeaching` after save. One indexed read on an admin write.
- History for a cell is oldest-first, page size 20. The matrix shows the in-force amount. The history pager reaches later pages.
- The in-force list filter is not a perfect index match. The table is admin-sized. No new index.
- Several in-force rows for one cell. The server can return more than one; `rateInForce` keeps the latest start.
- `setupGuard` is UX only. `SetupAccess` reads the role from the database.
- A zero amount is a stored amount. Product allows non-negative money.
- Titles and qualifications stop at 100, with a banner. Same catalog page cap as the rest of setup.
- Transport is a paged timeline, not a single “today” cell. The pager is the way to see older rows.

### Remaining open items
- None.

### Residual notes
- The lock is per teaching cell or per tenant transport timeline. The transaction covers one read plus one save. Admin writes are rare. `UseNpgsql` has no retry strategy, so the explicit transaction is valid.
- Lists stay paged, `AsNoTracking`, and projected in SQL.
- Rollback of the `Rates` migration drops `TeachingRates` and `TransportRates` only.
- In-force paging stops at 1,000 rows and shows “Some amounts in force today are not on the matrix.”
- A day boundary between the matrix request and render can omit a row that starts on the new day until the next load.
- Portal behavior was verified with unit tests (34) and a production build. No browser click-through was available.

### Re-review
Bugbot: no bugs. Security review: no medium or higher issues. Senior pass: no further actionable findings.
