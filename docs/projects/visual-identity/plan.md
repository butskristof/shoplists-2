# Visual identity — Planning Notes

Living document. Captures intent and design space for moving the frontend beyond the default
PrimeVue Aura look into something recognisably *Shoplists*.

When approach decisions stabilize (token strategy, typography, app icon direction), the headline
ones can be captured as ADRs. This file is the working plan, not a decision record.

---

## Context

Currently the frontend uses the default PrimeVue Aura preset and overrides almost nothing. The
result is functional but generic: it looks like a demo of PrimeVue rather than a product. The
foundations are good — design tokens / CSS variables auto-switch light/dark, native CSS is in
use everywhere, no Tailwind to undo — so building visual identity on top is mostly additive,
not a refactor.

---

## Goals

- **Distinctive, cohesive look.** A user opening the app should not immediately think "PrimeVue
  Aura demo". Color, type, spacing, and motion should feel intentional and recognisably tied to
  the app's purpose.
- **Consistency across every touchpoint.** PWA install icon, splash, in-app navigation,
  loading/empty/error states, microinteractions — all read as the same product.
- **Accessible by default.** Contrast ratios meet WCAG AA at minimum, reduced-motion respected,
  focus states visible, touch targets sized for thumbs.
- **Mobile-first feel.** Density, gestures, and interaction targets prioritise mobile. Desktop
  remains pleasant but is the secondary surface.
- **Light + dark parity.** Both themes are first-class. PrimeVue tokens already power this — keep
  the abstraction clean and avoid hard-coded colours.

## Non-Goals (for now)

- **Custom illustration system / mascot.** Out of scope for solo dev time; revisit if the app
  grows a marketing surface.
- **Motion / animation framework dependency** (Motion One, Framer Motion, etc.). Native CSS
  transitions + a small set of keyframes is enough; bring in a library only when the surface
  outgrows hand-rolled animations.
- **PrimeVue preset fork.** Stay on Aura; override via design tokens and per-component
  passthrough (PT) as needed. A fork is a long-term liability for a solo project.
- **Storybook / standalone design system site.** The app *is* the system. Revisit when component
  count and reuse justify it.
- **App-store marketing assets / brand kit / launch site.** Out of scope.

---

## Design space — open considerations

### Token layer

PrimeVue v4 exposes a layered token system: *primitive* (raw colour ramps, sizes), *semantic*
(roles like `surface`, `primary`, `text`), *component* (per-component overrides). The right
ownership for this project is:

- **Primitive layer**: leave PrimeVue defaults *or* publish our own colour ramps if we want a
  custom brand colour. Pick once; it propagates everywhere.
- **Semantic layer**: this is where we shape the brand. Spacing scale, type scale, radii,
  shadows, focus rings — owned by us, expressed as CSS variables in a single tokens file.
- **Component layer**: override per-component (Button radius, Dialog elevation, etc.) only when
  the semantic layer can't carry it.

Open: where do tokens live? `app/assets/css/tokens.css` imported once, vs. a PT preset file vs.
PrimeVue's `definePreset`. Lean: `definePreset` for token overrides, native CSS for layout/utility.

### Color

The current default Aura look is a neutral indigo. Options:

- Keep Aura's palette, change only the primary colour to something more food/grocery (warm,
  green, terracotta).
- Re-ramp neutrals to a warmer / cooler base — small change, big impact on overall feel.
- Pick a fully custom palette — biggest commitment, biggest visual identity payoff.

All three are reversible; cost scales with depth. Recommend starting from the cheapest move
(primary swap) and only going deeper if it isn't enough.

### Typography

Currently inheriting whatever the browser default + PrimeVue ships. The single biggest visual
identity lever is type:

- Pick a body face (Inter, Geist, IBM Plex Sans, etc. — variable fonts only, to keep payload tight).
- Pick a heading face *or* use the same family at heavier weights.
- Define a type scale (modular scale at 1.125 or 1.2) and stick to it.

Self-hosted via `@nuxt/fonts` (already a candidate; verify whether it's installed) or
`unplugin-fonts`. Avoid Google Fonts CDN — it's a privacy concern and adds a third-party hop.

### Spacing & rhythm

Establish an 8-pt (or 4-pt) base and use only those values. Audit current components: most
PrimeVue defaults are already on an 8-pt grid; we mostly need to align *our* spacing
(`ShoplistCard`, page layouts, dialog padding) to it.

### Iconography

`primeicons` is already in use. Decisions:

- Keep `primeicons` (broad, familiar, free).
- Augment with `@iconify/vue` for one-offs (huge library, tree-shakeable).
- Adopt a single coherent icon system (Lucide, Phosphor) — bigger commitment, bigger payoff for
  consistency.

Lean: PrimeIcons primary, Iconify on-demand for gaps. Reassess if the visual mismatch becomes
noticeable.

### Empty, loading, and error states

Today these are functional but minimal. They are *the* opportunity to inject personality without
touching feature code:

- Empty state for "no lists yet" — illustration / icon + tone-of-voice CTA, not just "No lists".
- Empty state for "list with no items".
- Loading: replace plain spinners with skeletons that match the eventual layout.
- Errors: tone, recovery action, clarity.

`StatePanel` already exists as a reusable primitive — extend it; don't fork.

### Microinteractions

Small touches that punch above their weight:

- Tick-off animation on `ShoplistItem` fulfilled toggle (current state: instant).
- Add-item input: subtle focus ring transition, autofocus on dialog open.
- Drag-to-reorder visual feedback (if/when reordering UX expands).
- Toast styling: consistent with the brand, not default Aura.
- Reduced-motion query respected for all of the above.

### App icon

The PWA install icon is the brand's first impression. Iterate it alongside the colour and type
decisions, not before — it should reflect the same identity, not lead it. The current asset is a
placeholder.

### iOS / PWA polish

The PWA project already covers safe-area handling, theme/background colour for splash, etc. Visual
identity reuses that surface — pick a `theme_color` and `background_color` that match the chosen
brand, and align the standalone status bar style.

---

## Phased rollout (sketch)

- **Phase 1 — Foundation.** Pick brand direction (primary colour, type pairing). Stand up the
  tokens file. Light + dark parity from day one. Audit the app for hard-coded colours and
  replace with semantic tokens.
- **Phase 2 — Component overrides.** PT overrides for the components actually in use (Button,
  InputText, Dialog, Toast, ConfirmDialog, Menu). Spacing audit. Focus-state pass.
- **Phase 3 — Personality.** Skeletons, empty-state illustrations, microinteractions on the most
  common interactions (tick, add, delete). App icon iteration.
- **Phase 4 — Cohesion polish.** Walk every screen on mobile + desktop, light + dark. Fix
  spacing/alignment regressions. A11y pass with axe / Lighthouse.

Each phase is shippable on its own; the app is never half-broken.

---

## Open questions

- **Brand direction.** Solo design pass, or commission someone for the colour + type starter?
- **Primary colour.** Food/grocery-adjacent (warm green, terracotta) or deliberately neutral
  (high-contrast monochrome with a single accent)?
- **Type pairing.** Single variable font (Inter / Geist) or two-family pairing?
- **Font hosting.** `@nuxt/fonts` vs `unplugin-fonts` vs self-host directly. Confirm what's
  already installed.
- **`definePreset` vs raw CSS variables** for token ownership. Confirm by trying one Button
  override end-to-end.
- **Icon system commitment.** Stay on PrimeIcons, or replace with a more cohesive set (Phosphor,
  Lucide)?
- **Reduced-motion strategy.** Per-animation `@media (prefers-reduced-motion: reduce)`, or a
  central `--motion-scale` token that animations multiply against?

---

## Priority signals (when to actually start)

- Functionally the app is good. Visual identity is now the gap between "working" and
  "delightful". For a portfolio / showoff project this gap matters more than another feature.
- Independent of every other project — no backend dependency, no schema risk. Safe to fork off
  any time and run in parallel with other work.
- Phase 1 (token foundation) is the highest-leverage starting point — every other phase
  compounds on it.
