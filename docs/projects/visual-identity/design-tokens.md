# Design tokens — Market direction

Research doc. Catalogues the design tokens defined in the Claude Design bundle
(Market light + Market dark) and maps them to PrimeVue's three-tier token
system (primitive → semantic → component). Companion to
`primevue-theming.md` (which describes how to *deliver* these via a preset).

**Sources:**
- Canonical values: `source/design-bundle/project/src/tokens.jsx`
- Usage in screens: `source/design-bundle/project/src/screens.jsx`
- System card (typography scale, shape/motion): `source/design-bundle/project/src/system-card.jsx`

---

## 1. Palette

### Light (Market)

| Role | Hex | Used for |
|---|---|---|
| `bg`          | `#F7F8F6` | App background. Cool near-white, faint green cast. |
| `surface`     | `#FFFFFF` | Cards, sheets, panels, inputs (resting). |
| `surfaceAlt`  | `#EEF1ED` | Hover / selected / edit-row highlight, progress-bar track. |
| `ink`         | `#111714` | Primary text. Near-black with green cast. |
| `inkSoft`     | `#3D453F` | Secondary text, icon default. |
| `muted`       | `#8A928C` | Tertiary text, metadata, placeholders. |
| `rule`        | `#E4E8E3` | 1px borders, dividers. |
| `primary`     | `#0F6B50` | Brand primary. Buttons (filled), check fill, progress. |
| `primaryInk`  | `#FFFFFF` | Text/icon on primary. |
| `accent`      | `#D9603E` | Coral accent (sparingly — highlights, nudges). |
| `accentSoft`  | `#F4D4C6` | Tinted accent background. |
| `berry`       | `#B03B2E` | Destructive (delete list/item). |
| `ok`          | `#178C6A` | Success / completed-state ticks. |

### Dark (Market)

Derived, not screenshotted. Elevation is expressed as stepped surface
lightness (`bg < surface < surfaceAlt`), and drop shadows are swapped for a
subtle top highlight.

| Role | Hex | Notes |
|---|---|---|
| `bg`          | `#0D1311` | Very dark green-black. Keeps brand cast. |
| `surface`     | `#161D1A` | One step up — elevated cards. |
| `surfaceAlt`  | `#202825` | Two steps up — hover / selected / edit row. |
| `ink`         | `#ECEFEA` | Warm near-white (not pure #FFF). |
| `inkSoft`     | `#B4BAB5` | |
| `muted`       | `#7C847F` | |
| `rule`        | `rgba(255,255,255,0.08)` | Low-alpha whites, not warm rule. |
| `primary`     | `#4FB896` | **Lightened** emerald — `#0F6B50` is too dark on dark bg. |
| `primaryInk`  | `#0B1512` | Dark ink on primary buttons (inverted). |
| `accent`      | `#E88066` | Coral, slightly desaturated to avoid vibrating. |
| `accentSoft`  | `rgba(232,128,102,0.2)` | Tinted. |
| `berry`       | `#E56A5E` | |
| `ok`          | `#5FD1A6` | |

### Full primary ramp (derived)

PrimeVue's `primary` slot expects a full 50–950 palette. The design only
specifies a single green. Derive the ramp from `#0F6B50` (e.g. via the
`palette('#0F6B50')` helper from `@primeuix/themes`) and then override the
scheme-specific `primary.color`, `primary.hoverColor`, `primary.activeColor`,
`primary.contrastColor` under `colorScheme.light` / `.dark` to the exact
values above. The ramp feeds states we haven't explicitly specified
(disabled, subtle-hover, etc.).

Reference (Noir pattern in PrimeVue docs): override both the `primary.*` ramp
and the scheme-specific `primary.color`.

### Full surface ramp

Aura's default surface is zinc. Design wants a green-tinted near-white.
**Decision #3: override the full ramp** so every PrimeVue component
(menus, hover states, disabled backgrounds, etc.) carries the brand cast,
not just hero surfaces. Use `palette()` or a hand-tuned green-tinted zinc
for 0–950; override `colorScheme.*.content.*` and `text.*` on top.

---

## 2. Typography

| Slot | Size / Line-height | Weight | Tracking | Notes |
|---|---|---|---|---|
| Display (H1)   | `44 / 48`   | 700 | `-1`     | On the design-system card only. |
| Page H1        | `32 / 34`   | 700 | `-0.8`   | `Your lists`, list names. |
| Section H1     | `28 / 31`   | 700 | `-0.6`   | Detail screen list name. |
| H2             | `20 / 26`   | 600 | `-0.3`   | |
| Body           | `15 / 22`   | 450 | —        | Weight 450 is a half-step; Inter Tight supports it. |
| Item row       | `16 / 21.6` | 400 | —        | Slightly larger than body for tap-readability. |
| Meta / hint    | `13`        | —   | `-0.1`   | `inkSoft` or `muted`. |
| **Eyebrow**    | `11 / 14`   | 600 | `+1.6`   | UPPERCASE. Replaces what the prototype called `mono`. |

**Font family (all slots):**
`"Inter Tight", "Helvetica Neue", Helvetica, Arial, sans-serif`

The chat iterated from `Inter Tight + JetBrains Mono` to
`Inter Tight everywhere`; the `mono` slot still exists in tokens for
back-compat but resolves to the same stack. Eyebrow labels get their
"refined" feel from uppercase + tracking, not from a monospace font.

**Implementation scope (per open-questions decisions #1, #2, #13):**

- **Font family**: switch from Inter to Inter Tight. `@nuxt/fonts`
  downloads and hosts locally at build time; swap is a one-line change
  plus a weight trim.
- **Weight list**: `[400, 450, 500, 550, 600, 650, 700]` scoped to
  Inter Tight. Trim the current project-wide `[100…900]` spray.
- **Sizes, weights, tracking**: the table above is *reference from the
  handoff*, not a target to chase. Current app H1/H2/body values stay
  as-is. The font switch alone delivers most of the visual shift; further
  tuning is explicitly out of scope for this pass.
- **Eyebrow labels**: keep close to the current `.section-heading` style
  (roughly 16px / uppercase / muted / modest tracking). Formalise as a
  shared `.eyebrow` class, but don't shrink to the handoff's 11px + 1.6
  spec.

---

## 3. Shape, shadow, motion

| Token | Light | Dark | Notes |
|---|---|---|---|
| `r` (radius sm)   | `4px`  | `4px`  | Buttons, inputs, small chips. |
| `rLg` (radius lg) | `10px` | `10px` | Cards, dialogs, list wrappers. |
| `shadow`          | `0 1px 0 rgba(15,107,80,0.04), 0 12px 28px -16px rgba(15,23,18,0.12)` | `inset 0 1px 0 rgba(255,255,255,0.04)` | On dark, elevation = stepped surfaces + 1px top highlight. |
| `motion`          | `120–220ms ease-out` | — | From the system card's "motion" token bit. |

Aura defaults to ~`6px` radius; the design asks for **tighter** (4px) on
sm, **looser** (10px) on lg. Both need explicit overrides.

---

## 4. Token mapping to PrimeVue

### Approach

PrimeVue 4 builds every component on a three-tier token chain:
**primitive → semantic → component**. Best practice is to override at the
semantic layer (and the `primary`/`surface` primitive ramps) — component
tokens are the "last resort". This keeps the theme coherent across every
component, including the ones we don't use yet.

The scheme-dependent values (`primary.color`, `surface.*`, `text.color`,
`content.background`, etc.) should be defined with a `colorScheme: { light,
dark }` block. **Important:** the PrimeVue docs warn that if the preset
originally defines a token inside `colorScheme` and we try to override it
with a bare value, the override is silently ignored — always match Aura's
structure. (See `primevue-theming.md` for the concrete preset skeleton.)

### Semantic token targets

Concrete mapping. Token paths use PrimeVue's dot notation; the generated
CSS variable is `--p-<flattened>` (e.g. `semantic.primary.color` →
`--p-primary-color`).

| Design role | PrimeVue semantic token | Light value | Dark value |
|---|---|---|---|
| `bg`         | — (app-level; not a PrimeVue semantic) | `#F7F8F6` exposed as `--app-background` | `#0D1311` |
| `surface`    | `colorScheme.light.content.background` / dark | `#FFFFFF` | `#161D1A` |
| `surfaceAlt` | `colorScheme.*.content.hoverBackground` | `#EEF1ED` | `#202825` |
| `ink`        | `colorScheme.*.text.color` | `#111714` | `#ECEFEA` |
| `inkSoft`    | `colorScheme.*.text.hoverColor` (reuse) or custom semantic | `#3D453F` | `#B4BAB5` |
| `muted`      | `colorScheme.*.text.mutedColor` | `#8A928C` | `#7C847F` |
| `rule`       | `colorScheme.*.content.borderColor` | `#E4E8E3` | `rgba(255,255,255,0.08)` |
| `primary`    | `colorScheme.*.primary.color` (+ full ramp under `primary.50…950`) | `#0F6B50` | `#4FB896` |
| `primaryInk` | `colorScheme.*.primary.contrastColor` | `#FFFFFF` | `#0B1512` |
| `berry`      | `colorScheme.*.danger` *(custom extend)* or map via `button.danger.*` | `#B03B2E` | `#E56A5E` |
| `ok`         | custom extend slot — no built-in semantic "success" at the top level; component-level `button.success.*` covers buttons. | `#178C6A` | `#5FD1A6` |
| `accent` / `accentSoft` | custom extend slot, e.g. `semantic.extend.accent.color` | `#D9603E` / `#F4D4C6` | `#E88066` / `rgba(232,128,102,0.2)` |

Focus ring: set `semantic.focusRing.color: '{primary.color}'`, width 2px,
offset 2px — matches the "confident" feel of the design.

Form hover border: `semantic.formField.hoverBorderColor: '{primary.color}'`
so inputs pick up the brand on hover (MarketDesignSystem demonstrates
inputs with a primary-colored border when editing).

### Non-scheme tokens (shape, typography)

These don't vary by color mode and can live at the top level of `semantic`:

| Design token | Semantic slot |
|---|---|
| `r` (4px)   | `semantic.formField.borderRadius`, `semantic.content.borderRadius` (small), `button.border.radius` |
| `rLg` (10px) | `content.borderRadius` (if treated as "card radius"), `dialog.border.radius`, custom `--app-card-radius` |
| Font stack   | Not a PrimeVue semantic — set via `body { font-family }` and let components inherit (per PrimeVue docs: *"There is no design for fonts as UI components inherit their font settings"*). |

### Component-level overrides (when semantic isn't enough)

| Component | Token overrides | Why |
|---|---|---|
| `Button`   | `button.border.radius: 4px`, `button.label.font.weight: 550`, `button.padding.y: 11px`, `button.padding.x: 16px` | Match Market's sm/md button spec. |
| `Checkbox` | `checkbox.width: 22px`, `checkbox.height: 22px`, `checkbox.border.radius: 5px`, `checkbox.border.color: {content.borderColor}`, `checkbox.checked.background: {primary.color}` | Design spec: 22px square, 5px radius (not circular), filled primary. |
| `Dialog`   | `dialog.border.radius: 10px`, `dialog.shadow: {semantic.shadow.lg}`, `dialog.header.padding`, `dialog.title.font.weight: 650` | Card-like dialogs. |
| `InputText`| `inputtext.border.radius: 4px`, `inputtext.focus.border.color: {primary.color}`, `inputtext.focus.ring.width: 0` (we use a thicker border at edit rows instead) | Matches EditRow (1.5px primary border when editing). |
| `ProgressSpinner` | Color via `--p-progressspinner-color-1..4` if Aura exposes them, else route through primary. | Loading states should carry brand. |

---

## 5. App-level CSS variables to expose

Already exposed in `app/styles/utilities.css`. Recommended additions /
renames so components can reference tokens consistently instead of
hand-copying PrimeVue surfaces:

```
/* App-level bridges — thin aliases over PrimeVue semantic tokens */
--app-background:      /* var(--p-content-background) alt */
--app-surface:         var(--p-content-background);
--app-surface-alt:     var(--p-content-hover-background);
--app-rule:            var(--p-content-border-color);
--app-ink:             var(--p-text-color);
--app-ink-soft:        /* NEW — map to design inkSoft */
--app-muted:           var(--p-text-muted-color);
--app-primary:         var(--p-primary-color);
--app-primary-ink:     var(--p-primary-contrast-color);

/* Brand-only (no PrimeVue equivalent) */
--app-accent:          /* #D9603E light, #E88066 dark */
--app-accent-soft:     /* ... */
--app-berry:           /* destructive — may map to --p-red-500 */
--app-ok:              /* success */

/* Shape */
--app-radius-sm:       /* 4px */
--app-radius-lg:       /* 10px */

/* Elevation */
--app-shadow-card:     /* multi-layer shadow, see §3 */

/* Motion */
--app-motion-fast:     120ms;
--app-motion:          180ms;
--app-motion-slow:     220ms;
--app-ease:            ease-out;
```

The bridge layer (`--app-*` → `--p-*`) means component CSS doesn't leak
PrimeVue variable names everywhere. It also makes brand-only tokens
(`--app-accent`) live next to the PrimeVue-derived ones for easy reference.

Caveat: today several components reference `--p-*` directly
(`--p-content-border-color`, `--p-surface-400`, `--p-text-muted-color`).
The migration can either (a) leave those alone and add `--app-*` for *new*
surfaces, or (b) sweep everything to the `--app-*` bridge. Pick in the
plan's "token bridge" phase — see `plan.md`.

---

## 6. Resolved mapping choices

All resolved; see `open-questions.md` decisions table for one-line recaps.

- **Surface ramp** (§1): full override.
- **`inkSoft`**: Aura has no dedicated "soft ink" semantic. Add a custom
  `text.softColor` via `semantic.extend` rather than hijacking
  `hoverColor`.
- **Accent**: define `--app-accent` / `--app-accent-soft` tokens in
  Phase 5; defer picking a specific usage site until a natural fit shows
  up in polish.
- **Destructive**: override the `red` primitive palette to berry shades.
  Knock-on effect: every Aura reference to red — including
  `StatePanel.vue`'s error variant — shifts to warm berry. Intentional.
