# PWA — Planning & Implementation Notes

Living document. Captures decisions, open questions, and phased rollout plan for turning
Shoplists into a Progressive Web App, with a primary target of iOS Safari.

Once decisions here stabilize, the "PWA approach" headline decision should be captured as a
short ADR under `docs/decisions/`. This file is the working plan, not a decision record.

---

## Goals

- **Installable on iOS home screen** with proper icon, name, standalone display, respect for
  safe-area (notch, home indicator).
- **Installable on Android / desktop Chrome** as a secondary benefit — same manifest, minimal
  extra work.
- **Graceful offline**: the app shell loads, navigation between cached routes works, API-dependent
  views surface clear offline state. No stale data served from cache.
- **Good dev loop**: able to iterate on iOS behavior against a real iPhone without constant
  deploy-to-prod cycles.
- **No regressions** to the existing Vue Query / BFF / OIDC / hybrid-SSR setup.

## Non-Goals (for now)

- Offline-first data (reading cached lists, queueing mutations, conflict resolution). Revisit
  when there's a concrete need.
- Push notifications.
- Background sync / periodic background sync.
- App Store distribution via Capacitor or similar wrappers.
- Custom splash screens per device. Rely on iOS-computed splash from manifest `theme_color` +
  `background_color` + icon.

---

## Approach

**PWA via `@vite-pwa/nuxt`**, which wraps `vite-plugin-pwa` (Workbox under the hood).

### Why PWA over alternatives

- **iOS 26** now defaults every home-screen shortcut to "Open as Web App" mode. Standalone,
  custom icon, theme color all work. Push notifications have been available to home-screen PWAs
  since iOS 16.4. The ceiling is real (storage caps, limited background capabilities) but outside
  this app's use case.
- **Capacitor / native wrapper**: adds build tooling, Apple Developer Program cost, review cycles,
  signing. Justified only if we hit a PWA limit we care about. Not today.
- **Native rewrite**: enormous cost, no user-visible benefit over a well-built PWA for a CRUD app.

### Why `@vite-pwa/nuxt`

- Canonical Nuxt module for PWA, actively maintained, built for Nuxt 3/4.
- The older `@nuxtjs/pwa` module is Nuxt 2-era and effectively abandoned for modern Nuxt.
- Wraps Workbox, which is Google's well-trodden service-worker library.
- Generates `manifest.webmanifest` from config (no hand-written JSON).
- Provides dev-mode toggle to disable the SW while iterating (`devOptions.enabled: false`) so we
  don't fight cache during development.

---

## How PWA layers interact with our stack

### Service worker vs Vue Query — the separation

- **Service worker** caches the *app delivery*: the HTML shell, JS bundles, CSS, fonts, icons,
  `/public` assets. It is what lets `https://shoplists/...` load when the network is flaky or
  offline.
- **Vue Query** caches *data* fetched from `/api/*`: parsed JSON, in JS memory, keyed by
  `shoplistKeys.*`.
- The two never touch the same bytes. SW serves the app; once running, the app uses VQ to manage
  server data. No conflict.

### `/api/*` stays `NetworkOnly`

- Authenticated, user-scoped, freshness-critical. Never cached by the SW.
- If offline: every query fails, VQ surfaces `isError`, views show offline state. That's the
  intended UX.
- `navigateFallbackDenylist` includes `/api/*` so Workbox never tries to serve these from cache
  on a navigation miss.

### Hybrid SSR/SPA routing

- Workbox `navigateFallback: '/'` serves the cached root HTML for any navigation that has no exact
  cache match. Client-side routing then renders the requested route.
- This is the "SPA shell" behavior — there is no separate `offline.html`. The app behaves normally
  offline; only API-backed data shows empty/error states.

### BFF proxy + OIDC session

- The Nitro BFF proxy (`server/api/[...path].ts`) is reached via same-origin `/api/*` calls. SW
  fetches carry cookies automatically (same-origin). `HttpOnly` session cookie is untouched by
  the SW — the SW doesn't inspect request bodies or cookies, it only decides whether to serve
  from cache or go to the network.
- Because `/api/*` is `NetworkOnly`, there is no risk of serving one user's cached responses to
  another session. Session boundaries remain clean.

### Mutations during offline

- VQ's default `networkMode: 'online'` pauses queries and mutations while offline and resumes on
  reconnect. Mutations already in-flight continue; new ones wait.
- For now: accept the default. If UX requires stronger offline-mutation semantics later, revisit
  with a proper design (optimistic mutations already exist; we'd need a durable mutation queue
  and conflict handling, which is Phase 4+ territory).

---

## Caching strategy (initial)

| Resource | Strategy | Rationale |
|---|---|---|
| App shell (HTML, JS, CSS) | Precache (injected at build) | Small, immutable per deploy. Precache = fast cold starts, reliable offline boot. |
| Fonts | Cache-first | Immutable, large, worth caching aggressively. |
| Icons / `/public` assets | Precache | Part of the app shell. |
| Runtime images (if any) | Stale-while-revalidate | Balance freshness with offline availability. |
| `/api/*` | **NetworkOnly** | Authenticated, user-scoped, freshness-critical. Never cached. |
| OIDC endpoints (`/auth/*`) | **NetworkOnly** | Security boundary. |

`registerType: 'autoUpdate'` — new SW takes over automatically on next navigation, with a
user-visible toast prompting reload so we don't silently leave users on stale code.

---

## Phased rollout

### Phase 1 — Installable + iOS polish (SW on defaults, no UX layer yet)

**Goal**: users can install to home screen on iOS and Android, get a good-looking standalone app,
and the SW silently precaches the app shell. No offline banner, no update prompt yet.

- Install module: `npx nuxt module add @vite-pwa/nuxt`.
- Configure `pwa` block in `nuxt.config.ts`:
  - `manifest`: `name`, `short_name`, `start_url`, `scope`, `display: 'standalone'`,
    `theme_color`, `background_color`, icon references.
  - `registerType: 'autoUpdate'` (silent update on next navigation — acceptable for solo user).
  - `devOptions: { enabled: false }` — no SW in `npm run dev`, avoiding stale-cache iteration
    pain.
  - Workbox defaults are fine here; we tighten caching rules in Phase 3.
- Generate icons via `@vite-pwa/assets-generator` from a single source image (Android + Apple
  + maskable variants).
- iOS meta tags in Nuxt `app.head`:
  - `apple-mobile-web-app-capable: yes`
  - `apple-mobile-web-app-status-bar-style`
  - `viewport` with `viewport-fit=cover`
  - `theme-color` (the module can inject this from manifest config)
- CSS: `env(safe-area-inset-*)` on root layout containers for notch / home indicator.
- **Validation**: real iPhone via mkcert HTTPS dev setup (see below). Share → Add to Home Screen,
  confirm icon, splash, standalone chrome, safe areas. Also confirm Android Chrome install
  prompt appears on a non-iOS device or desktop Chrome.

**Why accept the default SW now (revised from earlier plan)**: Chrome's install-prompt criteria
require a registered fetch-handling SW. iOS doesn't care. Accepting the module's default gives
us Android installability for free, and `devOptions.enabled: false` keeps the SW out of the dev
loop entirely. Silent auto-update is acceptable until we add the update-prompt UX in Phase 2.

**Exit criteria**: app installs cleanly on iOS and Android, looks right, opens standalone,
nothing is broken, SW silently precaches the shell.

### Phase 2 — Offline UX + update prompt (UX layer on top of existing SW)

**Goal**: honest, visible offline state; user-controlled update flow.

- Add VueUse: `npx nuxt module add @vueuse/nuxt`. Enables auto-imported `useOnline` (and
  friends) without per-file imports.
- Persistent offline banner in the root layout, driven by `useOnline()`. Visible whenever offline.
- Workbox config tightening:
  - Explicit `navigateFallback: '/'` for SPA routing (may already be the default).
  - `navigateFallbackDenylist` for `/api/*` and `/auth/*`.
- SW update flow: on `updatefound` + waiting SW, show a PrimeVue toast with a "New version —
  reload?" action. Replace silent auto-update with explicit user confirmation mid-session.
- **Validation**: DevTools → Application → Service Workers "offline" checkbox. Real-device
  Airplane mode. Verify: app loads, navigation works, queries resolve as errors, banner shows,
  VQ mutations pause and resume on reconnect, update prompt appears when a new build is deployed.

**Exit criteria**: offline state is visible and honest, update prompt works, no stale data
served from cache.

### Phase 3 — Polish & runtime caching

**Goal**: tighten caching rules, verify no storage bloat, address any Safari-specific quirks.

- Review actual cache sizes on device. Ensure caches have sensible `expiration` limits.
- Audit iOS safe-area handling, keyboard behavior, 100vh quirks (use `dvh` where appropriate).
- Lighthouse PWA audit (desktop Chrome) + Safari Web Inspector on device.

### Phase 4+ — Deferred

- Offline data (VQ persister → IndexedDB, mutation queue, conflict resolution). Real feature,
  real design work.
- Push notifications.
- Background sync.
- Capacitor wrap (only if an unrecoverable PWA limitation emerges).

---

## Dev workflow

### Primary loop — mkcert + real iPhone

1. `mkcert` installed on Mac, local root CA created.
2. Root CA installed on iPhone (AirDrop the `.pem`, install profile in Settings → General → VPN
   & Device Management, trust in General → About → Certificate Trust Settings).
3. Generate cert for Mac's LAN IP or `.local` hostname.
4. Nuxt dev server configured to serve HTTPS on that cert.
5. iPhone on same Wi-Fi → `https://<mac-ip-or-name>:3000` → SW registers, install works.
6. Debug via Safari Web Inspector: iPhone Settings → Safari → Advanced → Web Inspector ON; plug
   in via USB; Mac Safari → Develop menu → device → inspect.

### Secondary loop — iOS Simulator

- Fast iteration, good-enough Safari. Misses real-hardware quirks (haptics, notch rendering,
  keyboard behavior, network variability). Use for quick sanity checks, not sign-off.

### Tunnel (Cloudflare / ngrok / Tailscale) — only when needed

- For testing over cellular, or from off-LAN devices.
- Downsides: latency, public URL, OIDC redirect-URI coordination, cookie/header rewrites in some
  tunnels.

### Aspire's role

- Aspire already orchestrates the full stack locally. It does **not** solve iOS cert trust —
  mkcert still handles that piece on the frontend dev server.
- `WithExternalHttpEndpoints()` / `.AsExternal()` can expose backend resources if we ever need
  the phone to hit the API directly (we don't — phone goes through the BFF proxy like everyone
  else).

### SW in dev

- `devOptions.enabled: false` by default. SW caching during dev → stale asset hell.
- Flip on only when explicitly testing install / offline / update behavior, typically via
  `npm run build && npm run preview` over HTTPS.

---

## Open questions / to validate

- **Apple splash screens**: rely on iOS-computed (from manifest + theme/background colors + icon)
  vs generate dedicated images for every iPhone size? Starting with iOS-computed; revisit if the
  result looks cheap.
- **Icon source**: need a clean, high-resolution square source image (512x512+) to feed the
  asset generator. Who produces this?
- **`start_url` and `scope`**: `/` for both? Confirm no interaction with OIDC redirect flow —
  after login, does the PWA return cleanly to the start URL?
- **OIDC login inside standalone**: iOS sometimes opens auth redirects in Safari (not the PWA
  Web View), which can break the redirect loop. Need to verify login works end-to-end from the
  installed PWA. If broken, options include SFSafariViewController-style handling — but this is
  outside PWA control and might require UX adjustment (e.g., re-login less frequently,
  long-lived session).
- **Nitro/Vite HTTPS dev config**: exact config for mkcert + Nuxt 4 dev server. Confirm at Phase
  1 kickoff.
- **ADR timing**: write the PWA ADR at the end of Phase 1 (once the approach has been validated
  against a real device) rather than upfront.

---

## Reference

- `@vite-pwa/nuxt` module: https://nuxt.com/modules/vite-pwa-nuxt
- Vite PWA docs: https://vite-pwa-org.netlify.app/
- Apple PWA meta tags: `apple-mobile-web-app-capable`, `apple-mobile-web-app-status-bar-style`,
  `apple-touch-icon`
- Safe-area CSS: `env(safe-area-inset-top|right|bottom|left)`, `viewport-fit=cover`
