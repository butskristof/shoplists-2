import Aura from "@primeuix/themes/aura";

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: "2026-03-25",
  devtools: { enabled: true },

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

  nitro: {
    storage: {
      oidc: {
        driver: "redis",
        base: "oidc",
        host: "",
        port: 0,
        password: "",
      },
    },
  },

  oidc: {
    provideDefaultSecrets: false,
    middleware: {
      globalMiddlewareEnabled: false,
    },
  },
});
