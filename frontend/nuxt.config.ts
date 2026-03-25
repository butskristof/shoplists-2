import Aura from "@primeuix/themes/aura";

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: "2026-03-25",
  devtools: { enabled: true },

  modules: ["@primevue/nuxt-module", "@nuxt/fonts", "@nuxt/eslint"],

  css: [
    "primeicons/primeicons.css",
    "~/styles/reset.css",
    "~/styles/main.css",
  ],

  fonts: {
    defaults: {
      weights: [100, 200, 300, 400, 500, 600, 700, 800, 900],
    },
  },

  primevue: {
    options: {
      theme: {
        preset: Aura,
        options: {
          darkModeSelector: "system",
          cssLayer: {
            name: "primevue",
            order: "reset, primevue",
          },
        },
      },
    },
  },

  typescript: {
    strict: true,
  },

  eslint: {
    config: {
      standalone: false,
    },
  },
});
