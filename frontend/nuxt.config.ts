import Aura from "@primeuix/themes/aura";

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: "2026-03-25",
  devtools: { enabled: true },

  runtimeConfig: {
    redis: {
      host: "",
      port: 0,
      password: "",
    },
  },

  modules: [
    "@primevue/nuxt-module",
    "@nuxt/fonts",
    "@nuxt/eslint",
    "@nuxtjs/color-mode",
    "nuxt-oidc-auth",
  ],

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
        preset: Aura,
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
