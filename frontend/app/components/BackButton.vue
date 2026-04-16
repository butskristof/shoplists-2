<script setup lang="ts">
import type { RouteLocationRaw } from "vue-router";

defineProps<{
  to: RouteLocationRaw;
  label: string;
}>();
</script>

<template>
  <Button
    v-slot="slotProps"
    class="back-button"
    as-child
    text
  >
    <NuxtLink :to="to" class="back-button" :class="slotProps.class">
      <i class="pi pi-arrow-left" />
      {{ label }}
    </NuxtLink>
  </Button>
</template>

<style scoped>
.back-button {
  align-self: flex-start;
  /* negate the padding a bit so the arrow sits flush with the start of
  the page content */
  margin-left: calc(-1 * var(--p-button-padding-x));
}
</style>
