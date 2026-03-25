<script setup lang="ts">
import type { NuxtError } from "nuxt/app";

const props = defineProps<{
  error: NuxtError;
}>();

const isNotFound = computed(() => props.error.status === 404);
</script>

<template>
  <div class="error-page">
    <ErrorNotFound v-if="isNotFound" />
    <ErrorGeneral v-else :error="error" />
  </div>
</template>

<style scoped>
.error-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  justify-content: center;

  background: var(--p-surface-0);
}
</style>
