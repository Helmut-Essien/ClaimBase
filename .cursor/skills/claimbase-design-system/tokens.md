# ClaimBase design tokens

Product aliases live in [SKILL.md](SKILL.md). Atmosphere recipes: [atmosphere.md](atmosphere.md).

## Brand seeds

| Token | Value |
|-------|--------|
| Ink | `#1B3A4B` |
| Ink dark | `#122836` |
| Copper | `#C46B3A` |
| Copper dark | `#A3562C` |
| Paper | `#F4F1EA` |
| Paper border | `#E4DDD2` |
| Slate | `#1C1917` |
| White | `#FFFFFF` |
| Success | `#2F6F4E` |
| Warning | `#A16207` |
| Error | `#B91C1C` |
| Muted | `#64748B` |
| Mode | Light |
| Font | Source Sans 3 |
| Base radius | 6px |

## Typography scale

| Token | Size | Weight | Line height | Letter spacing |
|-------|------|--------|-------------|----------------|
| headline-lg | 28px | 700 | 36px | -0.01em |
| headline-md | 22px | 600 | 30px | — |
| body-md | 16px | 400 | 24px | — |
| body-sm | 14px | 400 | 20px | — |
| label-sm | 12px | 600 | 16px | 0.05em |

## Spacing

| Name | Value |
|------|--------|
| unit | 4px |
| gutter | 16px |
| margin-mobile | 16px |
| margin-desktop | 32px |
| container-max | 1280px |
| touch-min | 44px |

## Radius

| Name | Value | Use |
|------|--------|-----|
| md | 6px | Inputs |
| lg | 10px | Cards |
| full | 9999px | Status pills |

## Elevation

| Level | CSS |
|-------|-----|
| 1 | `0 2px 8px rgba(27, 58, 75, 0.08)` |
| 2 | `0 8px 24px rgba(27, 58, 75, 0.12)` |
| Outline | `1px solid #E4DDD2` |

## Tailwind theme (`Portal/tailwind.config.js`)

```js
ink: { DEFAULT: "#1B3A4B", dark: "#122836" },
copper: { DEFAULT: "#C46B3A", dark: "#A3562C" },
paper: { DEFAULT: "#F4F1EA", border: "#E4DDD2" },
slate: "#1C1917",
success: "#2F6F4E",
warning: "#A16207",
danger: "#B91C1C",
muted: "#64748B"
```

`fontFamily.sans` is Source Sans 3. Amount columns add `tabular-nums`.
