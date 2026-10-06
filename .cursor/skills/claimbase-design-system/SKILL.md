---
name: claimbase-design-system
description: >-
  ClaimBase visual design system learned from the AssetTag mobile app:
  AppBlue chrome, purple gradient actions, Open Sans, white cards on a gray
  page, and a gradient sign-in screen. Custom CSS includes a short comment
  saying why the rule exists. Use when styling the Angular Portal, the MAUI
  lecturer app, auth, claim tables, or brand/visual work.
---

# ClaimBase Design System

Visual source of truth for the Portal and the lecturer app. It follows the AssetTag mobile app: AppBlue bars, a purple sign-in gradient, Open Sans, and white cards on `#F8F9FA`. Implement the Portal in `Portal/` with Tailwind. Implement MAUI in `MobileApp/Resources/Styles`. Token tables: [tokens.md](tokens.md). Atmosphere: [atmosphere.md](atmosphere.md). Layout and screens: [claimbase-ui-ux](../claimbase-ui-ux/SKILL.md).

## Design intent

ClaimBase is a **calm university ledger**: clear enough for Finance to approve a semester, quiet enough that a checkmark cannot be mistaken for a deduction.

| Trait | Design implication |
|-------|-------------------|
| Accountable | Status, amounts, and omitted rows are visible. Approved claims look frozen. |
| Clear | Gray page, white cards, AppBlue headers |
| Efficient | 48px targets, one primary action per view |
| Distinct | Sign-in uses the purple gradient. Data tables stay plain white. |

## Core palette

| Role | Hex | Tailwind |
|------|-----|----------|
| AppBlue | `#005A9C` | `ink` |
| AppBlue dark | `#004578` | `ink-dark` |
| Primary | `#512BD4` | `primary` |
| Primary dark | `#2B0B98` | `primary-dark` |
| Page | `#F8F9FA` | `paper` |
| Card border | `#EEEEEE` | `paper-border` |
| Text | `#333333` | `slate` |
| White | `#FFFFFF` | `white` |
| Success | `#7CB342` | `success` |
| Warning | `#FF9800` | `warning` |
| Error | `#C62828` | `error` |
| Muted | `#666666` | `muted` |

`ink` and `copper` remain as Tailwind names so existing templates keep compiling. `ink` is AppBlue. `copper` is the same purple as `primary`.

**Rules**

- **AppBlue** — wordmark bar, active navigation, list-card edge, links.
- **Primary purple** — Sign in, Save, and other filled buttons. The fill is a left-to-right gradient from `#512BD4` to `#2B0B98`.
- **Page** — `#F8F9FA`. Cards are white with a 12px radius and a 4px AppBlue left edge.
- Shadows are a light gray lift: `0 1px 3px rgba(0, 0, 0, 0.08)`.

## Atmosphere

The sign-in brand panel is a flat purple gradient. The form sits on the page canvas. Recipes: [atmosphere.md](atmosphere.md). Tables, rate cells, and forms stay flat.

## Typography

Open Sans for the Portal and the lecturer app. Hierarchy is weight and size.

| Token | Size / weight | Use |
|-------|---------------|-----|
| headline-lg | 24px / 700 | Sign-in welcome |
| headline-md | 20px / 600 | Page titles, blue-bar titles |
| body-md | 16px / 400 | Inputs and body |
| body-sm | 14px / 400 | Table cells |
| label-sm | 12px / 600 / 0.05em | Field labels, column headers |

Use monospace for course codes and staff numbers. Amounts use the body face, tabular figures (`tabular-nums`).

## Layout

- 4px base unit. Mobile margin 16px. Content max 1280px.
- One sidebar: a drawer below `lg`, fixed open from `lg`. No bottom nav.
- Radius: inputs and buttons 20px, list cards 12px, chips 16px, status pills fully round.
- Primary and copper actions are `w-full` until `sm`.

## Components

### Buttons

| Variant | Style | When |
|---------|--------|------|
| Primary | Purple gradient, white text, 20px radius | Sign in, Save, Open semester, Build claim, Download PDF |
| Secondary | Purple outline | Cancel, Return claim |
| Ghost | Muted text | Toolbar |

Primary buttons use the gradient. Do not fill every control with AppBlue.

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

- Labels above fields, 14px semibold, sentence case. Focus ring uses primary purple.
- Errors: tinted banner plus the field message.
- Desktop tables: small-caps-style headers (`label-sm`), hairline dividers.
- Below `lg`: one card per row.

## Anti-patterns

- Beige paper, copper buttons, or a split ink login panel
- AppBlue on every button
- Texture on the rate matrix or claim lines
- Coloring a row red because the biometric device has no punch
- Dark mode as the default

## Ship checklist

- [ ] Gray page, white cards with an AppBlue edge, purple gradient primary buttons
- [ ] Open Sans
- [ ] Presence mark does not recolor the amount
- [ ] 44px targets and safe-area padding
- [ ] `prefers-reduced-motion` respected if any brand motion is added
- [ ] Production component CSS stays inside the Angular budget
- [ ] Custom CSS comments say why the rule exists (token mapping, gradient only on the sign-in brand panel, reduced motion)
