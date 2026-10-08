---
name: claimbase
description: >-
  Guides the ClaimBase multi-tenant lecturer-claims SaaS: Angular Portal,
  ASP.NET Core Clean Architecture API, .NET MAUI lecturer app using MVVM,
  PostgreSQL, JWT, Hangfire, QuestPDF. Session logs are the payable source;
  biometric punches are a PDF checkmark only. Enforces effective-dated
  hourly position × qualification rates that carry into later semesters
  and are snapshotted onto claim lines, semester gating, and full-stack
  field constraints. Generated code must ship with
  XML or JSDoc on every public member and inline comments on product
  invariants. Generated code is performance-first and idiomatic for Clean
  Architecture, ASP.NET Core, EF Core, Angular, and MAUI MVVM: paged
  indexed queries, async I/O, OnPush screens, and commands off the UI
  thread. Use for this repo, any implementation slice, tenants,
  semesters, courses, rates, sessions, claims, biometric import, PDF,
  Portal, MobileApp, forgot password, or reset password.
---

# ClaimBase

Multi-tenant SaaS that pays university lecturers for sessions they log. Companion files: [phases.md](phases.md), [reference.md](reference.md), [documentation.md](documentation.md), [production.md](production.md), [performance.md](performance.md), [mobile.md](mobile.md).

## Vision

1. Tenant admins open a semester, then maintain campuses, faculties, courses, qualifications, and staff. A campus owns its faculties, and a faculty owns its departments. Each lecturer's positions are records on that staff member. Teaching and transport rates are a separate dated schedule for the whole university. Opening a semester does not require new rates, and a campus does not get its own rate matrix.
2. Lecturers log each session on MobileApp: course code, start, end. The phone works offline and syncs later.
3. The claim engine prices each session with the hourly rate for that lecturer's rank and the course qualification in force on that date, multiplied by the session length in hours, plus one transport amount per distinct teaching day.
4. HoD reviews, Finance approves, and the PDF is generated on demand from the frozen lines.
5. Biometric Excel imports add a presence checkmark beside each session on that PDF.

**Payable source:** submitted session logs plus snapshotted rates.

**Report-only:** biometric punches. A power cut, a faulty device, or a missing file never reduces a claim amount.

**Non-goals:** timetable import, a separate rate setup for every semester, using punches as a pay gate, one database per tenant, Redis.

## Delivery rules

1. **One slice at a time** — full code, tests, config, then stop.
2. **Ask for confirmation** before the next slice.
3. No placeholders (`// TODO`, `// add logic here`).
4. External setup (Postgres) → `docker-compose` plus notes in [README.md](../../../README.md).
5. See [phases.md](phases.md) for slice scope. See [reference.md](reference.md) for entities, APIs, and Portal conventions. See [documentation.md](documentation.md) for XML/JSDoc. See [mobile.md](mobile.md) for MAUI.
6. **Constraints are full-stack** — any new field limit ships in the same slice on Domain + EF + FluentValidation + Shared DTOs and on the client that edits it (Portal `*_FIELD_LIMITS` or MAUI constants). Checklist: [reference.md](reference.md).
7. **Document generated code in the same edit** — a slice is incomplete without it. Every public C# member gets XML (`///`). Every exported Portal symbol gets JSDoc (`/** */`). Inline comments capture product rules a later edit might "simplify" (semester gate, rate snapshot, transport once per day, biometric flag not changing amount, tenant filter, approved-claim freeze, offline `clientId`). Do not narrate obvious lines. Full rules: [documentation.md](documentation.md).
8. **Production-ready by default** — [production.md](production.md).
9. **Fast, and idiomatic for each stack** — generated code is highly optimized for the hot path and follows both ClaimBase product rules and the usual best practices of Clean Architecture, ASP.NET Core, EF Core, MediatR, FluentValidation, Angular, MAUI MVVM, PostgreSQL, JWT, Hangfire, and QuestPDF. Page and index queries, project DTOs in SQL, stay async, lazy-load Portal routes with `OnPush`, and keep MAUI work off the UI thread. Do not add Redis or a second cache. Full rules: [performance.md](performance.md). Hangfire is only for biometric import and presence-flag refresh. Password-reset email uses a background channel, not Hangfire.

## Technology stack

| Layer | Technology |
|-------|------------|
| Portal | Angular standalone + Tailwind + Signals |
| MobileApp | .NET MAUI, MVVM (CommunityToolkit.Mvvm), local SQLite, sync outbox |
| Backend | ASP.NET Core Web API, MediatR, FluentValidation |
| ORM | EF Core + Npgsql |
| Database | PostgreSQL (Docker) |
| IDs | ULID strings via NUlid (`Ulid.NewUlid().ToString()`), 26 characters. Never `Guid` and never the `Ulid` type on the wire |
| Auth | ClaimBase-issued JWT. Forgot-password and reset-password use a one-hour single-use link |
| Passwords | BCrypt. A reset stores only the SHA-256 hash of the link token |
| Jobs | Hangfire (biometric import, presence refresh) |
| PDF | QuestPDF, generated on request |
| Excel | ClosedXML (or equivalent) for biometric workbooks |
| Tests | xUnit, NSubstitute, FluentAssertions, Testcontainers.PostgreSql |

Pin the current LTS/STS SDK and Angular major in the slice 0 project files. Do not invent a second stack.

## Solution layout

```
ClaimBase.sln
Portal/                                # Angular project name: portal
  src/app/
    core/                              # auth session, shell, tenant state
    shared/                            # pipes, validators, dumb UI
    features/                          # mirrors Application/Features
MobileApp/                             # MAUI app name: ClaimBase.MobileApp
Backend/src/
  ClaimBase.Api/
  ClaimBase.Application/
  ClaimBase.Domain/
  ClaimBase.Infrastructure/
  ClaimBase.Shared/
Backend/tests/
```

Root namespaces match project names. Keep the folders `Backend`, `Portal`, and `MobileApp`.

### Portal ↔ API mapping

| Backend | Angular |
|---------|---------|
| `Application/Features/{Name}/` | `features/{name}/` |
| `Shared/DTOs/{Name}/*.cs` | `features/{name}/data/*.models.ts` + `*_FIELD_LIMITS` |
| `Api/Controllers/{Name}Controller` | `features/{name}/data/*.api.ts` |
| FluentValidation | Angular validators + `[attr.maxlength]` |
| JWT / tenancy | `core/auth` + `core/tenant` |

Domain entities never go to Portal or MobileApp. Core must not import features.

## Strict layering

| Type | Location | Never in |
|------|----------|----------|
| Entities / invariants | `ClaimBase.Domain` | Api, Shared, clients |
| Use cases / handlers | `ClaimBase.Application/Features/{Feature}/` | Api, Infrastructure (except tests) |
| Persistence, JWT, Hangfire, PDF, Excel | `ClaimBase.Infrastructure` | Domain |
| Public HTTP contracts | `ClaimBase.Shared/DTOs/` | Domain entities on the wire |
| Controllers | `ClaimBase.Api` | Business rules |
| Admin UI | `Portal/src/app/` | Backend projects |
| Lecturer UI | `MobileApp/` | Portal |

Application depends on Domain + Shared only. Infrastructure implements Application interfaces. Api wires both.

This is a modular monolith: one API process, feature folders (`Identity`, `Academic`, `Rates`, `Sessions`, `Claims`, `Biometrics`). A handler may use Domain types. It must not call another feature's command handler. Presence refresh may set `ClaimLine.BiometricPresent` only. It must not write `Amount`.

`Staff` owns its position records (`StaffPosition`: title, effective from, effective to). A position is not a top-level record beside the lecturer. The `Position` table is only the shared title catalog (Professor, Senior Lecturer, Lecturer) so the rate matrix can price every lecturer who holds that title. Adding a title does not assign it to anyone. Assigning it creates a child record on that staff member.

## Security and tenancy

- **Tenant** is the university. Every business table has `TenantId`. EF global query filter from JWT `tenantId`.
- Cross-tenant ids return 404.
- JWT claims: `sub` = userId, `tenantId`, `role`, `departmentId` when the user is a Head of Department, and `pwd` (unix seconds) after a password reset.
- Forgot password always returns the same message. A link is sent only when exactly one account uses that email. The raw token is emailed, never stored, never logged in Production, and never returned by the API. The link lasts one hour. A newer request replaces the unused link. Reset updates the password and rejects access tokens issued before that change. Lecturers use the same API; the link opens the Portal.
- Roles: `TenantAdmin`, `Admin`, `HeadOfDepartment`, `Finance`, `Lecturer`.
- Lecturers use MobileApp. Portal accounts are the other four roles.
- A campus belongs to the university. A faculty belongs to one campus, and a faculty contains departments. Faculty names are unique on that campus, so two campuses may each have a Faculty of Science. A lecturer is assigned to at least one department and may be assigned to more, including departments on different campuses.
- Heads of Department see only lecturers assigned to their department. Finance and admins see the tenant. A lecturer in two departments is visible to both Heads. One HoD decision is enough to send that lecturer's claim to Finance.

## Coding standards

- Nullable reference types, async/await, constructor DI.
- **Primary keys are ULIDs.** Server code calls `EntityIds.New()`. The lecturer app calls `Ulid.NewUlid().ToString()` for `clientId` before the row is stored. Both use the NUlid package. Persist and send the value as a `string` of 26 Crockford characters. Do not use `Guid`, a truncated GUID, or an `Ulid` property on an entity or DTO. Stable development seed ids are fixed 26-character strings so local logins stay the same. New rows are not hand-typed ids.
- Feature folders + MediatR + FluentValidation.
- Private entity setters; factory methods with domain guards.
- Enums stored as PostgreSQL strings; EF CHECK constraints for enum sets and non-empty required strings.
- Shared DTO annotations match FluentValidation and EF `HasMaxLength`.
- Portal: standalone components, `inject()`, `OnPush`, lazy feature routes, Tailwind. Visuals: [claimbase-design-system](../claimbase-design-system/SKILL.md). UX: [claimbase-ui-ux](../claimbase-ui-ux/SKILL.md).
- Portal state: Signals. No NgRx. `takeUntilDestroyed()` for RxJS. `@for` tracks entity id.
- Money: `numeric(18,2)`, non-negative. Currency code lives on the tenant and is snapshotted onto the claim. Default tenant currency is `GHS`.
- Instants are `timestamptz` (UTC). Calendar days use the tenant time zone (default `Africa/Accra`).
- **Comments and docs are part of the code**, not a follow-up. Follow [documentation.md](documentation.md) for every new or changed file in the slice. CS1591 is a build error on `Backend/src` and `MobileApp`.
- Tests: unit tests for every handler and validator; integration tests with Testcontainers.PostgreSql. Test names document behavior. Test projects do not get XML docs.
- Commits only when the user asks.
- Config: nested settings via env `__`. Table in [reference.md](reference.md).
- **Stack idioms are required.** Follow the practices in [performance.md](performance.md) for Clean Architecture, ASP.NET Core, EF Core, MediatR, FluentValidation, Angular, MAUI, PostgreSQL, JWT, Hangfire, and QuestPDF. Product rules and framework practices both apply.

## Claim invariants

1. Session create requires a semester in `Open` whose date range covers the session's local start date.
2. Teaching amount = hourly rate for (position on the session date, course qualification) × hours. Hours are `(EndsAt − StartsAt)` in minutes ÷ 60. The line amount is rounded to 2 decimal places. A Senior Lecturer on a Diploma course can be GHS 10 per hour while a Lecturer on that same course is GHS 5 per hour.
3. Transport amount = transport rate in force on that local date × 1, once per staff member per local calendar day that has a teaching line. Transport is not hourly and does not depend on rank or qualification.
4. Resolve the lecturer's position from the position records owned by that staff member. Resolve qualification from the course. Copy position name, qualification name, rate, currency, and amount onto the claim line.
5. Overlapping effective ranges for the same position + qualification (or for transport) are rejected. A rate with no end date stays in force through later semesters. A new row is added only when management issues a new schedule.
6. A session with no matching rate stays off the claim and appears on the exception list.
7. Draft claims can be rebuilt. `Approved` claims are immutable.
8. `BiometricPresent` is true when the lecturer has a `BiometricId` and a punch on the session's local calendar date carries that same id. A missing id or a missing punch leaves the mark empty and still pays the line.
9. Part-time and full-time staff are both included. `EmploymentType` is printed, not used as a pay filter.
10. The same lecturer cannot have two sessions whose time ranges overlap.

## Local development

Ports stay off OrderFlow's defaults so both products can run together.

```bash
docker compose up -d                                              # Postgres :5434
dotnet run --project Backend/src/ClaimBase.Api/ClaimBase.Api.csproj   # :5190
cd Portal && npm start                                            # :4201
```

```bash
dotnet test ClaimBase.sln
dotnet ef database update --project Backend/src/ClaimBase.Infrastructure --startup-project Backend/src/ClaimBase.Api
dotnet ef migrations add Name --project Backend/src/ClaimBase.Infrastructure --startup-project Backend/src/ClaimBase.Api --output-dir Persistence/Migrations
```

Fill these commands in slice 0 once the projects exist. Until then, do not pretend the solution builds.
