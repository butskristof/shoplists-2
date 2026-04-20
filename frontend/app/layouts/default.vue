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
  /* Use 100vh (not 100dvh/100svh): iOS PWA standalone with viewport-fit=cover
     resolves dvh/svh to a smaller value on first paint, leaving a gap below the
     footer until a reflow corrects it. 100vh returns the full webview height. */
  min-height: 100vh;
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
  padding-bottom: max(var(--default-spacing), env(safe-area-inset-bottom));
  @media (min-width: 768px) {
    padding: var(--spacing-lg);
    padding-bottom: max(var(--spacing-lg), env(safe-area-inset-bottom));
  }
}
</style>
