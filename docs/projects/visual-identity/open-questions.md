# Decisions & open questions

All 17 original questions are **resolved**. Decisions summarised below;
rationale kept in the sections that follow. Any new ambiguities that come
up during implementation should be added to this file as fresh questions
rather than decided silently.

---

## Decisions (2026-04-23)

| # | Topic | Decision |
|---|---|---|
| 1  | Typography family | **Switch to Inter Tight**. `@nuxt/fonts` downloads at build time, so it's served from our origin, not Google at runtime. |
| 2  | Half-step weights | **Load them** (450, 550, 650). Design is metrics-tuned for them; trimming the rest of Inter's weights keeps total bytes in check. |
| 3  | Surface ramp | **Override the full ramp** with design surfaces. Worth the extra plumbing for a cohesive feel across every PrimeVue component. |
| 4  | Destructive colour | **Override the `red` primitive to berry.** Every Aura reference (including `StatePanel.vue` error variant) lands warmer. Intentional. |
| 5  | Accent (coral) | **Define the tokens, don't wire a site yet.** Expose `--app-accent` / `--app-accent-soft`; pick a natural moment during later polish. |
| 6  | Drag-handle grip | **Custom SVG.** Single-use 6-dot grip component; keep PrimeIcons everywhere else. |
| 7  | PWA icon bg | **Switch to design light bg (`#F7F8F6`).** Regenerate via `pwa-assets.config.ts`. |
| 8  | `manifest.theme_color` | **`#F7F8F6`** (background, not brand). Avoids a coloured-chrome flash before first paint. |
| 9  | Dark-mode splash | **Defer.** Accept a single light-mode manifest value; OS usually handles dark-side theming well enough that the brief splash isn't worth extra machinery. |
| 10 | Home list rows | **Follow the design.** Card layout with progress ring. Extract `ProgressRing` + `ShoplistCard`. |
| 11 | Edit-mode row | **Align with design.** `surfaceAlt` bg + 1.5px primary-border input on the row being edited. |
| 12 | Strikethrough | **No strikethrough.** Keep current opacity-based fade on fulfilled items. |
| 13 | Section headings | **Stay close to current styling.** Tune the existing `.section-heading` slightly (formalise as an `.eyebrow` utility), but don't shrink to the design's 11px / +1.6 tracking spec. H1s stay at their current sizing/weight/tracking too. |
| 14 | Progress bar on detail | **In scope.** Use PrimeVue `<ProgressBar mode="determinate" :value>` with token overrides. |
| 15 | Token bridge | **`--p-*` where possible, add `--app-*` only for brand tokens with no PrimeVue equivalent.** No big sweep. Because of #4, existing `--p-red-500` references auto-shift to berry. |
| 16 | Motion | **In scope.** Checkbox check-off animation, list-row transitions between "to get" / "fulfilled", dialog enter/leave, etc. Phase 10 in `plan.md`. |
| 17 | Empty / error states | **Also redesigned to match the new look.** Use the same card shape, eyebrow labels, muted ink — don't leave them looking like the old app. |

### Scope clarifications from these decisions

- **Typography is touched lightly.** Font family swaps, but H1/H2/body
  *sizes, weights, tracking* keep their current values. `design-tokens.md`
  §2's typography table is reference material from the design bundle, not
  a target to chase. This is deliberately a more conservative
  interpretation than the handoff shows.
- **No `--p-*` sweep.** `component-audit.md` §4's inventory is now
  informational — we only touch a reference if the override changes
  semantics in a way we actively want (e.g. the grip-handle colour going
  from `--p-surface-500` to `--app-muted` so it tracks the brand muted,
  not Aura zinc-500).
- **Phase 0 of `plan.md` is unblocked** — all decisions needed to start
  Phase 1 are above.
- **Phase 8 (font switch) is no longer optional.**

---

## Original questions (for rationale context)

### Typography

1. **Switch to Inter Tight?** — Decided #1.
2. **Half-step weights (450 / 550 / 650)?** — Decided #2.

### Palette

3. **Surface ramp strategy** — Decided #3.
4. **Destructive colour: `berry` vs PrimeVue `danger`** — Decided #4.
5. **Accent (coral) usage** — Decided #5.

### Icons

6. **Drag-handle grip** — Decided #6.
7. **PWA home-screen icon background** — Decided #7.

### PWA / chrome

8. **`manifest.theme_color` single value** — Decided #8.
9. **Dark-mode splash** — Decided #9.

### Components & layout

10. **Home list rows: card or flat?** — Decided #10.
11. **Edit-mode drag row highlight** — Decided #11.
12. **Strikethrough on fulfilled items** — Decided #12.
13. **Section headings → eyebrow** — Decided #13.
14. **Progress bar on detail screen** — Decided #14.

### Token bridge

15. **Migrate existing `--p-*` references to `--app-*`?** — Decided #15.

### Scope

16. **Motion / micro-interactions** — Decided #16.
17. **Empty states and error states** — Decided #17.
