# Dedicated DatabaseMigrator worker for EF Core migrations

**Status**: Decided

## Context

EF Core migrations need to be applied before the API starts serving requests. The common approach
of calling `MigrateAsync()` on API startup is an anti-pattern.

## Decision

A dedicated Worker Service (`DatabaseMigrator`) applies migrations, then exits. Aspire orchestrates
the startup sequence: Postgres → DatabaseMigrator (`WaitFor`) → API (`WaitForCompletion`).

## Why not migrate on API startup

1. **Race conditions**: Multiple API instances starting simultaneously can conflict on migration
   application, even with EF Core's distributed locking.
2. **Mixed concerns**: Deployment-time operations (schema changes) shouldn't be coupled to
   runtime operations (serving requests). Different permission levels, different failure modes.
3. **Elevated permissions**: Migrations require DDL permissions. The API should run with minimal
   DB permissions (DML only) in production.
4. **CD compatibility**: A standalone migrator container can run as a pre-deployment step in any
   CD pipeline — not tied to the API's lifecycle.

## Implementation notes

- The migrator host is a thin shell; actual logic lives in `DatabaseMigrationRunner` in the
  Persistence project (keeping `AppDbContext` internal).
- Postgres uses `WithDataVolume()` for data persistence across container recreations. No
  `WithLifetime(ContainerLifetime.Persistent)` — the container stops with the AppHost.
- Since EF Core 9, `MigrateAsync()` is fully self-contained (creates DB, acquires distributed
  lock, manages transactions). Do NOT wrap in `ExecuteAsync()` or explicit transactions.
  Do NOT call `EnsureCreatedAsync()` before it.
- `DesignTimeDbContextFactory` provides a dummy connection string for `dotnet ef` CLI — it never
  connects to a real database when scaffolding migrations.
