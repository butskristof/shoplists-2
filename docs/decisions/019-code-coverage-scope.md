# Code coverage scope

**Status**: Decided

## Context

PR validation now collects code coverage (`--coverage`, Cobertura) and renders it into the PR via
ReportGenerator (see the `backend` job in [`.github/workflows/pr-validation.yml`](../../.github/workflows/pr-validation.yml)).
Once the report was visible, two things stood out:

1. **`Shoplists.Domain` reported ~29% coverage** despite thorough domain unit tests. Investigation
   showed the *real* domain code is fully covered — the `Shoplist` and `ShoplistItem` entities are
   ~100%. The low number came entirely from `[StronglyTypedId]`-generated boilerplate on the ID
   structs (parse/format members plus the nested `…SystemTextJsonConverter` / `…TypeConverter`,
   mostly 0%). That code carries `[GeneratedCode]` but **not** `[ExcludeFromCodeCoverage]`, so the
   collector counts it. The same class of noise appeared as `Mediator.*` registration types
   compiled into `Shoplists.Application`.
2. **`Shoplists.Testing.Common` was counted** (~66%). It is test scaffolding (builders, data
   attributes) consumed by the test projects, not a system under test.

So a decision is needed on *what* counts toward coverage and, equally important, *where* that scope
is defined — we want one place to change it, not exclusion logic layered across collection and
reporting.

## Decision

**Coverage scope is defined in a single file: [`backend/tests/code-coverage.settings.xml`](../../backend/tests/code-coverage.settings.xml)** —
a Microsoft.Testing.Platform code-coverage settings file applied via the `coverage-settings` option
on the test run. The CI workflow applies **no** report-level filters; ReportGenerator renders
whatever the collector produced.

The settings file excludes:

- **Generated code, by the `GeneratedCodeAttribute`.** This is the precise, intent-matching signal —
  it drops StronglyTypedId IDs + their converters, Mediator registrations, and EF Core migrations
  uniformly, in any assembly. We deliberately do **not** exclude `CompilerGeneratedAttribute`: per
  the [Microsoft docs](https://learn.microsoft.com/visualstudio/test/customizing-code-coverage-analysis#include-or-exclude-assemblies-and-members),
  that would also drop `async`/`await`, `yield`, lambdas, and auto-properties from *real* methods.
- **The `Shoplists.Testing.Common` module**, via a `ModulePaths` exclude — test utilities, not a SUT.
- The standard `DebuggerHidden` / `DebuggerNonUserCode` / `ExcludeFromCodeCoverage` attributes
  (sensible defaults, kept explicit since supplying a settings file replaces the implicit set).

The file is registered as a solution item under `/tests/` in `Shoplists.slnx`.

The collector excludes test assemblies themselves by default
(`IncludeTestAssembly` is `false` in `Microsoft.Testing.Extensions.CodeCoverage`), so the unit/
integration test projects need no explicit entry.

## Alternatives considered

- **ReportGenerator filters in the workflow YAML** (`classfilters: +Shoplists.*`,
  `filefilters: -**/*.g.cs`). This is how the feature first shipped (filtering `Mediator.*` via a
  namespace whitelist). Rejected as the permanent home because: (a) it filters the *report* only —
  the raw Cobertura **artifact** stays noisy and inconsistent with the comment/summary; (b) it
  relies on namespace whitelists and `.g.cs` path heuristics rather than the precise `[GeneratedCode]`
  signal; and (c) it splits exclusion logic across two places, so "what do I touch to change scope?"
  has more than one answer. The `classfilters` line was removed when this file landed.
- **Annotating generated code with `[ExcludeFromCodeCoverage]`.** Not controllable: StronglyTypedId
  and Mediator are source generators with no option to emit it, and hand-editing generated output
  is not possible.

## Consequences

- `Shoplists.Domain` now reflects hand-written entities (~100%); coverage across all assemblies
  drops generated and migration noise. The numbers describe code we actually own and test.
- A new source generator that stamps `[GeneratedCode]` is excluded automatically — no action needed.
- A new **support/test-only assembly** (another `Testing.*` library) must be added to the
  `ModulePaths` exclude list here, or it will be counted.
- The settings path is passed **absolute** in CI: `--solution` launches each test app in its own
  working directory, so a relative path would not resolve consistently.
