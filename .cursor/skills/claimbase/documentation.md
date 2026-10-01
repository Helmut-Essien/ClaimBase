# Documentation conventions

Read from [SKILL.md](SKILL.md). Apply this **while writing** each file. A slice that compiles but skips these docs is not done.

Comments explain **why** and invariants. They do not restate the next line.

## Generation gate

Every new or changed file in the slice must satisfy the matching section below before the slice stops.

| Stack | Required | Enforced by |
|-------|----------|-------------|
| `Backend/src` | `///` on every public type and member | CS1591 build error |
| `MobileApp` | `///` on every public type and member, plus XAML comments where noted | CS1591 build error |
| `Portal` | JSDoc on every exported symbol | Review against this file (TypeScript has no CS1591) |
| All three | Inline `//` on the product-rule list at the bottom | Review against this file |
| Tests | No XML. The method name states the behavior. | — |
| EF migrations | No XML. Never hand-edit generated snapshots. | — |

`Directory.Build.props` sets `GenerateDocumentationFile=true` and treats CS1591 as an error for `Backend/src` and `MobileApp`. Test projects set `GenerateDocumentationFile=false` and `NoWarn` CS1591.

## Shared rules

**Write**

- One-sentence summaries that state purpose or the invariant.
- Exceptions callers must handle (`<exception>` in C#, `@throws` in JSDoc when the client branches on that status).
- An inline comment above any block in the product-rule list below.

**Skip**

- Narration of obvious code (`return result`, `inject(HttpClient)`, `i++`).
- Summaries that only repeat the identifier.
- `TODO`, `FIXME`, `HACK`, and commented-out dead code.
- XML on private nested JSON or SQL row types.
- A comment on every Tailwind class or every XAML property.

Public properties still need a one-line `///` even when the name looks obvious. CS1591 does not skip them.

## Backend (C# XML)

Use `///` on public types and members, including constructors, properties, enum members, constants, and `protected` overrides. Private helpers get `//` only when the why is not obvious.

| Kind | Required docs |
|------|----------------|
| Domain entity / factory / mutator | Type summary and every public property. Note max lengths, effective dating, and snapshot fields versus live lookups. |
| Enum | Type summary and every member. |
| `const` / static constraint | One line each (`SessionConstraints.MaxCourseCode`). |
| Command / query | Use-case summary. |
| Handler | Type summary and `Handle`: tenancy, side effects, exceptions. |
| Validator | Type summary and a one-line constructor summary. Extra comment only when a rule is non-obvious. |
| Shared DTO | Type summary and every public property. DataAnnotations do not satisfy CS1591. |
| Controller action | HTTP operation. Note 409 overlap or a frozen claim when that status is part of the contract. |
| Application interface | Port summary and every member. |
| Infrastructure adapter | External system and failure behavior. Implementations of an Application port use `/// <inheritdoc />`. |
| EF configuration | Type summary and `Configure`. Inline comment on each global query filter and CHECK. |
| `Program` / DI extension | Inline comment on tenant-filter registration, JWT claim mapping, and Hangfire job registration. |
| Hangfire job | Public type and `Execute`. State that presence refresh must not change amounts. |
| QuestPDF composer | Which columns are informational (presence mark, duration) and which are the snapshotted money. |

XML tags: `<summary>` always; `<param>` and `<returns>` when they add meaning; `<exception cref="...">` for application exceptions; `<remarks>` for snapshot and timezone rules.

```csharp
/// <summary>
/// Builds a draft claim for one staff member and semester.
/// Amounts come from session logs and the rate in force on each session date.
/// </summary>
/// <exception cref="ConflictAppException">An approved claim already exists for this staff member and semester.</exception>
public sealed class BuildClaimCommandHandler : IRequestHandler<BuildClaimCommand, ClaimDto>
{
    /// <summary>
    /// Snapshots teaching and transport rates onto new lines. Omits sessions that have no rate.
    /// </summary>
    public Task<ClaimDto> Handle(BuildClaimCommand request, CancellationToken cancellationToken)
    {
        // BiometricPresent stays false here. Presence refresh must not recalculate Amount.
        ...
    }
}
```

## Portal (TypeScript / Angular)

Use `/** */` on **exported** APIs. Components document the class when the selector is not enough, plus public methods and Signals that encode workflow.

| Kind | Required docs |
|------|----------------|
| `*.api.ts` | Class summary naming the controller. Every HTTP method. |
| `*.models.ts` | Type summary. `*_FIELD_LIMITS` states that it mirrors Shared DTO / Domain constraints. |
| Validators / pipes | Export summary. `@param` / `@returns` when the contract is not obvious. |
| `AuthService`, `TenantStateService` | Class summary. What each Signal holds and who updates it. |
| Guards / interceptor | Which roles pass, and that a lecturer JWT is rejected. |
| Standalone component | The job of the view. Signals for submitting, paging, and frozen claims. |
| `routes.ts` | File-level note when guards or lazy loading are the point of the file. |
| Template (`.html`) | Comment only for intent classes do not show: sidebar versus bottom nav, safe-area padding, presence mark is not a pay rule. |
| Specs | Test names document behavior. |

```typescript
/**
 * HTTP client for claim review.
 * Mirrors `ClaimsController`. Amounts are the snapshotted line values.
 */
@Injectable({ providedIn: 'root' })
export class ClaimApi {
  /** Downloads the PDF for an approved claim. Rates are not re-resolved. */
  pdf(id: string): Observable<Blob> { /* ... */ }
}

/** Client field limits. Must match `CourseConstraints` and Shared DTO `[StringLength]`. */
export const COURSE_FIELD_LIMITS = { code: 32, name: 200 } as const;
```

```html
<!-- Presence is evidence on the report. An empty mark does not change the amount. -->
```

## MobileApp

Same C# XML rule as `Backend/src`.

| Kind | Required docs |
|------|----------------|
| Sync service | Idempotency on `clientId`. Lecture times are the values entered, not the sync time. |
| Local entity | Which fields are created on the device and which the server assigns. |
| ViewModel | The user action. Document each `[RelayCommand]` and state that the page only binds to it. |
| XAML | Comment only for a non-obvious layout or for offline-save behavior the control name hides. |

## Styles

Custom CSS in `Portal` gets a short comment stating why the rule exists (token mapping, grain limited to the login panel, `prefers-reduced-motion`). Do not comment each utility in a template.

## Inline comments (required when the block exists)

Place the comment **above** the block.

- Session save checks an `Open` semester and the tenant-local start date.
- Teaching pay is the hourly rate for the lecturer's rank and the course qualification, multiplied by the session length in hours. An open-ended rate carries into later semesters until management issues a new one.
- Transport is one line per lecturer per local calendar day.
- Claim lines copy position, qualification, rate, currency, and amount so later edits do not rewrite history.
- `Approved` claims are not rebuilt.
- A query is tenant-scoped (`TenantId` from JWT or the global filter). HoD scope adds `departmentId` from the token, not from the body, and matches lecturers who have that department in `StaffDepartment`.
- A lecturer save includes at least one department. Deleting the last assignment is rejected.
- Presence refresh writes `BiometricPresent` only. A punch matches a lecturer when `BiometricId` is the same value on both rows.
- Part-time and full-time staff are both included.
- Overlapping sessions, position ranges, or rate ranges are rejected.
- Email is stored lowercase. Course codes are stored uppercase.
- Secrets, JWTs, and raw biometric workbooks are not logged.
- `clientId` deduplicates offline sync. `RecordedAt` can be much later than `StartsAt`.
- Production startup rejects Development secrets.
- List queries use `AsNoTracking` and page in SQL.

## Slice checklist

```
- [ ] Every new public C# member has /// (build fails on CS1591 if not)
- [ ] Every new exported Portal symbol has JSDoc
- [ ] Each product-rule branch in the diff has an inline comment
- [ ] Template/XAML/CSS comments exist only where intent is otherwise hidden
- [ ] No TODO, FIXME, or commented-out code
- [ ] Tests have no XML; names describe the case
```
