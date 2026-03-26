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
  padding: var(--spacing-lg);
}

.app-content {
  flex: 1;
  background: var(--p-surface-0);
  border-radius: calc(var(--default-spacing) * 0.75);
  border: 1px solid var(--p-surface-200);
}
</style>
