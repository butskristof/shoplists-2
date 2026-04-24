# Component audit — current vs Market

Inventory of every component that contributes to the UI today, paired with
what the Market design asks for and what kind of change lands it there.
Companion to `design-tokens.md` (token values) and `primevue-theming.md`
(how tokens are delivered).

**Legend:**
- **tokens** — reachable purely by design-token overrides (no code change).
- **CSS** — needs scoped-style edits in the SFC.
- **template** — needs template / logic change.
- **new** — introduce a new element that doesn't exist yet.

---

## 1. PrimeVue components in use

Enumerated from `frontend/app/`:

`Button`, `InputText`, `Checkbox`, `Dialog`, `ConfirmPopup`,
`ProgressSpinner`, `Toast`, directive `v-tooltip`.

### 1.1 Button

Used everywhere: page headers, dialogs, item rows, color-mode toggle,
back button (as-child wrapper).

| Aspect | Current (Aura defaults) | Market target | Change |
|---|---|---|---|
| Primary bg / text | Aura emerald (`#10b981`) on white | `#0F6B50` / white (light), `#4FB896` / `#0B1512` (dark) | **tokens** |
| Radius            | Aura default (~6px) | `4px` | **tokens** (`button.border.radius`) |
| Label weight      | 500 | `550` (half-step, Inter Tight) | **tokens** (`button.label.font.weight`) |
| `variant="text"`  | Aura tinted hover | Subtle `surface.alt` hover — keep | **tokens** (should come for free from `text.primary.hover.background`) |
| `severity="danger"` | Aura red-500 | `#B03B2E` (warmer "berry") | **tokens** or see §6 "destructive" decision |
| `severity="secondary"` | Aura slate | Keep close to default | **tokens** (minor) |
| `rounded` variant | Circular | Keep (edit-row icon buttons) | — |
| Icon-only size    | `--p-button-icon-only-width` | Design uses 32/40px tight icons on edit rows | **tokens** (already overridden locally in `ShoplistEditItem.vue`) |

No template changes. Design's "ghost" button kind (primary color with rule
border on transparent) maps to `variant="outlined"` + `severity="primary"`.

### 1.2 InputText

Used in dialogs, edit-item, add-item, edit-list-name.

| Aspect | Current | Market | Change |
|---|---|---|---|
| Border color (resting) | `content.borderColor` | same | **tokens** |
| Radius | Aura default | `4px` | **tokens** |
| Focus   | Aura focus ring | **1.5px primary border** (design EditRow), no ring | **tokens** (`inputtext.focus.border.color`, focus-ring width 0) or local `:dt` scoping |
| Invalid | Red border | Red border — keep Aura semantics but swap palette | **tokens** |

### 1.3 Checkbox

Used in `ShoplistShoppingItem.vue` (`size="large"`, `binary`).

| Aspect | Current | Market | Change |
|---|---|---|---|
| Shape        | Square 22×22, rounded corners (Aura) | Same 22px, `5px` radius, `1.5px` border | **tokens** |
| Unchecked bg | `checkbox.background` = surface | `surface` (#FFFFFF) | **tokens** |
| Checked bg   | `primary.color` | `primary.color` | **tokens** (already matches) |
| Checked border | `primary.color` | same | **tokens** |
| Check icon   | Aura check | Design's "rounded, confident" check — same visual, slightly fatter stroke | `IconCheck` in `icons.jsx` looks virtually identical to PrimeIcons's `pi-check`. Keep PrimeIcons. |

No template changes.

### 1.4 Dialog

Used in `CreateShoplistDialog.vue`, `DeleteShoplistDialog.vue`.

| Aspect | Current | Market | Change |
|---|---|---|---|
| Radius        | Aura default | `10px` (rLg) | **tokens** (`dialog.border.radius`) |
| Shadow        | Aura default | soft stacked shadow (design's `shadow`) | **tokens** (`dialog.shadow`) |
| Title weight  | Aura default | `650` (Inter Tight) | **tokens** (`dialog.title.font.weight`) |
| Header padding| OK | OK | — |

Footer button layout in both dialogs is a scoped flex row — fine, keep.

### 1.5 ConfirmPopup

Used in `ShoplistEditItem.vue` for delete confirmation.

| Aspect | Change |
|---|---|
| Overall look | **tokens** — inherits dialog/button tokens; nothing custom needed. |
| Accept = Delete, severity danger | Already wired correctly; will recolour automatically. |

### 1.6 Toast

Registered globally in `app.vue` (`position="bottom-right"`). No
theme-specific work — inherits severity + content tokens. **Token-only**.

### 1.7 ProgressSpinner

Used in `LoadingPanel.vue`. Accent stroke colour needs to pick up
`primary.color`. Aura spins with a 4-color rotating stroke
(`--p-progressspinner-color-{1..4}`). The design implies a single-stroke
spin in primary. Two options:
- Token-only: set all four colors to `primary.color` (minimal change).
- Custom: swap to a simpler single-stroke spinner component.

Recommendation: token-only now, revisit if the spin feels noisy.

### 1.8 Tooltip (directive)

Used on icon buttons. Defaults are fine; minor token overrides would align
the background/color to `content.*`. **Token-only**.

---

## 2. Custom components

Every SFC in `frontend/app/components/` + the two pages. Each entry notes
the current file and what changes land it on Market.

### 2.1 `AppHeader.vue`

Current:
```html
<NuxtLink class="logo">
  <i class="pi pi-shopping-cart logo-icon" />
  <div class="name">Shoplists</div>
</NuxtLink>
```
Sticky top, bottom rule, padding, safe-area top inset.

| Aspect | Change |
|---|---|
| Icon: `pi pi-shopping-cart` | **Keep** per user direction — no switch to the design's basket mark. |
| Logo colour | Already `var(--p-primary-color)` — token-driven. |
| Wordmark weight / tracking / size | **Keep current styling** — per decision #13, no H1/wordmark resizing in this pass. The font-family swap (Inter → Inter Tight in Phase 8) delivers the typographic refresh. |
| Border-bottom | 1px `--p-content-border-color` | Same after tokens recolour. No change. |

### 2.2 `AppFooter.vue`

Holds `<ColorModeToggle />`. Minimal. Consider showing the eyebrow
"built by …" style small-caps label there, or add PWA install prompt.
No design direction — **no change** required for visual identity pass.

### 2.3 `ColorModeToggle.vue`

Uses `<Button text rounded severity="secondary" />`. Recolours with
tokens. **Token-only**.

### 2.4 `pages/index.vue` — list overview

The home screen. This is the most visible parity target with the design's
`ScreenHome`.

Design:
- Eyebrow "Your week" — small-caps Inter Tight 11/14, +1.6 tracking
- `h1` Your lists — 32/34, 700, -0.8
- Action: filled primary "+ New list" pill on the right
- Lists rendered as **cards** in a vertical stack with 10px gap:
  - 44px **progress ring** (conic gradient, primary on rule-colored track)
  - Inner 34px circle showing `done/total`
  - Title row (17px, 600, -0.2) + meta (13, muted)
  - Chevron right in `inkSoft`

Current:
- `h1` + button page-header row: ✓ matches shape
- Lists rendered as a `<ul>` of plain `<li><NuxtLink class="list-row">…`
  with bottom 1px rule, no card look, no progress ring, plain chevron.

Changes needed (per decision #10: follow the design's card layout):
- **template**: wrap each row as a card (`var(--app-radius-lg)`, bordered,
  shadow). Replace flat-row pattern. Extract as `<ShoplistCard>` in
  `components/`.
- **new**: progress-ring component. Conic gradient trick works and has
  wide support. Extract as `<ProgressRing :done :total />` in
  `components/`.
- **template**: add eyebrow label above the h1 (use `.eyebrow` utility
  from `utilities.css` once added; see §2.8).
- **tokens**: chevron color, card border/shadow, hover `surface-alt`.
- **Keep h1 as-is** — no sizing/tracking/weight changes (decision #13).

### 2.5 `pages/lists/[id].vue` — list detail

Thin orchestrator (loading/not-found/error + `<ShoplistDetail />`). No
visual changes to the page itself. The child components carry the design.

### 2.6 `ShoplistDetail.vue`

Orchestrator for shopping vs edit view and delete-list action. No visual
changes beyond what its children get.

### 2.7 `ShoplistHeader.vue`

Title + edit/cancel-edit toggle + rename-in-place form.

Design (Detail screen):
- `h1` list name — 28/31, 700, -0.6
- Ghost "Edit" pill with pencil icon on the right, matched to progress bar

Current:
- `h1` lives inside `.list-name` with a `line-height: 50px` hack to
  prevent layout shift on edit-mode toggle. Functional, not pretty.
- Edit / exit-edit is a filled primary `<Button>`.

Changes needed:
- **Keep h1 as-is** — no sizing/tracking/weight changes (decision #13).
  The `line-height: 50px` hack (to prevent layout shift on edit toggle)
  stays — just document *why* it's there with a comment if it isn't
  already.
- **template** (optional): swap filled "Edit list" for `variant="outlined"`
  ghost button to match design's ghost style. Reduces visual weight on a
  busy row.

### 2.8 `ShoplistShoppingView.vue`

"Items to get" / "Fulfilled" two-section layout.

Design:
- Section label (eyebrow): 11/14, 600, +1.6, uppercase.
- Items are grouped inside a **card** (bordered, rounded `rLg`,
  `shadow`, `surface` background). Rows separated by 1px `rule`, no outer
  padding between rows.
- "Fulfilled" card has `opacity: 0.85`.
- Progress bar between title and first section: 6px track in `surfaceAlt`,
  fill in `primary`, 300ms transition.

Current:
- Two `<section>`s each with an `<h2 class="section-heading">` styled as
  muted uppercase (close to eyebrow).
- Item list is a plain `<ul>` — no card wrapper, no grouped background.
- No progress bar.

Changes needed:
- **template**: wrap each `<ul>` in a card container (class + border +
  radius + shadow). Remove inter-row padding, rely on 1px dividers inside.
- **template / new**: add PrimeVue `<ProgressBar mode="determinate"
  :value="pct" />` between list name and first section (decision #14) —
  either inline in `ShoplistHeader.vue` or as its own
  `<ShoplistProgress.vue>`. `pct = fulfilled / total * 100`.
- **CSS / utility**: formalise the section-heading as a shared `.eyebrow`
  class in `utilities.css`, but **keep it close to the current look**
  (decision #13) — roughly the current `font-size-base`, weight 600,
  `letter-spacing: 0.05em`, uppercase, muted. Don't shrink to the
  handoff's 11px + 1.6 tracking. Reuse the class above the home h1 too.

### 2.9 `ShoplistShoppingItem.vue`

Single row with checkbox + name.

Design:
- 14px padding, 1px rule divider between rows (no divider on last).
- Checked state: text in `muted` with line-through.

Current:
- `padding: var(--spacing-sm) var(--spacing-md)` → 8/16.
- Bottom 1px `--p-content-border-color` divider (applied always — leaves
  a line under the last row, which the design removes).
- Checked state: `opacity: 0.5`, line-through commented out.

Changes needed:
- **CSS**: no strikethrough — keep current `opacity: 0.5` behaviour on
  fulfilled items (decision #12). Leave the commented-out
  `text-decoration: line-through` in place or remove outright.
- **CSS**: remove the bottom border from the last row (`:last-child` or
  let card wrapper handle overflow + inner dividers). Easier once card
  wrapper lands in 2.8.

### 2.10 `ShoplistEditView.vue`

Drag-to-reorder item list + `ShoplistAddItem` row.

Design:
- Items wrapped in a single card (same shape as shopping view).
- The **row being edited** has `surfaceAlt` background + 1.5px primary
  border on the input.
- "Add new item" row: 1px dashed border, muted placeholder text, 40px
  filled primary `+` button on the right.
- Drag handle = 6-dot IconGrip in `muted`. PrimeIcons has
  `pi-ellipsis-v` (used today) or `pi-bars`. Neither is a 6-dot grip.
  Closest: leave `pi-ellipsis-v` (accepted trade-off) or add a custom SVG
  icon here only.

Changes needed:
- **template**: wrap `<ul>` in card (same as 2.8).
- **CSS**: editing row highlight = `surfaceAlt` background.
- **CSS**: add-item row dashed border treatment; currently just padded
  flex.
- **template / new**: swap `pi-ellipsis-v` for a dedicated 6-dot grip SVG
  (decision #6). Add `components/GripIcon.vue` (inline SVG, 24×24,
  currentColor) and use it only here. Reference `IconGrip` in
  `source/design-bundle/project/src/icons.jsx` for the exact dot
  positions.

### 2.11 `ShoplistEditItem.vue`

Individual editable row. Already does edit-in-place with save/cancel
icon buttons.

Design alignment:
- Editing row gets `surfaceAlt` background.
- Input has 1.5px primary border, no focus ring.
- Action icon buttons are 32×32 (primary for save, muted for cancel,
  berry for delete).

Changes:
- **CSS**: editing bg + primary-border input.
- **tokens**: map icon-button colours correctly (already using
  `severity="danger"` → auto via tokens).

### 2.12 `ShoplistAddItem.vue`

Placeholder-dashed row. Already fairly close to the design concept —
just needs the dashed border and filled 40px `+` button treatment.

### 2.13 `CreateShoplistDialog.vue` / `DeleteShoplistDialog.vue`

Both use PrimeVue `<Dialog modal>` + `<InputText>` + `<Button>`. Inherit
all theming. **Token-only**.

Minor: delete-dialog footer could use the `berry` / `danger` treatment
that tokens provide — already is via `severity="danger"`.

### 2.14 `StatePanel.vue`

Hard-codes `color: var(--p-red-500)` for error variant. With the red
primitive overridden to berry shades (decision #4), this reference
automatically shifts to warm berry — no code change needed here. Leave
the `--p-red-500` reference as-is (decision #15 says no sweep).

**Other states** (per decision #17): empty + error variants should match
the new look:
- **Empty state** (`pages/index.vue` "No lists yet"): wrap in the same
  card shape used for list rows, use the shared `.eyebrow` label above
  the title, muted ink, no bordered container.
- **Error state**: same card shape with berry accent on the icon and
  title, primary "Retry" button. Message body stays in `inkSoft`.
- **Shopping view empty** ("Ready to go!"): card shape + muted checklist
  icon + primary "Add items" CTA.
- **Detail not-found**: same treatment as the error state.

All of these are served by `StatePanel.vue` today — the component gets a
small restyle (card wrapper + eyebrow slot + slot ordering) and the
callers just keep passing the same props.

### 2.15 `LoadingPanel.vue`, `BackButton.vue`, `AuthInfo.vue`

All purely semantic PrimeVue usage. **Token-only**.

---

## 3. New components worth extracting

These appear multiple times in the design and don't exist yet:

| Component | Why |
|---|---|
| `ProgressRing` | Home list cards. Conic-gradient ring + inner disc with count. Reusable, well-scoped. |
| `ProgressBar`  | Detail screen header. PrimeVue has `<ProgressBar mode="determinate" :value>` — use it with token overrides; no new custom component needed. |
| `.eyebrow` utility | Shared caps-label style, close to the current `.section-heading` look. Put in `utilities.css`. Used above page h1s and for section headings. |
| `ShoplistCard` | Extract the home list row once it gains ring + card styling. Today it lives inline in `pages/index.vue`. |
| `GripIcon.vue` | Custom 6-dot drag-handle SVG (`components/GripIcon.vue`). Single-use in `ShoplistEditItem.vue`. |

---

## 4. Inventory of direct `--p-*` references (informational)

Per decision #15 there's **no `--p-*` → `--app-*` sweep**. Keep references
as `--p-*` where the value comes from PrimeVue; add `--app-*` only for
brand tokens that have no PrimeVue equivalent. The inventory below is
kept for reference — not an action list.

| File | Reference | Notes |
|---|---|---|
| `styles/main.css` | `--p-text-color`, `--p-content-background`, `--app-background` | Semantic. Keep. |
| `styles/utilities.css` | `--p-surface-50`, `--p-surface-950` (as source of `--app-background`) | With the full surface-ramp override (decision #3), these shift to design-tuned shades automatically. |
| `components/AppHeader.vue` | `--p-content-background`, `--p-content-border-color`, `--p-primary-color`, `--p-text-color` | All semantic. Keep. |
| `components/StatePanel.vue` | `--p-red-500` | With the red primitive → berry override (decision #4), this automatically lands on warm berry. Keep. |
| `components/ShoplistEditItem.vue` | `--p-surface-500` for drag-handle color | Still a reasonable neutral-muted. Keep, unless the full surface ramp override looks off here in Phase 3. |
| `pages/index.vue` | `--p-surface-400` (chevron), `--p-content-hover-background`, `--p-content-border-color` | Keep. Chevron auto-shifts with the surface override. |
| `components/ShoplistShoppingItem.vue` | `--p-content-border-color`, `--p-content-hover-background` | Keep semantic. |
| `components/ShoplistEditItem.vue` | Same as above | Keep semantic. |

---

## 5. Icons

Kept on PrimeIcons (per user). Icons currently used — cross-checked
against the design's custom `icons.jsx`:

| PrimeIcon used | Design equivalent | Action |
|---|---|---|
| `pi-shopping-cart` | `IconBasket` | **Keep PrimeIcon** — explicit user decision. |
| `pi-check`         | `IconCheck`   | Keep. |
| `pi-plus`          | `IconPlus`    | Keep. |
| `pi-pencil`        | `IconPencil`  | Keep. |
| `pi-trash`         | `IconTrash`   | Keep. |
| `pi-ellipsis-v`    | `IconGrip` (6-dot) | **Swap** to `GripIcon.vue` — single custom SVG, decision #6. |
| `pi-chevron-right` | `IconChevron` | Keep. |
| `pi-times`         | `IconX`       | Keep. |
| `pi-arrow-left`    | `IconArrowLeft` | Keep. |
| `pi-check-circle`, `pi-exclamation-circle`, `pi-question-circle`, `pi-list` | (not in design set) | Keep. |
| `pi-sun`, `pi-moon`, `pi-desktop` | (not in design set) | Keep — color-mode toggle. |

The grip icon is the only custom SVG on the plan — the rest of the
PrimeIcons surface stays untouched.
