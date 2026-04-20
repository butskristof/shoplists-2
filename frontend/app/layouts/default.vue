<template>
  <div class="app-shell">
    <AppHeader />
    <main class="app-main">
      <slot />
      <AppFooter />
    </main>
  </div>
</template>

<style scoped>
.app-shell {
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
  background-color: var(--app-background);
  /* Absorb horizontal notch insets; top/bottom are handled by inner children so
     scroll content can still flow under the translucent status bar when desired. */
  padding-inline: env(safe-area-inset-left) env(safe-area-inset-right);
}

.app-main {
  display: flex;
  flex-direction: column;
  flex: 1;

  padding: var(--default-spacing);
  padding-top: max(var(--default-spacing), env(safe-area-inset-top));
  padding-bottom: max(var(--default-spacing), env(safe-area-inset-bottom));
  @media (min-width: 768px) {
    padding: var(--spacing-lg);
    padding-top: max(var(--spacing-lg), env(safe-area-inset-top));
    padding-bottom: max(var(--spacing-lg), env(safe-area-inset-bottom));
  }
}
</style>
