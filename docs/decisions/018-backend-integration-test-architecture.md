# Backend integration test architecture

**Status**: Decided

## Context

Domain unit tests and application-layer validator unit tests are live. Handler-level integration
tests are the next layer in the testing rollout (see
[`docs/projects/testing/plan.md`](../projects/testing/plan.md), step 6). The item metadata project
is the immediate priority signal: it introduces schema migrations and API contract changes, the
highest regression-risk shape we have ahead.

Three architectural choices must land before any code does:

1. **Test boundary** — where in the stack do tests dispatch their requests, and what do they
   assert against?
2. **Test database strategy** — how is the DB provisioned, isolated between tests, and reset (or
   not) between runs?
3. **`ICurrentUser` substitution** — how do tests act as a specific user, and how do cross-user
   scenarios work?

These are coupled enough that they belong in one record rather than three; the chosen test
boundary constrains what database strategy is workable, which in turn constrains the auth
substitution mechanism.

Constraints driving the choice:

- **TUnit is parallel-by-default.** Anything that forces sequential execution costs the framework's
  primary advantage (see [ADR 014](014-test-framework.md)).
- **Source-generated Mediator + auto-registered FluentValidation** mean the composition root is
  load-bearing — tests must exercise the *real* DI wiring, not a hand-curated subset, or
  registration drift goes undetected.
- **Clean architecture layers** ([ADR 012](012-clean-architecture-layers.md)) — `AppDbContext` is
  internal to Persistence; external code goes through `IAppDbContext` or `DatabaseMigrationRunner`.
  Tests must respect these boundaries — with one sanctioned, test-only exception for direct DB
  arrange/assert (see *Direct database access* under Decision).
- **`OwnerId`-scoped authorization** ([ADR 007](007-resource-authorization.md)) — every read goes
  through `dbContext.CurrentUserShoplists()`, which filters by the current user's `OwnerId`. This
  invariant is load-bearing for the test isolation model below.

## Decision

Three coupled choices:

1. **Boundary: mediator-rooted, no HTTP.** Tests dispatch Request records through `ISender`.
   The test composition root mounts the production `AddApplication()` + `AddPersistence()`
   extensions verbatim, so the full mediator pipeline (`ValidationBehavior`, logging,
   auto-registered validators, Mediator's source-generated wiring) is exercised end-to-end. HTTP
   concerns — model binding, `ErrorOr → IResult` mapping ([ADR 002](002-error-result-pattern.md)),
   endpoint group config — are treated as transport and not covered by this test layer.

2. **Test database: Testcontainers PostgreSQL with user-scoped isolation.** One Postgres container
   per assembly run. Migrations applied once at fixture startup via the existing
   `DatabaseMigrationRunner` (the same entry point the production `DatabaseMigrator` worker calls
   — see [ADR 005](005-database-migration-strategy.md)). Schema state is not reset between tests.
   Each test gets a fresh `UserId` via per-test instance-field initialization on the test base
   class (TUnit instantiates the test class fresh for every `[Test]` method); the production
   `OwnerId` filter provides isolation between tests as a side effect of the existing
   authorization design.

3. **`ICurrentUser` substitution: scoped `TestCurrentUser` reading from a scoped
   `TestScopeContext`.** The session fixture registers `TestScopeContext` as scoped DI;
   `TestCurrentUser` (also scoped, matching production lifetime) takes it as a constructor
   dependency and exposes its `UserId`. The test base class — which owns per-test state as
   ordinary instance fields — populates the context immediately after creating each operation's
   scope. Each test runs as an ambient default `CurrentUserId` (a fresh id per test instance); to
   act as a different user, a test passes `asUser:` to `SendAsync`, which overrides the user for
   that single operation's scope. There is no mutable "current actor" to track — the default is
   read-only and deviations are local to the call. No `AsyncLocal`, no TUnit-specific value
   propagation — TUnit creates a fresh test class instance per `[Test]` method, so per-test
   instance fields are naturally isolated for parallel execution.

### Composition shape

Two classes split the concerns cleanly:

- **`ApplicationFixture`** (session-scoped via TUnit `[ClassDataSource<…>(Shared =
  SharedType.PerTestSession)]`) owns the Postgres container, the host, and the DI graph. The class
  is `public` (TUnit injects it), but its one consumer-facing method, `internal CreateScopeFor(UserId,
  FakeTimeProvider)`, is locked down to the test assembly. It produces a DI scope primed so that
  `ICurrentUser` and `TimeProvider` resolve to the passed values inside that scope.
- **`IntegrationTestBase`** (per-test, abstract) holds the ambient-default `CurrentUserId`
  (read-only) and `FakeTimeProvider` as `protected` instance members, and provides `protected`
  `SendAsync` overloads with an optional `asUser` parameter. Time-dependent tests mutate the clock
  via the exposed `TimeProvider` (`FakeTimeProvider.SetUtcNow(...)`) directly — no wrapper helper.
  Everything except the TUnit-injected `Fixture` is `protected`/`private`, so the only surface
  visible outside an inheriting test is the `[Test]` methods. The base does not know about the
  scope-priming mechanism — it just calls `Fixture.CreateScopeFor(asUser ?? CurrentUserId, TimeProvider)`.
- **`TestScopeContext`** is an internal scoped DI service inside the fixture that carries the
  per-test state into each scope. Only the fixture's `CreateScopeFor` method writes to it; the
  test base never imports the type.

Test composition root uses `Host.CreateApplicationBuilder()` — not `WebApplicationFactory`, not
`DistributedApplicationTestingBuilder`. `AddPersistence` exposes a dual-mode entry point — either
a `connectionName` (production hosts resolve from `IConfiguration`) or a raw `connectionString`
(tests pass the container's connection string directly). Both paths run `EnrichNpgsqlDbContext`,
so test and production EF Core configurations stay aligned:

```csharp
// Fixture (session-scoped) — DI registration:
var builder = Host.CreateApplicationBuilder();
builder.Services.AddApplication();
builder.AddPersistence(connectionString: _container.GetConnectionString());
builder.Services.AddScoped<TestScopeContext>();
builder.Services.AddScoped<ICurrentUser, TestCurrentUser>();
builder.Services.AddScoped<TimeProvider>(sp =>
    sp.GetRequiredService<TestScopeContext>().TimeProvider);
var host = builder.Build();
await DatabaseMigrationRunner.RunMigrationsAsync(host.Services, CancellationToken.None);
_scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();

// Fixture — primed-scope creation:
internal IServiceScope CreateScopeFor(UserId userId, FakeTimeProvider timeProvider)
{
    var scope = _scopeFactory.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TestScopeContext>();
    context.UserId = userId;
    context.TimeProvider = timeProvider;
    return scope;
}

// IntegrationTestBase — dispatch helper (asUser overrides the ambient default for one operation):
private async ValueTask<TResponse> ExecuteInScopeAsync<TResponse>(
    UserId? asUser,
    Func<ISender, ValueTask<TResponse>> dispatch)
{
    using var scope = Fixture.CreateScopeFor(asUser ?? CurrentUserId, TimeProvider);
    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
    return await dispatch(sender);
}
```

`SendAsync` overloads fold to a single private helper that creates a scope, writes the per-test
state into the scope's `TestScopeContext`, resolves `ISender`, and dispatches via a callback —
this is necessary because `IRequest<T>`, `ICommand<T>`, and `IQuery<T>` are sibling interfaces in
Mediator (all extending `IBaseRequest`), not parent/child, so a single overload via covariance
isn't possible. The per-operation scope mirrors ASP.NET Core's per-request scoping and prevents
tests from accidentally relying on tracked entities bleeding between arrange/act/assert.

### Direct database access (arrange/assert escape hatch)

Most tests arrange through handlers and read state back through query handlers. Two needs fall
outside what the mediator surface can express:

- **Arrange** that no handler can produce in one step — seeding items with out-of-order positions,
  or another user's data.
- **Assert** on state no handler can observe — most pointedly the cascade-delete proof: once a
  shoplist is deleted, no query can reach its items, so only a direct read of the items table
  distinguishes "cascade removed them" from "left them orphaned".

`IAppDbContext` is insufficient for the assert case — it exposes only `Shoplists` and
`CurrentUserShoplists()`; child entities are reached through the aggregate by design, so there is no
`Set<ShoplistItem>()`. The concrete `AppDbContext` has it, but is `internal` to Persistence.

Mechanism:

- **`InternalsVisibleTo("Shoplists.Application.IntegrationTests")`** on `Persistence.csproj` — the
  sole sanctioned exception to the "external code uses `IAppDbContext`" layer rule (it grants *test*
  code the concrete type without widening the production surface). `AppDbContext` is already
  DI-registered as the concrete type (`AddDbContext<AppDbContext>()`, with `IAppDbContext` forwarding
  to the same scoped instance), so a test scope resolves it directly.
- **`ExecuteDbAsync` on `IntegrationTestBase`** — void and `<TResult>` overloads:

```csharp
private protected async ValueTask ExecuteDbAsync(
    Func<AppDbContext, ValueTask> action, UserId? asUser = null)
{
    using var scope = Fixture.CreateScopeFor(asUser ?? CurrentUserId, TimeProvider);
    await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}
// + a ValueTask<TResult> overload for queries
```

  Properties that matter:
  - **Fresh scope per call**, exactly like `SendAsync` — a read after a dispatched operation sees
    committed state, with no change-tracker bleed between arrange/act/assert.
  - **`private protected`**, because it surfaces the internal `AppDbContext` from a `public` base.
    Every test class lives in the same assembly, so derived-and-same-assembly is the right
    accessibility — a plain `protected` member exposing an internal type fails to compile.
  - **Unfiltered access.** `context.Set<T>()` / `context.Shoplists` bypass the `OwnerId` filter
    (which lives only in `CurrentUserShoplists()`), so assertions can see across users and prove
    orphan removal. The `asUser` priming exists only to satisfy the `AppDbContext` constructor's
    `ICurrentUser` dependency; it does not scope `Set<T>()`.

Convention: **handler-arrange and read-back-through-queries stay the default**; `ExecuteDbAsync` is
reserved for what handlers genuinely can't express or observe. Cross-user *arrange* uses
`CreateShoplist` with `asUser:` rather than DB seeding wherever the handler can express it — the seed
is only for state outside the handler vocabulary (e.g. a specific position layout).

Handler-arrange is wrapped in thin helpers on `IntegrationTestBase` (`CreateShoplistAsync`, with an
`IEnumerable<string>` items overload, and `AddItemAsync`) — each dispatches the real `Create*` handler, asserts
success once (so a broken precondition fails with a clear message, not a downstream NRE), and returns
the new id. Only *preconditions* go through helpers; the operation under test stays an explicit
`SendAsync`. Helpers are added per aggregate as duplication appears (kept inline on the base while
there is one aggregate; split into partials if/when more arrive). A single generic `ExecuteDbAsync`
covers the raw-DB side rather than typed `AddAsync`/`FindAsync`/`CountAsync` helpers.

A single generic primitive was chosen over the speculated typed `AddAsync` / `FindAsync` /
`CountAsync` helpers: one seam, and a lambda expresses any EF Core operation (`Add`, `Set<T>()`,
`CountAsync`, `AnyAsync`, …). Revisit a typed convenience layer only if call sites show enough
repetition to justify it.

## Alternatives considered

- **`WebApplicationFactory<TEntryPoint>`** — Forces the HTTP boundary on every test. Adds JSON
  serialization, model binding, status code mapping, and middleware traversal to the per-test path.
  Most of what it would cover is generic infrastructure (the mapping table in ADR 002, thin
  endpoint dispatchers that just call `sender.Send(...).ToHttpResult()`) that rarely changes.
  Rejected: wrong cost/value tradeoff at the granularity of "every handler test." If endpoint
  logic ever grows beyond thin dispatchers, a thin smoke layer can be added later — it's additive.

- **Aspire `DistributedApplicationTestingBuilder`** — The modern Aspire-recommended pattern for
  testing a distributed system through its public surface. Boots the full AppHost (Postgres
  container + DatabaseMigrator worker + API host) and exposes `HttpClient`s to each resource.
  Rejected for handler-level testing: it re-imposes the HTTP boundary (no in-process access to the
  API's `ISender`), brings up the entire stack including OIDC configuration requirements, and
  pays a heavy startup cost per assembly run. Right tool for a future end-to-end smoke suite if
  one ever materialises; wrong tool here.

- **Respawn between tests** — Familiar pattern: every test sees an empty schema. Rejected because
  it forces sequential execution at whatever granularity Respawn is applied (per-test or
  per-class), slows the loop, and adds non-trivial fixture orchestration. The user-scoped approach
  gets isolation as a free side effect of the existing authorization design without per-test
  cleanup cost.

- **Per-test transaction rollback** — Wrap each test in an outer transaction and roll back on
  teardown. Rejected: EF Core's connection-per-context model fights with sharing a transaction
  across multiple `IServiceScope` boundaries, and getting it right is fiddly. Tests also don't
  observe `COMMIT`-time behaviour (triggers, deferred constraints) — irrelevant today, but a
  silent gap.

- **Template database cloning (`CREATE DATABASE … WITH TEMPLATE`)** — Postgres-specific, fast,
  fully isolated, supports full parallelism. Most production-grade option. Rejected as overkill at
  current scale: per-test connection-string juggling adds fixture complexity with no observable
  benefit while the `OwnerId` invariant holds. Reconsider if cross-aggregate or admin-scoped
  features ever break the invariant.

- **In-memory provider / SQLite** — Already rejected in the working plan. Behavioural drift from
  real PostgreSQL (JSON, case sensitivity, RETURNING clauses, concurrency) would undermine the
  whole point. Not reconsidered.

- **Splitting `AddPersistence` into a `ServiceCollection`-shaped overload for tests** — Would let
  tests use a bare service collection. Rejected: it would also skip `EnrichNpgsqlDbContext`
  (Aspire's Npgsql resilience policies, telemetry, connection diagnostics), creating a fidelity
  gap between test and production EF Core configuration. Instead, `AddPersistence` was extended
  to accept either `connectionName` *or* `connectionString` (one nullable parameter, the other
  null) — still on `IHostApplicationBuilder`, so Aspire enrichment runs in both modes. Tests use
  the connection-string mode and skip the in-memory `IConfiguration` ceremony; production hosts
  use the connection-name mode and keep their existing one-liner. See
  `Persistence/DependencyInjection.cs` for the runtime check (throws if both or neither parameter
  is set).

## Trade-offs accepted

- **Test isolation depends on the `OwnerId` invariant.** Every read goes through
  `dbContext.CurrentUserShoplists()`; writes set `OwnerId = currentUser.UserId`. If a future
  handler bypasses this (e.g., system-level admin queries, or an entity that isn't `OwnerId`-scoped),
  tests could silently bleed across each other. Mitigation: the cross-user authorization test in
  the initial spike pins the invariant — a handler that breaks the contract also breaks that test.

- **The database grows during a test run.** No per-test cleanup means every successful insert
  remains until the container stops. Not a problem at current test counts; if the suite ever grows
  to thousands of tests, add a teardown step to truncate or recreate the database between
  assembly runs.

- **The acting user is fixed per operation, not switchable mid-request.** The test base writes the
  user (the ambient `CurrentUserId`, or an `asUser:` override) into the scope's `TestScopeContext`
  once, immediately after creating the scope. No realistic test would want mid-request user
  switching anyway.

- **Aspire enrichment runs in tests.** `EnrichNpgsqlDbContext` registers Npgsql resilience
  policies, health checks, and OTel instrumentation. Health checks register without endpoints;
  OTel sources register without exporters. Both are no-ops in this configuration. We accept this
  as fidelity (same EF Core pipeline as production) rather than carrying a separate vanilla code
  path.

## Implementation notes

- **Project**: `tests/Application.IntegrationTests/` (new sibling of `Domain.UnitTests` and
  `Application.UnitTests`). Added to `Shoplists.slnx` under `/tests/`.
- **Packages**: `TUnit`, `Testcontainers.PostgreSql`, `Microsoft.Extensions.Hosting`,
  `Microsoft.Extensions.TimeProvider.Testing` (for `FakeTimeProvider`).
- **Project references**: `Application`, `Persistence`, `Domain`, `Testing.Common`.
- **Fixture location**: lives in `Application.IntegrationTests` for now. Promote to a
  `Testing.Integration.Common` shared library only when a second integration test assembly
  materialises (YAGNI).
- **Lifecycle**: TUnit `IAsyncInitializer` + `IAsyncDisposable` on the fixture; one container per
  assembly run via `[ClassDataSource<…>(Shared = SharedType.PerTestSession)]` (or equivalent).
- **Migrations**: call `DatabaseMigrationRunner.RunMigrationsAsync(host.Services, ct)` once at
  fixture init. Same entry point as the production `DatabaseMigrator` worker — no parallel
  migration mechanism.
- **Per-operation scope + folded `SendAsync`**: three public overloads on `IntegrationTestBase`
  (`IRequest<T>`, `ICommand<T>`, `IQuery<T>`, each taking an optional `asUser`) each delegate to a
  single private `ExecuteInScopeAsync<TResponse>(UserId? asUser, Func<ISender, ValueTask<TResponse>>
  dispatch)` helper that creates the scope, populates `TestScopeContext` with the resolved `UserId`
  (`asUser ?? CurrentUserId`) + `TimeProvider`, resolves `ISender`, and invokes the callback. The
  split into overloads is required because
  Mediator's `IRequest<T>` / `ICommand<T>` / `IQuery<T>` are sibling interfaces, not a hierarchy.
- **Per-test state via instance fields, no reset hook**: `IntegrationTestBase` declares
  `CurrentUserId` and `TimeProvider` as instance properties initialized inline
  (`= NewTestUserId()` / `= new FakeTimeProvider()`). TUnit creates a fresh test class instance
  per `[Test]` method, so the initializers run anew for every test. Schema state is intentionally
  *not* reset.
- **Cross-user assertions**: tests pass `asUser:` to `SendAsync` to run an operation as a
  different user; the ambient `CurrentUserId` is the implicit default for every other call. The
  initial spike includes a cross-user test that pins the user-scoping invariant.
- **`TimeProvider`**: registered via the scoped `TestScopeContext.TimeProvider` (always a
  `FakeTimeProvider`). No production handler reads it today, but the application layer already
  injects `TimeProvider` where relevant, so the fixture is ready when time-dependent features land.
- **Postgres image pinning**: both `AppHost.cs` (`.WithImageTag("17.6")`) and the test fixture
  (`new PostgreSqlBuilder("postgres:17.6")`) pin the engine version explicitly with
  cross-reference comments, rather than relying on Aspire's bundled default. Test and production
  exercise the same Postgres major+minor; future bumps are a deliberate two-line change.
- **No NSubstitute (yet)**: a thin `TestCurrentUser : ICurrentUser` class taking
  `TestScopeContext` as a constructor dependency does the substitution. Revisit if other
  handler-level test doubles ever require a substitution framework.

### Future extension points (deliberately deferred)

- ~~**`AddAsync<T>` / `FindAsync<T>` / `CountAsync<T>` helpers on the fixture**~~ — **Realized
  2026-06-02**, as a single generic `ExecuteDbAsync` primitive rather than typed helpers. See
  *Direct database access (arrange/assert escape hatch)* under Decision above.
- **HTTP-level smoke tests via `WebApplicationFactory`** for thin per-feature endpoint coverage.
  Add only if endpoint logic ever grows beyond `sender.Send(...).ToHttpResult()`.

### Initial spike

The first integration test PR covers `CreateShoplist` with three tests, in this order:

1. **Happy path** — valid request, assert `ErrorOr.IsError == false` and DB state matches
   (`Name`, `OwnerId`, `Id`).
2. **Validation failure** — null `Name`, assert `ErrorOr.IsError == true` and
   `FirstError.Type == ErrorType.Validation`. Pins `ValidationBehavior` short-circuit through the
   real pipeline.
3. **Cross-user isolation** — the ambient default user creates a shoplist; a second user
   (`asUser:`) calls `GetShoplists` and gets an empty result. Pins the user-scoping invariant that
   the whole DB strategy depends on.

CI is already wired: `dotnet test --solution Shoplists.slnx --no-build` runs in
`.github/workflows/pr-validation.yml` and picks up the new project automatically via slnx.
GitHub-hosted `ubuntu-latest` runners ship with Docker, so Testcontainers needs no extra setup.
Observed integration test runtime in local development: ~20s, dominated by one-time container
startup.

## Re-evaluation triggers

Revisit this decision if any of the following change:

- The `OwnerId` scoping invariant weakens (a handler or query bypasses
  `dbContext.CurrentUserShoplists()`).
- A handler needs to operate on cross-user data (admin features, system queries) — the test
  isolation model would need rework.
- Container startup or test runtime becomes a CI bottleneck (current expected marginal cost on PR
  validation: ~15–20s).
- Endpoint logic grows beyond thin dispatchers, motivating HTTP-level coverage.
- Aspire publishes a leaner in-process testing primitive that doesn't require the full
  distributed test host.
- Aspire enrichment causes test-environment problems that can't be resolved by configuration —
  would motivate splitting `AddPersistence` into host and service-collection shapes.
