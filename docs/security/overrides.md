# npm transitive-dependency overrides

Living registry of every `overrides` entry in `frontend/package.json`. See
[ADR 017](../../decisions/017-transitive-dependency-overrides.md) for the
policy. Re-read on every `npm audit` cleanup or deps-upgrade pass; for each
entry, temporarily remove the override, run `npm install` + `npm audit`, and
remove if no longer load-bearing.

## protobufjs

- **Pinned**: `^8.0.3` (minimum patched range; latest at time of writing is
  `8.2.0`)
- **Advisories**:
  - [GHSA-q6x5-8v7m-xcrf](https://github.com/advisories/GHSA-q6x5-8v7m-xcrf) — overlong UTF-8 decoding
  - [GHSA-2pr8-phx7-x9h3](https://github.com/advisories/GHSA-2pr8-phx7-x9h3) — DoS via crafted field names
  - [GHSA-66ff-xgx4-vchm](https://github.com/advisories/GHSA-66ff-xgx4-vchm) — code injection through bytes-field defaults
  - [GHSA-fx83-v9x8-x52w](https://github.com/advisories/GHSA-fx83-v9x8-x52w) — prototype injection in generated constructors
  - [GHSA-75px-5xx7-5xc7](https://github.com/advisories/GHSA-75px-5xx7-5xc7) — code-generation gadget after prototype pollution
  - [GHSA-jvwf-75h9-cwgg](https://github.com/advisories/GHSA-jvwf-75h9-cwgg) — process-wide DoS through unsafe option paths
  - [GHSA-685m-2w69-288q](https://github.com/advisories/GHSA-685m-2w69-288q) — DoS via unbounded protobuf recursion

  Vulnerable range: `>=8.0.0 <=8.0.1`. Patched in `8.0.2+`.
- **Parent dep**: `@opentelemetry/otlp-transformer@0.217.0` (pulled in by
  `@opentelemetry/sdk-node@^0.217.0` via the Nitro telemetry plugin in
  `frontend/server/plugins/telemetry.ts`). The transformer **pins
  `protobufjs: '8.0.1'` exactly** in its manifest, so the override is the only
  way to lift the resolved version without changing direct deps.

  `0.216.0` and `0.217.0` both pin exactly. `0.215.0` used `^8.0.1` (would
  resolve correctly on its own) but is not the version we want to be on.
  `npm audit fix --force` "fixes" by downgrading `sdk-node` to `0.215.0` — see
  ADR 017 for why we reject that path.
- **Risk for this project**: low. Used server-side (Nitro) for outbound OTLP
  export only; the protobuf decoder never sees attacker-controlled bytes. All
  listed advisories require the attacker to feed protobuf input into our
  decoder. We patch anyway to keep `npm audit` clean and avoid future drift.
- **Added**: 2026-05-13 (commit pending).
- **Removal condition**: remove when `@opentelemetry/otlp-transformer` ships a
  version (likely `0.218.0+`) whose `dependencies.protobufjs` allows `>=8.0.2`
  — check with `npm view @opentelemetry/otlp-transformer@latest dependencies.protobufjs`.

## serialize-javascript

- **Pinned**: `^7.0.5`
- **Advisories**:
  - [GHSA-76p7-773f-r4q5](https://github.com/advisories/GHSA-76p7-773f-r4q5) — XSS via insufficient escaping (original motivation, PR #54)
  - [GHSA-5c6j-r48x-rmvq](https://github.com/advisories/GHSA-5c6j-r48x-rmvq) — RCE via `RegExp.flags` / `Date.prototype.toISOString`
  - [GHSA-qj8w-gfj5-8c6v](https://github.com/advisories/GHSA-qj8w-gfj5-8c6v) — CPU-exhaustion DoS via crafted array-likes

  Vulnerable range: `<=7.0.4` across all majors. **The 6.x line was never
  patched** — fixes only landed in 7.0.5+.
- **Parent dep**: `@vite-pwa/nuxt → vite-plugin-pwa → workbox-build →
  @rollup/plugin-terser@0.4.4`, which declares `serialize-javascript: ^6.0.1`.
  Without the override the tree resolves to `serialize-javascript@6.0.2`,
  which `npm audit` still flags. The newer `@rollup/plugin-terser@1.0.0`
  (used via `nitropack` on a separate path) declares `^7.0.3` and resolves
  correctly without help; the override exists for the workbox path.
- **Risk for this project**: low. Build-time only — `workbox-build` runs
  during `nuxt build` to emit the service worker. Not in the runtime bundle,
  not on the request path. The advisories require attacker-controlled input
  to `serialize-javascript`, which the workbox build does not receive.
- **Added**: 2026-04-20 (PR #54, commit `43e1276`).
- **Removal condition**: remove when `workbox-build` releases a version that
  uses `@rollup/plugin-terser@>=1.0.0` (or any version whose
  `serialize-javascript` range starts at `^7.0.5` or `^6.0.3+`, neither of
  which exist at time of writing).
