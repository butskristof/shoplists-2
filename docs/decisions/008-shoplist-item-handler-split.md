# ShoplistItem handler split strategy

**Status**: Decided

## Context

ShoplistItem has multiple mutable properties with different update semantics. Need to decide
between a single "update everything" endpoint and dedicated endpoints per concern.

## Decision

A hybrid approach with dedicated handlers for operations that have distinct side effects or
semantics:

- **`UpdateShoplistItem`** (`PUT`): Entity-level properties (Name, future quantity/price/unit).
  A single handler that grows as properties are added. Does **not** include `IsChecked` or
  `Position`.
- **`UpdateShoplistItemPosition`** (`PATCH /position`): Dedicated because reordering mutates
  sibling items (shifts positions) — fundamentally different side effects than a property change.
- **`UpdateShoplistItemChecked`** (`PATCH /checked`): Dedicated because checked state has distinct
  semantics — client sends desired state (`{ isChecked: bool }`) for idempotency, not a toggle.
- **`CreateShoplistItem`** and **`DeleteShoplistItem`**: Standard CRUD.

## Domain method design

Position-sensitive operations are methods on `Shoplist` (the aggregate root), centralizing position
invariant logic:

- `AddItem(name)` — assigns position (max + 1), adds to collection, returns item
- `RemoveItem(id)` — removes from collection, closes position gap
- `MoveItem(id, newPosition)` — shifts affected range, returns `false` if out of bounds

### Error handling convention

- **Item not found**: Domain methods throw `InvalidOperationException`. This is exceptional — the
  handler has already loaded items; if missing, it's a race condition or bug. Handlers find-or-404
  the item via `shoplist.Items.FirstOrDefault(...)` before calling domain methods.
- **Position out of range**: `MoveItem` returns `false`. This is a business rule validation that
  the handler translates to a user-facing error.

Simple property changes (`Name`, `IsChecked`) are done directly on tracked entity references —
no domain method needed.
