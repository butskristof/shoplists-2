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
  tests/                     -> (empty; see docs/projects/testing/plan.md)
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
- **Self-generated identity**: aggregate roots and child entities own their identity. Initialize Id
  properties to `<TypeId>.New()` (e.g. `public ShoplistId Id { get; init; } = ShoplistId.New();`)
  rather than relying on EF Core to fill them in. Pair this with `ValueGeneratedNever()` on the
  key in the entity configuration (NOT `ValueGeneratedOnAdd()`) — when EF Core thinks the key is
  store-generated but finds a non-default value, it assumes the entity already exists and emits
  `UPDATE` instead of `INSERT` for nav-collection adds (see ADR 016). Side benefit: entities are
  valid immediately on construction, so domain unit tests can distinguish instances without a DB.
- **Self-validating input**: domain methods and property setters guard against bad input shape
  using BCL throw helpers (e.g. `ArgumentException.ThrowIfNullOrWhiteSpace(value)`); strings are
  canonicalised (`Trim()`) on the way in. Use C# 14 `field`-backed setters when validation must
  fire on every assignment, not just at construction. App-layer FluentValidation rules remain the
  primary user-facing check (uniform `ValidationProblemDetails`); domain guards are defense in
  depth and should never fire from a well-formed handler call.
- **Aggregate boundary on construction**: child entities have an `internal` parameterless
  constructor (e.g. `internal ShoplistItem() { }`); they can only be created by the aggregate root
  or other Domain-assembly code. External layers (Application, Persistence) reference the type
  but must go through aggregate methods like `shoplist.AddItem(...)`. EF Core materialises via
  reflection regardless of constructor accessibility.
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

- Always explicit: `HasKey`, `ValueGeneratedNever` (the domain assigns the Id — see "Self-generated identity" above), `IsRequired` on strings
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

### Aspire Version Upgrades

Use the Aspire CLI for all Aspire version bumps — it's the only path that keeps
the `Aspire.AppHost.Sdk/X.Y.Z` SDK line in `AppHost.csproj` in lockstep with the
`Aspire.*` package versions in `Directory.Packages.props`. Manual / Rider-driven
bumps miss the SDK line.

```bash
aspire update --self                              # update the CLI first
aspire update --apphost backend/AppHost/AppHost.csproj \
              --channel stable --nuget-config-dir backend -y
```

Non-Aspire packages (EF Core, Mediator, etc.) are upgraded separately. See ADR
015 for the full procedure and trade-offs.

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

### Vue Query Patterns

`app/composables/useShoplist.ts` is the reference. Follow these patterns for new data-mutating
composables:

- **Query-key factory**: export a `<resource>Keys` object (e.g. `shoplistKeys.all`,
  `shoplistKeys.detail(id)`) and reference it from every `useQuery` / `invalidateQueries` /
  `setQueryData` call. Do not inline `["resource", …]` arrays.
- **Optimistic updates by default** for single-resource mutations. Shape:
  - `onMutate`: `cancelQueries` on affected keys, snapshot previous cache via `getQueryData`, write
    optimistic state with `setQueryData`, return the snapshot as an `OptimisticContext`.
  - `onError`: roll back from `context` and surface a PrimeVue toast (`useToast()`) — mutation
    failures must be visible to the user.
  - `onSettled`: invalidate the affected keys (detail + list) so the server reconciles.
  - Type the mutation as `useMutation<TData, Error, TVariables, OptimisticContext>` so `context`
    is typed in `onError`.
- **Mutation wrappers**: fire-and-forget operations use `.mutate()` and return `void` (e.g.
  `addItem`, `toggleItemFulfilled`). Use `.mutateAsync()` only when the caller must await the
  outcome — return response data when needed (e.g. `createList` returns the created ID) or
  `Promise<void>` for post-success sequencing (navigation, emits). Let errors propagate — the
  mutation's `onError` handles user-facing feedback (toasts), and a thrown error naturally prevents
  subsequent steps. Do not wrap in try/catch returning boolean success flags.

### Component Design

- **Pages are thin orchestrators.** Extract route params, call `useHead`, handle top-level
  loading/error/not-found states, delegate to a child component for the actual feature UI.
  Pages should not contain business logic, interaction state, or complex templates.
- **Single responsibility per SFC.** Each component does one thing. If a component renders
  different UIs based on a mode prop (e.g. `isEditMode`, `isAddRow`), that is two components —
  split them. Prefer many focused components over one configurable component with many props/modes.
- **`<script setup>` guideline: ~50 lines.** Not a hard limit, but exceeding it significantly is
  a signal to extract child components or move logic into a composable.
- **One composable per resource, one file per composable.** `useShoplists.ts` and `useShoplist.ts`,
  not combined. Query key factory in its own `queryKeys.ts`.
- **View-specific derived state belongs in the view component.** Composables provide data +
  mutations. Filtered/grouped subsets (e.g. `itemsToGet`, `fulfilledItems`) are computed in the
  component that needs them.
- **Extract reusable UI primitives on first duplication.** `StatePanel`, `LoadingPanel`,
  `BackButton` exist — use them. If a new pattern appears twice, extract immediately rather than
  letting ad-hoc CSS diverge.
- **Dialogs as components.** Confirmations and forms that appear in a modal (`Dialog`) get their
  own SFC (e.g. `CreateShoplistDialog`, `DeleteShoplistDialog`). The parent controls visibility
  via a `v-if` + ref boolean, not inline template.

### Vue Code Style

- **`<form @submit.prevent>` for any submittable input.** Add item, rename, create list — all use
  forms. This gives Enter-to-submit for free, is semantically correct, and is accessible. Do not
  use `@keydown.enter` on inputs.
- **`autofocus` attribute** instead of `nextTick(() => ref.value?.$el?.focus())`.
- **`computed` over `watch` for derived state.** Validation flags, trimmed values, filtered lists —
  if it can be expressed as a function of reactive state, use `computed`. Only use `watch` for
  genuine side effects (API calls, imperative DOM mutations that cannot be declarative).
- **No `ref` for pure derivations.** `inputInvalid` should be
  `computed(() => hasAttemptedSave.value && !trimmedName.value)`, not a ref toggled in handlers.
- **`defineModel` for two-way parent state.** When a child component controls parent state (e.g.
  edit mode toggle), use `defineModel` + `v-model` rather than prop + emit pairs.
- **`.mutate()` for fire-and-forget, `.mutateAsync()` only when awaiting** — see Vue Query Patterns
  above for the full convention.

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

## Dependency Security Overrides

`frontend/package.json` uses npm `overrides` to patch known-vulnerable transitive deps. 
Policy: ADR 017. Registry of current entries withadvisory IDs, parent deps, and removal 
conditions: `docs/security/overrides.md`.

**Review trigger** — every `npm audit` cleanup or deps-upgrade pass: re-read the registry,
and for each entry temporarily remove the override, run `npm install` + `npm audit`, and
remove the entry (in both `package.json` and the registry) if it's no longer load-bearing.
Never run `npm audit fix --force` — it can silently downgrade direct deps. See ADR 017.

When adding a new override, write the registry entry in the same change and update the
`"//overrides"` breadcrumb at the top of the overrides block if the description drifts.

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

### Project documentation

This file is loaded into context on every interaction — keep it focused on **actionable conventions,
rules, and patterns** needed for forward development. It is not a decision log, changelog, or task tracker.

Three places capture project knowledge:

- **`docs/decisions/NNN-title.md`** — ADR-lite records for locked-in architectural or tooling  decisions 
  (Status, Context, Decision, Alternatives/trade-offs). Only the resulting convention or rule lands in 
  this file — not the reasoning behind it.
- **`docs/projects/<project-name>/`** — working docs for in-flight work (both user-facing features
  and technical concerns like testing or hosting). Suggested files: `plan.md` (goals, non-goals,
  phased rollout), `open-questions.md`, `resources.md`, `progress.md`. Headline decisions that
  stabilize graduate to an ADR.
- **This file** — conventions and rules only.

Read existing records in both folders for format and tone.
