# Testing — Planning Notes

Living document. Captures intent, open questions, and rollout plan for introducing an automated
test suite to Shoplists. Currently the repo has zero automated tests — verification is manual via
Playwright MCP tooling.

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

## Framework lean: TUnit

TUnit is the current lean (flagged in an earlier version of CLAUDE.md). Reasons to confirm or
reconsider during setup:

- **Pro TUnit**: modern, source-generated, fits the bleeding-edge posture of the rest of the stack
  (.NET 10, Mediator source generators, StronglyTypedId). Parallel-by-default, async-first API.
- **Pro xUnit**: safer default, larger community, more MCP/agent familiarity, more examples in
  Microsoft docs.
- **Decision should land as an ADR** once the first test project exists — capture the trade-off
  taken and the reasoning at that moment.

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

1. Pick framework, create `backend/tests/Application.Tests/` (or similar) project, wire into
   `Shoplists.slnx`.
2. Stand up Testcontainers PostgreSQL fixture. Prove with one handler test (e.g. `CreateShoplist`
   happy path + validation failure).
3. Write ADR capturing framework choice + fixture approach.
4. Extend `.github/workflows/pr-validation.yml` with a test step (only after the suite is stable
   and fast enough to not block PRs).
5. Backfill tests for existing handlers opportunistically when touching them. Do not block on a
   coverage pass.
6. Adopt TDD-first for new handlers going forward.

---

## Open questions

- TUnit vs xUnit — confirm at setup time.
- Testcontainers vs Aspire test host.
- How to handle `ICurrentUser` in tests — static substitution or a builder pattern.
- Frontend tests at all? If yes, Vitest for composables, Playwright for critical user flows. Defer.
- Coverage reporting — needed? If yes, Coverlet + ReportGenerator is the standard .NET path.
