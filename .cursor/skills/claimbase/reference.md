# ClaimBase reference

Read from [SKILL.md](SKILL.md). This file is the contract to implement. Routes marked here are the target API. Do not add routes that contradict it.

Nothing in this file is implemented until the matching slice in [phases.md](phases.md) is done.

## Tenancy and roles

JSON camelCase. Anonymous `GET /health`.

| Role | Portal | MobileApp | Scope |
|------|--------|-----------|--------|
| `TenantAdmin` | Yes | No | Whole tenant, including users |
| `Admin` | Yes | No | Academic setup, rates, staff, biometric import, session entry on behalf of staff |
| `HeadOfDepartment` | Yes | No | Own `departmentId` only |
| `Finance` | Yes | No | Claims and PDF for the tenant |
| `Lecturer` | No | Yes | Own sessions and own claim status |

JWT claims: `sub`, `tenantId`, `role`, `departmentId` (HoD only), `pwd` (unix seconds of the last password reset; absent until the first reset). A token whose `pwd` claim does not match `User.PasswordChangedAt` is rejected. That check is one primary-key read because there is no refresh-token table to revoke.

Exception middleware:

| Exception | HTTP |
|-----------|------|
| `UnauthorizedAppException` | 401 |
| `ForbiddenAppException` | 403 |
| `NotFoundAppException` | 404 |
| `ConflictAppException` | 409 |
| `ValidationException` | 400 |

Cross-tenant resource ids are 404.

## Target HTTP API

| Method | Route | Roles | Slice |
|--------|-------|-------|-------|
| POST | `/api/auth/login` | Anonymous | 1 |
| POST | `/api/auth/forgot-password` | Anonymous | 1 |
| POST | `/api/auth/reset-password` | Anonymous | 1 |
| GET | `/api/auth/me` | JWT | 1 |
| GET/POST | `/api/campuses` | TenantAdmin, Admin | 2 |
| GET/POST | `/api/faculties` | TenantAdmin, Admin | 2 |
| GET/POST | `/api/departments` | TenantAdmin, Admin | 2 |
| POST | `/api/staff/{id}/departments` | TenantAdmin, Admin | 2 |
| DELETE | `/api/staff/{id}/departments/{departmentId}` | TenantAdmin, Admin | 2 |
| GET/POST/PUT | `/api/semesters` | TenantAdmin, Admin | 2 |
| POST | `/api/semesters/{id}/open` | TenantAdmin, Admin | 2 |
| POST | `/api/semesters/{id}/close` | TenantAdmin, Admin | 2 |
| GET/POST | `/api/qualifications` | TenantAdmin, Admin | 2 |
| GET/POST/PUT | `/api/courses` | TenantAdmin, Admin | 2 |
| GET/POST/PUT | `/api/staff` | TenantAdmin, Admin | 2 |
| GET/POST | `/api/staff/{id}/positions` | TenantAdmin, Admin | 2 |
| GET/POST | `/api/position-titles` | TenantAdmin, Admin | 2 |
| GET/POST | `/api/rates/teaching` | TenantAdmin, Admin | 3 |
| GET/POST | `/api/rates/transport` | TenantAdmin, Admin | 3 |
| GET/POST | `/api/sessions` | Lecturer (own), Admin, TenantAdmin | 4 |
| GET | `/api/sessions/{id}` | Lecturer (own), HoD (department), Admin, TenantAdmin, Finance | 4 |
| POST | `/api/claims/build` | Admin, TenantAdmin, Finance | 5 |
| GET | `/api/claims` | HoD (department), Finance, Admin, TenantAdmin | 5 |
| GET | `/api/claims/{id}` | same | 5 |
| POST | `/api/claims/{id}/hod-decision` | HeadOfDepartment | 5 |
| POST | `/api/claims/{id}/finance-decision` | Finance | 5 |
| POST | `/api/biometric-imports` | Admin, TenantAdmin | 6 |
| GET | `/api/biometric-imports/{id}` | Admin, TenantAdmin | 6 |
| GET | `/api/claims/{id}/pdf` | Finance, Admin, TenantAdmin | 6 |
| POST | `/api/sessions/{id}/exception` | Lecturer (own) | 7 |

Login body: `email`, `password`. Response: `token`, `expiresAt`, `tenantId`, `tenantName`, `userId`, `email`, `displayName`, `role`, `currencyCode`.

Forgot-password body: `email`. Response: `message` = "If that email belongs to a staff account, a reset link is on its way." The same message is returned when the address is unknown and when two tenants share it. No token is returned. The email link is `{Portal:BaseUrl}/login/reset-password?email={email}&token={token}` and lasts one hour. A newer request for that user consumes the previous unused link. The request only looks up the email, then queues the same follow-up for a hit and a miss. The worker creates the hashed link and sends the email, so an unknown address is not faster. Two resets of one link serialize on that user: one succeeds. When `Email:Host` is empty, Development writes the link to gitignored `logs/password-resets.log` and tests run the worker inline. Production requires SMTP and does not write that file.

Reset-password body: `email`, `token`, `newPassword`, `confirmPassword`. Success message: "Password has been reset successfully." A missing, expired, used, or mismatched link is HTTP 400 with "Invalid reset token." A wrong email does not spend the link. Success replaces the bcrypt hash, sets `PasswordChangedAt`, and consumes outstanding links for that user. The caller signs in again. Login, forgot-password, and reset-password share the per-IP and per-email rate limit. `GET /api/auth/me` does not.

Session create: `clientId`, `courseCode`, `startsAt`, `endsAt`. Server assigns the open semester from the local start date. A second POST with the same `clientId` returns the original session.

Claim build body: `staffId`, `semesterId`. Response includes lines and `omittedSessions` (no rate).

HoD decision: `approved` bool, `note?`. Finance decision: `approved` bool, `note?`.

List endpoints accept `page` and `pageSize` (1–100, default 20).

## Domain (specified)

Money is `numeric(18,2)` and ≥ 0. Instants are UTC. A "day" is the calendar date of `StartsAt` in the tenant time zone.

**Tenant:** Id, Name (≤200), CurrencyCode (exactly 3), TimeZoneId (≤64, default `Africa/Accra`), CreatedAt

**User:** Id, TenantId, Email (unique per tenant, lowercase, ≤320), DisplayName (≤200), PasswordHash, PasswordChangedAt? (UTC, whole seconds, null until the first reset), Role, DepartmentId? (required for HoD), StaffId? (required for Lecturer), CreatedAt

**PasswordResetToken:** Id, TenantId, UserId, TokenHash (64 lowercase hex, unique, SHA-256 of the raw token), ExpiresAt, CreatedAt, UsedAt?. The raw token is never stored. Anonymous forgot and reset ignore the tenant filter. A portal query still uses the filter.

**Campus:** Id, TenantId, Name (≤200, unique per tenant). A campus does not own semesters or rates.

**Faculty:** Id, TenantId, CampusId, Name (≤200, unique per campus)

**Department:** Id, TenantId, FacultyId, Name (≤200, unique within the faculty)

**Semester:** Id, TenantId, Name (≤200), StartDate, EndDate, Status (`Draft` \| `Open` \| `Closed`). EndDate ≥ StartDate. Only one `Open` semester may cover a given date (overlapping open semesters are rejected).

**Qualification:** Id, TenantId, Name (≤80, unique per tenant)

**PositionTitle:** Id, TenantId, Name (≤80, unique per tenant). Shared catalog used by the rate matrix. Not a lecturer's appointment.

**Course:** Id, TenantId, Code (≤32, unique per tenant, stored uppercase), Name (≤200), QualificationId

**Staff:** Id, TenantId, StaffNumber (≤32, unique per tenant), DisplayName (≤200), Email? (≤320), EmploymentType (`PartTime` \| `FullTime`), BiometricId? (≤64, unique per tenant when set). When present, `BiometricId` is the id stored on the biometric device for that person. It is not a separate code that an admin maps later. A lecturer can be saved without one. Positions and department assignments are child records, loaded and edited with the staff member. Saving a lecturer with zero departments is rejected. Removing the last department assignment is rejected.

**StaffDepartment:** Id, TenantId, StaffId, DepartmentId. Unique per staff member and department. At least one row per staff member.

**StaffPosition:** Id, TenantId, StaffId, PositionTitleId, EffectiveFrom, EffectiveTo?. Child of `Staff`. Half-open ranges must not overlap for one staff member. This is the record the claim engine reads.

**TeachingRate:** Id, TenantId, PositionTitleId, QualificationId, Amount, EffectiveFrom, EffectiveTo?. `Amount` is cedis per hour in the tenant currency. One cell is one rank × one course qualification (Senior Lecturer × Diploma, Lecturer × Diploma). The rate follows the title, not an individual lecturer. No overlapping ranges for the same pair. `EffectiveTo` null means the amount continues through later semesters. Rates are not stored on a semester and are not copied when a semester is created. Add a row only when management issues a new amount; set the previous row's end to the new row's start.

**TransportRate:** Id, TenantId, Amount, EffectiveFrom, EffectiveTo?. One timeline per tenant, paid once per teaching day for every rank. The same carry-forward rule applies: an open-ended row stays in force until management replaces it.

**SessionLog:** Id, TenantId, ClientId (≤32, unique per tenant), SemesterId, StaffId, CourseId, StartsAt, EndsAt, RecordedAt, Status (`Submitted` \| `Exception` \| `Void`), CourseCode snapshot (≤32). EndsAt > StartsAt. No overlap for the same StaffId.

**BiometricImport:** Id, TenantId, FileName (≤260), ContentHash (64 hex), Status (`Queued` \| `Processed` \| `Failed`), ImportedAt. Unique (TenantId, ContentHash).

**BiometricPunch:** Id, TenantId, ImportId, BiometricId (≤64), StaffId?, PunchedAt. `BiometricId` is copied from the device file. `StaffId` is set when a staff row has that same `BiometricId`. A device id with no staff row stays unmatched (`StaffId` null).

**Claim:** Id, TenantId, SemesterId, StaffId, CurrencyCode snapshot, Status (`Draft` \| `PendingHod` \| `PendingFinance` \| `Approved` \| `Rejected`), CreatedAt. One live claim per (TenantId, SemesterId, StaffId).

**ClaimDepartment:** Id, TenantId, ClaimId, CampusName (≤200), FacultyName (≤200), DepartmentName (≤200). Copied from the lecturer's assignments when the claim is built. The PDF reads this snapshot. Later assignment changes do not rewrite it.

**ClaimLine:** Id, TenantId, ClaimId, LineType (`Teaching` \| `Transport`), SessionLogId? (teaching), TeachingDate, CourseCode?, QualificationName?, PositionName?, RateAmount, Quantity, Amount, StartsAt?, EndsAt?, BiometricPresent, EmploymentType snapshot

On a teaching line, `RateAmount` is the hourly rate and `Quantity` is the session length in hours (minutes ÷ 60). `Amount` is `RateAmount × Quantity`, rounded to 2 decimal places. On a transport line, `Quantity` is 1 and `Amount` equals `RateAmount`. Transport lines have null course fields. `BiometricPresent` defaults false.

## Rate resolution

At build time, for each submitted session:

1. Load that staff member's position record whose range contains the session's local start date.
2. Load the teaching rate for that position and the course qualification whose range contains that date. An open-ended rate from an earlier semester matches. Do not look for a rate row that belongs to the semester.
3. If either is missing, omit the session and return it in `omittedSessions`. Hours are not applied when there is no rate.
4. Group included sessions by local date. For each date, load the transport rate. A missing transport rate omits that day's transport line and lists the date.

Approved claims are not rebuilt. Changing a rate, a position, or a course qualification afterward does not update existing claim lines.

## Presence

A teaching line is present when the lecturer has a `BiometricId` and any punch with that same id has a `PunchedAt` on the session's local calendar date. A lecturer with no biometric id, or a device id that matches nobody, gets an empty mark. Refresh writes `BiometricPresent` only. To link an unknown device id, set that exact id on the staff record and refresh. There is no alias table.

## Constraints (full stack)

When adding a writable field, complete every applicable layer in the same slice.

| Layer | What to add |
|-------|-------------|
| Domain factory | Trim, max length, email lowercase, course code uppercase |
| EF | `HasMaxLength`, required, unique indexes, CHECK for enums and non-empty strings |
| FluentValidation | Same bounds |
| Shared DTO | Matching data annotations |
| Portal or MAUI | `*_FIELD_LIMITS`, validators, maxlength, trim on submit |
| Tests | Validator tests; migration if the schema changed |
| Docs | XML on every new public C# member; JSDoc on every new Portal export; inline comment on each product-rule branch. Same slice. See [documentation.md](documentation.md). |

### Canonical limits

| Field | Max | Notes |
|-------|-----|--------|
| Email | 320 | Lowercase |
| Password | 128 | Min 8. Reset uses the same bounds |
| Reset token | 128 | On the wire only. The column stores a 64-character hash |
| DisplayName / Tenant / Campus / Faculty / Department / Semester / Course name | 200 | Trimmed |
| Qualification / Position name | 80 | |
| Course code | 32 | Uppercase |
| StaffNumber | 32 | |
| BiometricId | 64 | Optional on staff. When set, the same value as the device id. Unique per tenant |
| CurrencyCode | 3 | ISO |
| TimeZoneId | 64 | IANA |
| ClientId | 26 | ULID from the device (`Ulid.NewUlid().ToString()`) |
| Note | 400 | Approval notes |
| FileName | 260 | Biometric workbook |

## Application feature pattern

```
Features/{Name}/
  {Verb}{Name}Command.cs
  {Verb}{Name}CommandHandler.cs
  {Verb}{Name}CommandValidator.cs
```

Register via `AddMediatR` and `AddValidatorsFromAssembly`. Controllers send MediatR requests only.

New external work (Excel, PDF, clock, file storage): interface in `Application/Common/Interfaces`, adapter in `Infrastructure`.

## Portal conventions

```
Portal/src/app/
  core/
    auth/             # session, guards, interceptor, AUTH_FIELD_LIMITS
    layout/           # shell
    tenant/           # TenantStateService (name, currency, time zone)
  shared/
    pipes/            # money pipe using tenant currency
    validators/
  features/
    academic/         # campuses, faculties, departments, semesters, courses, qualifications
    staff/            # lecturer plus their position records
    rates/
    sessions/
    claims/
    biometrics/
  app.routes.ts
  environments/
    environment.ts                 # apiUrl http://localhost:5190
    environment.production.ts      # apiUrl '' same-origin
```

| Path | Guard | Slice |
|------|-------|-------|
| `/login` | guest | 1 |
| `/login/forgot-password` | guest | 1 |
| `/login/reset-password` | guest | 1 |
| `/app` | auth, not Lecturer | 1 |
| `/app/campuses`, `/app/faculties`, `/app/semesters`, `/app/courses`, `/app/staff` | Admin, TenantAdmin | 2 |
| `/app/rates` | Admin, TenantAdmin | 3 |
| `/app/sessions` | Admin, TenantAdmin, HoD, Finance | 4 |
| `/app/claims`, `/app/claims/:id` | HoD, Finance, Admin, TenantAdmin | 5 |
| `/app/biometrics` | Admin, TenantAdmin | 6 |

Add a nav item only when the route exists. Unknown URLs render a 404 page. There is no public marketing site in these slices.

JWT storage key: `claimbase.token`.

## Ports and config

| Service | URL / value |
|---------|-------------|
| API | http://localhost:5190 |
| Portal | http://localhost:4201 |
| Postgres | localhost:5434, db `claimbase_db`, user `claimbase` |
| JWT issuer / audience | `ClaimBase.Api` / `ClaimBase.Portal` |

| Env var | Required | Used for |
|---------|----------|----------|
| `ConnectionStrings__DefaultConnection` | Yes outside Development | PostgreSQL |
| `JWT__KEY` | Yes | Signing key, ≥ 64 chars in Production |
| `Portal__BaseUrl` | Yes in Production | Absolute Portal origin used in reset links. Production must be `https`. Development is `http://localhost:4201` |
| `Email__Host` | Yes in Production | SMTP host. Empty in Development writes the link to gitignored `logs/password-resets.log` |
| `Email__Port` | No | Default 587 |
| `Email__Username` | No | Empty skips SMTP authentication |
| `Email__Password` | No | SMTP password. Never commit a Production value |
| `Email__FromAddress` | Yes in Production | From address on the reset email |
| `Email__FromName` | No | Default `ClaimBase` |
| `Email__UseStartTls` | No | Default true |
| `CORS__ORIGINS` | No | Empty in Production means same-origin Portal |
| `ForwardedHeaders__KnownProxies__0` | No | Extra reverse-proxy IP allowed to set `X-Forwarded-For`. Loopback stays trusted |
| `ForwardedHeaders__KnownNetworks__0` | No | Extra reverse-proxy CIDR allowed to set `X-Forwarded-For` |
| `DataProtection__KeysPath` | Production volume | Key ring |
| `Hangfire__WorkerCount` | No | Default 1 |

Dev secrets live in `appsettings.Development.json` only.
