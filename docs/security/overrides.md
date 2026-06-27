# npm transitive-dependency overrides

Living registry of every `overrides` entry in `frontend/package.json`. See
[ADR 017](../../decisions/017-transitive-dependency-overrides.md) for the
policy. Re-read on every `npm audit` cleanup or deps-upgrade pass; for each
entry, temporarily remove the override, run `npm install` + `npm audit`, and
remove if no longer load-bearing.

## No active overrides

Both prior entries were removed on 2026-06-27 once the dependency tree resolved
to non-vulnerable versions without them:

- **protobufjs** — `@opentelemetry/otlp-transformer` no longer pins protobufjs
  directly; it now resolves via `@grpc/proto-loader@0.8.1` (`protobufjs: ^7.5.5`),
  landing on `7.6.4`. The advisories that motivated the override affect the 8.x
  line only, so the 7.x fallback is clean.
- **serialize-javascript** — the workbox build path now pulls
  `@rollup/plugin-terser@1.0.0` (`serialize-javascript: ^7.x`), resolving to
  `7.0.6` on its own.

## Accepted advisories (not overridden)

Low-risk transitive advisories we deliberately do not fix, recorded here so
`npm audit` output isn't re-investigated from scratch each pass.

### esbuild

- **Advisory**: [GHSA-g7r4-m6w7-qqqr](https://github.com/advisories/GHSA-g7r4-m6w7-qqqr)
  — arbitrary file read via the esbuild dev server on Windows. Severity: low.
- **Vulnerable copy**: `esbuild@0.27.7`, pulled transitively by `vite@7.x` and
  `@nuxt/fonts`. (The `esbuild@0.28.1` under `nitropack` is already out of range.)
- **Risk for this project**: negligible. Dev-server/build-time only — never in
  the production bundle or on the request path — and the advisory is
  Windows-specific, while local dev is on macOS.
- **Why not fixed**: `vite` pins esbuild to `^0.27.x`, so lifting it to a patched
  `>0.28.0` requires forcing a breaking `vite`/`nuxt` toolchain bump
  (`npm audit fix --force`), which our policy rejects. An `overrides` entry
  forcing esbuild `0.28` risks breaking vite's tight esbuild API coupling.
- **Removal condition**: drop this note once `vite`/`nuxt` adopt esbuild `>=0.28.1`
  on their own and `npm audit` no longer reports it.
- **Recorded**: 2026-06-27.
