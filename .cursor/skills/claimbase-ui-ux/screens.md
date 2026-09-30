# ClaimBase Portal screen specs

Visual system: [../claimbase-design-system/SKILL.md](../claimbase-design-system/SKILL.md). UX rules: [SKILL.md](SKILL.md).

Design each screen for phone, tablet, and desktop. Same information architecture. Tables appear at `lg`.

---

## 1. Login

**Route:** `/login`

### Purpose
Day-to-day sign-in for tenant staff. Accounts are issued by a tenant admin.

### Mobile
- Brand block about `42svh`, safe-area padding, then the form.
- Email, password, full-width primary Sign in.

### Desktop (`lg+`)
- Brand panel about 42% width. Form on the paper canvas.
- Headline: "Lecturer claims from the sessions they log."
- Trust line: "University staff only."

### Content
- No signup tab.
- If the API returns a lecturer role, show "Use the ClaimBase mobile app" and clear the token.

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

---

## 3. Semesters

**Route:** `/app/semesters`

### Purpose
Create a semester before anyone can log sessions.

### Content
- Name, start date, end date, status.
- Actions: Open, Close. Open is refused when another open semester covers the same dates.
- Closed semesters stay visible and read-only.

---

## 4. Courses and qualifications

**Route:** `/app/courses`

### Purpose
Catalog that session logs and the rate matrix both use.

### Content
- Qualifications are an editable list on this area.
- Course form: code, name, qualification. Code is uppercase.
- Deleting a qualification that courses still use is a 409 shown inline.
- Position titles are not edited here. A lecturer's position is recorded on the staff page.

---

## 5. Faculties and departments

**Route:** `/app/faculties`

### Purpose
Create a faculty, then add the departments that sit under it.

### Content
- Faculty name, unique in the tenant.
- Under each faculty: department name, unique inside that faculty.
- A department always displays its faculty. There is no department without a faculty.

## 6. Staff

**Route:** `/app/staff`

### Purpose
People who can be claimed, including part-time and full-time.

### Content
- Staff number, name, employment type, optional biometric id, optional email. When the biometric id is filled in, it is the id on the device.
- Department assignments: at least one, each shown as faculty and department. More than one is allowed. The form cannot save with an empty list, and the last assignment cannot be removed.
- Position records on this lecturer: title, effective from, effective to. This is the only place a person is given a position.
- Overlapping position dates show the API 409 on that row.
- The title dropdown lists shared titles (Professor, Senior Lecturer, and any the tenant added). Adding a row here creates the lecturer's position record. It does not create a new title unless the admin is maintaining the title list beside the rates.
- Employment type is a label. The form does not say full-time staff are excluded.

---

## 7. Rates

**Route:** `/app/rates`

### Purpose
Set the teaching matrix and the transport rate.

### Content
- Matrix: one amount per position and qualification, plus effective from / to.
- Transport section: a single timeline for the tenant. Copy states it is paid once per teaching day, for every position.
- Gap hint: dates with no row will be omitted from claims, not priced at zero silently.

---

## 8. Sessions

**Route:** `/app/sessions`

### Purpose
See logged lectures. Admins may enter one on behalf of a lecturer.

### Content
- Filters: semester, faculty, department, staff, course code. Paged. A department filter lists only departments in the selected faculty.
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
