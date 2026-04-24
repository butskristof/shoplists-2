# Visual identity

Shoplists currently ships with the out-of-the-box PrimeVue Aura look.
Functionally it's solid, but there's no real identity. This project
replaces the default look with the **Market** direction from the Claude
Design handoff (see `source/design-bundle/`): a warm, grocery-adjacent
palette built on a deep emerald primary (`#0F6B50`), cool near-white
backgrounds, tight Inter Tight neo-grotesque type, and a derived dark
mode.

**Chosen direction:** Market (confirmed in the design chat). Pantry was
discarded. Background is the cool near-white variant (`#F7F8F6`), not
the beige originally proposed. Dark mode is derived from Market, not
from current PrimeVue defaults.

**Kept explicit:** The in-app header icon stays on PrimeIcons
(`pi pi-shopping-cart`). Not swapping to the design's custom
`IconBasket`/`LogoMarket` marks.

**All open questions are resolved** — see `open-questions.md` for the
decision table and §"Scope clarifications" there. Short version:
- Inter Tight with half-step weights (450/550/650).
- Full surface-ramp override; destructive colour overrides red primitive
  to berry.
- Home list as cards with progress ring; detail screen grows a
  `<ProgressBar>`.
- Typography tuned *lightly* — font family swaps but H1/H2/body sizes,
  weights, tracking stay at current app values (not the handoff's
  larger/tighter spec).
- No `--p-*` sweep; add `--app-*` only for brand tokens with no PrimeVue
  equivalent.
- Motion, empty states, error states all in scope.

---

## Project docs

- `plan.md` (this file) — roadmap & phases.
- `design-tokens.md` — canonical token values + mapping to PrimeVue's
  primitive/semantic/component token tiers.
- `primevue-theming.md` — how we ship those tokens through
  `definePreset`, dark mode, CSS layering, migration order.
- `component-audit.md` — component-by-component (PrimeVue + custom SFCs)
  current vs target, change type.
- `pwa-and-shell.md` — manifest colours, meta tags, icons, font loading,
  overscroll strips, splash.
- `open-questions.md` — consolidated choices that need user input.
- `source/` — raw Claude Design handoff bundle (README, chat transcript,
  HTML/JSX prototype, screenshots of the pre-redesign app).

---

## Goals

1. Replace the generic Aura look with a cohesive Market-direction visual
   system (colours, type, shape, elevation).
2. Deliver as much as possible through PrimeVue design tokens (a custom
   preset) rather than component-level CSS overrides.
3. Preserve functional behaviour: every interaction that works today
   (create, add items, tick, edit, reorder, delete) keeps working.
4. Keep light/dark mode coherent — dark is *derived* from the Market
   tokens, not re-designed independently.
5. Raise visual consistency across screens: shared eyebrow labels, card
   shape, progress indicator treatment, button styling.

## Non-goals

- Reworking information architecture or flows.
- Moving off PrimeVue.
- Swapping the icon set away from PrimeIcons (except possibly one-off
  custom SVGs where PrimeIcons has no match — see `open-questions.md` #6).
- Shipping the design's custom `LogoMarket` / `IconBasket` marks.
- Full motion design system. Easing + durations are tokenised (see
  `design-tokens.md` §3), but choreographed animations (satisfying
  check-off, slide-to-fulfilled) are deferred unless question #16 in
  `open-questions.md` is answered "in scope".
- Pixel-perfect reproduction. The design is a *reference*, not a spec.
  Match tokens and overall feel; don't chase exact margins.

---

## Phased roadmap

Ordered so each phase produces a visible, revertible improvement.
Mirrors `primevue-theming.md` §5 but scoped to the whole project, not
just the preset.

### Phase 0 — Alignment (done)

All 17 questions in `open-questions.md` are decided. Re-read that file
and the design bundle's `chats/chat1.md` before starting Phase 1.

### Phase 1 — Preset scaffold

Files: `app/theme/preset.ts` *(new)*, `nuxt.config.ts`.

- Create `app/theme/preset.ts` that re-exports Aura unchanged via
  `definePreset`.
- Swap the `nuxt.config.ts` import from `Aura` to `MarketPreset`.
- Verify no visual regressions. This is the "scaffold in place" phase —
  no tokens are changed yet.

### Phase 2 — Primary palette + focus ring

- Override `semantic.primary.*` ramp with `palette('#0F6B50')`.
- Override `colorScheme.light.primary` and `.dark.primary` with the
  exact design values (`#0F6B50`/`#FFFFFF` light,
  `#4FB896`/`#0B1512` dark) plus hover/active shades.
- Override `semantic.focusRing` to use `{primary.color}`.
- Playwright pass: home → detail → edit → dialogs, light + dark.
  Capture before/after.

### Phase 3 — Surfaces, text, rules

- Per Q3 decision: either override `surface.*` ramps or only
  `content.*` + `text.*`.
- Override `colorScheme.*.content.background`, `.hoverBackground`,
  `.borderColor`, `.color` to design `surface`/`surfaceAlt`/`rule`/`ink`.
- Override `colorScheme.*.text.color` and `.mutedColor` to design
  `ink` / `muted`.
- Overscroll strips (`body::before/after`) should pick up the new
  colours automatically — verify.

### Phase 4 — Shape

- `button.border.radius: 4px`
- `inputtext.border.radius: 4px`
- `checkbox.border.radius: 5px`, `width/height: 22px`
- `dialog.border.radius: 10px`
- Expose `--app-radius-sm`, `--app-radius-lg` in `utilities.css` (see
  token bridge below).

### Phase 5 — Token bridge + sweep

- Add `--app-*` bridge variables in `app/styles/utilities.css` per
  `design-tokens.md` §5.
- Per Q15 decision: either sweep existing `--p-*` references (see
  `component-audit.md` §4) to `--app-*`, or only add new brand tokens.
- Replace hard-coded `var(--p-red-500)` in `StatePanel.vue` with the
  chosen destructive alias.
- Add brand-only tokens that have no PrimeVue equivalent: accent,
  accent-soft, berry, ok, shadow-card, motion easing/durations.

### Phase 6 — Component-level polish

Driven by `component-audit.md`. Grouped by which SFCs change:

- **Home** (`pages/index.vue`): per Q10 decision — either extract
  `ShoplistCard` + `ProgressRing` and wrap rows as cards, or restyle
  flat rows with better eyebrow label + tighter h1.
- **Detail** (`components/ShoplistShoppingView.vue`,
  `ShoplistShoppingItem.vue`, `ShoplistHeader.vue`):
  - Wrap item sections in cards.
  - Add 6px progress bar in header (Q14 if in scope).
  - Eyebrow section labels per Q13.
  - Strikethrough on fulfilled per Q12.
- **Edit** (`components/ShoplistEditView.vue`, `ShoplistEditItem.vue`,
  `ShoplistAddItem.vue`):
  - Card wrapper for item list.
  - Editing-row `surfaceAlt` highlight + primary-bordered input.
  - Dashed "add new item" row treatment.
- **Dialogs** (`CreateShoplistDialog.vue`, `DeleteShoplistDialog.vue`):
  - Token-only; nothing to do beyond what Phase 4 lands.
- **AppHeader**: minor CSS — letter-spacing, weight, icon/text sizing.

### Phase 7 — PWA & shell

Per `pwa-and-shell.md` §7 checklist. Done after Phase 2/3 so
`manifest.theme_color` picks a colour that *actually exists* in the
system (not an orphan).

- Update `manifest.theme_color` / `background_color`.
- Emit scheme-specific `<meta theme-color>` in `app.vue`.
- Optional (Q7): regenerate PWA icon with new background via
  `npm run generate:pwa-assets`.

### Phase 8 — Typography switch

- Update `main.css` `font-family` to `"Inter Tight", …` stack.
- Trim `@nuxt/fonts` weights to `[400, 450, 500, 550, 600, 650, 700]`
  scoped to Inter Tight (the current project-wide
  `defaults.weights: [100…900]` loads every weight for every family —
  narrow to the ones we actually use).
- Do **last** — font metrics differ subtly from Inter; visual-test every
  screen after switching so we don't confuse font-induced reflow with
  other issues.
- Scope reminder (per decision #13): H1/H2/body sizes, weights, tracking
  stay at current app values. Only the font *family* changes.

### Phase 9 — Verification pass

- Light + dark Playwright walkthroughs.
- Axe / contrast check on every text/background pair against WCAG AA.
- Smoke-test on a real iPhone in standalone mode (status bar blend,
  safe-area insets, overscroll strips).
- Review vs the design's `ScreenHome` / `ScreenDetail` / `ScreenEdit`
  references in `source/design-bundle/project/src/screens.jsx`.

### Phase 10 — Motion

Candidates (in rough priority order):

- Checkbox check-off animation on tick — the "satisfying" moment.
- Row transition between "Items to get" and "Fulfilled" on tick
  (FLIP / `TransitionGroup`). Might need a small layout shim because
  the two sections are separate `<ul>`s today.
- Dialog enter/leave (fade + slight scale) — PrimeVue has these via
  component tokens; check before hand-rolling.
- Button pressed-state (subtle scale or shadow shift).
- Progress bar fill transition on tick.

Use `--app-motion-*` tokens from Phase 5.

---

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| Overriding surface ramp bleeds colour cast into non-hero components | Phase 3 gate: if Q3 picks full override, visual-test every component (Dialog, Toast, Tooltip, ConfirmPopup) explicitly before moving on. |
| Dark mode derivation looks wrong in practice | Playwright pass in Phase 2 and Phase 3 forces side-by-side light/dark review each step. |
| Font switch reflows content | Phase 8 is last; standalone phase so reflows are attributable. |
| Token overrides silently ignored (PrimeVue `colorScheme` gotcha) | Call out in `primevue-theming.md` §2 — always mirror Aura's structure. Verify via `$dt()` in devtools if a token seems not to apply. |
| PWA icon regeneration overwrites manual tweaks | The generator runs only on `npm run generate:pwa-assets`; don't run unless we've decided to change the source SVG or its config. |
| Scoped `:deep()` overrides in existing components fight new tokens | Phase 5 sweep catches the worst offenders. For genuinely-scoped cases, prefer `:dt` on the component instance over `:deep()`. |

---

## Estimates (rough)

Not effort estimates for commitment, just relative sizing:

| Phase | Size |
|---|---|
| 0 Alignment | XS (discussion) |
| 1 Preset scaffold | XS |
| 2 Primary + focus | S |
| 3 Surfaces / text | M |
| 4 Shape | S |
| 5 Token bridge | M |
| 6 Component polish | L (the big one) |
| 7 PWA | S |
| 8 Font switch | S |
| 9 Verification | M |
| 10 Motion | M |

Phase 6 is where the "feel" change lands. Phases 1-5 are plumbing.
Phases 7-10 are polish on top.

---

## Definition of done

- Every PrimeVue component in `component-audit.md` §1 renders Market
  colours/shape in both light and dark.
- Every custom SFC in `component-audit.md` §2 matches the Market design
  in spirit (not pixel-perfect — see Non-goals).
- No direct references to PrimeVue primitive tokens (`--p-red-500`,
  `--p-surface-400`) in app components for brand purposes; only
  semantic or `--app-*` aliases.
- PWA chrome (status bar, splash, home-screen icon) matches light/dark
  system preference.
- All `open-questions.md` items are either answered or explicitly
  deferred with a decision recorded here.
- Accessibility: AA contrast on every text/background pair used in
  layout.
