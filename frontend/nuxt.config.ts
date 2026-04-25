import { Market } from "./app/styles/theme";

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: "2026-03-25",
  devtools: { enabled: true },

  runtimeConfig: {
    backendApiUrl: "",
    redis: {
      host: "",
      port: 0,
      password: "",
      tls: "false",
    },
  },

  modules: [
    "@primevue/nuxt-module",
    "@nuxt/fonts",
    "@nuxt/eslint",
    "@nuxtjs/color-mode",
    "nuxt-oidc-auth",
    "nuxt-security",
    "@vite-pwa/nuxt",
  ],

  pwa: {
    registerType: "autoUpdate",
    devOptions: { enabled: false },
    // vite-plugin-pwa only wires pwa-assets.config.ts into the head-injection virtual
    // module when this integration is explicitly enabled — default is `false`.
    // Without it `<NuxtPwaAssets />` emits only the manifest link, not the icon/
    // theme-color tags derived from the assets config.
    pwaAssets: { config: true },
    manifest: {
      // Stable PWA identity. Must never change after launch — browsers use this to
      // recognise the app across updates, independently of start_url.
      id: "/",
      name: "Shoplists",
      short_name: "Shoplists",
      description: "Grocery shopping lists",
      lang: "en",
      dir: "ltr",
      start_url: "/",
      scope: "/",
      display: "standalone",
      theme_color: "#0F6B50",
      background_color: "#F7F8F6",
      icons: [
        { src: "pwa-64x64.png", sizes: "64x64", type: "image/png" },
        { src: "pwa-192x192.png", sizes: "192x192", type: "image/png" },
        { src: "pwa-512x512.png", sizes: "512x512", type: "image/png" },
        { src: "maskable-icon-512x512.png", sizes: "512x512", type: "image/png", purpose: "maskable" },
      ],
    },
    workbox: {
      // @vite-pwa/nuxt seeds globPatterns with a couple of JSON manifest entries,
      // which suppresses Workbox's default `**/*.{js,wasm,css,html}` fallback — so
      // without an explicit list the precache holds only 3 tiny files and the SPA
      // shell is never cached. Be explicit to actually precache the app shell.
      // Note: Nuxt renders HTML via Nitro at runtime and does not emit a static
      // `/` into `.output/public/`, so no HTML is precached here. This enables
      // fast repeat loads but not offline navigation — offline navigation is
      // deferred to Phase 2 in docs/plans/pwa.md and requires a prerendered
      // app-shell route as the `navigateFallback` target.
      globPatterns: ["**/*.{js,css,png,svg,ico,webmanifest,woff2}"],
    },
  },

  security: {
    // SECURITY: do not enable corsHandler without revisiting the x-csrf
    // CSRF design in server/api/[...path].ts. If CORS permits any foreign
    // origin to send the `x-csrf` header, the preflight-based defense is
    // bypassed from that origin. See also the Origin header check in the
    // BFF handler, which is an active backstop for this.
    corsHandler: false,
  },

  css: [
    "primeicons/primeicons.css",
    "~/styles/reset.css",
    "~/styles/utilities.css",
    "~/styles/main.css",
    "~/styles/custom.css",
  ],

  fonts: {
    defaults: {
      // load in everything until we get a sense of what we actually use
      weights: [100, 200, 300, 400, 500, 600, 700, 800, 900],
    },
  },

  primevue: {
    options: {
      theme: {
        preset: Market,
        options: {
          darkModeSelector: ".dark-mode",
          cssLayer: {
            name: "primevue",
            order: "reset, primevue",
          },
        },
      },
    },
  },

  colorMode: {
    preference: "system",
    fallback: "light",
    classSuffix: "-mode",
  },

  typescript: {
    strict: true,
  },

  eslint: {
    config: {
      standalone: false,
    },
  },

  oidc: {
    provideDefaultSecrets: false,
    defaultProvider: "oidc",
    providers: {
      oidc: {
        // Injected via environment variables (Aspire parameters)
        clientId: "",
        clientSecret: "",
        redirectUri: "",
        openIdConfiguration: "",
        authorizationUrl: "",
        tokenUrl: "",
        userInfoUrl: "",
        logoutUrl: "",
        // OIDC protocol settings
        responseType: "code",
        grantType: "authorization_code",
        authenticationScheme: "body", // Pocket ID only supports client_secret_post
        pkce: true, // additional protection against auth code interception
        state: true, // csrf protection
        nonce: true, // ID token replay attack protection
        // make sure to include offline_access to get back refresh token
        scope: ["openid", "profile", "offline_access"],
        // limit to fields which are relevant for the application
        filterUserInfo: ["sub", "name", "preferred_username"],
        // ensure tokens are valid
        validateAccessToken: true,
        validateIdToken: true,
        // do not expose tokens in user session client-side
        exposeAccessToken: false,
        exposeIdToken: false,
        logoutRedirectParameterName: "post_logout_redirect_uri",
        callbackRedirectUrl: "/",
        logoutRedirectUri: "/",
      },
    },
    session: {
      automaticRefresh: true,
      expirationCheck: true,
      maxAge: 60 * 60 * 24 * 7, // 7 days — within Pocket ID's 30-day refresh token lifetime
      expirationThreshold: 60, // seconds
      cookie: {
        // https://docs.duendesoftware.com/bff/fundamentals/session/handlers/#choosing-between-samesitelax-and-samesitestrict
        // can't do strict since IdP will be hosted on other site
        sameSite: "lax",
      },
    },
    middleware: {
      globalMiddlewareEnabled: true, // protect everything except /auth by default
    },
  },
});
