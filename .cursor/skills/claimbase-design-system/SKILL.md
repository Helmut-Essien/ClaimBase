---
name: claimbase-design-system
description: >-
  ClaimBase visual design system: Ink, Copper, and Paper tokens, Source Sans 3,
  restrained grain on brand panels, elevation, buttons, cards, forms, and
  status chips. Custom CSS includes a short comment saying why the rule
  exists. Use when styling the Angular Portal, auth, claim tables, or
  brand/visual work.
---

# ClaimBase Design System

Visual source of truth for the Portal. Implement in `Portal/` with Tailwind and Source Sans 3. Token tables: [tokens.md](tokens.md). Atmosphere: [atmosphere.md](atmosphere.md). Layout and screens: [claimbase-ui-ux](../claimbase-ui-ux/SKILL.md).

## Design intent

ClaimBase is a **calm university ledger**: clear enough for Finance to approve a semester, quiet enough that a checkmark cannot be mistaken for a deduction.

| Trait | Design implication |
|-------|-------------------|
| Accountable | Status, amounts, and omitted rows are visible. Approved claims look frozen. |
| Calm | Paper canvas, ink text, one copper accent |
| Efficient | 44px targets, one primary action per view |
| Distinct | Brand panels use ink and copper. Data tables stay plain white. |

## Core palette

| Role | Hex | Tailwind |
|------|-----|----------|
| Ink | `#1B3A4B` | `ink` |
| Ink dark | `#122836` | `ink-dark` |
| Copper | `#C46B3A` | `copper` |
| Copper dark | `#A3562C` | `copper-dark` |
| Paper | `#F4F1EA` | `paper` |
| Paper border | `#E4DDD2` | `paper-border` |
| Slate | `#1C1917` | `slate` |
| White | `#FFFFFF` | `white` |
| Success | `#2F6F4E` | `success` |
| Warning | `#A16207` | `warning` |
| Error | `#B91C1C` | `error` |
| Muted | `#64748B` | `muted` |

**Rules**

- **Ink** — wordmark, nav active, primary buttons, login brand panel.
- **Copper** — the single high-intent action on a page (`Build claim`, `Download PDF`). Not Save, not Sign in.
- **Paper** — page background. Cards are white.
- Shadows are ink-tinted: `0 2px 8px rgba(27, 58, 75, 0.08)`.

## Atmosphere

Grain and a light dot-mesh belong on the login brand panel only. Recipes: [atmosphere.md](atmosphere.md). Tables, rate cells, and forms stay flat.

## Typography

Source Sans 3 for the whole Portal, including login. Hierarchy is weight and size.

| Token | Size / weight | Use |
|-------|---------------|-----|
| headline-lg | 28px / 700 | Page titles |
| headline-md | 22px / 600 | Dialogs, claim lecturer name |
| body-md | 16px / 400 | Inputs and body |
| body-sm | 14px / 400 | Table cells |
| label-sm | 12px / 600 / 0.05em | Field labels, column headers |

Use monospace for course codes and staff numbers. Amounts use the body face, tabular figures (`tabular-nums`).

## Layout

- 4px base unit. Mobile margin 16px. Content max 1280px.
- App sidebar at `lg+` only.
- Radius: inputs `rounded-md`, cards `rounded-lg`, status `rounded-full`.
- Primary and copper actions are `w-full` until `sm`.

## Components

### Buttons

| Variant | Style | When |
|---------|--------|------|
| Primary | Ink background, white text | Sign in, Save, Open semester |
| Accent | Copper background, white text | Build claim, Download PDF |
| Secondary | Ink outline | Cancel, Return claim |
| Ghost | Muted text | Toolbar |

Solid fills. No gradient buttons.

### Status chips

| Status | Treatment |
|--------|-----------|
| Open / Approved | Success tint |
| Draft / PendingHod / PendingFinance | Neutral |
| Closed / Rejected | Muted or error |
| Omitted (no rate) | Warning |
| Present | Success check, not a pill that changes the amount color |
| Not present | Empty checkbox outline in muted ink |

Amount text stays `slate` whether or not the checkmark is set.

### Forms and tables

- Labels above fields. Focus ring uses ink.
- Errors: tinted banner plus the field message.
- Desktop tables: small-caps-style headers (`label-sm`), hairline dividers.
- Below `lg`: one card per row.

## Anti-patterns

- Forest/gold retail palette, purple gradients, or pure-white page backgrounds
- Copper on every button
- Texture on the rate matrix or claim lines
- Coloring a row red because the biometric device has no punch
- Dark mode as the default

## Ship checklist

- [ ] Paper page, white cards, ink primary, copper only for claim/PDF actions
- [ ] Source Sans 3
- [ ] Presence mark does not recolor the amount
- [ ] 44px targets and safe-area padding
- [ ] `prefers-reduced-motion` respected if any brand motion is added
- [ ] Production component CSS stays inside the Angular budget
- [ ] Custom CSS comments say why the rule exists (token mapping, grain only on the login panel, reduced motion)
