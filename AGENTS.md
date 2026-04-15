# CLAUDE.md

## Project Overview

Shoplists is a grocery shopping web app. MVP: users own lists of items that can be ticked on/off.
See `README.md` for future feature ideas.

---

## Tech Stack

### Frontend
- **Nuxt 4** (Vue 3, TypeScript), hybrid SSR → SPA
- **PrimeVue v4** (Aura preset, styled mode with design tokens / CSS variables) and **PrimeIcons** for icons
- **Native CSS** with nesting, scoped `<style scoped>`. **No Tailwind — non-negotiable.**
  - PrimeVue design tokens (`var(--p-surface-0)`) for all colors (auto light/dark)
- **`openapi-fetch`** with types generated from backend OpenAPI spec (`npm run generate:api`)
- **`nuxt-oidc-auth`** for OIDC flow; tokens stored encrypted in Valkey, never exposed client-side
- **Node**: latest LTS (v24), **npm** as package manager
- **Testing**: Playwright via MCP tooling (interactive, no test code in repo)

### Backend
- **.NET 10**, Minimal APIs, Entity Framework Core, PostgreSQL
- **Clean architecture** with feature-organized application layer
- **Mediator** (martinothamar) — source-generated, MediatR-compatible API
- **ErrorOr** for result/error pattern (no HTTP coupling in Application layer)
- **FluentValidation** for input validation via mediator pipeline behavior
- **Meziantou.Analyzer** + `TreatWarningsAsErrors` — analyzer violations fail the build
- **OpenAPI** via built-in `Microsoft.AspNetCore.OpenApi`, **Scalar** for docs UI (dev-only)

### Infrastructure
- **Aspire** (AppHost) orchestrates the full local stack
- **Run locally**: `aspire run` from repo root. Aspire assigns ports dynamically — use Aspire MCP to discover URLs.
- **Auth**: External OIDC provider; not a concern of this app

---

## Project Structure

```
backend/
  Shoplists.slnx
  AppHost/                   -> Aspire AppHost (orchestrates full stack)
  Directory.Build.props      -> Shared build config (TFM, analyzers, TreatWarningsAsErrors)
  src/
    1-core/
      Domain/                -> Entities, value objects, domain logic
      Application/           -> Use cases, mediator handlers, validation, pipelines
    2-infrastructure/
      Infrastructure/        -> External service integrations (no EF Core)
      Persistence/           -> EF Core DbContext, configs, migrations
      ServiceDefaults/       -> Shared Aspire service config
    3-hosts/
      Api/                   -> Minimal API host, endpoints, auth, OpenAPI
      DatabaseMigrator/      -> Worker service that applies migrations, then exits
  tests/                     -> (empty; test framework not chosen yet — see Open Decisions)
frontend/
  ...                        -> Nuxt application
```

Folder names are bare; each project uses `Shoplists.*` as its root namespace
(e.g. folder `Application/` → namespace `Shoplists.Application`).

**Dependency direction**: 1-core → 2-infrastructure → 3-hosts (numbered folders visualize this).
AppHost orchestrates hosts but is not part of the layered architecture.

**Layer constraints** (see ADR 012 for full responsibilities):
- `Infrastructure` does NOT contain EF Core / database concerns — those live in `Persistence`
- `AppDbContext` is `internal` — external code uses `IAppDbContext` (defined in Application) or `DatabaseMigrationRunner`
- `Application` defines interfaces (`IAppDbContext`, `ICurrentUser`); other layers implement them

---

## Backend Conventions

### Handler Pattern

Each use case is a single static class with nested Request, Validator, and Handler. Follow this
template for new handlers:

```csharp
public static class CreateShoplist
{
    public sealed record Request(string? Name) : ICommand<ErrorOr<Response>>;

    public sealed record Response(ShoplistId Id);

    internal sealed class Validator : BaseValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Name).ValidString(required: true);
        }
    }

    internal sealed class Handler(
        ILogger<Handler> logger,
        IAppDbContext dbContext,
        ICurrentUser currentUser
    ) : ICommandHandler<Request, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(
            Request request, CancellationToken cancellationToken)
        {
            var shoplist = new Shoplist { Name = request.Name!, OwnerId = currentUser.UserId };
            dbContext.Shoplists.Add(shoplist);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new Response(shoplist.Id);
        }
    }
}
```

Key rules:
- **Request properties are nullable** — FluentValidation enforces non-null (ensures uniform `ValidationProblemDetails`)
- **`Response` record**: wrap results in a nested `public sealed record Response(...)` rather than
  returning primitives or domain entities. Queries project into it via `.Select(...)` inside the
  LINQ query (see `GetShoplists`) so EF translates the projection to SQL.
- **Validators**: `internal`, inherit `BaseValidator<T>`, validate input *shape* only (non-null, format, range)
- **Business rule validation** (uniqueness, state transitions, authorization, anything needing DB access) belongs in the **handler** — return `Error.Validation` / `Error.Conflict` / `Error.NotFound` via `ErrorOr`
- **Handlers**: `internal`, use primary constructors for DI
- **Return `ErrorOr<T>`** from all handlers
- **Single file** unless handler method exceeds ~100 lines
- Child entity handlers go in a subfolder under the aggregate root (e.g., `Features/Shoplists/Items/`)

### Application Layer Structure

- `Application/Common/` — cross-cutting concerns (auth interfaces, persistence interface, mediator
  pipelines, validation helpers). Rarely touched during feature work.
- `Application/Features/<AggregateRoot>/` — one folder per aggregate root, one file per use case.
  Child-entity handlers nest in a subfolder (e.g. `Features/Shoplists/Items/`).

### Domain Entity Conventions

- **Property accessors**: `{ get; init; }` by default. Use `{ get; set; }` only for genuinely mutable properties.
- **`required`** on properties that must be set at construction (Name, FKs). Exclude `Id` and properties with defaults.
- **Collection navigation**: private `List<T>` field + public `IReadOnlyList<T>` accessor. Mutate through aggregate root methods.
- **Naming**: Use `Shoplist`, `ShoplistItem` — not `ShoppingList`. Domain-specific naming.

### Strongly-Typed IDs

All entities use strongly-typed IDs via `StronglyTypedId` source generator.
- Default: Guid-backed with `[StronglyTypedId]` (assembly defaults in `Domain/StronglyTypedIdDefaults.cs`)
- String-backed IDs: `[StronglyTypedId(Template.String, "string-efcore")]` (e.g., `UserId`)

**Checklist for new entities:**
1. Define ID struct + entity class in `Domain/Models/<Feature>/`
2. Register value converter in `ConfigureStronglyTypedIdConversions` (`Persistence/Extensions/`)
3. Add `DbSet` to `AppDbContext` (aggregate roots only)
4. Create `IEntityTypeConfiguration<T>` in `Persistence/EntityConfigurations/`
5. Add ID type to `StronglyTypedIdSchemaTransformer` mapping dictionary (`Api/OpenApi/`)
6. Generate migration: `cd backend/src/2-infrastructure/Persistence && dotnet ef migrations add <Name>`

### EF Core Configuration

- Always explicit: `HasKey`, `ValueGeneratedOnAdd`, `IsRequired` on strings
- Default max string length via `ConfigureConventions` (512). Override per-property if needed.
- Use `IsImmutableAfterInsert()` extension on non-key properties that must never change (e.g., `OwnerId`, parent FKs). Do NOT apply to PKs.

### Endpoint Conventions

Endpoints in `Api/Endpoints/`, one static class per feature. Thin dispatchers:
```csharp
private static Task<IResult> GetShoplists(ISender sender) =>
    sender.Send(new GetShoplists.Request()).ToHttpResult();
```
- `MapGroup()` per feature with sub-path and tags
- `MapShoplistsApi()` registers a root-level group (no path prefix) with `.RequireAuthorization()`;
  each feature specifies only its sub-path (`/shoplists`, etc.). The `/api` prefix seen from the
  frontend comes from the BFF proxy, not the backend.
- `ErrorOr` → HTTP mapping via `ToHttpResult()` extensions (`Api/Extensions/ErrorOrExtensions.cs`).
  Return appropriate `ErrorType` from handlers (`Validation`, `NotFound`, `Conflict`, `Unauthorized`)
  — mapping to status codes is automatic. See ADR 002 for the full mapping table.
- Default success: `Ok(value)`. Override via `onSuccess` for `Created` / `NoContent`.

**OpenAPI metadata**: `.WithTags()` on the group, `.WithName()` and `.Produces<T>()` per endpoint.
- OpenAPI spec: `/openapi/v1.json`
- Scalar docs UI: `/scalar/v1` (dev-only)

### Authorization

- `.RequireAuthorization()` on the root group (in `MapShoplistsApi()`) protects all API endpoints
- **Reads**: Use `dbContext.CurrentUserShoplists()` (scoped to current user's data)
- **Writes**: Use `dbContext.Shoplists` DbSet directly
- Child entities are implicitly scoped through aggregate root (load parent via `CurrentUserShoplists().Include(...)`)
- Non-API hosts (DatabaseMigrator) register a dummy `ICurrentUser` that throws on access

### Migrations

Applied by `DatabaseMigrator` worker (not on API startup). Aspire orchestration:
Postgres → DatabaseMigrator (`WaitFor`) → API (`WaitForCompletion`).

Generate: `cd backend/src/2-infrastructure/Persistence && dotnet ef migrations add <Name>`
(No `--startup-project` needed — `DesignTimeDbContextFactory` handles it.)

See ADR 005 for runner implementation notes.

### Other Backend Rules

- **Type accessibility**: New types are `internal sealed` by default. Widen to `public` only when
  another project needs the type; drop `sealed` only when designed for inheritance.
- **No `ConfigureAwait(false)`** — no-op on ASP.NET Core / generic host (MA0004 suppressed in `.editorconfig`)
- **Logging in handlers**: Log state and decisions (entity IDs, branches taken), not action descriptions. Pipeline behaviors handle cross-cutting logging.

---

## Frontend Conventions

### BFF Proxy

`frontend/server/api/[...path].ts` proxies `/api/*` to the backend and injects the access token
server-side — the browser never sees it. The proxy enforces layered same-origin protection (CSRF
header, origin check, cookie stripping, `redirect: "manual"`, session-lifecycle trigger) and CORS
is intentionally disabled. See ADR 006 for the full design. Do not weaken any of these without
review.

### API Client (`useApi()`)

All API calls go through the `openapi-fetch` client from `useApi()` (`app/composables/useApi.ts`).
The composable handles client-side vs SSR differences (baseUrl, cookie forwarding) internally —
see ADR 009 for the mechanism.

Key rules:
- Call `useApi()` (and any composable built on it, e.g. `useShoplists`) synchronously at the top of
  `<script setup>`, before any `await`.
- Wrap calls in Vue Query `useQuery`/`useMutation` inside dedicated composables (see
  `app/composables/useShoplist.ts`).
- `app/generated/api.d.ts` is auto-generated — **never hand-edit**. Regenerate with
  `npm run generate:api` after any backend contract change.

### Session & Config

- Startup validation: `server/plugins/oidc-storage.ts` (Redis) and
  `server/plugins/validate-runtime-config.ts` (backendApiUrl). Extend these when adding new
  required config.

### UI Design

- **Mobile-first** design, gracefully enhance for desktop
- Both viewports are primary use cases

---

## API Contract Sync

Backend OpenAPI is the source of truth for API contracts. Frontend types are generated from it.

**Flow when changing the API:**
1. Update backend handler / endpoint / Request / Response
2. Run backend (via Aspire) so the OpenAPI spec at `/openapi/v1.json` reflects the changes
3. From `frontend/`, run `npm run generate:api` — regenerates `app/generated/api.d.ts`
4. Update frontend code to match the new types (TypeScript will surface breakages)
5. Run `npm run typecheck` to confirm

`app/generated/api.d.ts` must never be hand-edited. The script's backend URL is hardcoded —
revisit if Aspire's dynamic ports break it.

---

## Validation Commands

**Frontend** (run from `frontend/`):
```
npm run lint:check   # ESLint
npm run typecheck    # vue-tsc
npm run build        # production build
```

**Backend** (run from `backend/`):
```
dotnet build              # compile + analyzers (TreatWarningsAsErrors)
dotnet csharpier check .  # CSharpier formatting
dotnet format style --verify-no-changes
dotnet format analyzers --verify-no-changes
```

All must pass before any task is complete. Do NOT call `npx nuxi`/`npx nuxt` directly.

**Style config to align with:**
- Frontend: `frontend/.editorconfig`, ESLint config (`@antfu/eslint-config` via `@nuxt/eslint`, no Prettier)
- Backend: `backend/.editorconfig`, CSharpier defaults, Meziantou.Analyzer rules
- Follow tooling output — do not override or bypass it

---

## MCP Tooling

| Server | When to use |
|---|---|
| **nuxt** | Nuxt 4, Vue, Nitro questions |
| **mslearn** | .NET, EF Core, Aspire, Microsoft tech |
| **aspire** | Local dev: resource status, URLs, logs, traces |
| **playwright** | UI verification (no test code in repo) |
| **primevue** | PrimeVue v4 components, theming, design tokens |
| **context7** | Any other library docs |

**Hard rule**: Before implementing with bleeding-edge tech (.NET 10, Nuxt 4, Aspire, PrimeVue v4,
Mediator, ErrorOr, FluentValidation, EF Core 10), look up current docs via the appropriate MCP tool.
Do not rely on training data for API shapes or config patterns.

---

## Running the Application

1. Check if Aspire is already running (user typically runs it in Rider) — use Aspire MCP to discover URLs
2. If not running: `aspire run` from repo root
3. Last resort: `npm run dev` in frontend dir (rare)

### Observability & Debugging

OpenTelemetry is wired into both sides: `ServiceDefaults` covers the .NET services (logs, traces,
metrics), and `frontend/server/plugins/telemetry.ts` covers the Nitro server (traces + metrics via
OTLP gRPC; the log export pipeline is scaffolded but Nitro/consola logs are not yet emitted). All
signals flow into the Aspire dashboard. **Use the Aspire MCP** to inspect:
- Live logs per .NET resource (backend, migrator, Postgres, Valkey)
- Distributed traces across the stack (frontend → BFF → backend → DB)
- Metrics and resource state

Reach for this before adding ad-hoc logging or guessing at failure modes.

---

## Agent Conduct

- Challenge suggestions when better approaches exist — goal is good engineering, not blind execution
- Prefer small, incremental changes. Avoid large rewrites unless discussed.
- When uncertain, ask — do not assume.

### Keeping this file lean

This file is loaded into context on every interaction — keep it focused on **actionable conventions,
rules, and patterns** needed for forward development. It is not a decision log or changelog.

When making architectural or tooling decisions, record the rationale in `docs/decisions/NNN-title.md`
(ADR-lite format: Status, Context, Decision, Alternatives/trade-offs). Only add the resulting
**convention or rule** to this file — not the reasoning behind it. Read existing records in
`docs/decisions/` for the format to follow.

---

## Open Decisions

| Decision | Status |
|---|---|
| Test framework & setup | Not started (likely TUnit) |
| CI/CD pipeline | Not started |
| OpenAPI nullable trade-off | Accepted for now; revisit if painful |
