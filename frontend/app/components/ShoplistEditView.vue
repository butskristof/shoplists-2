<script setup lang="ts">
import type { ShoplistItem } from "~/types/shoplist";
import { useDraggable } from "vue-draggable-plus";

defineProps<{
  items: ShoplistItem[];
}>();

const emit = defineEmits<{
  addItem: [name: string];
  deleteItem: [itemId: string];
  updateItemName: [itemId: string, name: string];
  updateItemPosition: [itemId: string, position: number];
}>();

const editList = ref<HTMLElement | null>(null);
useDraggable(editList, {
  handle: ".drag-handle",
  animation: 150,
  onUpdate(event) {
    const itemId = event.item.dataset?.itemId;
    if (itemId == null || event.newIndex == null)
      return;
    emit("updateItemPosition", itemId, event.newIndex + 1);
  },
});
</script>

<template>
  <section aria-label="Edit list items">
    <ul ref="editList" class="item-list">
      <li v-for="item in items" :key="item.id" :data-item-id="item.id">
        <ShoplistEditItem
          :item="item"
          @update:name="name => emit('updateItemName', item.id, name)"
          @delete="emit('deleteItem', item.id)"
        />
      </li>
    </ul>
    <ShoplistAddItem @add="name => emit('addItem', name)" />
  </section>
</template>

<style scoped>
.item-list {
  list-style: none;
  padding: 0;
}
</style>
