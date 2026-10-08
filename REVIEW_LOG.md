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
