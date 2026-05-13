# Manage npm transitive-dependency overrides explicitly

**Status**: Decided

## Context

`npm audit` periodically surfaces vulnerabilities in **transitive** npm
dependencies — packages we never list in `package.json` but that get pulled in
by something we do. Two recent examples:

- `serialize-javascript@6.0.2` (vulnerable, RCE + DoS) reached via
  `@vite-pwa/nuxt → vite-plugin-pwa → workbox-build → @rollup/plugin-terser@0.4.4`,
  which declares `serialize-javascript: ^6.0.1`. The 6.x line was never patched
  — the fix only landed in 7.0.5+.
- `protobufjs@8.0.1` (vulnerable, prototype pollution + code injection + DoS)
  reached via `@opentelemetry/sdk-node → @opentelemetry/otlp-transformer@0.217.0`,
  which **pins `protobufjs: '8.0.1'` exactly** (no caret).

`npm audit fix` does not help in either case: the vulnerable version is locked
in by the parent's manifest, not by our top-level deps. `npm audit fix --force`
"resolves" the protobufjs case by **downgrading** `@opentelemetry/sdk-node`
from `0.217.0` to `0.215.0` (the last version whose transformer used a caret
range on `protobufjs`). That is a silent regression of a direct dependency, not
a fix.

The remaining options are:

1. Wait for the parent to release a fixed version, then bump.
2. Force a fixed transitive version with an `overrides` entry in `package.json`.

Option 2 is appropriate when the upstream advisory **is already patched**
(i.e. a fixed transitive version exists and is wire-compatible) but the parent
hasn't shipped an updated dependency range yet. Option 1 is preferred when no
fix exists upstream, or when the override would have to fork an unreleased
patch.

A previous override (`serialize-javascript: ^7.0.5`, PR #54, April 2026) was
added without any in-repo breadcrumb. Six months later it was not obvious
whether it was still doing anything, what advisory had motivated it, or under
what condition it should be removed. A verification pass while introducing
this ADR confirmed the override is in fact load-bearing (without it
`serialize-javascript@6.0.2` re-enters the tree). That's the failure mode this
ADR is designed to prevent: undocumented overrides that nobody dares touch.

## Decision

We use the `overrides` field in `frontend/package.json` to force fixed
transitive versions when, and only when, all of the following hold:

1. **The advisory is real for this project's usage** — i.e. the vulnerable
   code path is actually reachable. For libraries used only on the Nitro
   server with no attacker-controlled input (e.g. outbound OTLP export), we
   still patch, but the urgency is "next deps pass", not "drop everything".
2. **A fixed version exists** that is wire-compatible with the version the
   parent depends on. For caret-pinned advisories, picking the minimum
   patched version (e.g. `^8.0.3` rather than `^8.2.0`) keeps the delta small.
3. **The override is registered** in `docs/security/overrides.md` with the
   full metadata listed below.
4. **A breadcrumb** is added to `package.json` immediately above the
   `overrides` block (npm rejects `//`-prefixed keys *inside* `overrides`
   because it parses them as package names, so the breadcrumb lives one level
   up):

   ```json
   "//overrides": "See docs/security/overrides.md for advisory IDs, parent deps, and removal conditions.",
   "overrides": {
     "protobufjs": "^8.0.3"
   }
   ```

Every override entry in the registry must include:

- **Package** and pinned range.
- **Advisory ID(s)** (GHSA / CVE) being addressed.
- **Parent dependency** that pulls in the vulnerable version, and *why* the
  parent can't be bumped instead (exact pin? not yet released? would force a
  major-line move?).
- **Date added** and the version of the parent dep at that time, so a future
  reader can tell whether enough has changed upstream to retry.
- **Removal condition** — the specific upstream change that makes the override
  redundant (e.g. "remove when `@opentelemetry/otlp-transformer` ships a
  version that depends on `protobufjs` >= 8.0.3").

### Review trigger

The registry is re-read on every dependency-upgrade pass and on every
`npm audit` cleanup. For each entry, verify whether the removal condition has
been met by **temporarily removing the override, running `npm install` and
`npm audit`, and inspecting the resolved tree** — do not rely on theoretical
range resolution. If the entry is still load-bearing, leave it; if not, remove
it from both `package.json` and the registry in the same commit.

This trigger is codified in `CLAUDE.md` so the agent applies it too.

### What we don't do

- **`npm audit fix --force`** — never. It can silently downgrade direct
  dependencies and is not auditable from a code-review diff.
- **Overrides without a registry entry** — leaves the next maintainer with
  the same problem PR #54 left.
- **`overrides` for non-security reasons** (e.g. forcing a newer version of a
  library for a feature). That belongs in a regular dependency upgrade, not in
  this mechanism.

## Alternatives considered

- **Wait for upstream fixes only.** Cleanest, but leaves known-vulnerable
  code in the tree for as long as the parent dep takes to release. For the
  `serialize-javascript` case that was over a year of waiting on
  `@rollup/plugin-terser@0.4.4` (still unfixed). Rejected as default.
- **Renovate / Dependabot with auto-merge.** Helps keep parent deps moving,
  but does not solve transitive pins. We can layer it on later if manual deps
  passes start lagging; it is not a substitute for the registry. Out of scope
  for this ADR.
- **Forking the vulnerable transitive dep.** Last resort if no upstream patch
  exists. Not currently needed; would warrant its own ADR.
- **Inline JSON5-style comments in `package.json`.** Not supported by npm
  parsers. The `"//"`-prefixed-key trick is the closest valid approximation.

## Trade-offs accepted

- **Manual review burden.** The registry only works if we actually re-read it
  on every deps pass. The codified trigger in `CLAUDE.md` reduces the chance
  of skipping it but does not eliminate it.
- **Override scope.** npm `overrides` apply globally across the dependency
  tree. A patched version that breaks one consumer would break all consumers.
  In practice the fixed versions we pick are point releases on the same major,
  so this risk is small; the verification step in the review trigger (`npm
  install` + `npm audit` + a build) catches regressions.
- **The registry is in `docs/security/`**, a path that did not exist before
  this ADR. Future security-adjacent docs (e.g. threat model notes, pen-test
  findings) can live there too, but no decision is being made about that
  here.
