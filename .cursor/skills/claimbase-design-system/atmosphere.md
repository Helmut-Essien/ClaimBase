# ClaimBase atmosphere

Use texture on the **login brand panel** only. Claim tables, the rate matrix, and forms stay flat so amounts scan cleanly.

## Film grain

```css
.cb-texture-grain {
  position: relative;
  isolation: isolate;
}
.cb-texture-grain::after {
  content: "";
  pointer-events: none;
  position: absolute;
  inset: 0;
  opacity: 0.04;
  background-image: url("data:image/svg+xml,%3Csvg viewBox='0 0 256 256' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.8' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E");
  z-index: 1;
}
```

Keep grain under the headline (`z-10` on the copy).

## Dot mesh on ink

```css
.cb-texture-dots-on-ink {
  background-image: radial-gradient(
    circle at 1px 1px,
    rgba(255, 255, 255, 0.14) 0.75px,
    transparent 0
  );
  background-size: 22px 22px;
}
```

## Motion

If the brand panel has a single decorative shift, animate `transform` only and disable it under `prefers-reduced-motion: reduce`. Do not animate table rows.

## Leave off

- Rate cells, claim lines, session cards, and PDF-preview UI
- Grain opacity above 0.05
- A second product's green or gold brand colors
