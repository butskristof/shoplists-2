# Use TUnit + Microsoft.Testing.Platform for backend tests

**Status**: Decided

## Context

Backend tests are needed before the Phase 2 projects (item metadata, realtime) introduce
non-trivial regression risk. Picking a framework now also unblocks TDD on new handlers.

Constraints driving the choice:

- **Bleeding-edge .NET 10 stack** with source generators throughout (Mediator, StronglyTypedId).
  Tooling that integrates cleanly with source generators and modern SDK features is preferred.
- **Async-heavy code** — handlers are `ValueTask`-returning, EF Core is async, mediator pipeline
  is async. The framework's assertion API should be async-first, not retrofitted.
- **MTP-first posture** — Microsoft.Testing.Platform is the strategic .NET test runner; the legacy
  VSTest bridge (`TestingPlatformDotnetTestSupport`) is being removed in MTP v2 on .NET 10.
  Aligning with MTP now avoids a future migration.

## Decision

Two coupled choices:

1. **[TUnit](https://github.com/thomhurst/TUnit)** as the test framework.
2. **Microsoft.Testing.Platform (MTP) via .NET 10 SDK's `dotnet test`** as the runner, opted in
   per-solution via `test.runner` in `backend/global.json`.

The choices are paired: TUnit is MTP-native, and the .NET 10 SDK ships first-class MTP-native
`dotnet test` — opting into one makes the other the natural fit.

## Alternatives considered

- **xUnit v3** — strongest alternative. MTP-native, modernized API, much larger community than
  TUnit. Lost on: TUnit's source-generated tests, parallel-by-default execution, and richer
  built-in async assertion API. xUnit v3 remains the obvious migration target if TUnit hits a wall.
- **xUnit v2** — industry default, highest agent/MCP familiarity. Rejected: not async-first by
  design; VSTest-default; doesn't fit the MTP posture.
- **NUnit** — mature, MTP-supported. Heavier ceremony, less idiomatic in modern minimal-API
  projects. Brings nothing xUnit doesn't.
- **MSTest** — Microsoft's canonical MTP framework. Verbose assertions, less idiomatic in
  community projects. No clear win over xUnit.

## Trade-offs accepted

- **Smaller community than xUnit/NUnit/MSTest** — fewer Stack Overflow answers, less agent/MCP
  familiarity. Mitigated by: small framework surface, narrow test scope (domain + handlers),
  and direct access to authoritative TUnit docs via context7.
- **TUnit-specific assertion API** — moderate switching cost if we ever migrate. Acceptable given
  the test surface size.

## Implementation notes

- `test.runner` set in `backend/global.json` enables MTP-native `dotnet test`. In MTP mode the
  CLI requires `--solution` / `--project` flags (unlike the legacy VSTest mode which accepted a
  positional path).
- Canonical invocations (run from `backend/`):
  ```
  dotnet test --solution Shoplists.slnx              # full solution
  dotnet test --project tests/Domain.UnitTests/...   # single project
  dotnet run --project tests/Domain.UnitTests/...    # bypass dotnet test, use the exe directly
  ```
- `Tests.Common` references `TUnit.Core` (the library-only package), **not** the full `TUnit`
  meta package. The meta package brings runner wiring inappropriate for a shared library.
- Test classes are `public sealed` — public is required by TUnit, sealed follows the project
  default.

## Re-evaluation triggers

Revisit this decision if any of the following change:

- TUnit becomes unmaintained or breaks compatibility with a newer .NET SDK.
- Recurring tooling friction surfaces in CI or IDE integration that isn't resolvable upstream.
- A need arises to integrate with .NET test ecosystem tooling that doesn't support TUnit.
