# Resource authorization via CurrentUserShoplists()

**Status**: Decided

## Context

Users must only access their own data. Need a pattern that's simple, consistent, and extensible
when list sharing is added later.

## Decision

`IAppDbContext` exposes `CurrentUserShoplists()` — an `IQueryable<Shoplist>` scoped to
`OwnerId == currentUser.UserId`. Implemented in `AppDbContext`, which injects `ICurrentUser`.

### Convention

- **Reads** (list, get, update, delete): Use `CurrentUserShoplists()` — if a user requests
  another user's shoplist, it surfaces as NotFound (no information leakage).
- **Writes** (create, remove): Use `Shoplists` DbSet directly — `Add()` and `Remove()` are not
  query operations.
- This is a convention, not enforced by the type system. Code review must verify new handlers
  follow it.

### Aggregate root scoping

Child entities (e.g., `ShoplistItem`) have no `DbSet` — they're only accessible through their
parent's navigation property. If the parent is loaded via `CurrentUserShoplists()`, items are
implicitly scoped. No additional authorization checks needed.

### Future extensibility

When list sharing is added, `CurrentUserShoplists()` expands to include shared lists. Callers
don't change — the authorization boundary moves internally.

### Testing implication

Authorization is a first-class testing concern. Integration tests must verify cross-user isolation:
User A cannot see, update, or delete User B's lists. These tests guard against regressions where
a handler accidentally uses the raw `Shoplists` DbSet.

### ICurrentUser in non-API hosts

`AppDbContext` requires `ICurrentUser` via DI. Non-API hosts (DatabaseMigrator, future background
jobs) register a dummy `ICurrentUser` that throws on access — the DI container resolves the
dependency, but user-scoped queries are never executed.
