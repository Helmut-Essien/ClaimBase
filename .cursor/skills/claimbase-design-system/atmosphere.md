# ClaimBase atmosphere

The sign-in brand panel is the only place that uses the purple wash. Claim tables, the rate matrix, and the sign-in form stay flat so the fields scan cleanly.

## Sign-in card

The page is `#F8F9FA` with a quiet AppBlue dot field: 1px dots, 18px apart, mixed about 42% ink into the page color. The class is `cb-signin-canvas`. Dots do not animate, and they sit only on the page behind the card. The card is centered, `max-w-6xl`, 12px radius, and lifts with `0 8px 24px rgba(0, 0, 0, 0.08)` because it is the only surface on the page. From `lg` the split is about 42% brand and the rest form. Below `lg` the brand band stays near `42svh` and scrolls from the crest when the copy is taller than the band.

The brand panel runs from `#512BD4` (top left) to `#2B0B98` (bottom right). It holds the crest, a PORTAL eyebrow, the ClaimBase wordmark, the university motto "Excellence • Morality • Service", and the product sentence. The motto is one title-case line in white, with the crest. It is not repeated in the form. Two soft circles sit behind that copy (`bg-white/5` and `bg-black/10`, blurred). They are decoration. They do not animate, and they are hidden from assistive tech. The page stays the light dotted canvas.

The form half is the gray page. The fields sit in a white card with the light list-card shadow. Leading icons are inline SVG. Do not load an icon font for this screen.

## Leave off

- Rate cells, claim lines, session cards, and PDF-preview UI
- A sample audit cycle, quarter, currency version, or ACTIVE badge
- "Office of the Bursar" or any office name that is not on the tenant record
- Grain, a dot field on the card or on any data surface, or a second product palette
- Dark mode as the default
