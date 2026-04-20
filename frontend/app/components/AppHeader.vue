<script setup lang="ts">
</script>

<template>
  <div class="app-header">
    <div class="app-container header-content">
      <NuxtLink to="/" class="logo" aria-label="Shoplists home">
        <i class="pi pi-shopping-cart logo-icon" aria-hidden="true" />
        <div class="name">
          Shoplists
        </div>
      </NuxtLink>
    </div>
  </div>
</template>

<style scoped>
.app-header {
  background-color: var(--p-content-background);
  border-bottom: 1px solid var(--p-content-border-color);

  position: sticky;
  top: 0;
  z-index: 100;

  /* add additional padding for iPhone header area */
  padding-top: env(safe-area-inset-top);
}

.header-content {
  padding: var(--default-spacing);
  @media (min-width: 640px) {
    padding-inline: 0;
  }

  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-lg);
}

.logo {
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: var(--spacing-sm);

  font-size: var(--font-size-2xl);
  line-height: var(--font-size-2xl--line-height);
  text-decoration: none;

  .logo-icon {
    font-size: 1.75rem;
    color: var(--p-primary-color);
  }

  .name {
    color: var(--p-text-color);
    font-weight: var(--weight-semibold);
  }
}
</style>
