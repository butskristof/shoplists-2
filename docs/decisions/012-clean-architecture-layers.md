# Clean architecture layer boundaries and project placement

**Status**: Decided

## Context

The backend uses Clean Architecture with five projects (Domain, Application, Infrastructure,
Persistence, Api) plus AppHost and ServiceDefaults. Several of the placement and separation
choices are non-obvious.

## Layer responsibilities

| Layer | Responsibility | Constraint |
|---|---|---|
| **Domain** | Entities, value objects, domain logic | No dependencies beyond what the domain model needs (currently EF Core base package for source-generated value converters — see ADR 004) |
| **Application** | Use cases, mediator handlers, validators, pipelines | Depends only on Domain. Defines interfaces (`IAppDbContext`, `ICurrentUser`) for infrastructure concerns |
| **Infrastructure** | External service integrations (TimeProvider, future API clients, file storage) | **Does NOT contain EF Core / database concerns** — those live in Persistence |
| **Persistence** | EF Core `AppDbContext`, entity configurations, migrations, `DatabaseMigrationRunner` | `AppDbContext` is **`internal`** — external code accesses migration functionality through `DatabaseMigrationRunner` |
| **Api** | Minimal API host, endpoint mapping, JWT validation, ProblemDetails mapping, OpenAPI | Implements HTTP-context-dependent interfaces (e.g., `ICurrentUser` reading from `HttpContext`) |
| **DatabaseMigrator** | Worker that applies migrations and exits | Thin shell — actual logic lives in `DatabaseMigrationRunner` in Persistence |

## Why Persistence is separated from Infrastructure

Two reasons:

1. **Package organization** — keeping EF Core dependencies isolated to one project clarifies
   what each project actually uses.
2. **Avoid bloating DatabaseMigrator** — DatabaseMigrator references Persistence (for the
   migration runner) but should not transitively pick up unrelated infrastructure dependencies
   (TimeProvider, file storage, etc.).

## Why `AppDbContext` is internal

External code should not directly access the DbContext — it goes through `IAppDbContext` (defined
in Application) for normal queries, and through `DatabaseMigrationRunner` for migration ops. This
keeps the public API surface of the Persistence project minimal.

`DesignTimeDbContextFactory` (also in Persistence) provides a dummy connection string so
`dotnet ef` CLI tooling can scaffold migrations without a real database.

## AppHost placement at `backend/` root

AppHost lives at the solution root rather than under `3-hosts/` because it orchestrates the
**entire stack** including the frontend and infrastructure services (Postgres, Valkey) — not just
.NET hosts. Placing it under `3-hosts/` would suggest it's part of the backend layered
architecture, which it isn't.

## ServiceDefaults under `2-infrastructure/`

ServiceDefaults provides shared Aspire service config (OpenTelemetry, health checks, resilience).
It's consumed only by .NET backend projects (Api, DatabaseMigrator), not the frontend or
infrastructure containers — hence its placement under `2-infrastructure/` rather than at the root.
