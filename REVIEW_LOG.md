# Review log

## Iteration 1

### Fixed

- **High — plaintext password in `localStorage`.** `login.component.ts` saved the password on every keystroke and restored it, including after sign-out. The draft now stores the email only, and opening the page rewrites an older draft so a previously saved password is removed. Tests cover both cases.
- **Medium — keyboard focus on navigation.** Shell links and the sidebar sign-out control had no visible focus ring. Nav links use an AppBlue focus outline; the sidebar sign-out button matches the phone header.

### Dismissed

- **One university crest on the login brand panel.** Bugbot noted the Methodist University Ghana logo on a multi-tenant sign-in screen. That asset was added because the product owner asked for the AssetTag login logo on this page. It is a branding choice, not a defect in this slice.

### Open

- None in this iteration. Re-review is next.

## Iteration 2

### Fixed

- **Low — draft semester chip used warning colors.** `.chip-draft` was orange (`#fff3e0` / `#e65100`), the same family as a problem state. Draft is a normal step before Open. The chip is now neutral gray with slate text, and Closed stays the muted chip.

### Dismissed

- **Email left in `localStorage` after sign-out.** Security review marked this below the reporting threshold. The page remembers the address the person typed. It does not store the password, and an older draft that did is rewritten on the next visit.
- **Methodist University crest on the sign-in panel.** Requested for this login page. Not a logic or access-control defect.

### Open

- Re-review after the chip change.

## Iteration 3

### Fixed

- **Medium — login field errors were not tied to the inputs.** A touched invalid email or password showed a message that assistive tech did not associate with the field. Each input now sets `aria-invalid` and `aria-describedby` when its message is visible.
- **Low — the phone brand band clipped its headline.** `overflow-hidden` plus a `42svh` cap cut the sentence on a short screen. The band scrolls instead.

### Dismissed

- None new. Email persistence and the university crest stay dismissed from iteration 2.

### Open

- Re-review of this iteration.

### Notes

- The password field still uses `autocomplete="current-password"` so the browser password manager can fill it. The app does not keep the password.
- Bottom-nav labels stay at 10px so seven destinations fit a phone. Each control is at least 44px tall. The link text is the accessible name.

## Iteration 4

### Fixed

- **Medium — table headers dropped below the contrast minimum.** `.cb-th` was changed from muted (`#666`) to `#999` (about 2.8:1 on white). Headers use muted again (about 5.7:1).
- **Medium — gray chips failed contrast.** Unselected filter chips and the Closed chip used `#666` on `#e0e0e0` (about 4.3:1). Their text is slate. The Open chip used `#2e7d32` on `#e8f5e9` (about 4.2:1 at 12px). Its text is `#1b5e20` (about 7:1).
- **Low — the lecturer placeholder invited a sign-in that does not exist, and lost its heading.** The screen said "Welcome back" and "Sign in to log the sessions you taught" above a note that sign-in opens later. The title is ClaimBase again, marked as a level-1 heading, and the subtitle no longer asks the lecturer to sign in. Decorative circles are excluded from the accessibility tree.

### Dismissed

- **Other setup forms still show errors without `aria-describedby`.** Those paragraphs were not part of this slice's edits. Login, the form this slice rewrote, associates its errors.
- **Disabled Sign in until the form is valid.** Blur marks a field touched and then shows its error. The button stays disabled so an empty form is not submitted.
- **10px bottom-nav labels.** Seven equal slots are the shell spec. Targets are at least 44px tall, and the visible word is the accessible name.

### Open

- Re-review after the contrast and mobile-copy fixes.

## Iteration 5

### Fixed

- **Medium — a lecturer sent to the mobile app saw an error banner.** The notice used `cb-banner`, the same red panel as a failed password. Sign-in succeeded and the token was cleared on purpose. The notice is now a blue status note (`cb-note`, `role="status"`), and the login test checks it is not the error banner.

### Dismissed

- None new.

### Open

- None. Bugbot reported no bugs. Security review reported no medium or higher issues. Senior and UI reviews found no further actionable items.

### Residuals

- The sign-in draft keeps the email after sign-out. It does not keep the password.
- The Methodist University Ghana crest is on the sign-in panel because that logo was requested for this page.
- Home still shows zero claims and "No recent claims" until that slice exists. "Please wait…" is only the open-semester request.

## Iteration 6

### Fixed

- **Medium — the open phone drawer did not take the rest of the page out of the tab and screen-reader order.** The focus trap only handled Tab while focus was already inside the sidebar. The header and the main content are now `inert` until the drawer closes. The drawer is named Menu, matching the button that opens it.
- **Medium — the drawer breakpoint did not match the rest of the layout.** Tailwind `lg` is `64rem`. The sidebar pin and `matchMedia` used `1024px`, so a larger root font could show the phone header and the desktop sidebar together. Both now use `64rem`.

### Dismissed

- None new. Bugbot and the security review reported nothing to fix in this pass.

### Open

- None. The re-review found no bugs and no medium or higher security issues. Senior and UI reviews found no further actionable items. The drawer test also checks that focus moves to Close.

### Residuals

- The sign-in draft keeps the email after sign-out. It does not keep the password.
- The Methodist University Ghana crest is on the sign-in panel because that logo was requested for this page.
- Home still shows zero claims and "No recent claims" until that slice exists.
- Sign out on a phone is inside the menu. The top bar carries the menu button and the university name.

## Iteration 7

### Fixed

- **Medium — the phone brand band hid the crest when the copy was taller than the band.** `justify-center` on the scrolling band pushed the top into unreachable space. On a 360px-tall screen the crest sat 30px above the scroll origin and `scrollTop` could not go below 0. The band now uses `justify-content: safe center`, so a short band stays centered and overflow scrolls from the crest. The page card uses auto margins for the same reason: centering no longer clips the top when the card is taller than the screen. Rechecked on that short screen (crest 24px inside the band, which is the padding) and on a 1440px desktop (the motto block stays vertically centered).

### Dismissed

- **Sign-in button has no faded disabled color.** The empty form still disables the control, so it does not submit and assistive tech announces it as unavailable. The fill stays the full purple gradient because that washed-out 60% opacity was the color that looked wrong on the page. Security review found no medium or higher issue. Email in `localStorage`, Google Fonts, and the lack of a content security policy are unchanged and outside this slice.

### Open

- None. The second Bugbot pass found no bugs. The second security review found no medium or higher issues. Senior and UI reviews found no further actionable items.

### Residuals

- The sign-in draft keeps the email after sign-out. It does not keep the password.
- The Methodist University Ghana crest and the motto "Excellence • Morality • Service" are on the brand panel because that branding was requested for this page.
- Forgot password tells staff to ask a tenant admin. There is no reset email yet.
- Home still shows zero claims and "No recent claims" until that slice exists.
