# ClaimBase slices

Deliver **one slice at a time**. Confirm with the user before starting the next. Every slice follows [production.md](production.md), [performance.md](performance.md), and [documentation.md](documentation.md).

Code in a slice is unfinished until public members have XML or JSDoc and non-obvious product rules have an inline comment. Do not leave documentation for a later pass. The same slice must be fast on its hot path and idiomatic for the stack it touches: Clean Architecture boundaries, EF Core and PostgreSQL practices, Angular standalone practices, and MAUI MVVM. See [performance.md](performance.md).

Slice 0 is in the tree. Later slices stay `Planned` until that slice is built.

| Slice | Focus | Status |
|-------|-------|--------|
| 0 | Scaffold: `ClaimBase.sln`, Clean Architecture, Portal, MAUI shell, Docker Postgres, README run steps | **Done** |
| 1 | Identity: tenant, users, JWT, roles, EF global tenant filter, Portal login shell | Planned |
| 2 | Academic setup: faculties, departments, semesters, qualifications, courses, staff, department assignments, and position records on each lecturer | Planned |
| 3 | Rates: teaching matrix (position × qualification) and tenant transport rate, overlap rejection | Planned |
| 4 | Session logs: API semester gate, Portal list, MAUI offline log + sync | Planned |
| 5 | Claims: builder, snapshots, HoD then Finance approval, Portal review | Planned |
| 6 | Biometric Excel import, presence flags, on-demand QuestPDF | Planned |
| 7 | Lecturer corrections and post-approval adjustment claims | Planned |

## Slice 0 acceptance

- Solution name `ClaimBase`. Folders `Backend`, `Portal`, `MobileApp` match [SKILL.md](SKILL.md).
- Api starts, Portal production build succeeds, MAUI project compiles.
- Postgres via `docker-compose` on port **5434**, database `claimbase_db`.
- `Directory.Build.props` turns CS1591 into an error on `Backend/src` and on `MobileApp`. Test projects suppress it.
- Public scaffold types (`Program`, health endpoint, DI registration, MAUI `App` / shell) already have XML docs. Comment why the tenant filter and Hangfire are registered the way they are, once those exist.
- README run commands match the projects that were created.
- No business endpoints yet. `GET /health` is enough.

## Slice 1 acceptance

- Sign-in with email and password. JWT carries `sub`, `tenantId`, `role`, and `departmentId` for HoD.
- Seed one development tenant and one user per role. Document the passwords in `appsettings.Development.json` only.
- Global query filter on `TenantId`. Integration test proves tenant A cannot read tenant B.
- Portal: `/login` and an empty `/app` shell. Lecturer accounts receive 403 on Portal routes.

## Slice 2 acceptance

- Semester statuses: `Draft` → `Open` → `Closed`. Session writes do not exist yet; the status model does.
- Qualifications are a tenant lookup. Position titles are a tenant lookup used only by rates and by staff position records (not a fixed enum of "Degree" / "Professor").
- Courses have a unique code per tenant and one qualification.
- A faculty owns its departments. Department names are unique inside the faculty.
- Staff have `PartTime` or `FullTime`, at least one department assignment, an optional `BiometricId` equal to the id on the biometric device when the lecturer has one, and an optional lecturer user. A lecturer may belong to more than one department.
- Each position appointment is a child of that staff member: title, effective from, effective to. Overlapping ranges for the same lecturer are rejected. There is no position screen that is not the staff member's record.
- Portal screens for these setups, with `*_FIELD_LIMITS`.

## Slice 3 acceptance

- Teaching rate unique on (tenant, position, qualification, effective range). Overlaps → 409.
- Transport rate is one timeline per tenant, not per position.
- A date with no row is a gap. The API does not invent an amount.
- Portal rate matrix: rows are positions, columns are qualifications, with effective dates.

## Slice 4 acceptance

- Lecturer (and Portal on behalf of a lecturer, Admin / TenantAdmin only) creates a session: course code, start, end.
- Semester must be `Open` and the local start date must fall inside it.
- End after start. Overlap with another session for that staff member → 409.
- MAUI stores the log locally, syncs with a client-generated id, and keeps the lecture times the lecturer entered. `RecordedAt` is when the device saved the log. Login, log, and session status screens use MVVM as in [mobile.md](mobile.md).
- Sync of an already accepted client id returns the existing session (idempotent).
- Portal lists sessions paged. Biometric data is not required to save a session.

## Slice 5 acceptance

- Build a draft claim for one staff member and one semester from submitted sessions.
- Each teaching line snapshots position, qualification, rate, currency, start, end, and amount = rate × 1.
- One transport line per distinct local day, snapshotted from the transport rate on that date.
- Sessions with no teaching rate, or days with no transport rate, are listed as exceptions and omitted from the claim.
- HoD submits a decision for lecturers assigned to that HoD's department. A lecturer in several departments needs only one of those HoDs to approve. Finance approves or returns the claim. Approved claims reject further rebuilds.
- Building a claim copies faculty and department names onto `ClaimDepartment`.
- Part-time and full-time staff both produce claims.
- `BiometricPresent` exists on the line and stays false until slice 6. Amounts do not wait for it.

## Slice 6 acceptance

- Upload an `.xlsx` biometric export. Store the file hash. The same hash for the same tenant does not insert punches again.
- Each punch stores the device id. `StaffId` is set only when `Staff.BiometricId` equals that id. Unknown device ids stay on an exception list with `StaffId` null until an admin saves that same id on the lecturer.
- Hangfire parses the file and refreshes `BiometricPresent` (any punch on that local calendar date).
- The job does not update `Amount`, rate snapshots, or claim status.
- `GET` claim PDF (QuestPDF) shows a checkmark when `BiometricPresent` is true and an empty mark when it is false. Totals match the snapshotted lines.

## Slice 7 acceptance

- A lecturer can flag a submitted session that was entered wrongly, before the claim is approved.
- After approval, corrections are a new adjustment claim that references the original line. The original PDF stays reproducible.

## Out of scope until explicitly requested

Timetable import, hourly or per-minute pay, pay holds when the biometric device fails, native push notifications, Redis, per-tenant databases, a public marketing site.
