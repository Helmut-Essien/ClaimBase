---
iteration: 2
max_iterations: 0
completion_promise: "NO ISSUES"
---

Review the uncommitted slice 3 backend rates changes with Bugbot, Security Review, and a senior pass. Fix verified issues and repeat until all three are clean.

## Iteration 2

### Findings fixed
- Medium: concurrent teaching or transport creates could both pass the overlap check and store overlapping dates. `EfRateSchedule.LockTimelineAsync` holds a transaction advisory lock across the read, the open-ended close, and the save. `ConcurrentCreates_KeepASingleOpenTeachingRateAndASingleOpenTransportRate` covers it.

### Findings dismissed
- Database exclusion constraint as a second backstop. Dismissed: it needs the `btree_gist` extension, which the rest of the schema does not use, and the API writer is now serialized and tested.
- Catalog existence checks sit outside the lock. Dismissed: same check-then-insert pattern as courses and staff positions. A title deleted in that window fails the foreign key; it does not attach another tenant's catalog row.
- `DateOnly.MaxValue` stands in for an open end. Dismissed: shared `DateRange` behavior, same as staff appointments, and it only matters on 9999-12-31.
- Finance receives the academic-setup forbidden message. Dismissed: shared `SetupAccess`; the status is 403 and the wording is the existing contract.
- `hashtextextended` collisions. Dismissed: a collision only makes two timelines wait on each other. It does not mix tenants or skip the overlap check.
- A bounded replacement leaves a gap after its end. Dismissed: the previous open row ends on the new start and is not resumed. That matches the schedule rule.

### Remaining open items
- None.

### Residual notes
- The lock is per teaching cell or per tenant transport timeline. Admin writes are rare, and the transaction covers one read plus one save.
- Lists stay paged, `AsNoTracking`, and projected in SQL. Overlap history for one cell is loaded only on create.
