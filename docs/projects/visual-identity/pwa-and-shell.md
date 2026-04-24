# PWA & app shell

Covers the visual-identity concerns that live outside Vue components:
manifest colours, meta tags, font loading, safe-area handling, overscroll
strips, and icon choices. Order of play so they don't fight each other
once the tokens change.

---

## 1. Manifest colours (`frontend/nuxt.config.ts`)

Current:
```ts
manifest: {
  theme_color: "#10b981",        // Aura emerald (Tailwind emerald-500)
  background_color: "#09090b",   // zinc-950
  …
},
```

`theme_color` sets the system chrome (Android address bar, iOS standalone
status bar tint in some modes) and must be a single hex. It can't respond
to dark mode unless we emit the `<meta name="theme-color">` twice with
`media="(prefers-color-scheme: …)"`. `background_color` is the splash
screen paint before first render.

### Target values

| Token | Light | Dark | Notes |
|---|---|---|---|
| `manifest.theme_color`      | design `primary` `#0F6B50` **or** `bg` `#F7F8F6` | — | The manifest can only hold one value. See §1.2 for the choice. |
| `manifest.background_color` | `#F7F8F6` (matches `bg`) | n/a | Single value. Keep aligned with light-mode bg so splash isn't jarring in light mode. Dark-mode users will briefly see light splash — acceptable. |
| `<meta name="theme-color">` light | `#F7F8F6` (or `#FFFFFF` if header is surface) | — | Emit via `useHead` with media query. |
| `<meta name="theme-color">` dark  | — | `#0D1311` | Emit via `useHead` with `(prefers-color-scheme: dark)`. |

### 1.1 Status bar vs theme colour

Today `apple-mobile-web-app-status-bar-style` is `"black-translucent"` in
`app.vue`. That makes iOS render content *under* the status bar, and the
visual effect is driven by whatever paints under the strip — i.e. the
overscroll strips (`body::before`) and the header's
`--p-content-background`. Token recolour handles this for free: the
header background shifts with `content.background`, and iOS reads the
`<meta theme-color>` underneath. Keep `black-translucent`.

### 1.2 Which single `manifest.theme_color` to pick

Two schools:
- **Brand colour (`#0F6B50`)** — matches logo/icon. Older Android splash
  screens use this behind the icon; looks cohesive with maskable icon.
- **Background colour (`#F7F8F6`)** — matches first-paint; no flash of
  coloured chrome.

Recommendation: `#F7F8F6` for `manifest.theme_color` to avoid a coloured
flash, and rely on the splash icon itself (green on green-tinted bg) for
brand. Confirmed once decided; see `open-questions.md`.

### 1.3 PWA icon

User said **keep the PrimeIcons shopping cart** for now — applies to the
in-app header. Whether the PWA home-screen icon (`pwa-source.svg` →
generated via `pwa-assets.config.ts`) changes is a separate call. The
current source is a green cart on zinc-950 (`darkSurface = "#09090b"` in
the assets config). To match Market:
- Keep the cart glyph (same decision as the in-app logo).
- Swap the background from zinc-950 to design dark `bg` (`#0D1311`) so
  the tile matches dark mode, or to the design `primary` (`#0F6B50`) for
  a brand-forward maskable icon.

Regeneration: `npm run generate:pwa-assets` — regenerates icons from
`public/pwa-source.svg` using `pwa-assets.config.ts`. **Don't** hand-edit
the generated PNGs.

---

## 2. Meta tags (`frontend/app/app.vue`)

Current:
```ts
{ name: "viewport", content: "width=device-width, initial-scale=1, viewport-fit=cover" },
{ name: "description", content: "Grocery shopping lists" },
{ name: "mobile-web-app-capable", content: "yes" },
{ name: "apple-mobile-web-app-capable", content: "yes" },
{ name: "apple-mobile-web-app-title", content: "Shoplists" },
{ name: "apple-mobile-web-app-status-bar-style", content: "black-translucent" },
```

Additions for visual identity:

```ts
{ name: "theme-color", content: "#F7F8F6", media: "(prefers-color-scheme: light)" },
{ name: "theme-color", content: "#0D1311", media: "(prefers-color-scheme: dark)" },
```

These override the static manifest value in supported browsers and make
the chrome track our colour scheme.

**Don't add**: `format-detection`, `apple-touch-icon` manually — the
`<NuxtPwaAssets />` wrapper already emits those from `pwa-assets.config.ts`
(and the existing hydration workaround in `app.vue` is sensitive — see
the comment in that file).

---

## 3. Font loading

Current: `@nuxt/fonts` with `weights: [100, 200, 300, 400, 500, 600, 700, 800, 900]`
across *all* detected families. Since only `main.css` specifies
`font-family: "Inter"`, Nuxt Fonts loads Inter at every weight.

Market uses **Inter Tight** (a separate Google Fonts family, not a weight
of Inter). The design uses weights: 400, 450, 500, 550, 600, 650, 700.
Note the half-steps (450, 550, 650) — Inter Tight supports these as
distinct optical weights.

### Options

1. **Keep Inter.** Skip the half-steps; map 450 → 500, 550 → 500, 650 →
   600. Cheap. Less tight on display sizes.
2. **Switch to Inter Tight.** Add to `main.css`:
   `font-family: "Inter Tight", "Inter", …fallbacks`. Trim
   `@nuxt/fonts` weights to the design's subset (400, 500, 600, 700 at
   minimum; add 450/550/650 if we want the half-steps).

Recommendation: **option 2** — the design is specifically built around
Inter Tight's tracking. The visual difference is real. Cost is one extra
font family over the wire.

Flagged in `open-questions.md`.

---

## 4. Overscroll strips (`frontend/app/styles/main.css`)

Existing `body::before` / `body::after` strips paint `--p-content-background`
at the top and `--app-background` at the bottom so iOS/macOS rubber-band
doesn't reveal a wrong colour.

**After token work:**
- `--p-content-background` → `#FFFFFF` (light) / `#161D1A` (dark).
- `--app-background` → `#F7F8F6` (light) / `#0D1311` (dark).
- Top strip matches header; bottom strip matches main bg. Still correct.

Memory note (`project_overscroll_strips.md`): do NOT collapse these to a
single body background. The token change doesn't touch the mechanism —
just verify both strips show the expected colour post-migration.

---

## 5. App shell height (`frontend/app/layouts/default.vue`)

Uses `min-height: 100vh` deliberately (not `dvh`/`svh`) to work around an
iOS PWA standalone first-paint gap. Memory note
`project_ios_pwa_vh.md`. Nothing to change; flagging so the token work
doesn't "modernise" this into a regression.

---

## 6. Splash / first paint

Nuxt 4 with SSR renders HTML at request time; the initial document already
carries the right `<meta theme-color>` if we emit it via `useHead`. The
service worker precache (`workbox.globPatterns`) caches JS/CSS/assets but
not the HTML shell — so the first paint is network-bound and the coloured
chrome is set by the meta/manifest.

No changes here for visual identity; just noting it so future "offline
shell" work (docs/projects/pwa/) knows the `<meta theme-color>` and
`background_color` still need to be present in the prerendered shell.

---

## 7. Check-list for the PWA pass

Done in the PWA phase of `plan.md` — copied here for convenience:

- [ ] Update `manifest.theme_color` / `background_color` (§1).
- [ ] Emit scheme-specific `<meta theme-color>` in `app.vue` (§2).
- [ ] Regenerate PWA icons if `pwa-source.svg` background changes (§1.3).
- [ ] Verify overscroll strips still paint correctly in light + dark (§4).
- [ ] Optional: switch font family to Inter Tight + trim weight list (§3).
- [ ] Verify iOS standalone status-bar blend (black-translucent + new
      header bg).
- [ ] Verify Android task-switcher thumbnail picks up new theme colour.
