---
name: claimbase-ui-ux
description: >-
  ClaimBase Portal UI/UX: role-based shell (collapsible sidebar on a phone, fixed sidebar from lg),
  iOS safe areas, login, academic setup, rate matrix, session lists, claim
  review, biometric import, and PDF download. Biometric checkmarks are
  informational.   Form limits mirror backend Shared DTOs. Generated Portal code includes
  JSDoc on exported APIs and template comments where layout intent is not
  obvious. Use when building Portal pages, navigation, forms, tables, empty
  states, or claim reports.
---

# ClaimBase Portal UI/UX

Visual tokens: [claimbase-design-system](../claimbase-design-system/SKILL.md). Screen specs: [screens.md](screens.md). Product rules: [claimbase](../claimbase/SKILL.md). Lecturer phone UI: [mobile.md](../claimbase/mobile.md).

## Principles

1. **Mobile-first Portal, then desktop density.** Finance staff open claims on phones. Escalate to tables at `lg`.
2. **One job per view.** Setup, sessions, and payment approval stay on different screens.
3. **Money is the snapshot.** Show the rate and amount stored on the claim line. Do not offer a "recalculate from today's rates" action on an approved claim.
4. **The checkmark is evidence, not a pay switch.** A missing mark never looks like a deduction.
5. **Role-honest navigation.** Hide links the signed-in role cannot call.
6. **Fast screens.** Same-origin `/api` in production builds. `OnPush`, Signals, `@for track` by id, debounced search, one page from the API. Details: [performance.md](../claimbase/performance.md).

## Responsive shell

Chrome lives in `core/layout`. Feature pages do not add a second app bar or their own sidebar.

| Breakpoint | Shell |
|------------|--------|
| Below `lg` | White top bar: menu button, ClaimBase as a small label, the university name in bold. The menu opens the same sidebar as an off-canvas drawer (backdrop, Close, Escape). Sign out sits at the bottom of that drawer. |
| `lg+` | The sidebar stays open, `w-64`. There is no top bar and no menu button. Active route is filled AppBlue. Sign out sits at the bottom of the sidebar. Content max width 1280px. |

The sidebar is a drawer below `lg` and stays open from `lg` (64rem). Do not pin it open at `md`, and do not add a bottom nav. While the drawer is open, the page behind it is inert.

`index.html` viewport includes `viewport-fit=cover`. Sticky bars pad `env(safe-area-inset-top)` and `env(safe-area-inset-bottom)`. Touch targets are at least 44×44px. Truncate tenant and course names.

**Nav** (add a link only when that slice's route exists):

| Item | Route | Roles |
|------|-------|--------|
| Home | `/app` | All Portal roles |
| Campuses | `/app/campuses` | TenantAdmin, Admin |
| Faculties | `/app/faculties` | TenantAdmin, Admin |
| Semesters | `/app/semesters` | TenantAdmin, Admin |
| Courses | `/app/courses` | TenantAdmin, Admin |
| Staff | `/app/staff` | TenantAdmin, Admin |
| Rates | `/app/rates` | TenantAdmin, Admin |
| Sessions | `/app/sessions` | Admin, TenantAdmin, HoD, Finance |
| Claims | `/app/claims` | HoD, Finance, Admin, TenantAdmin |
| Biometrics | `/app/biometrics` | Admin, TenantAdmin |

## Screen patterns

### Login

- Split layout from `lg`: purple brand panel and the form on the page canvas.
- Below `lg`: brand block capped near `42svh` so the form is on screen.
- Email and password. The password starts masked, with a closed-eye control that reveals it.
- No license key. No self-service signup in MVP (users are created for the tenant).
- Lecturer JWTs that hit the Portal are signed out with a short message to use the mobile app.

### Home

- Open semester name, claims waiting on the signed-in role, count of sessions omitted for a missing rate.
- Zeros until data exists. No sample claims.

### Setup lists

- Title and one primary action (`w-full` until `sm`).
- Search chips scroll horizontally on small screens.
- Cards below `lg`, table from `lg`.
- Empty catalog copy is different from "no rows match this search".

### Rate matrix

- Positions down the side, qualifications across the top, amount in each cell, effective dates visible.
- Saving a cell posts one teaching-rate row. An overlap error shows inline. Do not clear the rest of the matrix.

### Sessions

- Columns: lecturer, course code, start, end, duration, semester.
- The session list does not show a pay figure. The claim later multiplies the hourly rate by this duration.

### Claim detail

- Header: lecturer, employment type, semester, status, currency.
- Teaching lines: course, position, qualification, start, end, hourly rate, hours, amount, presence mark.
- Presence mark: a check when `biometricPresent` is true, an empty box when false. Caption: "Device check. This does not change the amount."
- Transport lines grouped by date.
- Omitted sessions listed under the lines, with the reason (no teaching rate, no position on that date, no transport rate).
- HoD actions only in `PendingHod`. Finance actions only in `PendingFinance`. Approved claims show Download PDF and no edit actions.

### Biometric import

- File picker limited to `.xlsx`.
- Result: rows imported, duplicate file message, device ids that match no lecturer.
- An unknown id is resolved by putting that same id on the staff record. The screen does not say those lecturers will not be paid.

## Interaction

| Topic | Rule |
|-------|------|
| Money | Tenant currency, two decimals (`GHS 1,250.00` when the code is GHS) |
| Dates | Tenant time zone, shown with the zone abbreviation once per page |
| Touch | ≥ 44×44px |
| Loading | Disable the CTA and show "Please wait…" |
| Empty | One sentence and one action the role is allowed to take |
| Errors | Inline banner with the API message. 409 overlap stays on the form. |

## Angular notes

- Standalone, `OnPush`, lazy `routes.ts`.
- `TenantStateService` holds tenant name, currency, and time zone from `/api/auth/me`.
- `core` does not import `features`.
- Forms use reactive forms, `*_FIELD_LIMITS`, `[attr.maxlength]`, inline errors.
- Course codes display and submit in uppercase. Emails submit lowercase.
- **Document the view in the same change.** JSDoc on exported components, services, models, guards, and `*_FIELD_LIMITS`. HTML comments only where Tailwind does not show intent (safe-area split, checkmark is not a pay rule, role-specific actions). Rules: [documentation.md](../claimbase/documentation.md).

## Workflow

1. Read [screens.md](screens.md) for the screen.
2. Apply the design system.
3. Layout phone, then `lg`.
4. Bind real DTOs. Keep empties honest.
5. Ship field constraints with the API slice.
6. Add JSDoc and the few template comments the screen needs before considering the screen done.
7. Stop at the slice boundary and ask before the next.

## Anti-patterns

- A pay total that shrinks because the checkmark is empty
- Recalculate button on an approved claim
- Beige paper, copper buttons, or a split ink login panel
- A bottom nav, or a persistent sidebar below `lg`
- Sticky bars without safe-area insets
- Nav items for routes that are not implemented
- Unpaged tables
- Showing concurrency tokens or internal ids as the primary label (staff name and course code are the labels)
