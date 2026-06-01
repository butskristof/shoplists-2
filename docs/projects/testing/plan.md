# Testing — Planning Notes

Living document. Captures intent, open questions, and rollout plan for the automated test suite.
Domain unit tests are now live (TUnit). Handler-level integration tests are the next major shape
to stand up. Frontend verification remains manual via Playwright MCP tooling.

When decisions stabilize (framework choice, test DB strategy), the headline ones should be captured
as ADRs under `docs/decisions/`. This file is the working plan, not a decision record.

---

## Goals

- **Safety net for refactors and schema changes.** The phase-2 projects (item metadata, realtime,
  visual overhaul) all touch areas where a silent regression would be hard to catch manually.
- **Enable a TDD workflow** for new backend handlers. The handler pattern
  (`Features/<Aggregate>/<UseCase>.cs` — nested Request, Validator, Handler returning `ErrorOr<T>`)
  is well-shaped for red-green-refactor against mediator requests.
- **Low-friction local loop.** `dotnet test` must be fast and reliable; no flaky DB setup, no
  manual fixture juggling.
- **Wire into existing CI.** `pr-validation.yml` already runs backend build + format checks —
  extend it with a test step once the suite exists.

## Non-Goals (for now)

- 100% coverage targets. Coverage is a byproduct; the aim is confidence in critical paths.
- Frontend unit tests (Vitest on components). Manual Playwright verification is adequate while
  the UI is still stabilizing. Revisit when component count + churn justifies the setup cost.
- End-to-end tests that hit the real OIDC provider. Too much ceremony for the payoff; the BFF
  proxy + OIDC flow is thin and rarely changes.
- Load / performance testing. Not a concern at current scale.

---

## Framework: TUnit

TUnit is the chosen framework. Picked for its source-generated, parallel-by-default, async-first
API which fits the bleeding-edge posture of the rest of the stack (.NET 10, Mediator source
generators, StronglyTypedId). Trade-off accepted: smaller community and less agent/MCP familiarity
than xUnit, mitigated by the fact that the framework is small and the test surface is well-defined.

Captured as [ADR 014](../../decisions/014-test-framework.md) — covers framework + MTP runner
choice, alternatives (xUnit v3 is the obvious fallback), trade-offs, and re-evaluation triggers.

### Test runner: MTP-native `dotnet test`

TUnit uses Microsoft.Testing.Platform (MTP). On .NET 10 SDK, `dotnet test` ships with a new
MTP-native mode that replaces the legacy VSTest bridge (the `TestingPlatformDotnetTestSupport`
MSBuild property is the *old* mechanism for .NET 9 SDK and earlier — Microsoft is removing it in
MTP v2 on .NET 10).

Opt-in is set once at the solution level in `backend/global.json`:

```json
{
  "sdk": { "rollForward": "latestFeature", "version": "10.0.100" },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

Canonical invocations (run from `backend/`):

```
dotnet test --solution Shoplists.slnx              # full solution
dotnet test --project tests/Domain.UnitTests/...   # single project
dotnet run --project tests/Domain.UnitTests/...    # bypass dotnet test, use the exe directly
```

Note the `--solution` / `--project` flags — required in MTP mode, unlike legacy VSTest mode where
you'd pass the path positionally. CI wiring should use the `--solution` form against
`Shoplists.slnx`.

## Conventions

These are the conventions agreed during the domain unit test bootstrap. They apply project-wide;
adjust here when patterns evolve.

- **Naming**: `Method_Scenario_ExpectedResult`. The scenario clause may be omitted when the
  expected result alone is unambiguous (`AddItem_AppendsItemToItemsCollection`). For sequence-heavy
  scenarios that don't compress into a method name, use `[DisplayName("…")]` for runner output.
- **Class layout**: one test class per *acted-upon* method on the SUT, grouped under an entity
  folder mirroring the source structure:
  `Domain.UnitTests/Models/<Feature>/<Entity>Tests/<Method>Tests.cs`. Example:
  `Models/Shoplists/ShoplistTests/AddItemTests.cs`. The "act" is the method whose behaviour the
  test characterises — prior calls (other entity methods) are arrange.
- **AAA structure**: blank lines separate Arrange / Act / Assert. No `// Arrange` comments — the
  blank-line shape carries the structure.
- **One behaviour per test**: a single `[Test]` covers one behaviour. Multiple `Assert.That` calls
  in one test are fine when they describe facets of the same behaviour ("new item has correct
  name AND correct ShoplistId"); not fine when they're really two scenarios glued together.
- **Builders**: prefer `Tests.Common` builders over hand-constructed entities. The implicit cast
  is the default form — `Shoplist sut = new ShoplistBuilder().WithName("X");` — when no
  post-construction setup is needed. Use explicit `.Build()` only when the call site needs to keep
  the builder around for further configuration.
- **Builders use the entity's domain methods** for state setup that exercises invariants (e.g.
  `sut.AddItem(...)` rather than reaching into private state). Builder helpers may be added once
  duplication shows up across tests, but should delegate to the entity rather than reimplement
  domain logic.
- **Assertions**: TUnit's built-in async assertions (`await Assert.That(value).IsEqualTo(...)`).
  No FluentAssertions / Shouldly — fewer dependencies, async-aware out of the box.
- **Parametric data**: `[Arguments(...)]` for compile-time data, `[MethodDataSource]` for complex
  data. Use parametric tests when the cases hit the same code branch with varied inputs; split
  into separate tests when cases hit different branches.
- **Shared test data**: when the same parametric set recurs across multiple tests
  (null/empty/whitespace strings, boundary integers, etc.), promote it to a custom
  `DataSourceGeneratorAttribute<T>` subclass under `Tests.Common/TestData/` with a semantic
  attribute name (e.g. `[NullEmptyOrWhitespaceStrings]`). Reads as documentation at the call site
  and centralises the data set when new edge cases are added. `Tests.Common` references
  `TUnit.Core` — the library-only package — rather than the full `TUnit` meta-package, which
  includes the runner wiring inappropriate for a shared library.
- **Test class accessibility**: `public sealed`. Public is required by TUnit; sealed follows the
  project default ("internal sealed by default; widen to public when another project needs the
  type, drop sealed only when designed for inheritance").
- **`Tests.Common` types**: `public sealed`. The whole project exists to be consumed by other test
  projects — keep types public rather than maintain `[InternalsVisibleTo]` lists.

## Scope — what kinds of tests

Leaning toward **handler-level integration tests** as the primary shape:

- Exercise the full mediator pipeline (validators + handler + EF Core) against a real PostgreSQL
  instance (Testcontainers or Aspire test host).
- Asserts on `ErrorOr<T>` outcomes and DB state.
- Skips HTTP + auth concerns — `ICurrentUser` is substituted directly.

Lighter shapes that may still be useful:

- **Pure unit tests** for `Application/Common/` helpers (validation extensions, pipeline behaviors).
- **Domain tests** for any entity method with non-trivial invariants (once the domain grows richer
  than the current CRUD-only state).

Open question: do we also test endpoints (via `WebApplicationFactory` / Aspire distributed test
host)? Probably overkill for thin dispatchers that just forward to the mediator, but revisit if
endpoint logic ever grows beyond `sender.Send(...).ToHttpResult()`.

## Test database strategy

Resolved — **Testcontainers PostgreSQL with user-scoped isolation**, one container per assembly
run, migrations applied once via `DatabaseMigrationRunner`, no per-test cleanup. Each test gets a
fresh `UserId`; the production `OwnerId` filter (`dbContext.CurrentUserShoplists()`) provides
isolation between tests as a side effect of the existing authorization design. Full alternatives
analysis (WebApplicationFactory, Aspire distributed test host, Respawn, per-test transaction
rollback, template-DB cloning, in-memory provider) in
[ADR 018](../../decisions/018-backend-integration-test-architecture.md).

---

## Priority signals (when to actually start)

Not blocking phase 2, but these projects each raise the value of having tests in place:

- **Item metadata** — schema migrations, new fields, API contract changes. Highest regression risk.
  Ideally tests land before or alongside this one.
- **Realtime updates** — new transport, cache invalidation, multi-client correctness. Tests are
  useful but the hard parts (connection lifecycle, reconnection) resist simple test coverage.
- **Visual identity** — UI-only, no backend regression risk. Tests add little value here.

Rough heuristic: if the next project touches schema or backend logic non-trivially, pause and
bootstrap the test project first.

---

## Rollout sketch

1. ~~Pick framework~~ ✓ TUnit chosen.
2. ~~Bootstrap shared `Tests.Common` builders project + first test project~~ ✓ Domain unit tests
   live in `Tests.Common` + `Domain.UnitTests`, wired into `Shoplists.slnx`.
3. ~~Opt into MTP-native `dotnet test`~~ ✓ via `backend/global.json`.
4. **Cover `Shoplist` domain methods** — `AddItem` reference test in place; `RemoveItem`,
   `MoveItem` next. (in flight)
5. ~~**Application-layer / handler tests** — stand up `Application.UnitTests` for pipeline behaviours
   and validation helpers (no DB).~~ ✓ Validation helpers + per-feature validator tests live.
   `ValidationBehavior` unit tests deliberately deferred — see Progress entry below.
6. **Handler integration tests** (in progress) — new `Application.IntegrationTests` project with
   a Testcontainers PostgreSQL fixture rooted at the mediator boundary. Prove with `CreateShoplist`
   (happy path + validation failure + cross-user isolation). Architecture captured in
   [ADR 018](../../decisions/018-backend-integration-test-architecture.md).
7. ~~**Write the framework + fixture ADR(s)**~~ ✓ Two ADRs:
   [ADR 014](../../decisions/014-test-framework.md) (TUnit + MTP runner) and
   [ADR 018](../../decisions/018-backend-integration-test-architecture.md) (integration test
   architecture: boundary, DB strategy, `ICurrentUser` substitution).
8. ~~**Extend `.github/workflows/pr-validation.yml`**~~ ✓ Already wired —
   `dotnet test --solution Shoplists.slnx --no-build` picks up new test projects automatically
   via slnx; GitHub-hosted `ubuntu-latest` runners ship with Docker for Testcontainers.
9. ~~Backfill tests for existing handlers.~~ ✓ Done as a deliberate pass (2026-06-02) rather than
   opportunistically — all ten handlers now have integration coverage. See Progress entry.
10. Adopt TDD-first for new handlers going forward. **← now the active mode.**

---

## Progress

- **2026-06-02** — Integration coverage backfilled across all existing handlers + direct-DB escape
  hatch added.
  - Went past the `CreateShoplist` spike to cover every handler: `GetShoplists`, `GetShoplist`,
    `UpdateShoplist`, `DeleteShoplist`, and all five item handlers (`CreateShoplistItem`,
    `UpdateShoplistItem`, `UpdateShoplistItemFulfilled`, `UpdateShoplistItemPosition`,
    `DeleteShoplistItem`). One file per use case, item handlers nested under `Features/Shoplists/Items/`
    mirroring source. ~32 new tests (≈43 total). All green; CI picks them up via slnx.
  - **Infra — direct DB access for arrange/assert.** Some scenarios can't go through the mediator
    surface: seeding out-of-order positions or other users' data, and *verifying* state no handler
    can observe (the cascade-delete proof — after a list is deleted, no query can reach its orphaned
    items). Added:
    - `InternalsVisibleTo("Shoplists.Application.IntegrationTests")` on `Persistence.csproj` so tests
      can resolve the concrete (internal) `AppDbContext` and reach `Set<ShoplistItem>()` (unreachable
      via `IAppDbContext`, which exposes only `Shoplists` + `CurrentUserShoplists()`). Sanctioned
      test-only access; the production rule (external code uses `IAppDbContext`) still holds.
    - `ExecuteDbAsync(Func<AppDbContext, ValueTask>[, asUser])` + `<TResult>` overload on
      `IntegrationTestBase` — fresh scope per call (mirrors `SendAsync`), so reads reflect committed
      state with no change-tracker bleed. `private protected` because it surfaces the internal
      `AppDbContext` from a public base. Access is UNFILTERED (`Set<T>()` / `Shoplists` bypass the
      `OwnerId` filter, which lives only in `CurrentUserShoplists()`); `asUser` priming just satisfies
      the ctor.
  - **Conventions settled during the backfill** (now also in `AGENTS.md` → Backend Conventions →
    Testing):
    - **Boundary-per-handler isolation, validation-once.** The cross-user `NotFound` boundary is
      asserted on every id-taking handler (each calls `CurrentUserShoplists()` independently, so a
      regression in one wouldn't be caught by testing another). The validation-short-circuit /
      persists-nothing characterisation stays a single representative test in `CreateShoplistTests`;
      per-rule shape validation is already covered by `Application.UnitTests` validators.
    - **Handler-arrange by default**, reading state back through query handlers (`GetShoplist` /
      `GetShoplists`). `ExecuteDbAsync` reserved for what handlers can't express/observe. Cross-user
      arrange uses `CreateShoplist` with `asUser:` rather than DB seeding, where the handler can
      express it.
    - **Inline arrange asserts dropped.** Verified `ErrorOr<T>.Value` returns `default` (not a throw)
      on an error state, so a broken arrange surfaces via the subsequent `.Value` use; tests assert
      only their own responsibility, not the success of arrange steps.
  - **Arrange helpers extracted** (same session, after test review): `CreateShoplistAsync`,
    `AddItemAsync`, `CreateShoplistWithItemsAsync` on `IntegrationTestBase` — handler-based (dispatch
    the real `Create*` handler), assert success once, return the id. All tests refactored onto them
    (~20× inline arrange removed). Principle held: only *preconditions* use helpers; the act under
    test stays an explicit `SendAsync` (so `CreateShoplistItemTests` keeps `CreateShoplistItem`
    inline as its SUT). Kept inline on the base for now (one aggregate); split into partials if/when
    more aggregates bring their own helpers. Convention captured in `AGENTS.md` + ADR 018.

- **2026-05-31** — Integration test user-acting surface simplified to a per-send override.
  - Replaced the mutable `SetUserId(...)` / `private set` `CurrentUserId` pair with a read-only
    ambient-default `CurrentUserId` (still a fresh id per test instance) plus an optional
    `asUser` parameter on every `SendAsync` overload (`asUser ?? CurrentUserId` inside the
    dispatch helper). Single-user tests stay zero-ceremony (never mention a user); cross-user
    tests express the deviation locally and immutably at the call site instead of juggling an
    ambient "current actor" across statements.
  - Cross-user test renamed `Shoplist_IsNotVisibleToOtherUsers` — the old `...ByUserA...UserB`
    name implied an A/B notion the code no longer carries; there's just the default user and
    `asUser:` for another.
  - `TestScopeContext` / `CreateScopeFor` channel unchanged — `asUser` flows into the existing
    `userId` argument. [ADR 018](../../decisions/018-backend-integration-test-architecture.md)
    updated in place (choice 3, composition shape, trade-offs, impl notes, spike).
  - **Still parked** for a follow-up pass: the three `SendAsync` overloads + the
    `ExecuteInScopeAsync` callback indirection, and dropping the dead `IRequest<T>` overload (no
    handler uses it — all are `ICommand`/`IQuery`). Doc/ADR update for those lands with that pass.

- **2026-05-15** — Handler integration test architecture decided and shipped.
  - Architecture session worked through the three coupled choices (boundary, DB strategy,
    `ICurrentUser` substitution) and resulting fixture shape. Outcome captured in
    [ADR 018](../../decisions/018-backend-integration-test-architecture.md).
  - **Boundary**: mediator-rooted (no HTTP). Tests dispatch Request records through `ISender`
    against the real production DI graph (`AddApplication()` + `AddPersistence()`), so the full
    pipeline (`ValidationBehavior`, logging, auto-registered validators) is exercised. HTTP
    concerns are treated as transport.
  - **DB strategy**: Testcontainers PostgreSQL, one container per assembly run. Migrations
    applied once via the existing `DatabaseMigrationRunner` (same entry point the production
    worker uses). **No per-test cleanup** — each test gets a fresh `UserId`, and the production
    `OwnerId` filter (`dbContext.CurrentUserShoplists()`) provides isolation between tests as a
    side effect of the existing authorization design. Full parallelism falls out.
  - **Per-test state lives on the test base class as instance fields**, not on the session
    fixture and not via `AsyncLocal`. TUnit instantiates the test class fresh for every `[Test]`
    method, so initializers like `CurrentUserId = NewTestUserId()` run anew per test —
    parallel-safe by construction without any reset hook. A scoped `TestScopeContext` carries the
    current state into each operation's DI scope; `TestCurrentUser` and the `TimeProvider`
    factory read it.
  - **Composition root**: `Host.CreateApplicationBuilder()`. `AddPersistence` was extended with a
    dual-mode entry point — `connectionName` (production hosts resolve from `IConfiguration`) or
    raw `connectionString` (tests pass the Testcontainers connection string directly). Both paths
    run Aspire's `EnrichNpgsqlDbContext`, so production and test EF Core configurations stay
    aligned. Test fixture skips the previously-needed in-memory `IConfiguration` ceremony.
  - **Postgres image pinning**: both `AppHost.cs` (`.WithImageTag("17.6")`) and the test fixture
    (`new PostgreSqlBuilder("postgres:17.6")`) pin the engine version explicitly with
    cross-reference comments, rather than relying on Aspire's bundled default. Same Postgres
    major+minor on both sides; future bumps are a deliberate two-line change.
  - **`SendAsync` folded**: three public overloads on `IntegrationTestBase` (`IRequest<T>`,
    `ICommand<T>`, `IQuery<T>`) delegate to one private `ExecuteInScopeAsync` helper that creates
    the scope, populates `TestScopeContext`, resolves `ISender`, and invokes a dispatch callback.
    Three overloads are needed because Mediator's `IRequest<T>` / `ICommand<T>` / `IQuery<T>` are
    sibling interfaces, not a hierarchy.
  - **Scope model**: per-operation scope. Each `SendAsync` creates and disposes its own
    `IServiceScope` — mirrors ASP.NET Core's per-request scoping and prevents tracked entities
    from bleeding between arrange/act/assert.
  - **`FakeTimeProvider`** wired from the start (registered as `TimeProvider`), so the fixture is
    ready when time-dependent features land.
  - **Spike scope** (initial PR): three tests on `CreateShoplist` — happy path, validation
    failure, cross-user isolation. CI was already wired (`dotnet test --solution Shoplists.slnx`
    in `pr-validation.yml`) — no workflow change required; the new project is picked up via the
    slnx and GitHub-hosted runners ship with Docker for Testcontainers.
  - **Deferred extension points** documented in ADR: `AddAsync<T>` / `FindAsync<T>` /
    `CountAsync<T>` fixture helpers (add when a test needs them), HTTP-level smoke tests via
    `WebApplicationFactory` (add only if endpoint logic grows beyond thin dispatchers), and
    NSubstitute (a 10-line `TestCurrentUser` class is enough for now).

- **2026-04-29** — Application-layer unit tests bootstrap.
  - `Application.UnitTests` project (added to slnx earlier) is now populated. 88 test cases across
    12 files: `Common/Validation/FluentValidationExtensionsTests/` (3 files covering
    `NotNullWithErrorCode`, `NotEmptyWithErrorCode`, `ValidString`) plus per-feature validator
    tests for every use case that has a validator (8 of 9 — `GetShoplists` has no inputs).
  - **Assertion style**: `FluentValidation.TestHelper` (`TestValidateAsync` +
    `ShouldHaveValidationErrorFor` / `WithErrorMessage`). The package is part of the main
    `FluentValidation` NuGet, available transitively via `FluentValidation.DependencyInjectionExtensions`
    in Application — no new dep added. Error codes are matched via `WithErrorMessage` (not
    `WithErrorCode`) because the rules use `WithMessage(ErrorCodes.X)`.
  - **Internals visibility**: `Application.csproj` now exposes internals to
    `Shoplists.Application.UnitTests` via `<InternalsVisibleTo>` so the `internal sealed` validators
    and `BaseValidator<T>` are reachable without widening their accessibility.
  - **Validator test conventions**: one test class per `<UseCase>.Validator`, named
    `<UseCase>ValidatorTests.cs`, mirroring source layout (`Features/Shoplists/`,
    `Features/Shoplists/Items/`). SUT instantiated as a private readonly field. AAA structure with
    blank-line separators. Reuses `[NullEmptyOrWhitespaceStrings]` for string-required cases and
    `[Arguments(...)]` for boundary integers. Validators with multiple `RuleFor`s have an
    `AllFieldsNull` / `BothNull` test pinning class-level cascade Continue.
  - **Cascade pinning**: `UpdateShoplistItemPositionValidatorTests.Position_Null_OnlyEmitsRequiredNotInvalid`
    is the only test that explicitly pins `RuleLevelCascadeMode = Stop` — the only validator where
    cascade is observable (chained `NotNull` + `GreaterThanOrEqualTo` on the same property). It
    reaches into `result.Errors` directly and asserts a single error message rather than relying
    on TestHelper's `.Only()`, which pins "only this property fails" not "only one error".
  - **`ValidationBehavior` — unit tests deferred** (explicitly): every observable branch is
    exercised by integration tests (no-validators path via `GetShoplists`, pass-through on every
    happy path, short-circuit + `(dynamic)errors` conversion on every "invalid request" case).
    The cleverness budget is one line, on a stable `ErrorOr` API. Revisit if (a) multiple
    validators are ever registered for one request — currently 1:1 by source-gen — or (b) the
    dynamic-dispatch line is refactored.
  - All 139 tests passing (51 domain + 88 application). `dotnet build`, `dotnet csharpier check`,
    `dotnet format style/analyzers --verify-no-changes` all clean.

- **2026-04-29** — Domain follow-ups: aggregate-boundary + position invariant.
  - `ShoplistTests/ItemsTests` (1 test) — pins `Items` as a read-only view: mutation through
    `(ICollection<ShoplistItem>)sut.Items` throws `NotSupportedException`. Defends the aggregate
    boundary against a refactor that returns the internal `List<T>` directly.
  - `ShoplistTests/AddItemTests` — added `AddItem_AssignsContiguousPositionsFromOne(count)`
    parametric (1, 3, 7) asserting `sut.Items.Select(i => i.Position)` is a permutation of
    `1..count`. Closes a real gap: the existing `HasPositionMaxPlusOne` only pinned the *new*
    item's position, so an impl that mutated existing items' positions would have passed.
  - `ShoplistTests/RemoveItemTests` — added `RemoveItem_LeavesContiguousPositionsFromOne` (4 items,
    mid-removal). Mostly declarative — the existing mid + last tests already pin every remaining
    position by value; this restates the invariant explicitly.
  - **MoveItem skipped intentionally**: the existing Down/Up tests already assert every item's
    position after a reindex, which IS the permutation invariant. Adding a dedicated invariant
    test would be strictly redundant.
  - All 51 tests passing; build, CSharpier, format style, format analyzers all clean.

- **2026-04-28** — Domain test bootstrap.
  - `Tests.Common` project with `ShoplistBuilder` (public sealed; implicit cast to `Shoplist`).
  - `Domain.UnitTests` project covering `Shoplist` construction, all three mutation methods, and
    `Name` setter validation on both entities (27 test methods, 38 cases including `[Arguments]`
    parametrics):
    - `ShoplistTests/ConstructorTests` (3 tests, 5 cases) — non-empty self-generated Id, name
      validation at construction time, name trimming at construction time.
    - `ShoplistTests/AddItemTests` (8 tests, 12 cases) — first-position, max+1 reindex, collection
      append, name + ShoplistId initialization, non-empty ShoplistItem Id, name validation, name
      trimming, default `IsFulfilled = false`.
    - `ShoplistTests/RemoveItemTests` (5 tests) — unknown id throws, removal from collection,
      only-item edge, mid-list reindex, last-item edge.
    - `ShoplistTests/MoveItemTests` (7 tests, 8 cases) — unknown id throws, out-of-range returns,
      same-position no-op, move-down and move-up reindex.
    - `ShoplistTests/NameTests` (2 tests, 4 cases) — `Shoplist.Name` setter validation and
      trimming post-construction. Pins setter behaviour against a hypothetical refactor that
      moved validation into a constructor.
    - `ShoplistItemTests/NameTests` (2 tests, 4 cases) — same shape for `ShoplistItem.Name`
      (mutated through an item obtained via `Shoplist.AddItem`, since the ctor is internal).
  - First entry under `Tests.Common/TestData/`: `[NullEmptyOrWhitespaceStrings]` attribute,
    consumed by all four name-validation tests in place of three repeated `[Arguments]` lines.
  - Conventions agreed and documented above.
  - `backend/global.json` extended with `test.runner` to enable MTP-native `dotnet test`.
  - Verified: `dotnet test --solution Shoplists.slnx --no-build` runs the full suite (38/38
    passing).
  - **Domain changes** (driven by the test bootstrap):
    - `Shoplist.Id` and `ShoplistItem.Id` now self-generate via `<TypeId>.New()` initializers.
      Previously they defaulted to `Guid.Empty` and relied on EF Core to fill in identity on
      insert — fine in production but unworkable in domain unit tests (every in-memory item
      collided on Id).
    - `Shoplist.Name` and `ShoplistItem.Name` use C# 14 `field`-backed setters with
      `ArgumentException.ThrowIfNullOrWhiteSpace(value)` and store the trimmed canonical value.
      Defense in depth alongside FluentValidation at the App layer.
    - `ShoplistItem` parameterless constructor is now `internal` — child entities can only be
      created from inside the Domain assembly. External callers go through `Shoplist.AddItem(...)`.
    - All three conventions captured in `CLAUDE.md` under Domain Entity Conventions.
  - **Outstanding**: `global.json` is not yet listed under `Solution Items` in `Shoplists.slnx`.
    The dotnet CLI's `dotnet sln add` only accepts projects, not loose files — adding it requires
    a manual slnx edit. Decide whether to add it (IDE-visibility convenience) or leave it
    file-system-only.

---

## Open questions

- Frontend tests at all? If yes, Vitest for composables, Playwright for critical user flows. Defer.
- Coverage reporting — needed? If yes, Coverlet + ReportGenerator is the standard .NET path.

Resolved (see [ADR 018](../../decisions/018-backend-integration-test-architecture.md)):
Testcontainers vs Aspire test host; `ICurrentUser` substitution mechanism.
