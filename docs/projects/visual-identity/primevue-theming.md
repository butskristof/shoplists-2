# PrimeVue theming strategy

How we deliver the Market design tokens through PrimeVue. Companion to
`design-tokens.md` (which catalogues the tokens themselves). This doc
describes *where* and *how* to wire them in the current Nuxt app, and what
that means for CSS layering, dark mode, and the existing scoped component
styles.

**Current setup** (from `frontend/nuxt.config.ts` and `app/styles/`):
- Preset: `Aura` (imported as-is, no `definePreset` customisation).
- `darkModeSelector: ".dark-mode"`, driven by `@nuxtjs/color-mode`
  (`classSuffix: "-mode"` → emits `.light-mode` / `.dark-mode`).
- `cssLayer: { name: "primevue", order: "reset, primevue" }`. App CSS
  outside layers wins over PrimeVue.
- App vars in `app/styles/utilities.css` bridge a small spacing/typography
  scale and alias `--app-background` to `--p-surface-50` / `--p-surface-950`.

---

## 1. Where to put the preset

A custom preset is a small JS/TS module. Three viable locations in a Nuxt 4
app:

1. **Inline in `nuxt.config.ts`** — quick, but the file is already dense.
2. **`app/theme/preset.ts`** — exported and imported from `nuxt.config.ts`.
   Clean, keeps Nuxt config slim, plays well with typechecking.
3. **Built up at runtime via a plugin** — only needed if we want
   `updatePreset()` / `usePreset()` switching. Not the case for us.

Recommendation: **option 2** — a single `app/theme/preset.ts` exporting
`MarketPreset`. Use the `extend` slot for brand-only tokens (accent, ok,
shadow-card, etc.) so they're reachable via `$dt()` and CSS vars.

```ts
// app/theme/preset.ts (illustrative — not to be committed until we agree)
import { definePreset, palette } from "@primeuix/themes";
import Aura from "@primeuix/themes/aura";

export const MarketPreset = definePreset(Aura, {
  semantic: {
    primary: palette("#0F6B50"),
    focusRing: { width: "2px", style: "solid", color: "{primary.color}", offset: "2px" },
    colorScheme: {
      light: {
        primary:     { color: "#0F6B50", contrastColor: "#FFFFFF", hoverColor: "#0C5A43", activeColor: "#094A37" },
        content:     { background: "#FFFFFF", hoverBackground: "#EEF1ED", borderColor: "#E4E8E3", color: "#111714" },
        text:        { color: "#111714", mutedColor: "#8A928C" },
        formField:   { hoverBorderColor: "{primary.color}", focusBorderColor: "{primary.color}" },
      },
      dark: {
        primary:     { color: "#4FB896", contrastColor: "#0B1512", hoverColor: "#66C8A9", activeColor: "#7DD8BD" },
        content:     { background: "#161D1A", hoverBackground: "#202825", borderColor: "rgba(255,255,255,0.08)", color: "#ECEFEA" },
        text:        { color: "#ECEFEA", mutedColor: "#7C847F" },
        formField:   { hoverBorderColor: "{primary.color}", focusBorderColor: "{primary.color}" },
      },
    },
  },
  components: {
    button:   { borderRadius: "4px", labelFontWeight: "550" },
    checkbox: { width: "22px", height: "22px", borderRadius: "5px" },
    inputtext:{ borderRadius: "4px" },
    dialog:   { borderRadius: "10px" },
  },
  extend: {
    app: {
      accent:     { color: "#D9603E", soft: "#F4D4C6" },
      berry:      "#B03B2E",
      ok:         "#178C6A",
      shadowCard: "0 1px 0 rgba(15,107,80,0.04), 0 12px 28px -16px rgba(15,23,18,0.12)",
      motion:     { fast: "120ms", base: "180ms", slow: "220ms", ease: "ease-out" },
    },
  },
});
```

```ts
// nuxt.config.ts (diff — not yet applied)
- import Aura from "@primeuix/themes/aura";
+ import { MarketPreset } from "./app/theme/preset";

  primevue: {
    options: {
      theme: {
-       preset: Aura,
+       preset: MarketPreset,
        options: { /* unchanged */ },
      },
    },
  },
```

**Exact values above are illustrative — hover/active shades are guessed
ramp steps; the real preset should use `palette()` or manually-tuned values
verified in light+dark before committing.** This doc is for planning; the
preset goes in during the "Preset" phase of `plan.md`.

---

## 2. Dark mode

Today: `colorMode.preference: "system"` with `.dark-mode` class applied at
the `<html>` level. This already matches PrimeVue's
`darkModeSelector: ".dark-mode"` and is exactly how the tokens above
activate — no extra wiring needed.

Design-side dark mode is *derived* (not screenshotted), so the mapping in
§4 of `design-tokens.md` is the target. Validate each derived value with
Playwright once the preset lands — the chat explicitly called out that
screenshots of the current PrimeVue dark mode were *not* supplied so the
implementer wouldn't anchor to defaults.

**Gotcha** (from PrimeVue docs): when a token is defined inside
`colorScheme` in the base preset, a bare override at the root level is
silently ignored. Always mirror Aura's structure — if Aura puts
`primary.color` inside `colorScheme.light`, our override must too.
`definePreset` does not warn about this.

---

## 3. CSS layering

The current `order: "reset, primevue"` means unlayered app CSS wins. This
is convenient today but has a corollary worth documenting:

- Preset-driven changes (CSS variables) apply regardless of layer, so
  token work is unaffected.
- Any scoped component CSS that currently references `--p-*` variables
  directly (see `component-audit.md` §4) will automatically pick up the
  new values — no component-level churn required for recolour.
- Places where we've **hard-coded values** (e.g. `color: var(--p-red-500)`
  in `StatePanel.vue`, or direct uses of `--p-surface-400`) will NOT
  auto-migrate. Those need a sweep to point at semantic or `--app-*`
  tokens.

Recommendation: add the token-bridge layer in `utilities.css` (see
`design-tokens.md` §5) and sweep references to `--app-*` for anything
brand-relevant. Leave neutral references (`--p-surface-200` as a separator
in a non-branded context) alone.

---

## 4. Scoped tokens via `:dt`

PrimeVue supports per-component token scoping with the `dt` prop:
```vue
<Checkbox :dt="{ width: '24px', checkedBackground: '{primary.hoverColor}' }" binary />
```
Prefer this over `:deep()` CSS overrides. Relevant to a handful of
components where the global token isn't quite right for one instance
(e.g. the edit-mode item row's extra-thick primary border).

---

## 5. Migration order

Order-sensitive; doing these out of sequence creates visual regressions
that are hard to attribute.

1. **Add preset scaffold only** — ship `preset.ts` but keep values
   identical to Aura. Verify nothing changes visually.
2. **Primary palette + focus ring** — switch the green. This is the
   biggest visual shift and most of the "new look" lands here.
3. **Surfaces & text** — `content.background/borderColor/color`, `text.*`.
4. **Shape** — radii (small change, big "feel" shift).
5. **Component tokens** — `checkbox`, `button`, `dialog`, `inputtext`.
6. **App-level CSS vars** — add `--app-*` bridge, sweep `--p-surface-400`
   and `--p-red-500` style direct references in existing components.
7. **Font switch (if chosen)** — Inter → Inter Tight. Do last because it
   affects every spacing calc.

Each step is independently revertible if something looks wrong.

---

## 6. Verification plan

For every step above:

1. Run the app via Aspire (`aspire run` — per CLAUDE.md, check MCP for
   existing instance first).
2. Playwright walkthrough: home → create list → add items → tick items →
   edit mode → rename → delete. Capture screenshots in light and dark.
3. Compare against `source/design-bundle/project/src/screens.jsx` (don't
   open the HTML in a browser — read the JSX).
4. Check contrast ratios for text-on-primary and text-on-surface pairs
   (need AA at minimum for body, AAA for small text where practical).

---

## 7. Things NOT to change during token work

- PrimeIcons usage (shopping-cart header logo stays, per user).
- The `cssLayer: { name: "primevue", order: "reset, primevue" }` config —
  it's correct and removing it would regress scoped-style overrides.
- The `.dark-mode` class selector (already aligned with the design's
  toggle model).
- The `body::before/after` overscroll strips (see `pwa-and-shell.md`) —
  they reference `--p-content-background` / `--app-background`, both of
  which are within the token system.
- `@nuxt/fonts` weight preloading — but see the font-switch note in
  `pwa-and-shell.md` §3.
