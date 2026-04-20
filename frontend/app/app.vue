<script setup lang="ts">
useHead({
  htmlAttrs: { lang: "en" },
  titleTemplate: title => title ? `${title} — Shoplists` : "Shoplists",
  meta: [
    // viewport-fit=cover lets content extend under the iOS status bar / home indicator,
    // which we then offset with env(safe-area-inset-*) in the default layout.
    { name: "viewport", content: "width=device-width, initial-scale=1, viewport-fit=cover" },
    { name: "description", content: "Grocery shopping lists" },
    // Modern standard; emit apple- counterpart too for iOS < 17 which still reads the old name.
    { name: "mobile-web-app-capable", content: "yes" },
    { name: "apple-mobile-web-app-capable", content: "yes" },
    { name: "apple-mobile-web-app-title", content: "Shoplists" },
    { name: "apple-mobile-web-app-status-bar-style", content: "black-translucent" },
  ],
});
</script>

<template>
  <!--
    Workaround for https://github.com/vite-pwa/nuxt/issues/223 —
    <NuxtPwaAssets /> registers its head entries via a mutated ref and without
    dedup keys, so its client-side setup re-pushes every <link> that was already
    emitted during SSR. In combination with nuxt-security (which injects nonces
    server-side) hydration can't merge the entries, so icons and apple-touch-icon
    get rendered twice in the DOM. Wrapping in <ClientOnly> with a #fallback
    slot makes the component run only during SSR, which is enough — the emitted
    head tags hydrate normally and nothing re-pushes on the client.
    Revisit once issue #223 is closed and drop the wrapper.
  -->
  <ClientOnly>
    <template #fallback>
      <NuxtPwaAssets />
    </template>
  </ClientOnly>
  <NuxtRouteAnnouncer />
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
  <ConfirmPopup />
  <Toast position="bottom-right" />
</template>
