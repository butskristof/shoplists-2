<script setup lang="ts">
const route = useRoute();
const listId = route.params.id as string;

const { list, isPending, isError, isNotFound } = useShoplist(listId);

useHead({
  title: computed(() => list.value?.name),
});
</script>

<template>
  <div class="list-detail app-container">
    <BackButton to="/" label="Back to lists" />

    <LoadingPanel v-if="isPending" />

    <StatePanel
      v-else-if="isNotFound"
      icon="pi pi-question-circle"
      title="List not found"
      message="This list may have been deleted, or the link is incorrect."
    />

    <StatePanel
      v-else-if="isError"
      variant="error"
      icon="pi pi-exclamation-circle"
      message="Something went wrong loading your list."
    />

    <ShoplistDetail v-else-if="list" :list-id="listId" />
  </div>
</template>

<style scoped>
.list-detail {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--default-spacing);
}
</style>
