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
  toggleItemFulfilled,
  updateItemPosition,
  deleteList,
  isDeletingList,
} = useShoplist(props.listId);

const isEditMode = useRouteQuery("edit", String(false), {
  transform: {
    get: v => v === String(true),
    set: v => String(v),
  },
  mode: "replace",
});

function enterEditMode() {
  isEditMode.value = true;
}

const showDeleteDialog = ref(false);

async function handleDeleteList() {
  await deleteList();
  await navigateTo("/");
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
      @toggle-item="toggleItemFulfilled"
      @enter-edit-mode="enterEditMode"
    />

    <div v-if="isEditMode" class="delete-list">
      <Button
        icon="pi pi-trash"
        label="Delete list"
        severity="danger"
        variant="text"
        @click="showDeleteDialog = true"
      />

      <DeleteShoplistDialog
        v-if="showDeleteDialog"
        :list-name="list!.name"
        :is-deleting-list="isDeletingList"
        @close="showDeleteDialog = false"
        @delete="handleDeleteList"
      />
    </div>
  </template>
</template>

<style scoped>
.delete-list {
  display: flex;
  justify-content: flex-end;
}
</style>
