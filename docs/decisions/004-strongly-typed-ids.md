# Strongly-typed entity IDs via StronglyTypedId

**Status**: Decided

## Context

Raw `Guid` parameters are error-prone — `Delete(ShoplistId, ShoplistItemId)` with two Guids can
have arguments silently transposed. Strongly-typed IDs prevent this at compile time.

## Decision

Andrew Lock's [StronglyTypedId](https://github.com/andrewlock/StronglyTypedId) source generator
(1.0.0-beta08). Guid-backed by default with assembly-level defaults in
`Domain/StronglyTypedIdDefaults.cs`. Each ID type generates `IEquatable`, `IComparable`,
JSON converters, TypeConverter, and a nested `EfCoreValueConverter`.

### The Domain ↔ EF Core trade-off

The Domain project references `Microsoft.EntityFrameworkCore` (the base package, not a provider).
This is a pragmatic trade-off: the source generator produces a nested `EfCoreValueConverter` class
that needs the EF Core base types. Without this, value converters would need to live in the
Persistence project, separated from their ID types.

The alternative — hand-writing converters in Persistence — was rejected because it creates a
maintenance burden (every new ID type needs a converter in a different project) and the EF Core
base package has no infrastructure dependencies (no Npgsql, no SQL Server).

### String-backed IDs

Override assembly defaults with `[StronglyTypedId(Template.String, "string-efcore")]`.
Used for `UserId` because OIDC subject claims are opaque strings and the app uses system values
like `DATABASE_MIGRATION` for non-user contexts.

### Registration safety net

Value converters are registered explicitly per-type in `ConfigureStronglyTypedIdConversions`.
If you forget to register a new ID type, `dotnet ef migrations add` fails with a clear error —
you cannot silently miss it.
