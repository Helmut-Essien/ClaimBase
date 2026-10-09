# Review log

## Iteration 1

### Findings fixed

- High: `.vscode/tasks.json` stop tasks used the workspace path as the shell command. The path contains spaces, so Stop would not run the script and the API or Portal listener would stay up. The tasks now run `bash .vscode/stop-listener.sh` with the workspace as the working directory.
- Medium: `Portal/src/styles.css` generic row hover was declared after the selected-row rule and replaced the AppBlue selection on hover. The selected rule now wins.
- Medium: `faculties.component.html` made the department pane `lg:sticky`. A long department list could stick taller than the viewport and hide its own pager. Sticky was removed.
- Medium: switching faculties cleared the count to zero before the request returned, so the pane said "0 departments" while loading. The count stays hidden until that load finishes.
- Medium: desktop tables used automatic layout, so a long name expanded the pane and `truncate` never applied. Tables in a pane are `table-layout: fixed`, cells ellipsize, and the full name is on `title`. Phone cards wrap long names.
- Low: faculty and department tables had no `scope="col"` on headers.

### Findings dismissed

- Bugbot: no bugs.
- Security review: no medium or higher issues. Killing whatever listens on 5190 or 4201 is local debug cleanup, not an application trust boundary. A password-reset email dropped because the host is stopping is the existing cancellation behavior, not a new auth or disclosure bug.
- Analytics, feature flags, and dark mode: this slice does not add product instrumentation, and the design system keeps the Portal on the light theme.

### Remaining

- Iteration 2 opened from the re-review below.

## Iteration 2

### Findings fixed

- Medium: `faculties.component.html` showed "0 departments" after a failed department load because loading had finished and the list was empty. The count is hidden when that request failed and no rows are shown. Covered by `faculties.component.spec.ts`.
- Low: the selected faculty name in the department pane did not wrap, so a long name could overflow the pane. It now uses `break-words`.

### Findings dismissed

- Security review (second pass): no medium or higher issues. Same local-debug and shutdown notes as iteration 1.
- Overlapping debug sessions killing a newer listener on 5190 or 4201: real, but it only happens if two sessions share a port. Fixing it needs a PID recorded at launch, which is a larger debug-workflow change than this slice. Left as a residual note.

### Remaining

- Iteration 3 opened from the re-review below.

## Iteration 3

### Findings fixed

- Medium: after creating a faculty, `selectedFacultyId` pointed at a row that was not on the current page, so the department pane rendered as an empty card. It now says "Please wait…" until that faculty is in the list. The previous faculty's departments are cleared immediately so they cannot appear under the new name. Covered by `faculties.component.spec.ts`.
- The "pane stays empty if the faculty reload fails" part of that finding does not match the template: a faculty load error replaces the panes with the error banner. No extra change.

### Findings dismissed

- Security review (third pass): no medium or higher issues.

### Remaining

- Iteration 4 opened from the re-review below.

## Iteration 4

### Findings fixed

- Medium: `faculties.component.html` still showed the department total when a later page failed, because any non-empty `departments()` list kept the count while the error banner hid the rows. The count is now hidden whenever `departmentLoadError()` is set. Covered by `faculties.component.spec.ts`.

### Findings dismissed

- Security review (fourth pass): no medium or higher issues.

### Remaining

- None after iteration 5.

## Iteration 5

### Findings fixed

- None. The slice was already updated in iterations 1–4.

### Findings dismissed

- Bugbot: one candidate was removed by its findings validator. The follow-up described it as a suspected compilation issue. Evidence it does not reproduce: `ng test` compiled `faculties.component` and passed 4 tests, and `PasswordResetEmailQueueTests` passed.
- Security review: no medium or higher issues.
- Senior review: no actionable infrastructure, performance, or correctness issues beyond the fixes above. Host shutdown can still cancel a password-reset send that is already in progress; waiting for SMTP before exit could hang shutdown, so that behavior stays. Two debug sessions on the same port can still stop each other.
- UI/UX review: contrast of the selected row is 6.25:1 for the faculty name and 5.03:1 for the campus name on `#e3f2fd`. Keyboard selection uses the faculty-name button. Loading, empty, and error states are covered by the faculties spec. Dark mode, analytics, and localization are outside this slice; the design system stays on the light theme and the Portal copy is English.

### Remaining

- None.

### Notes

- Postgres from `docker compose up -d` is still left running on purpose when debugging stops.
- A stop task can still close a listener that a newer debug session binds to the same port if the two overlap.

## Iteration 6

Portal navigation UI slice (uncommitted). Reviews: Bugbot, security review, senior pass, UI/UX pass. Repeated until the concrete findings were fixed.

### Findings fixed

- Medium: `.cb-gap` used `#e65100` on `#fff3e0`, which is 3.46:1. The 12px warning is now `#bf360c` (5.11:1).
- Medium: semester and course edit banners were `role="status"` bound to the live form value, so each keystroke re-announced the banner. The banner now keeps the name or code from when Edit was pressed. Covered by the semester and course specs.
- Medium: Edit scrolled the form up and left focus on the Edit button, which had moved off screen. Focus now moves to the name or code field. Covered by those specs.
- Medium: `.cb-anchor` used a fixed `5.5rem` scroll offset. The phone header also adds `env(safe-area-inset-top)`, so a notched phone could cover the field. The offset now includes that inset, and from `lg` it drops to `1.5rem` because that header is hidden.
- Medium: the staff title catalog was `lg:sticky`. A long title list is taller than the viewport and would pin its own end out of reach. Sticky was removed. The catalog still sits in the side column from `lg`.
- Low: the pager range could read backwards (`41–25 of 25`) when the page number was past the last page. The start is capped at the total. Covered by `pager.component.spec.ts`.
- Low: "No appointment" did not say the gap is for today. Screen readers now hear "No appointment in force today" via visually hidden text.

### Findings dismissed

- Bugbot, first pass: no bugs.
- Bugbot, later passes: one candidate each time was removed by the findings validator, including a pager range that is now clamped and tested. No remaining file and line was left after the validator.
- Security review, every pass: no medium or higher issues. Templates use Angular interpolation. Element ids passed to `scrollIntoView` and `focus` are fixed. Home still loads semesters only for setup roles, and the API still enforces that.
- Senior review: `appointmentTitle` only labels the staff list. It does not price a claim. A page is 20 lecturers, and the screen is `OnPush`. Moving `calendarToday` did not change the UTC fallback. No new deploy, log, or query path.
- UI/UX review: choosing a faculty or a rate cell does not focus a text field, so a phone does not open the keyboard while browsing. The rate status line updates when the position or qualification changes, not on each amount keystroke. Dark mode, analytics, and localization stay out of this slice. The design system is the light theme, and Portal copy is English. Tenant currency still formats as `GHS 1,250.00`.

### Remaining

- None.

### Notes

- Portal unit tests: 56 passed.

## Iteration 7

Pagination slice (uncommitted). Reviews: Bugbot, security review, senior pass, UI/UX pass. Repeated until the later passes were clean.

### Findings fixed

- Medium: paging the faculty campus chips left `campusFilter` set while the pressed chip left the screen, so the faculty list stayed filtered with no selected chip. The selected campus stays in the chip group, including after a faculty is created on a campus that is not on the current chip page. Covered by `faculties.component.spec.ts`.
- Medium: a failed campus load was stored on `facultyLoadError`, which the faculty request clears, and the template treated a zero total as "No campuses yet". Chip-page and form-lookup failures now have their own banners and leave the faculty list in place. Covered by the faculties spec.
- Medium: a failed position-title catalog request still said "No position titles yet", and a failed appointment lookup said to add a title first. Those failures now have their own alerts. Covered by `staff.component.spec.ts`.
- Low: a slower appointment-title lookup could overwrite a newer one. The dropdown ignores a stale response. Covered by the staff spec.

### Findings dismissed

- Bugbot, first pass: two candidates were removed by the findings validator and were not restated with a file and line. The one finding that remained (the hidden campus filter) was verified and fixed.
- Bugbot, after the fixes above: no bugs.
- Security review, every pass: no medium or higher issues. Templates still use Angular interpolation. Campus and title ids are still checked on the server inside the tenant. Lookup caps do not grant another tenant's rows.
- Senior review: the extra campus and title requests are one catalog page and one lookup, each capped by `PageLimits`. They do not download every page. No new deploy, log, or query path.
- UI/UX review: the off-page campus chip is a 2.75rem button in the "Campus filter" group, with `aria-pressed` and the existing ink fill. Dark mode, analytics, and localization stay out of this slice. The design system is the light theme, and Portal copy is English.

### Remaining

- None.

### Notes

- Portal unit tests: 63 passed.
- Home still finds the open semester with one lookup of 100, ordered by start date descending. An older open semester past that page would not appear on Home. The dashboard is not a paged catalog.
