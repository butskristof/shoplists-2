<script setup lang="ts">
import type { NuxtError } from "nuxt/app";

defineProps<{
  error: NuxtError;
}>();
</script>

<template>
  <div class="error-content">
    <i class="pi pi-exclamation-circle error-icon" />

    <div class="error-text">
      <h1 class="error-title">
        Something went wrong
      </h1>
      <p class="error-message">
        An unexpected error occurred.
      </p>
    </div>

    <div class="error-details">
      <dl>
        <dt>Status</dt>
        <dd>{{ error.status }}</dd>
        <template v-if="error.statusText">
          <dt>Message</dt>
          <dd>{{ error.statusText }}</dd>
        </template>
        <template v-if="error.message && error.message !== error.statusText">
          <dt>Details</dt>
          <dd>{{ error.message }}</dd>
        </template>
      </dl>
    </div>

    <Button
      v-slot="slotProps"
      as-child
      text
    >
      <NuxtLink to="/" :class="slotProps.class" @click="clearError()">
        <i class="pi pi-arrow-left" />
        Back to Home
      </NuxtLink>
    </Button>
  </div>
</template>

<style scoped>
.error-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-lg);
}

.error-icon {
  font-size: 6rem;
  color: var(--p-red-500);
}

.error-text {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--default-spacing);
  text-align: center;
}

.error-title {
  font-size: 3rem;
  font-weight: 700;
  color: var(--p-surface-900);
  line-height: 1.2;
}

.error-message {
  font-size: 1.125rem;
  color: var(--p-surface-500);
  line-height: 1.5;
}

.error-details {
  background: var(--p-surface-50);
  border: 1px solid var(--p-surface-200);
  border-radius: var(--p-border-radius-md);
  padding: 1rem 1.25rem;
  max-width: 480px;
  width: 100%;

  & dl {
    display: grid;
    grid-template-columns: auto 1fr;
    gap: 0.5rem 1rem;
    margin: 0;
  }

  & dt {
    font-weight: 600;
    color: var(--p-surface-700);
    font-size: 0.875rem;
  }

  & dd {
    margin: 0;
    color: var(--p-surface-600);
    font-size: 0.875rem;
    word-break: break-word;
  }
}
</style>
