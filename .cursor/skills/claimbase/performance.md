# Performance generation

Read from [SKILL.md](SKILL.md) when implementing a slice. Generated code must be highly optimized for speed and must follow both ClaimBase product rules and the established practices of the frameworks in this solution. A slice that is correct but loads a whole tenant into memory, blocks a thread, skips an index, or bypasses the architecture is not done.

Speed does not override tenancy, rate snapshots, MVVM, paging, or the rule that biometric data never changes an amount. Prefer a simple indexed query over a cache. Do not add Redis.

Hangfire is allowed for two jobs only: parse a biometric import, and refresh `BiometricPresent`. Those jobs must not recalculate money.

## Stack and architecture practices

Apply the usual practice for each technology. ClaimBase rules in [SKILL.md](SKILL.md) still win when they are stricter.

| Stack | Practice |
|-------|----------|
| Clean Architecture | Domain has no infrastructure types. Application orchestrates. Infrastructure implements ports. Api maps HTTP to MediatR and contains no claim math. Dependencies point inward. |
| Modular monolith | One host. A feature handler does not call another feature's command handler. |
| ASP.NET Core | Constructor DI, options pattern, `IHostEnvironment` checks, async actions, problem responses from the exception middleware, health at `GET /health`. Do not new up services inside controllers. |
| MediatR | One command or query per use case. The controller sends the request and returns the DTO. Validation runs in the pipeline via FluentValidation, not as hand-written `if` blocks in the controller. |
| FluentValidation | One validator per write command. Lengths match Domain and Shared DTOs. |
| EF Core | Fluent configuration classes, migrations generated from the model, no lazy loading, `AsNoTracking` on reads, global query filter for `TenantId`, `ExecuteUpdate` / set-based SQL for bulk presence flags. Do not edit migration snapshots by hand. |
| PostgreSQL | `timestamptz` for instants, `numeric` for money, partial or composite indexes that match the `WHERE` clause, `ILIKE` for search. |
| JWT | Validate issuer, audience, and lifetime. Read tenant and role from claims. Do not trust ids in the body for authorization. |
| Hangfire | Enqueue a job with an explicit `TenantId` argument. Keep the job idempotent. Do not capture an HTTP `HttpContext`. |
| QuestPDF | Compose from the snapshotted claim lines. Stream the response. Do not re-query live rates while painting an approved claim. |
| Angular | Standalone components, `inject()`, `OnPush`, Signals, lazy `loadComponent` / `loadChildren`, typed reactive forms, `takeUntilDestroyed()`. The feature `data/` folder owns HTTP. `core/` does not import features. |
| MAUI MVVM | `CommunityToolkit.Mvvm`, DI-constructed view models, compiled bindings where the page sets `x:DataType`, async `[RelayCommand]` for I/O. Pages do not touch SQLite or `HttpClient`. |

## Best practices that keep it fast

- One user action, one round trip. The write response returns the DTO the screen needs.
- Async all the way. `CancellationToken` on every I/O. No `.Result`, `.Wait()`, or `Task.Run` wrapped around EF or HTTP.
- Filter, sort, and page in PostgreSQL. Project to DTOs in the query. Do not materialize entities to map two fields.
- Add the index in the same slice as the query that needs it. Leading column is `TenantId` on tenant-owned tables.
- Keep payloads small: max lengths, paged lists, PDF streamed, Excel parsed forward and inserted in batches.
- Portal routes are lazy. Components use `OnPush` and Signals. Lists track by id.
- MAUI commands stay off the UI thread. Sync is one batch. Bindings read view-model state, not a fresh query per row.

## Backend

- **Reads:** `AsNoTracking()` on lists and gets. Project to DTOs in SQL when the handler does not mutate entities.
- **Writes:** one transaction per use case. Building a claim inserts the claim and its lines in that transaction. Do not `SaveChanges` per session.
- **Pagination:** collection endpoints use `page` / `pageSize` (default 20, cap 100). Never return every session or punch in the tenant.
- **Indexes:** `(TenantId)` plus filter columns in the same slice. Expected indexes:
  - Course `(TenantId, Code)` unique
  - SessionLog `(TenantId, StaffId, StartsAt)` and unique `(TenantId, ClientId)`
  - TeachingRate `(TenantId, PositionTitleId, QualificationId, EffectiveFrom)`
  - StaffPosition `(TenantId, StaffId, EffectiveFrom)`
  - Department `(TenantId, FacultyId, Name)` unique
  - StaffDepartment `(TenantId, DepartmentId)` and unique `(TenantId, StaffId, DepartmentId)`
  - Claim `(TenantId, SemesterId, StaffId)` unique
  - Staff `(TenantId, BiometricId)` unique; several lecturers may have a null id
  - BiometricPunch `(TenantId, BiometricId, PunchedAt)`
  - BiometricImport `(TenantId, ContentHash)` unique
- **SQL shape:** filter in the database. Search with `ILIKE` on codes and names, not `ToLower().Contains`.
- **N+1:** rate resolution for a claim loads rates and position history for that staff member in bulk, then matches in memory. Do not query once per session.
- **Claim size:** one lecturer's semester is bounded. Still cap omitted-session payloads (return counts plus the first page of ids if the list is large).
- **Presence refresh:** one update statement per claim (or per staff/date set), not a round-trip per line.
- **PDF:** stream the document. Do not load every claim in the semester to print one lecturer.
- **Excel:** parse with a forward-only reader where the library allows. Persist punches in batches inside one transaction per import.
- **Async:** `CancellationToken` on every I/O. Outbound HTTP is not required for MVP. Do not block on `.Result`.

## Portal

- Lazy-load each feature route.
- `ChangeDetectionStrategy.OnPush` on standalone components. Signals for UI state. No NgRx.
- `@for` tracks entity `id`.
- `takeUntilDestroyed()` on subscriptions. Debounce search (~300ms).
- Request one page from the API. Do not download a semester of sessions and filter in the browser.
- No per-row animation on claim tables.

## MobileApp

- Sync sends the outbox in one batch, not one HTTP call per keystroke.
- The local session list reads SQLite from the ViewModel command. It does not block the UI thread on the network.
- ViewModels expose bindable state. Pages do not reload lists from code-behind.
- Course-code lookup hits the API when online and uses the last synced course list when offline. Keep that list to the tenant's courses, not a full staff dump.

## Anti-patterns

- Recomputing an approved claim from live rates while generating the PDF
- Updating `Amount` inside the presence job
- Loading all punches for a tenant to mark one claim
- Unpaged Portal tables
- A second background stack besides Hangfire
- MAUI code-behind that calls the API, writes SQLite, or handles sign-in
- Sync-over-async, per-row queries, or client-side filtering of an unpaged list
- A cache added before the query is indexed and paged

## Slice checklist

```
- [ ] List and get queries are AsNoTracking, paged, and projected to DTOs in SQL
- [ ] New filters have an index that starts with TenantId
- [ ] Writes use one transaction; claim build and punch import do not SaveChanges per row
- [ ] No .Result / .Wait() on I/O
- [ ] Portal feature route is lazy; new components are OnPush; @for tracks id; search is debounced
- [ ] MAUI I/O runs inside an async command, not the page code-behind or the UI thread
- [ ] The change follows that stack's row in "Stack and architecture practices" (layering, MediatR, EF, Angular, or MVVM)
```
