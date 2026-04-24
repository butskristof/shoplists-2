# Visual identity — source material

Raw source artefacts behind the design direction. The project docs one level up
(`plan.md`, `design-tokens.md`, `primevue-theming.md`, `component-audit.md`,
`pwa-and-shell.md`) derive from these. Treat this folder as read-only reference;
update the docs, not the bundle.

## `design-bundle/`

Handoff bundle from Claude Design (claude.ai/design), exported 2026-04-20.
The Claude Design handoff comes with its own README (`design-bundle/README.md`)
aimed at coding agents — worth reading before diving into the `.html`/`.jsx`
files.

- `design-bundle/README.md` — original handoff instructions
- `design-bundle/chats/chat1.md` — the conversation that led to the chosen
  direction ("Market"), the decision to drop the beige background, and the
  derived dark-mode palette. The *intent* lives here, not in the HTML.
- `design-bundle/project/Shoplists Design System.html` — entry HTML that
  assembles the React/Babel prototype
- `design-bundle/project/src/tokens.jsx` — **canonical token definitions**
  (Market light + Market dark). This is the source of truth for colours,
  radii, shadows, typography stack.
- `design-bundle/project/src/screens.jsx` — Home / Detail / Edit screen
  prototypes. Demonstrates composition patterns (ListCard with progress ring,
  ItemRow, EditRow, SectionLabel eyebrow).
- `design-bundle/project/src/system-card.jsx` — design-system display card:
  typography scale with specs, palette swatches, button/check samples,
  shape & motion tokens.
- `design-bundle/project/src/icons.jsx` — 24×24 stroke icon set drawn for the
  design. **Not adopted** — we stay on PrimeIcons for now (see `plan.md`).
- `design-bundle/project/src/logos.jsx` — Pantry (rejected) and Market
  (chosen) logomarks. Also **not adopted** — we keep the PrimeIcons shopping
  cart for now.
- `design-bundle/project/frames/` — iOS frame + canvas helpers used only to
  render the prototype. Not relevant for implementation.
- `design-bundle/project/uploads/` — original screenshots of the current
  (pre-redesign) app the user sent in to kick off the exercise. Useful as
  a "before" reference when reviewing progress.

## What to read first

1. `design-bundle/chats/chat1.md` — understand the intent and the chosen
   direction (Market, no mono font, lighter background than the original
   proposal, derived dark mode).
2. `design-bundle/project/src/tokens.jsx` — the raw palette/shape/typography
   numbers that every doc in this project references.
3. Then jump back up to `../plan.md` for the phased roadmap.
