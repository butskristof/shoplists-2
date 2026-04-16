<script setup lang="ts">
import { useRouteQuery } from "@vueuse/router";

const props = defineProps<{ listId: string }>();

const {
  list,
  sortedItems,
  updateListName,
  addItem,
  deleteItem,
  updateItemName,
  toggleItem,
  updateItemPosition,
} = useShoplist(props.listId);

const isEditMode = useRouteQuery("edit", String(false), {
  transform: {
    get: v => v === String(true),
    set: v => String(v),
  },
  mode: "replace",
});

function toggleEditMode() {
  isEditMode.value = !isEditMode.value;
}
</script>

<template>
  <template v-if="list">
    <ShoplistHeader
      v-model:edit-mode="isEditMode"
      :name="list.name"
      :hide-actions="list.items.length === 0"
      @update:name="updateListName"
    />

    <ShoplistEditView
      v-if="isEditMode"
      :items="sortedItems"
      @add-item="addItem"
      @delete-item="deleteItem"
      @update-item-name="updateItemName"
      @update-item-position="updateItemPosition"
    />
    <ShoplistShoppingView
      v-else
      :items="sortedItems"
      @toggle-item="toggleItem"
      @enter-edit-mode="toggleEditMode"
    />
  </template>
</template>

<style scoped>
</style>
