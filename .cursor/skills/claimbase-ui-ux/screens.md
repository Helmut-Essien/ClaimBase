# ClaimBase Portal screen specs

Visual system: [../claimbase-design-system/SKILL.md](../claimbase-design-system/SKILL.md). UX rules: [SKILL.md](SKILL.md).

Design each screen for phone, tablet, and desktop. Same information architecture. Tables appear at `lg`.

---

## 1. Login

**Route:** `/login`

### Purpose
Day-to-day sign-in for tenant staff. Accounts are issued by a tenant admin.

### Shell
Centered card on `#F8F9FA`, max width 1152px, 12px radius. The page behind the card is a quiet AppBlue dot field (`cb-signin-canvas`). The brand panel is the purple gradient. Two soft circles sit behind the wordmark and are `aria-hidden`. The form sits in a flat white card on the gray half. Atmosphere: [../claimbase-design-system/atmosphere.md](../claimbase-design-system/atmosphere.md).

### Mobile
- Brand block about `42svh`, scrolling from the crest when the copy is taller than the band, then the form. Page padding includes safe-area insets.
- Email, password, full-width primary Sign in. Fields are 48px with a leading icon. The focus ring shows only while the field is focused.
- Password starts masked. The control shows a closed eye until it is pressed.
- Hint under the email: "This browser remembers the last email." Hint under the password: "The password is not stored in this browser."
- The Methodist University Ghana crest sits in a white tile with a PORTAL eyebrow and the ClaimBase wordmark.
- The university motto sits under that wordmark, one line: "Excellence • Morality • Service".

### Desktop (`lg+`)
- Brand panel about 42% width. Form card on the gray half.
- Motto: "Excellence • Morality • Service". Headline: "Lecturer claims from the sessions they log." Trust line: "University staff only."
- Title: "Sign in to ClaimBase". Supporting line: accounts are issued by a tenant admin.

### Content
- No signup tab. No remember-me checkbox. No version string. No TLS or compliance link.
- The motto stays on the brand panel. The form title stays "Sign in to ClaimBase", and the product sentence stays the level-1 heading.
- Do not invent an audit period, an ACTIVE badge, or an office name such as "Office of the Bursar".
- "Forgot password?" is on the password label row and routes to `/login/forgot-password`.
- Sign in uses the full purple gradient from the first paint. It stays disabled until the email and password are valid, without fading that fill. While the request is in flight the label is "Please wait…".
- A standing note, before any error, says lecturers log sessions in the mobile app and this portal is for university staff.
- If the API returns a lecturer role, replace that note with "Use the ClaimBase mobile app" (`role="status"`, not the error banner) and clear the token.

### Forgot password

**Route:** `/login/forgot-password` (guest). Same shell as sign-in.

The page calls `POST /api/auth/forgot-password` and keeps the sign-in shell:

- One email field, same `AUTH_FIELD_LIMITS` as sign-in, plus `resetToken: 128` for the reset page. Submit lowercases the trimmed address. Prefill from the login draft is fine. Do not store a password.
- Primary button: "Send reset link". While the request is in flight, disable it and show "Please wait…".
- Show the API message either way: "If that email belongs to a staff account, a reset link is on its way." Do not add a branch for "email not found".
- "Back to sign in" is the text link. The purple button is the send action.
- Nothing from this step is written to `localStorage`.

### Reset password

**Route:** `/login/reset-password` (guest). Query: `email`, `token`. Same shell as sign-in. The email link is the only way onto this page.

- If either query value is missing, or the token is longer than 128 characters, show "This reset link is invalid. Request a new one." and a link to `/login/forgot-password`. Do not show the password fields.
- Read `email` and `token` once, then remove them from the address bar so the raw token is not left in history. A rejected stored session must not navigate away from this link.
- New password and confirm password. Same password limits as sign-in. Both start masked, each with a closed-eye control. They are not stored.
- Primary button: "Reset password". While the request is in flight, disable it and show "Please wait…".
- `POST /api/auth/reset-password` with `email`, `token`, `newPassword`, `confirmPassword`.
- Success shows "Password has been reset successfully." and a "Back to sign in" link. Do not sign the user in from this page.
- HTTP 400 shows "Invalid reset token." and a link to request a new one. A mismatch stays on the form.
- Nothing from this step is written to `localStorage`.

---

## 2. Home

**Route:** `/app`

### Purpose
What this role should do next.

### Content
- Open semester, or a prompt to open one (Admin / TenantAdmin).
- Counts: claims awaiting this role, sessions omitted for a missing rate.
- Recent claims for the role's scope (lecturers in the HoD's department, or the whole tenant for Finance).
- Empty state is zero counts, not illustrative sample rows.
- Finding the open semester is one lookup of at most 100. Home is not a paged catalog.

---

## 3. Semesters

**Route:** `/app/semesters`

### Purpose
Create a semester before anyone can log sessions.

### Content
- Name, start date, end date, status.
- Actions: Open, Close. Open is refused when another open semester covers the same dates.
- Closed semesters stay visible and read-only. The list pages at 20.

---

## 4. Courses and qualifications

**Route:** `/app/courses`

### Purpose
Catalog that session logs and the rate matrix both use.

### Content
- Qualifications are an editable list on this area, paged at 20. The qualification dropdown on a course is a lookup of at most 100 and says when it is short.
- Course form: code, name, qualification. Code is uppercase. The course list pages at 20.
- Deleting a qualification that courses still use is a 409 shown inline.
- Position titles are not edited here. A lecturer's position is recorded on the staff page.

---

## 5. Campuses, faculties, and departments

**Routes:** `/app/campuses`, `/app/faculties`

### Purpose
Create a campus, then the faculties on that campus, then the departments under each faculty.

### Content
- Campus name, unique in the tenant. Semesters and rates are not chosen here.
- Faculty name, unique on the selected campus. The same name may be used on another campus.
- Under each faculty: department name, unique inside that faculty.
- A faculty always displays its campus. A department always displays its faculty. There is no faculty without a campus, and no department without a faculty.
- Campus, faculty, and department catalogs page at 20. Faculty campus chips page the same way. The campus dropdown on the faculty form is a lookup of at most 100 and says when it is short.

## 6. Staff

**Route:** `/app/staff`

### Purpose
People who can be claimed, including part-time and full-time.

### Content
- Staff number, name, employment type, optional biometric id, optional email. When the biometric id is filled in, it is the id on the device.
- Department assignments: at least one, each shown as campus, faculty, and department. More than one is allowed, including departments on different campuses. The form cannot save with an empty list, and the last assignment cannot be removed.
- Position records on this lecturer: title, effective from, effective to. This is the only place a person is given a position.
- Overlapping position dates show the API 409 on that row.
- The lecturer list pages at 20. The position-title catalog beside it pages at 20. The title dropdown on an appointment is a lookup of at most 100 and says when it is short. Adding a row on the lecturer creates that person's appointment. It does not create a title; titles are maintained on the catalog.
- Department assignment dropdowns are lookups of at most 100 and say when they are short. Appointments already stored on the lecturer stay on that record.
- Employment type is a label. The form does not say full-time staff are excluded.

---

## 7. Rates

**Route:** `/app/rates`

### Purpose
Set the hourly teaching matrix and the transport rate. This page is not part of creating a semester. An amount with no end date keeps applying in later semesters until management issues a new one.

### Content
- Matrix: cedis per hour for one rank and one qualification (Senior Lecturer × Diploma, Lecturer × Diploma), plus effective from / to. Leave the end date empty to carry the amount forward. The axes are lookups of at most 100 and say when they are short. The matrix stays one grid.
- Teaching-rate history under a cell, and the transport timeline, page at 20.
- Saving a new amount starts a new row. It does not ask the admin to re-enter the whole matrix for the next semester.
- Transport section: a single timeline for the tenant. Copy states it is paid once per teaching day, for every position, and is not an hourly rate.
- Gap hint: dates with no row will be omitted from claims, not priced at zero silently.

---

## 8. Sessions

**Route:** `/app/sessions`

### Purpose
See logged lectures. Admins may enter one on behalf of a lecturer.

### Content
- Filters: semester, campus, faculty, department, staff, course code. Paged. A faculty filter lists only faculties on the selected campus. A department filter lists only departments in the selected faculty.
- Columns include start, end, and duration. No amount column.
- Create form (Admin / TenantAdmin): staff, course code, start, end.
- Offline lecturer entry is not replicated here. This form requires the network.

---

## 9. Claims

**Route:** `/app/claims` and `/app/claims/:id`

### List
- Lecturer, faculties and departments snapshotted on the claim, semester, status, total.
- HoD sees claims for lecturers assigned to their department, including lecturers who also sit in other departments. Finance sees the tenant.

### Detail
- Teaching table and transport table.
- Presence column: check or empty. Helper text: "Device check. This does not change the amount."
- Omitted sessions with reasons.
- HoD: Approve or return, with an optional note, only while `PendingHod`.
- Finance: Approve or return only while `PendingFinance`.
- Approved: Download PDF. No rate editors on this page.

---

## 10. Biometrics

**Route:** `/app/biometrics`

### Purpose
Import a device workbook. Punches match lecturers by the biometric id already stored on staff.

### Content
- Upload `.xlsx`. Show processed, duplicate, or failed.
- Unknown ids list: device id and punch count. The fix is to save that same id on the lecturer. There is no separate mapping from a device id to a different staff code.
- After the staff id is corrected, a refresh updates checkmarks on existing lines.
- The page states that import does not change claim totals.

---

## 11. Not found

**Route:** `/404` and `**`

Unknown URLs render this page. They do not redirect to `/app` or `/login`.
