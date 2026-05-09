# Use the Aspire CLI to upgrade Aspire versions

**Status**: Decided

## Context

The Aspire AppHost project pins two coupled version references that must move
together:

- The **MSBuild SDK version** in `<Project Sdk="Aspire.AppHost.Sdk/X.Y.Z">` —
  not a NuGet PackageReference, invisible to package managers like Rider's UI.
- The **`Aspire.*` NuGet packages** in `Directory.Packages.props` (CPM is on).

Bumping only the packages and forgetting the SDK line (or vice versa) yields a
mismatched build that may succeed locally but drift in CI.

## Decision

Use the **Aspire CLI's `aspire update` command** as the canonical upgrade path
for both minor (e.g. 13.2 → 13.3) and major Aspire version bumps. It is the path
Microsoft documents and the only one that handles the SDK version line, the CPM
package versions, and dependency resolution in a single coordinated step.

Non-Aspire packages (EF Core, Mediator, FluentValidation, etc.) are upgraded
separately through normal package-manager workflows — `aspire update` does not
touch them.

## Alternatives considered

- **Manual edits in Rider / `dotnet add package`** — Rider's package UI doesn't
  see the `Aspire.AppHost.Sdk/X.Y.Z` line in the `<Project>` element, so the SDK
  version silently lags behind the packages. Rejected as error-prone.
- **.NET Upgrade Assistant** — Microsoft's general-purpose porting tool. Aimed
  at major version upgrades (e.g. 9.x → 13.0) and brings extra ceremony for the
  routine minor bumps we expect to do. Aspire CLI is the recommended path going
  forward.

## Trade-offs accepted

- **`aspire update` is marked Preview** in the CLI command table (vs Stable for
  `aspire run` etc.). Working well in practice; review the diff after each run.
- The CLI may collapse multi-line elements in `AppHost.csproj` (e.g. wrapped
  `ProjectReference`). Purely cosmetic — re-wrap by hand if desired.

## Implementation notes

Procedure (run from repo root):

```bash
# 1. Update the CLI itself
aspire update --self

# 2. Update the project — interactive by default; flags below skip prompts
aspire update \
  --apphost backend/AppHost/AppHost.csproj \
  --channel stable \
  --nuget-config-dir backend \
  -y

# 3. Review what changed and validate
git diff backend/
dotnet build backend/Shoplists.slnx
```

- **`--nuget-config-dir backend`** — first run materialises `backend/nuget.config`
  pinning nuget.org and clearing inherited package sources, for reproducibility.
  Subsequent runs reuse it; the flag becomes optional once the file is committed.
- **`--channel stable`** — defaults to stable; pass `daily` only when chasing a
  pre-release fix (and revert afterwards).
- **`-y`** — auto-confirms the update prompts. Skip the flag (and run in a real
  terminal) for an interactive review of each proposed change before applying.
- **SDK simplification side-effect**: starting from Aspire 13.0 the
  `Aspire.AppHost.Sdk` bundles `Aspire.Hosting.AppHost`. Running `aspire update`
  on a project that still references it explicitly will remove the now-redundant
  PackageReference and PackageVersion entries.

## Re-evaluation triggers

Revisit if `aspire update` graduates to Stable and changes its prompting or
flag surface, or if a minor bump introduces breaking changes that the CLI
doesn't help mediate.
