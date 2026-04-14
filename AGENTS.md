# CLAUDE.md

This file is the primary source of project-specific instructions for AI agents working on this codebase.
It should be kept up-to-date as architectural and tooling decisions are made.

> **Self-updating rule**: When a TBD item is decided during a session, propose updating the relevant
> section of this file before considering the task complete.

---

## Project Overview

Shoplists is a web application for grocery shopping. The MVP is deliberately simple: users own lists
of items that can be ticked on/off. More advanced features (sharing, purchase tracking, price analysis,
suggestions) will follow once the basics are solid and the app gets real usage.

See `README.md` for feature ideas.

---

## Tech Stack

### Frontend
- **Framework**: Nuxt 4 (Vue 3, TypeScript)
- **Rendering**: Hybrid — SSR on initial load, hydrating to SPA
- **BFF**: Nitro catch-all route at `server/api/[...path].ts` proxies `/api/*` to the backend API.
  See *Frontend BFF & API Client Conventions* below for the security contract.
- **Authentication**: `nuxt-oidc-auth` handles the OIDC flow. Access tokens are stored encrypted in
  Valkey server-side only (`exposeAccessToken: false`, `exposeIdToken: false`) — they never reach
  the browser.
- **Language**: TypeScript with strict type safety as a quality goal
- **Node**: Latest LTS (currently v24)
- **Package manager**: npm
- **CSS**: Native CSS (with nesting). **No Tailwind — this is non-negotiable.**
  - Scoped `<style scoped>` in SFC components for all layout/styling
  - PrimeVue design tokens (CSS variables like `var(--p-surface-0)`) for all colors — they auto-switch
    between light/dark mode
  - Breakpoints: mobile < 1024px, desktop >= 1024px (`@media (min-width: 1024px)`)
- **Icons**: PrimeIcons (via `primeicons` npm package, CSS imported in nuxt config)
- **UI library**: PrimeVue v4 (Aura preset, styled mode with design tokens / CSS variables)
- **API client generation**: `openapi-fetch` with TypeScript types generated from the backend's
  OpenAPI spec via `npm run generate:api` (`openapi-typescript`) into `app/generated/api.d.ts`.
  Constructed via the `useApi()` composable in `app/composables/useApi.ts`, which returns an
  `openapi-fetch` client wired correctly for the current rendering context (SSR vs client). All
  requests target the BFF proxy at `/api`. See *Frontend BFF & API Client Conventions* below.
- **Testing**: Use Playwright via MCP tooling during implementation to functionally and visually verify
  changes. No Playwright test code in the repo — testing is done interactively by the agent.

### Backend
- **Runtime**: .NET 10
- **API style**: Minimal APIs
- **API documentation**: OpenAPI via `Microsoft.AspNetCore.OpenApi` (built-in .NET 10), Scalar for docs UI
- **ORM**: Entity Framework Core
- **Database**: PostgreSQL
- **Architecture**: Clean architecture with feature-organized application layer (see Backend
  Architecture & Conventions below)
- **Mediator**: [Mediator](https://github.com/martinothamar/Mediator) (martinothamar) — MIT-licensed,
  source-generated, MediatR-compatible API shape. Chosen over MediatR (license concerns), Wolverine
  (overkill for in-process mediation), and Immediate.Handlers (too immature).
- **Result/error pattern**: [ErrorOr](https://github.com/amantinband/error-or) — clean ergonomics,
  entry-point-agnostic error types (NotFound, Validation, Conflict, etc.), no HTTP coupling in the
  Application layer. Mapping to HTTP status codes / ProblemDetails happens at the API boundary.
- **Validation**: [FluentValidation](https://docs.fluentvalidation.net/) — used for input/format
  validation (shape of data: non-null, non-negative, valid format) via a mediator pipeline behavior.
  Business rule validation (uniqueness, authorization, state transitions) happens in handlers where
  DB access is available.
- **Analyzers**: Meziantou.Analyzer (installed via `Directory.Build.props`) — provides compile-time
  enforcement including primary constructor parameter reassignment prevention (MA0143). Combined with
  `TreatWarningsAsErrors`, analyzer violations fail the build.
- **Testing**: Unit and integration tests, strict coverage expected. Framework TBD (likely TUnit).
  Exact setup to be decided in a dedicated session.
- **Solution structure**: Clean architecture with numbered folder groupings for dependency direction
  clarity. See Project Structure below for the full layout.

### Infrastructure & Local Development
- **Orchestration**: Aspire (AppHost) manages the full local stack — backend services, frontend
  dev server, PostgreSQL, and any other dependencies. 
- **To run locally**: Run `aspire run` from the repo root. It spins up everything.
  Aspire assigns ports dynamically — use the Aspire MCP to discover resource URLs.
- **Authentication**: OpenID Connect to an external provider (Pocket ID or Entra External ID).
  Auth is NOT a concern of this application — we plug in via standard OIDC on both frontend and backend.
- **CI/CD**: GitHub Actions (TBD). PR validations will enforce build, test, and lint checks.
  CD will build container images and push to a registry. Deployment is out of scope for now.

### MCP Tooling

The following MCP servers are configured in `.mcp.json` and available to agents:

| Server | Purpose | When to use |
|---|---|---|
| **nuxt** | Nuxt documentation, modules, deploy providers, changelog | Nuxt 4 API questions, module discovery, deployment config |
| **mslearn** | Microsoft Learn docs search, fetch, and code samples | .NET, EF Core, Aspire, and any Microsoft technology |
| **aspire** | Inspect running Aspire resources, logs, and traces | Local dev diagnostics — check resource status, discover dynamically assigned URLs, view logs, inspect traces |
| **playwright** | Full browser automation (navigate, click, fill, screenshot, snapshot) | UI verification during and after implementation — no Playwright test code in repo |
| **primevue** | PrimeVue v4 component docs, props, events, slots, theming, accessibility | PrimeVue component APIs, design tokens, passthrough, theming, usage examples |
| **context7** | Up-to-date documentation and code examples for any library | General-purpose doc lookup for libraries not covered by dedicated MCP servers |

**Tool selection guidance for documentation lookups:**
- Nuxt 4 / Vue / Nitro → **nuxt** MCP (primary), **context7** (supplementary)
- .NET / EF Core / Aspire / Minimal APIs → **mslearn** MCP (primary), **context7** (supplementary)
- PrimeVue v4 (components, theming, design tokens) → **primevue** MCP (primary), **context7** (supplementary)
- Other libraries (test frameworks, mediator libs, etc.) → **context7**
- Always prefer a dedicated MCP tool over web search when one exists for the technology.

---

## Project Structure

```
repo root/
  backend/
    Shoplists.slnx                   -> .NET solution (references all backend + Aspire projects)
    AppHost/                         -> Aspire AppHost (orchestrates full stack: backend, frontend, Postgres)
    Directory.Build.props            -> Shared build config (TFM, nullable, analyzers, TreatWarningsAsErrors)
    src/
      1-core/
        Shoplists.Domain/            -> Entities, value objects, domain logic (no external dependencies)
        Shoplists.Application/       -> Use cases, mediator handlers, validation, cross-cutting concerns
      2-infrastructure/
        Shoplists.Infrastructure/    -> External service integrations (TimeProvider, API clients, file storage)
        Shoplists.Persistence/       -> EF Core DbContext, entity configurations, repositories
        ServiceDefaults/             -> Shared Aspire service configuration (OpenTelemetry, health checks, resilience)
      3-hosts/
        Shoplists.Api/               -> ASP.NET Core Minimal API host, endpoint mapping, auth middleware
        Shoplists.DatabaseMigrator/  -> Worker service that applies EF Core migrations, then exits
    tests/
      Shoplists.Domain.UnitTests/
      Shoplists.Application.UnitTests/
      Shoplists.Application.IntegrationTests/
      Shoplists.Tests.Shared/        -> Shared test infrastructure (builders, fixtures, helpers)
  frontend/
    ...                              -> Nuxt application
```

**Dependency direction** (numbered folders visualize this):
- **1. Core** depends on nothing (only framework/language features and chosen libraries like Mediator, ErrorOr, FluentValidation, StronglyTypedId, and EF Core base package for generated value converters)
- **2. Infrastructure** depends on 1. Core (implements interfaces defined in Application/Domain)
- **3. Hosts** depends on 1. Core and 2. Infrastructure (wires everything together via DI)
- AppHost depends on Hosts projects (to orchestrate them) but is not part of the layered architecture

**Aspire placement**: AppHost lives at the solution root (`backend/`) because it orchestrates the
entire stack including frontend and infrastructure services. ServiceDefaults lives under
`backend/src/2-infrastructure/` as it's consumed only by .NET backend projects.

---

## Backend Architecture & Conventions

### Clean Architecture Layers

- **Domain**: Entities, value objects, enums, domain logic. No dependencies on external libraries
  beyond what's needed for the domain model itself. No infrastructure concerns.
- **Application**: Use cases organized by feature. Depends on Domain. Defines interfaces for
  infrastructure concerns (e.g., `IAppDbContext`, `ICurrentUser`). Contains mediator handlers,
  FluentValidation validators, and pipeline behaviors.
- **Infrastructure**: Implements interfaces defined in Application for external service integrations
  (TimeProvider, future API clients, file storage, etc.). Does NOT contain EF Core / database concerns.
- **Persistence**: EF Core DbContext, entity configurations, migration files, and the
  `DatabaseMigrationRunner` entry point. `AppDbContext` is `internal` — external code accesses
  migration functionality through `DatabaseMigrationRunner`. Also contains
  `DesignTimeDbContextFactory` for `dotnet ef` CLI tooling. Separated from Infrastructure to keep
  packages organized and to avoid the DatabaseMigrator host carrying unrelated dependencies.
- **Api**: ASP.NET Core Minimal API host. Maps HTTP endpoints to mediator requests. Handles
  authentication (JWT validation), authorization, and HTTP-specific concerns (ProblemDetails mapping,
  OpenAPI documentation). Implements infrastructure interfaces that depend on HTTP context (e.g.,
  `ICurrentUser` reading from `HttpContext`).
- **DatabaseMigrator**: Worker Service host that applies EF Core migrations, then exits. Orchestrated
  by Aspire: starts after Postgres is healthy (`WaitFor`), and the API blocks on its completion
  (`WaitForCompletion`). If it fails, the API never starts — the failure is visible in the Aspire
  dashboard. The host is a thin shell; actual migration logic lives in `DatabaseMigrationRunner` in
  Persistence (keeping `AppDbContext` internal). Also suitable for CD pipelines (run migrator
  container before deploying API). Running migrations on API startup is an anti-pattern.

### Application Layer Organization

```
Application/
  Common/                          -> Cross-cutting concerns shared by all features
    Authentication/                -> ICurrentUser (interface; implemented in Api project)
    Configuration/                 -> Settings DTOs (bound from appsettings.json by host)
    Persistence/                   -> IAppDbContext (interface; implemented in Persistence project)
    Pipeline/                      -> Mediator pipeline behaviors (logging, validation)
    Validation/                    -> FluentValidation base classes, extensions, error codes
  Features/                        -> Use cases organized by domain concept
    Shoplists/
      CreateShoplist.cs            -> Static class containing Request, Validator, Handler
      GetShoplist.cs
      GetShoplists.cs
      ...
      Items/                       -> Child entity handlers nested under aggregate root
        CreateShoplistItem.cs
        DeleteShoplistItem.cs
        UpdateShoplistItem.cs
        UpdateShoplistItemPosition.cs
        UpdateShoplistItemChecked.cs
```

### Handler File Convention

Each use case lives in a single file as a static class with nested types. This keeps related code
together and scopes type names to avoid collisions (e.g., `CreateList.Request` vs `GetList.Request`).

```csharp
public static class CreateList
{
    // Request: sealed record with positional (primary constructor) syntax.
    // Implements ICommand<ErrorOr<T>> or IQuery<ErrorOr<T>>.
    // Properties are nullable with FluentValidation enforcing non-null — this ensures uniform
    // ValidationProblemDetails responses and keeps OpenAPI contract control in our hands.
    public sealed record Request(string? Name) : ICommand<ErrorOr<Guid>>;

    // Validator: internal, inherits BaseValidator<Request>
    // Only validates input shape (non-null, format, range) — NOT business rules
    internal sealed class Validator : BaseValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Name).NotNullOrEmptyWithErrorCode();
        }
    }

    // Handler: internal, uses primary constructors for DI
    // Business logic and data access happen here
    internal sealed class Handler(
        ILogger<Handler> logger,
        IAppDbContext dbContext
    ) : ICommandHandler<Request, ErrorOr<Guid>>
    {
        public async ValueTask<ErrorOr<Guid>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // Implementation
        }
    }
}
```

**Key conventions:**
- **Primary constructors** for dependency injection (enforced readonly via Meziantou.Analyzer MA0143)
- **Positional record syntax** for Request types (e.g., `record Request(string? Name) : ICommand<...>;`).
  Properties are nullable with FluentValidation `NotNull` rules — ensures all validation errors flow
  through the same pipeline and produce uniform `ValidationProblemDetails` responses. Trade-off:
  OpenAPI schema generates nullable types, so generated TypeScript clients have nullable fields.
  Accepted for now; may integrate FluentValidation rules into OpenAPI schema generation later.
- **`internal` visibility** for Validator and Handler — only the Request (and Response if applicable)
  need to be public for the API layer to reference them.
- **ErrorOr<T>** as the return type for all handlers — provides a consistent application layer
  contract regardless of entry point (API, background job, message consumer).
- **Split into folder** only when a handler file grows very large (100+ lines in the handler method).
  For typical CRUD, single file is preferred.

### Child Entity Handler Organization

Child entity handlers (e.g., ShoplistItem) live in a subfolder under their aggregate root's feature
folder: `Features/Shoplists/Items/`. The namespace mirrors the folder structure
(`Application.Features.Shoplists.Items`). This keeps child entity use cases grouped with their
aggregate root rather than promoted to a sibling feature folder.

**Handler split strategy for ShoplistItem** — a hybrid approach:
- **`UpdateShoplistItem`** (`PUT`): entity-level properties (Name, future quantity/price/unit). A
  single handler that grows as properties are added. Does not include `IsChecked` or `Position`.
- **`UpdateShoplistItemPosition`** (`PATCH /position`): dedicated handler because reordering mutates
  sibling items (shifts positions) — different side effects than a simple property change.
- **`UpdateShoplistItemChecked`** (`PATCH /checked`): dedicated handler because checked state has
  distinct semantics (client sends desired state for idempotency, not a toggle). Request body is
  `{ isChecked: bool }`.
- **`CreateShoplistItem`** (`POST`) and **`DeleteShoplistItem`** (`DELETE`): standard CRUD.

**Aggregate root methods for collection & position management**: Operations that mutate the `_items`
private collection or coordinate positions across items are methods on `Shoplist` (the aggregate
root), not inline in handlers. This centralizes position invariant logic:
- `ShoplistItem AddItem(string name)` — assigns position (max + 1), adds to collection, returns item
- `void RemoveItem(ShoplistItemId)` — removes from collection, closes position gap. Throws
  `InvalidOperationException` if item not found (caller should validate first).
- `bool MoveItem(ShoplistItemId, int newPosition)` — shifts affected range. Returns `false` if
  position is out of bounds (handler translates to validation error). Throws
  `InvalidOperationException` if item not found (caller should validate first).

**Error handling convention for domain methods**: Item-not-found is an exceptional situation (the
handler has already loaded the shoplist with items — if the item isn't there, it's a race condition
or programming error), so domain methods throw. Position-out-of-range is a business rule validation
that the handler translates to a user-facing error, so it returns `false`. All handlers find-or-404
the item via `shoplist.Items.FirstOrDefault(...)` before calling domain methods.

Simple property changes (`Name`, `IsChecked`) are done directly on item references obtained via
`shoplist.Items.FirstOrDefault(...)` — the returned objects are EF Core tracked entities, so
property mutations are detected by `SaveChangesAsync`.

**Authorization**: All item handlers load the parent shoplist via
`CurrentUserShoplists().Include(s => s.Items)`. If the shoplist doesn't belong to the current user,
it surfaces as NotFound. Items are implicitly scoped through the aggregate root.

### Logging Philosophy

- Pipeline behaviors handle cross-cutting logging (handler entry/exit, timing, validation failures).
- OpenTelemetry + Aspire provide trace and metric data automatically.
- In handlers, log **state and decisions** (entity IDs, which branch was taken, counts), not actions
  the code already describes ("Mapped request to entity"). Debug/Trace level is fine for granular
  operational insight — these are filtered out in production by default and available when
  investigating issues.

### Async Conventions

- **Do not use `ConfigureAwait(false)`**. All backend projects run on ASP.NET Core or the generic
  host, neither of which has a `SynchronizationContext`. `ConfigureAwait(false)` is a no-op in these
  environments. The Meziantou.Analyzer rule MA0004 is suppressed globally in `.editorconfig`.

### Error Handling at the API Boundary

- Handlers return `ErrorOr<T>` — the Application layer never throws exceptions for expected failures.
- The API layer maps `ErrorOr` error types to HTTP responses:
  - `ErrorType.Validation` → 400 + `ValidationProblemDetails`
  - `ErrorType.NotFound` → 404 + `ProblemDetails`
  - `ErrorType.Conflict` → 409 + `ProblemDetails`
  - `ErrorType.Unauthorized` → 403 + `ProblemDetails`
  - etc.
- Unexpected exceptions are caught by `UseExceptionHandler()` middleware and mapped to 500 +
  `ProblemDetails`. `UseStatusCodePages()` covers additional failed requests (404, 405, etc.) that
  don't throw exceptions but have empty bodies. `AddProblemDetails()` is registered in DI to enable
  automatic `ProblemDetails` formatting for both middlewares.
- Mapping is implemented via `ToHttpResult()` extension methods in `Api/Extensions/ErrorOrExtensions.cs`:
  - Extension on `ValueTask<ErrorOr<T>>` enables `await sender.Send(req).ToHttpResult()` (no awkward
    parenthesizing the await).
  - Sync `onSuccess` overload (`Func<T, IResult>?`) for simple mappings (Created, NoContent).
  - Async `onSuccess` overload (`Func<T, Task<IResult>>`) for success paths that need async work.
  - Default success mapping: `TypedResults.Ok(value)`. Override via `onSuccess` parameter for 201
    Created (`value => TypedResults.Created(...)`) or 204 NoContent (`_ => TypedResults.NoContent()`).
  - Multiple validation errors are grouped by `Error.Code` into `ValidationProblemDetails`.
  - Non-validation errors use the first error to determine status code.

### Minimal API Endpoint Conventions

Endpoints are organized in `Api/Endpoints/`, one static class per feature. Each class defines an
extension method on `IEndpointRouteBuilder` that creates a `MapGroup()` with the feature's sub-path
and tags, then maps individual endpoints within that group.

**Structure:**
```
Api/
  Authentication/
    AuthenticationSettings.cs      -> Settings record (Authority, Audience) bound from config
    HttpContextCurrentUser.cs      -> ICurrentUser impl, reads "sub" claim from HttpContext
  Endpoints/
    ShoplistEndpoints.cs           -> MapGroup("/shoplists"), maps GET/POST/PUT/DELETE
    ShoplistItemEndpoints.cs       -> MapGroup("/shoplists/{listId}/items"), maps item CRUD + position/checked
  Extensions/
    EndpointRouteBuilderExtensions.cs -> MapShoplistsApi() — top-level /api prefix group
    ErrorOrExtensions.cs           -> ToHttpResult() extensions for ErrorOr → IResult mapping
  OpenApi/
    BearerSecuritySchemeTransformer.cs -> Adds Bearer security scheme to OpenAPI doc
    StronglyTypedIdSchemaTransformer.cs -> Makes strongly-typed IDs appear as string/uuid in OpenAPI
```

**Top-level wiring**: `Program.cs` calls `app.MapShoplistsApi()`, which creates a `/api` prefix
group and delegates to each feature's `Map*Endpoints()` method. The `/api` prefix is applied once
here; feature mappers only specify their own sub-path (e.g., `/shoplists`).

**Endpoint pattern** — endpoints are thin dispatchers:
```csharp
private static Task<IResult> GetShoplists(ISender sender) =>
    sender.Send(new GetShoplists.Request()).ToHttpResult();
```

**OpenAPI metadata**: `.WithTags()` on the group, `.WithName()` and `.Produces<T>()` per endpoint.
Scalar serves the docs UI at `/scalar/v1` (dev-only). The OpenAPI spec is at `/openapi/v1.json`.

**OpenAPI schema transformers**: `StronglyTypedIdSchemaTransformer` rewrites strongly-typed ID
schemas. When adding a new ID type, also add it to the transformer's type mapping dictionary.

### Backend Authentication & Authorization

**Approach**: JWT Bearer token validation. The API validates access tokens issued by an external OIDC
provider. The frontend (Nuxt BFF) handles the OIDC flow, stores tokens server-side in Valkey, and
attaches the access token as a `Bearer` header when proxying API calls.

**JWT Bearer registration** (`Api/Extensions/DependencyInjection.cs`):
- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` with Authority and
  Audience from `AuthenticationSettings`.
- **`MapInboundClaims = false`** — preserves OIDC-standard claim types (`"sub"`, `"name"`, etc.).
  Without this, ASP.NET Core remaps them to long XML namespace URIs. This means claim lookups use
  `"sub"` directly, not `ClaimTypes.NameIdentifier`.

**Configuration** (`Api/Authentication/AuthenticationSettings.cs`):
- `Authority`: OIDC issuer base URL. The JWT handler auto-discovers signing keys at
  `{Authority}/.well-known/openid-configuration`.
- `Audience`: Expected `aud` claim in access tokens.
- Values come from Aspire parameters (`oidc-authority`, `oidc-audience`) injected as environment
  variables (`Authentication__Authority`, `Authentication__Audience`).

**ICurrentUser implementation** (`Api/Authentication/HttpContextCurrentUser.cs`):
- Reads the `"sub"` claim from `HttpContext.User` via `IHttpContextAccessor`.
- Wraps the value in `UserId` (string-backed strongly-typed ID).
- Throws `InvalidOperationException` if no `sub` claim is present — this is a configuration error
  since all API endpoints require authentication.

**Authorization strategy**: `.RequireAuthorization()` on the `/api` route group in
`EndpointRouteBuilderExtensions.MapShoplistsApi()`. This protects all API endpoints while leaving
OpenAPI, Scalar, and health check endpoints unaffected (they live outside `/api`). We chose this
over `FallbackPolicy` because all real endpoints are already centralized under `/api`.

**Resource authorization — `CurrentUserShoplists()`**: Resource-level authorization (ensuring users
can only access their own data) is implemented via a scoped query method on `IAppDbContext`:
- `IQueryable<Shoplist> CurrentUserShoplists()` — returns only shoplists where
  `OwnerId == currentUser.UserId`. Implemented in `AppDbContext`, which injects `ICurrentUser`.
- All **read** operations on shoplists (list, get, update, delete) use `CurrentUserShoplists()`
  instead of the raw `DbSet`. If a user requests another user's shoplist, it naturally surfaces as
  `NotFound` — no information leakage about other users' data.
- **Write** operations (create, remove) use the `Shoplists` DbSet directly — `Add()` and `Remove()`
  are not query operations.
- **Convention**: reads go through `CurrentUserShoplists()`, writes go through `Shoplists` DbSet.
  This is a convention, not enforced by the type system. Code review should verify new handlers
  follow this pattern.
- **Aggregate root scoping**: Child entities (e.g., `ShoplistItem`) have no `DbSet` and are only
  accessible through their parent's navigation property. If the parent Shoplist is scoped by
  `CurrentUserShoplists()`, items are implicitly scoped too. No additional authorization checks
  needed for child entities accessed through the aggregate root.
- **Sharing (future)**: When list sharing is added, the `CurrentUserShoplists()` implementation
  expands to include shared lists. Callers don't change.
- **Testing**: Authorization is a first-class testing concern. Integration tests must verify
  cross-user isolation: User A cannot see, update, or delete User B's lists; `GetShoplists` returns
  only the current user's data. These tests guard against regressions where a handler accidentally
  uses the raw `Shoplists` DbSet instead of `CurrentUserShoplists()`.

**`ICurrentUser` in non-API hosts**: `AppDbContext` requires `ICurrentUser` via constructor
injection. Hosts that don't have an HTTP context (DatabaseMigrator, future background jobs) must
register a dummy `ICurrentUser` that throws on access — these hosts never execute user-scoped
queries, but the DI container still needs to resolve the dependency. See `MigrationCurrentUser`
in the DatabaseMigrator host and `DesignTimeCurrentUser` in `DesignTimeDbContextFactory`.

**OpenAPI security** (`Api/OpenApi/BearerSecuritySchemeTransformer.cs`):
- `IOpenApiDocumentTransformer` that declares a Bearer security scheme in the OpenAPI document
  components and sets it as a document-level security requirement.
- Document-level security means "all operations require this by default" — the standard OpenAPI
  pattern. Individual operations can override with an empty security array if needed.
- Makes Scalar show the Bearer token input for authenticated testing.

### Strongly-Typed Entity IDs

Every entity uses a strongly-typed ID instead of raw `Guid` to prevent accidental argument
transposition and improve code clarity. IDs are generated using Andrew Lock's
[StronglyTypedId](https://github.com/andrewlock/StronglyTypedId) source generator (1.0.0-beta08).

**Package setup (Domain project):**
- `StronglyTypedId` + `StronglyTypedId.Templates` — both with `PrivateAssets="all" ExcludeAssets="runtime"`
  (compile-time source generator only, no runtime footprint)
- `Microsoft.EntityFrameworkCore` — needed for the generated `EfCoreValueConverter` nested class.
  Pragmatic trade-off: Domain depends on the EF Core base package (not a provider) so that source-generated
  value converters live next to their ID types.

**Assembly-level defaults** (`Domain/StronglyTypedIdDefaults.cs`):
```csharp
[assembly: StronglyTypedIdDefaults(Template.Guid, "guid-efcore")]
```
All ID types get: `IEquatable<T>`, `IComparable<T>`, `IFormattable`, `IParsable<T>`,
`System.Text.Json.JsonConverter`, `System.ComponentModel.TypeConverter`, and a nested
`EfCoreValueConverter` class.

**Defining a new ID type** — ID types get their own file in the same folder as their entity
(`Domain/Models/<Feature>/<Entity>Id.cs` + `Domain/Models/<Feature>/<Entity>.cs`):
```csharp
// ShoplistId.cs — Guid-backed (uses assembly defaults)
[StronglyTypedId]
public partial struct ShoplistId;

// Shoplist.cs
public class Shoplist
{
    public ShoplistId Id { get; init; }
}
```

**Non-Guid ID types** — override the assembly defaults by specifying template and EF Core converter
explicitly. Example: `UserId` is string-backed because OIDC subject claims are opaque strings and
the app may use system values (`DATABASE_MIGRATION`, `SYSTEM`, etc.):
```csharp
[StronglyTypedId(Template.String, "string-efcore")]
public partial struct UserId;
```

**EF Core registration** — explicit per-type in the `ConfigureStronglyTypedIdConversions` extension
method (`Persistence/Extensions/ModelConfigurationBuilderExtensions.cs`), which is called from
`AppDbContext.ConfigureConventions`:
```csharp
configurationBuilder.Properties<ShoplistId>()
    .HaveConversion<ShoplistId.EfCoreValueConverter>();
```
If you forget to register a new ID type, `dotnet ef migrations add` fails with a clear error — you
cannot silently miss it.

**Entity configuration** — each entity gets an `IEntityTypeConfiguration<T>` class in
`Persistence/EntityConfigurations/`.

**Checklist for adding a new entity with a strongly-typed ID:**
1. Define the ID struct and entity class in `Domain/Models/<Feature>/<Entity>.cs`
2. Register the ID's value converter in `ConfigureStronglyTypedIdConversions` extension method
3. Add the `DbSet<Entity>` to `AppDbContext` (only for aggregate roots — child entities are accessed
   through their parent's navigation property)
4. Create `<Entity>Configuration` in `Persistence/EntityConfigurations/`
5. Generate and apply the migration

### Domain Entity Conventions

**Naming**: Entity names align with the app's domain language. The app is "Shoplist", so entities
use `Shoplist`, `ShoplistItem` — not `ShoppingList`. Keep names concise and domain-specific.

**Property accessors**:
- `{ get; init; }` — **default for most properties**, including entity IDs. EF Core materializes
  entities through backing fields, so `init` works fine for hydration. Use `init` for all properties
  unless mutation is an explicit, known use case. This includes `Id` (assigned once by
  `ValueGeneratedOnAdd`), foreign keys (e.g., `ShoplistId` on `ShoplistItem`), and ownership
  properties (e.g., `OwnerId`).
- `{ get; set; }` — use only for properties that are genuinely mutable after creation (e.g., `Name`
  which can be updated, `IsChecked` which toggles). Only "open up" from `init` to `set` when
  mutation is an actual use case.
- **`required`** — use on all properties that must be explicitly set at construction time. This
  includes `Name`, foreign keys, and any value where a silent default would be a bug (e.g.,
  `Position` with 1-based ordering — defaulting to 0 would be wrong). Exclude `Id` (handled by
  `ValueGeneratedOnAdd`) and properties with legitimate defaults (`IsChecked = false`).

**Collection navigation properties** — use a private backing field with a public read-only accessor:
```csharp
private readonly List<ShoplistItem> _items = [];
public IReadOnlyList<ShoplistItem> Items => _items.AsReadOnly();
```
EF Core discovers the `_items` backing field by convention. External code gets read-only access;
mutations go through the aggregate root. Child entities don't get a `DbSet` — they're accessed only
through their parent.

### EF Core Entity Configuration Conventions

Entity configurations should be **explicit over implicit** — don't rely on EF Core conventions when
the intent can be stated directly in the configuration. This makes the database schema inspectable
from the configuration code alone.

- **`HasKey`** — always explicit, even though EF Core would find `Id` by convention.
- **`ValueGeneratedOnAdd`** — always explicit on ID properties. Works as a fallback; if the
  application sets the ID before saving, EF uses that value instead.
- **`IsRequired`** — always explicit on required string properties, even when the C# type is
  non-nullable. Reinforces intent at the persistence layer.
- **Max lengths** — handled globally by `ConfigureConventions` (`DefaultMaxStringLength = 512`).
  Only configure per-property if a column needs a different limit.
- **Indexes** — add explicitly for non-FK properties used in common query patterns (e.g.,
  `OwnerId` for "all lists by user"). EF Core auto-creates indexes for FK columns in configured
  relationships.
- **`IsImmutableAfterInsert()`** — custom `PropertyBuilder<T>` extension
  (`Persistence/Extensions/PropertyBuilderExtensions.cs`) that sets
  `AfterSaveBehavior(PropertySaveBehavior.Throw)`. Apply to non-key properties that must never
  change after the initial insert (e.g., `OwnerId`, foreign keys to parent aggregates like
  `ShoplistId` on `ShoplistItem`). Do NOT apply to primary key properties — EF Core already
  protects those via `ValueGeneratedOnAdd` conventions.

### Database Migrations

**Architecture**: Migrations are applied by a dedicated Worker Service (`DatabaseMigrator`) that runs
as part of Aspire orchestration. This avoids the anti-pattern of running migrations on API startup,
which causes race conditions with multiple API instances, mixes deployment concerns with runtime
concerns, and requires elevated DB permissions at runtime.

**Aspire orchestration flow**:
1. Postgres container starts and becomes healthy
2. DatabaseMigrator starts (`WaitFor(postgres)`)
3. Migrator applies pending migrations via `MigrateAsync()`, then exits (code 0)
4. API starts (`WaitForCompletion(migrator)`)

If the migrator fails (non-zero exit), the API never starts. The failure is immediately visible in
the Aspire dashboard with full logs and traces.

**Postgres container lifecycle**: The Postgres container uses a data volume (`WithDataVolume()`) that
persists database files across container recreations. When the AppHost restarts, a new container is
created but mounts the same volume — all data is preserved. The migrator checks
`__EFMigrationsHistory` and exits as a no-op if everything is already applied. No
`WithLifetime(ContainerLifetime.Persistent)` is used — the container stops with the AppHost.

**EF Core 9+ `MigrateAsync()` behavior**: Since EF Core 9, `MigrateAsync()` is fully self-contained:
- Creates the database if it does not exist
- Acquires a distributed lock to prevent concurrent migrations
- Manages transactions and execution strategy internally
- Applies only pending migrations (checks `__EFMigrationsHistory`)
- No-ops if the database is already up to date

Do NOT wrap `MigrateAsync()` in `CreateExecutionStrategy().ExecuteAsync()` or an explicit
transaction — EF Core 9+ handles both internally and throws if you add an outer transaction.
Do NOT call `EnsureCreatedAsync()` before `MigrateAsync()` — it bypasses migrations and creates the
schema directly, causing `MigrateAsync()` to fail.

**Generating migrations** — run from the Persistence project directory:
```
cd backend/src/2-infrastructure/Persistence
dotnet ef migrations add <MigrationName>
```
No `--startup-project` flag is needed. The `DesignTimeDbContextFactory` in Persistence provides a
dummy connection string that satisfies the Npgsql provider at design time — `dotnet ef` never
connects to a real database when scaffolding migrations. It diffs the current C# model against the
`ModelSnapshot.cs` file (not the database state).

**Where migrations live**: Migration files are generated in `Persistence/Migrations/`. They are
checked into source control. The `DatabaseMigrator` host references Persistence and calls
`DatabaseMigrationRunner.RunMigrationsAsync()` to apply them at runtime.

---

## Frontend BFF & API Client Conventions

### BFF Proxy

A single Nitro catch-all route (`frontend/server/api/[...path].ts`) proxies all `/api/*` requests to
the backend. The browser never sees the access token — it stays encrypted in server-side Valkey
storage.

**Why a BFF at all**: `nuxt-oidc-auth` currently has no way to return the access token server-side
without also exposing it to the browser via the `UserSession` object. Reading the persistent session
storage directly in a server-only route keeps the token off the wire to the client.

**Request flow:**
1. Browser calls `/api/<path>` via the `api` client with an `x-csrf: 1` header.
2. BFF validates the CSRF header, validates/refreshes the session, extracts the access token.
3. BFF forwards the request to the backend with `Authorization: Bearer <token>`.

**Security measures (all required, do not remove without discussion):**

1. **CSRF header check** — rejects requests without `x-csrf: 1` with 401 (Duende-aligned). A
   non-standard header forces a CORS preflight, restricting callers to same-origin (or explicitly
   allowed origins). Protects against both state-changing CSRF and cross-origin data theft via GET.
   The `api` client sets this header globally so no per-call plumbing is needed. See
   https://docs.duendesoftware.com/bff/#csrf-attacks.
2. **Origin check** — when an `Origin` header is present on the incoming request, it must match
   `getRequestURL(event).origin`. Active same-origin backstop that does not rely on the absence of
   CORS headers elsewhere in the stack (e.g. if someone flips `nuxt-security`'s `corsHandler` on
   later, this check still holds). Same-origin GETs that omit `Origin` entirely are allowed — the
   check only rejects *mismatched* origins, not missing ones.
3. **Session lifecycle** — `getUserSession(event)` is called before token extraction, wrapped in
   try/catch so any failure returns a clean 401. `getUserSession` triggers expiration checks and
   automatic token refresh per the OIDC config. **Without this call the `automaticRefresh: true` /
   `expirationCheck: true` config is dead code** — expired tokens would be silently forwarded to
   the backend.
4. **Cookie stripping** — outgoing request to the backend sets `cookie: ""`. The backend
   authenticates via Bearer only; forwarding the session cookie is unnecessary data leakage.
5. **No redirect following** — `redirect: "manual"` in fetch options. 3xx responses from the
   backend are passed back to the client instead of being silently followed (e.g. to login pages).

**CORS is intentionally disabled** (`security.corsHandler: false` in `nuxt.config.ts`). Do not
enable it without revisiting the CSRF design — if CORS allows a foreign origin to send the
`x-csrf` header, the preflight-based defense collapses for that origin. The Origin check above is
an active backstop, but the comment in `nuxt.config.ts` is the first stop for anyone considering
CORS changes.

### Session Storage Model

`nuxt-oidc-auth` splits session data across two tiers:
1. **h3 cookie session** (httpOnly, `sameSite: lax`) — session ID, provider, expiry metadata.
2. **Persistent session in Valkey** — encrypted access/refresh/id tokens.

`server/utils/auth.ts::getAccessToken()` reads tier 2 via `getUserSessionId()` and decrypts the
access token. It assumes the session has already been validated by `getUserSession` — it does NOT
trigger refresh itself. Always call `getUserSession` first.

**Startup config validation**: critical runtime config that no other module enforces is validated
at startup via Nitro plugins — `server/plugins/oidc-storage.ts` checks the Redis connection, and
`server/plugins/validate-runtime-config.ts` checks `backendApiUrl`. Failing at startup surfaces
misconfiguration in the Aspire dashboard immediately instead of yielding cryptic per-request
errors later. When adding new required config, extend the validation plugin.

### API Client

All API calls go through an `openapi-fetch` client constructed by the `useApi()` composable in
`app/composables/useApi.ts`. The composable returns a client wired correctly for the current
rendering context — this is necessary because the same call site runs in two very different
environments:

- **Client**: relative `baseUrl: "/api"`, browser handles cookies automatically.
- **Server (SSR / `onServerPrefetch`)**: absolute `baseUrl: "${origin}/api"` (Node's undici fetch
  rejects relative URLs), and a custom `fetch` wrapper that explicitly forwards the incoming
  request's `cookie` header so the BFF's `getUserSession` finds the encrypted OIDC session. Without
  cookie forwarding, the BFF returns 401, the SSR query fails, dehydrate drops the failed query,
  and the client hydrates empty — producing a hydration mismatch.

The server `fetch` wrapper must seed its `Headers` from the incoming `Request.headers` (not from
`init.headers`). `openapi-fetch` constructs a `Request` object internally and per the fetch spec,
`init.headers` *replaces* the Request's headers wholesale — passing a partial header set would
strip the `x-csrf` header `openapi-fetch` set, and the BFF would return 400.

`useApi()` must be called within a Nuxt setup context (composable, `<script setup>`),
synchronously and before any top-level `await`, since `useRequestURL` and `useRequestEvent` rely
on the active component instance.

TypeScript types in `app/generated/api.d.ts` are auto-generated — **do not hand-edit**. Regenerate
after backend OpenAPI changes with `npm run generate:api` (requires the backend running locally;
see the script for the hardcoded URL, revisit if Aspire's dynamic ports break this).

Consumers wrap calls in Vue Query `useQuery` / `useMutation` inside composables — see
`app/composables/useShoplist.ts` for the canonical pattern: call `const api = useApi()` once at
the top of the composable, then use `api.GET / api.POST / …` from inside query/mutation functions.
On SSR, `onServerPrefetch(() => suspense())` ensures the initial render has data.

**SSR-safe composable usage in components**: Composables that register `onServerPrefetch` (like
`useShoplists` / `useShoplist`) must be called synchronously at the top of `<script setup>`, before
any top-level `await`. `onServerPrefetch` binds to the current component instance, which is only
reliably tracked during synchronous setup. Getting this wrong fails silently: the page still
renders, but SSR no longer prefetches — you lose initial-paint data and get a client-side refetch
instead. When writing new components, call SSR-aware composables first and build subsequent logic
on their returned refs reactively.

---

## Development Practices

### Documentation-First for Bleeding-Edge Technologies

This project uses recent/bleeding-edge versions of many technologies (.NET 10, Nuxt 4, Aspire 13.2,
TUnit, etc.) that are likely underrepresented or inaccurately represented in training data.

**Hard rule**: Before implementing anything involving the technologies listed below, look up current
documentation using the appropriate MCP tool (see MCP Tooling section above). Do not rely on training
data for API shapes, configuration patterns, or library usage. Plan the approach using verified
documentation before writing code.

Technologies requiring doc verification:
- Aspire (all versions — not ".NET Aspire", just "Aspire" since the polyglot rebrand) → **mslearn**
- TUnit (or whichever test framework is chosen) → **context7**
- Nuxt 4 → **nuxt** MCP
- PrimeVue v4 (design tokens, component APIs) → **primevue** MCP
- Mediator (martinothamar) → **context7**
- ErrorOr → **context7**
- FluentValidation → **context7**
- EF Core with .NET 10 (verify new APIs/patterns) → **mslearn**
- Any other library added that is < 1 year old or in preview → **context7**

Always prefer a dedicated MCP tool over web search. See the MCP Tooling section for tool selection.

### Code Quality & Validation

Before considering any task complete:
1. **Build** — all projects must compile without errors or warnings
2. **Tests** — all existing tests must pass
3. **Lint** — all configured linters must pass
4. **Format** — all configured formatters must pass
5. **Playwright verification** — for UI changes, use the Playwright MCP to visually and functionally
   verify the result

**Frontend validation commands** — always use the npm scripts defined in `frontend/package.json`,
run from the `frontend/` directory. Do NOT call `npx nuxi` or `npx nuxt` directly.
```
npm run lint:check   # ESLint (no auto-fix)
npm run typecheck    # nuxt typecheck (vue-tsc)
npm run build        # production build
```

**Backend validation commands** — run from the `backend/` directory.
```
dotnet build              # compile + Roslyn analyzers + Meziantou.Analyzer (TreatWarningsAsErrors)
dotnet csharpier check .  # formatting check (CSharpier)
dotnet format style --verify-no-changes       # .editorconfig code style rules
dotnet format analyzers --verify-no-changes   # analyzer-driven style rules
```

These match what CI runs in `.github/workflows/pr-validation.yml`.

Code style is enforced by tooling:
- **Frontend**: `.editorconfig` in `frontend/`, ESLint with @antfu/eslint-config (ESLint Stylistic,
  no Prettier)
- **Backend**: `.editorconfig` in `backend/`, CSharpier for formatting, Meziantou.Analyzer for code
  quality, `dotnet format` for code style. CSharpier is installed as a local dotnet tool
  (`backend/dotnet-tools.json`); .NET 10 auto-restores local tools on first invocation.

Follow whatever these tools dictate. Do not override or bypass them.

### API Contract Sync

OpenAPI specs generated by the backend are the source of truth for API contracts. The frontend API
client should be generated from these specs so types stay in sync. Exact tooling TBD.

### UI Design

- **Mobile-first**: Design and implement for mobile viewports first.
- **Desktop enhancement**: Gracefully enhance for larger screens — do not just stretch the mobile layout.
- Both mobile and desktop are primary use cases.

### Running the Application

The user typically has Aspire running in Rider already. When you need to access the running frontend
(e.g., for Playwright verification):

1. **First**: Use the Aspire MCP to discover the frontend URL from the running Aspire instance.
2. **If no Aspire instance is running**: Start it with `aspire run` from the repo root.
3. **Last resort only**: Fall back to `npm run dev` in the frontend directory — this should be rare,
   as Aspire is the standard way to run the full stack.

Never start `npm run dev` as a first instinct — always check Aspire first.

### Agent Conduct

- Challenge ideas and suggestions when better approaches exist. The goal is good software engineering,
  not blind execution of requests.
- When making architectural or tooling decisions, propose updating this file.
- Prefer small, incremental changes. Avoid large rewrites unless explicitly discussed and approved.
- When uncertain about requirements or approach, ask — do not assume.

---

## TBD Decisions Tracker

The following decisions are explicitly deferred. Each should be resolved in a dedicated session and
this file updated accordingly.

| Decision | Notes | Status |
|---|---|---|
| Backend architecture pattern | Clean architecture, feature-organized application layer | **Decided** |
| Backend solution structure | Numbered folder groupings, Aspire at repo root, see Project Structure | **Decided** |
| Mediator library | Mediator (martinothamar) — source-gen'd, MIT, MediatR-compatible | **Decided** |
| Result/error pattern library | ErrorOr — clean ergonomics, no HTTP coupling in Application | **Decided** |
| Validation library | FluentValidation — input shape validation via pipeline behavior | **Decided** |
| Handler conventions | Static class wrapper, nullable props, primary constructors, internal visibility | **Decided** |
| Strongly-typed entity IDs | StronglyTypedId source generator, Guid-backed, EF Core converters via `guid-efcore` template | **Decided** |
| Database migrations | Dedicated DatabaseMigrator worker host, Aspire WaitForCompletion, migrations in Persistence, DesignTimeDbContextFactory for CLI | **Decided** |
| Backend authentication | JWT Bearer via `Microsoft.AspNetCore.Authentication.JwtBearer`, `MapInboundClaims = false`, `ICurrentUser` reads `"sub"` from HttpContext, group-level `RequireAuthorization()` on `/api` | **Decided** |
| Resource authorization | `CurrentUserShoplists()` on `IAppDbContext`/`AppDbContext`, scoped by `OwnerId`. Convention: reads via scoped method, writes via raw DbSet. Aggregate root scoping for child entities. `IsImmutableAfterInsert()` for immutable non-key properties. | **Decided** |
| ShoplistItem endpoint design | Hybrid handler split: `UpdateShoplistItem` (PUT) for entity-level props, `UpdateShoplistItemPosition` (PATCH) and `UpdateShoplistItemChecked` (PATCH) as dedicated handlers. Aggregate root methods on `Shoplist` for collection/position management. Handlers in `Items/` subfolder under `Features/Shoplists/`. | **Decided** |
| Test framework & setup | Likely TUnit; integration test infrastructure | Not started |
| UI library | PrimeVue v4 with Aura preset, styled mode | **Decided** |
| CSS approach details | Scoped native CSS, PrimeVue tokens for colors, 1024px breakpoint | **Decided** |
| API client generation | `openapi-fetch` + `openapi-typescript`, constructed via `useApi()` composable in `app/composables/useApi.ts` (SSR-aware: absolute URL + cookie forwarding on server, relative URL on client), BFF at `/api` | **Decided** |
| Frontend BFF & auth | Nitro catch-all proxy at `server/api/[...path].ts`. CSRF via `x-csrf` header, session refresh via `getUserSession`, cookie stripping, manual redirect. Tokens stored encrypted in Valkey, never exposed client-side. | **Decided** |
| API documentation UI | Scalar (`Scalar.AspNetCore`) at `/scalar/v1`, dev-only. OpenAPI via built-in `Microsoft.AspNetCore.OpenApi` | **Decided** |
| OpenAPI nullable trade-off | Nullable C# props → nullable TypeScript. Accepted for now; revisit if painful (FluentValidation→OpenAPI schema integration or derived types) | **Accepted (revisit later)** |
| ErrorOr → HTTP mapping | `ToHttpResult()` extensions on `ValueTask<ErrorOr<T>>` and `ErrorOr<T>` in `Api/Extensions/ErrorOrExtensions.cs`. Default Ok, overridable via `onSuccess` param. | **Decided** |
| OpenAPI strongly-typed ID schemas | `StronglyTypedIdSchemaTransformer` in `Api/OpenApi/`. New ID types must be added to the transformer's mapping dictionary. | **Decided** |
| Minimal API endpoint organization | Manual extension methods per feature in `Api/Endpoints/`, `MapGroup()` for grouping, top-level `/api` prefix via `MapShoplistsApi()`. No Carter. | **Decided** |
| Code style configuration | Frontend: @nuxt/eslint + @antfu/eslint-config, .editorconfig. Backend: CSharpier + Meziantou.Analyzer + .editorconfig. | **Decided** |
| CI/CD pipeline | GitHub Actions configuration | Not started |
