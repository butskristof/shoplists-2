# Configure self-generated PKs as `ValueGeneratedNever`

**Status**: Decided

## Context

Aggregate roots and child entities assign their own Ids at construction (see the
"Self-generated identity" rule in `AGENTS.md`):

```csharp
public ShoplistItemId Id { get; init; } = ShoplistItemId.New();
```

This keeps entities valid immediately on construction and lets domain unit
tests distinguish instances without a database.

The EF Core entity configurations originally used `ValueGeneratedOnAdd()` on
these keys, on the assumption that EF Core would simply "respect" a pre-set
non-default Guid on insert. That assumption was wrong for one specific path:
**new child entities discovered through a navigation collection during
`DetectChanges`** — i.e. the `shoplist.AddItem(...)` → `_items.Add(item)` →
`SaveChangesAsync()` flow that handlers like `CreateShoplistItem` rely on.

EF Core's behaviour for that path is documented in the
[EF Core 3.0 breaking changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-3.x/breaking-changes#low-impact-changes):

> Starting with EF Core 3.0, if an entity is using generated key values and
> some key value is set, then the entity will be tracked in the `Modified`
> state. This means that a row for the entity is assumed to exist and it will
> be updated when `SaveChanges` is called. If the key value isn't set, or if
> the entity type isn't using generated keys, then the new entity will still
> be tracked as `Added` as in previous versions.

In other words: `ValueGeneratedOnAdd` + a pre-assigned key value tells EF
"this row already exists", so EF emits `UPDATE`. The `UPDATE` matches zero
rows, and `SaveChangesAsync` throws `DbUpdateConcurrencyException`. This is
exactly the bug that hit `CreateShoplistItem` in production — see the trace in
the corresponding incident discussion.

`dbContext.X.Add(root)` does not suffer the same fate because `Add()` is an
explicit signal that overrides the `IsKeySet` heuristic for the whole graph.
The bug only manifests on the nav-collection-discovery path.

## Decision

Configure self-generated PKs with **`ValueGeneratedNever()`**:

```csharp
builder.HasKey(x => x.Id);
builder.Property(x => x.Id).ValueGeneratedNever();
```

This is the configuration EF Core itself recommends for this scenario (see
the "Mitigations" section of the breaking-changes doc linked above). It tells
EF the truth: the application generates these Ids, not the store. With
`ValueGeneratedNever`, EF reverts to pre-3.0 behaviour for the
nav-collection-discovery path and marks new entities as `Added` regardless of
whether the Id has a value.

Applies to every aggregate-root and child-entity PK in this codebase. No
column DDL changes — `ValueGenerated` is pure EF metadata, so the model
snapshot picks up the change but no migration is needed.

## Alternatives considered

- **Explicit `dbContext.Set<T>().Add(item)` in handlers** — Works, but
  requires exposing a generic `Add<T>` (or each child `DbSet`) on
  `IAppDbContext`, softening the persistence boundary. Worse, it is a
  recurring discipline tax: every future child-entity handler must remember to
  call `Add` after the aggregate method, and forgetting reproduces the
  original `UPDATE`-instead-of-`INSERT` bug. Closing the trap structurally
  beats relying on handler authors to remember it.
- **`SaveChangesAsync` interceptor that fixes up state** — Walks the
  `ChangeTracker`, finds new-but-tracked-as-`Modified` entities, flips them to
  `Added`. Generic, but writing a correct predicate ("is this reachable from a
  tracked parent and not present in the DB?") is non-trivial and produces
  magic that future readers have to reverse-engineer. Rejected.
- **Drop the `= TypeId.New()` initializer** — Lets EF's
  default-key heuristic work again, but loses domain validity at construction,
  contradicts the "Self-generated identity" rule, and pushes Id assignment
  back into EF/Persistence — the opposite of the direction we want. Rejected.

## Trade-offs accepted

- **`Add`/`Attach`/`Update` no longer auto-detect new vs existing for these
  entities.** With `ValueGeneratedNever`, EF can no longer tell "new" from
  "existing" by inspecting the key — it relies on whether the entity is
  tracked. In a single-DbContext-per-unit-of-work flow this is fine
  (`Add` and nav-collection traversal both produce `Added`). Disconnected-graph
  flows like `dbContext.Update(detachedEntity)` would always insert; we don't
  use that pattern.
- **`StronglyTypedIdSchemaTransformer` and value converters are unaffected.**
  Only the per-entity `Property(...).ValueGeneratedNever()` line changes.
- **`IsKeySet` is now always `true`** for these entities — it is no longer
  usable as a "is this new?" predicate in this codebase. We don't reach for
  it; flagging here so it is not added later under a stale assumption.

## Prevention

Domain unit tests cannot catch this class of bug because they never reach
`SaveChangesAsync`. The first handler-level integration test that creates a
`ShoplistItem` would have failed immediately. The handler-integration-test
phase in `docs/projects/testing/plan.md` is therefore promoted from "next
phase" to "do now" so this category of bug is structurally caught going
forward.
