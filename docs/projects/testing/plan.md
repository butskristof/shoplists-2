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

**Action item**: write an ADR capturing this decision now that the first test project exists.

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

Open question. Options:

- **Testcontainers PostgreSQL** — one container per test run, fresh DB, fast with layer caching.
  Standard approach.
- **Aspire distributed application testing** — spin up the real AppHost in a test host. Closer to
  production shape, more setup, slower.
- **In-memory provider / SQLite** — rejected. Behavioural drift from real PostgreSQL (JSON, case
  sensitivity, concurrency) would undermine the whole point.

Lean: Testcontainers. Revisit if Aspire test host proves ergonomic enough.

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
5. **Application-layer / handler tests** — stand up `Application.UnitTests` for pipeline behaviours
   and validation helpers (no DB).
6. **Handler integration tests** — new project + Testcontainers PostgreSQL fixture. Prove with one
   handler (e.g. `CreateShoplist` happy path + validation failure).
7. **Write the framework + fixture ADR(s)** — TUnit decision; `dotnet test` opt-in mechanism;
   Testcontainers vs Aspire test host (when picked).
8. **Extend `.github/workflows/pr-validation.yml`** with `dotnet test --solution Shoplists.slnx`
   once the suite is stable and fast enough to not block PRs.
9. Backfill tests for existing handlers opportunistically when touching them. Don't block on a
   coverage pass.
10. Adopt TDD-first for new handlers going forward.

---

## Progress

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

- Testcontainers vs Aspire test host for handler integration tests.
- How to handle `ICurrentUser` in tests — static substitution or a builder pattern.
- Frontend tests at all? If yes, Vitest for composables, Playwright for critical user flows. Defer.
- Coverage reporting — needed? If yes, Coverlet + ReportGenerator is the standard .NET path.
