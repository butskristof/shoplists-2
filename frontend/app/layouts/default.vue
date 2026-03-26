<template>
  <div class="app-shell">
    <AppHeader />
    <main class="app-main">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--p-surface-50);
}

.app-main {
  display: flex;
  flex-direction: column;
  flex: 1;

  padding: var(--default-spacing);
  @media (min-width: 768px) {
    padding: var(--spacing-lg);
  }
}
</style>
