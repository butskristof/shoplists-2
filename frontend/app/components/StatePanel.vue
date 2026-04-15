<script setup lang="ts">
const { variant = "default" } = defineProps<{
  icon: string;
  message: string;
  title?: string;
  variant?: "default" | "error";
}>();
</script>

<template>
  <div class="state-panel" :class="{ error: variant === 'error' }">
    <i class="icon" :class="icon" />
    <h1 v-if="title">
      {{ title }}
    </h1>
    <p>{{ message }}</p>
    <slot name="action" />
  </div>
</template>

<style scoped>
.state-panel {
  padding: var(--spacing-lg) var(--spacing-md);

  display: flex;
  flex-direction: column;
  gap: var(--default-spacing);
  align-items: center;
  text-align: center;

  &.error {
    color: var(--p-red-500);

    h1 {
      color: inherit;
    }
  }

  .icon {
    font-size: var(--font-size-4xl);
    line-height: var(--font-size-4xl--line-height);
  }
}
</style>
