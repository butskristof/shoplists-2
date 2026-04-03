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
- **BFF**: Implemented in Nuxt (Nitro server routes). The browser authenticates via a secure httpOnly
  cookie. The BFF extracts the access token from the encrypted cookie and proxies API calls to the
  backend with the token attached. Token and session management are the BFF's responsibility.
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
- **API client generation**: TBD (possibly Nuxt Open Fetch module; decide based on current Nuxt best practices)
- **Testing**: Use Playwright via MCP tooling during implementation to functionally and visually verify
  changes. No Playwright test code in the repo — testing is done interactively by the agent.

### Backend
- **Runtime**: .NET 10
- **API style**: Minimal APIs
- **API documentation**: OpenAPI (UI TBD, likely Scalar)
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
  enforcement including primary constructor parameter reassignment prevention (MA0147). Combined with
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
  Shoplists.slnx                    -> .NET solution (references all backend + Aspire projects)
  AppHost/                           -> Aspire AppHost (orchestrates full stack: backend, frontend, Postgres)
  backend/
    Directory.Build.props            -> Shared build config (TFM, nullable, analyzers, TreatWarningsAsErrors)
    ServiceDefaults/                 -> Shared Aspire service configuration (OpenTelemetry, health checks, resilience)
    src/
      1. Core/
        Shoplists.Domain/            -> Entities, value objects, domain logic (no external dependencies)
        Shoplists.Application/       -> Use cases, mediator handlers, validation, cross-cutting concerns
      2. Infrastructure/
        Shoplists.Infrastructure/    -> External service integrations (TimeProvider, API clients, file storage)
        Shoplists.Persistence/       -> EF Core DbContext, entity configurations, repositories
      3. Hosts/
        Shoplists.Api/               -> ASP.NET Core Minimal API host, endpoint mapping, auth middleware
        Shoplists.DatabaseMigrations/-> EF Core migration runner (Aspire-orchestrated, runs before API starts)
    tests/
      Shoplists.Domain.UnitTests/
      Shoplists.Application.UnitTests/
      Shoplists.Application.IntegrationTests/
      Shoplists.Tests.Shared/        -> Shared test infrastructure (builders, fixtures, helpers)
  frontend/
    ...                              -> Nuxt application
```

**Dependency direction** (numbered folders visualize this):
- **1. Core** depends on nothing (only framework/language features and chosen libraries like Mediator, ErrorOr, FluentValidation)
- **2. Infrastructure** depends on 1. Core (implements interfaces defined in Application/Domain)
- **3. Hosts** depends on 1. Core and 2. Infrastructure (wires everything together via DI)
- AppHost depends on Hosts projects (to orchestrate them) but is not part of the layered architecture

**Aspire placement**: AppHost lives at repo root (not inside `backend/`) because it orchestrates the
entire stack including frontend and infrastructure services. ServiceDefaults stays in `backend/` as
it's consumed only by .NET backend projects.

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
- **Persistence**: EF Core DbContext, entity configurations, repository implementations. Separated from
  Infrastructure to keep packages organized and to avoid the DatabaseMigrations host carrying unrelated
  dependencies.
- **Api**: ASP.NET Core Minimal API host. Maps HTTP endpoints to mediator requests. Handles
  authentication (JWT validation), authorization, and HTTP-specific concerns (ProblemDetails mapping,
  OpenAPI documentation). Implements infrastructure interfaces that depend on HTTP context (e.g.,
  `ICurrentUser` reading from `HttpContext`).
- **DatabaseMigrations**: Standalone host that runs EF Core migrations. Orchestrated by Aspire
  (`WaitFor(migrator)`) so the API doesn't start until migrations complete. Also suitable for CD
  pipelines (run migrator before deploying API). This is the correct pattern — running migrations on
  API startup is an anti-pattern.

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
    ShoppingLists/
      CreateList.cs                -> Static class containing Request, Validator, Handler
      GetList.cs
      GetLists.cs
      ...
    Items/
      TickItem.cs
      ...
```

### Handler File Convention

Each use case lives in a single file as a static class with nested types. This keeps related code
together and scopes type names to avoid collisions (e.g., `CreateList.Request` vs `GetList.Request`).

```csharp
public static class CreateList
{
    // Request: sealed record, implements ICommand<ErrorOr<T>> or IQuery<ErrorOr<T>>
    // Properties are nullable with FluentValidation enforcing non-null — this ensures uniform
    // ValidationProblemDetails responses and keeps OpenAPI contract control in our hands.
    public sealed record Request : ICommand<ErrorOr<Guid>>
    {
        public string? Name { get; init; }
    }

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
- **Primary constructors** for dependency injection (enforced readonly via Meziantou.Analyzer MA0147)
- **Nullable request properties** with FluentValidation `NotNull` rules — ensures all validation
  errors flow through the same pipeline and produce uniform `ValidationProblemDetails` responses.
  Trade-off: OpenAPI schema generates nullable types, so generated TypeScript clients have nullable
  fields. Accepted for now; may integrate FluentValidation rules into OpenAPI schema generation later.
- **`internal` visibility** for Validator and Handler — only the Request (and Response if applicable)
  need to be public for the API layer to reference them.
- **ErrorOr<T>** as the return type for all handlers — provides a consistent application layer
  contract regardless of entry point (API, background job, message consumer).
- **Split into folder** only when a handler file grows very large (100+ lines in the handler method).
  For typical CRUD, single file is preferred.

### Logging Philosophy

- Pipeline behaviors handle cross-cutting logging (handler entry/exit, timing, validation failures).
- OpenTelemetry + Aspire provide trace and metric data automatically.
- In handlers, log **state and decisions** (entity IDs, which branch was taken, counts), not actions
  the code already describes ("Mapped request to entity"). Debug/Trace level is fine for granular
  operational insight — these are filtered out in production by default and available when
  investigating issues.

### Error Handling at the API Boundary

- Handlers return `ErrorOr<T>` — the Application layer never throws exceptions for expected failures.
- The API layer maps `ErrorOr` error types to HTTP responses:
  - `ErrorType.Validation` → 400 + `ValidationProblemDetails`
  - `ErrorType.NotFound` → 404 + `ProblemDetails`
  - `ErrorType.Conflict` → 409 + `ProblemDetails`
  - `ErrorType.Unauthorized` → 403 + `ProblemDetails`
  - etc.
- Unexpected exceptions are caught by global middleware and mapped to 500 + `ProblemDetails`.
- Exact mapping implementation TBD when the API project is set up.

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
4. **Format** — all configured formatters must pass (CSharpier for C#, others TBD)
5. **Playwright verification** — for UI changes, use the Playwright MCP to visually and functionally
   verify the result

**Frontend validation commands** — always use the npm scripts defined in `frontend/package.json`,
run from the `frontend/` directory. Do NOT call `npx nuxi` or `npx nuxt` directly.
```
npm run lint:check   # ESLint (no auto-fix)
npm run typecheck    # nuxt typecheck (vue-tsc)
npm run build        # production build
```
These match what CI runs in `.github/workflows/pr-validation.yml`.

Code style is enforced by tooling (`.editorconfig` in frontend, ESLint with @antfu/eslint-config for
frontend — backend formatter TBD, likely CSharpier). Follow whatever these tools dictate. Do not
override or bypass them.

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
| Test framework & setup | Likely TUnit; integration test infrastructure | Not started |
| UI library | PrimeVue v4 with Aura preset, styled mode | **Decided** |
| CSS approach details | Scoped native CSS, PrimeVue tokens for colors, 1024px breakpoint | **Decided** |
| API client generation | Nuxt Open Fetch or alternative | Not started |
| API documentation UI | Likely Scalar | Not started |
| OpenAPI nullable trade-off | Nullable C# props → nullable TypeScript. Accepted for now; revisit if painful (FluentValidation→OpenAPI schema integration or derived types) | **Accepted (revisit later)** |
| ErrorOr → HTTP mapping | Exact implementation for mapping ErrorOr types to ProblemDetails at API boundary | Not started |
| Code style configuration | Frontend: @nuxt/eslint + @antfu/eslint-config (ESLint Stylistic, no Prettier), .editorconfig in frontend. Backend: likely CSharpier + .editorconfig (TBD). | **Decided (frontend)** |
| CI/CD pipeline | GitHub Actions configuration | Not started |
