<script setup lang="ts">
</script>

<template>
  <div class="index-page app-container">
    <h1>Hello</h1>
    <NuxtLink :to="{ name: 'lists-id', params: { id: 'weekly-groceries' } }">
      Weekly groceries
    </NuxtLink>
    <ComponentDemo />
  </div>
</template>

<style scoped>
.index-page {
  display: flex;
  flex-direction: column;
  gap: var(--default-spacing);
}
</style>
