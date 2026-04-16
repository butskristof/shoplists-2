<script setup lang="ts">
import type { ShoplistItem } from "~/types/shoplist";
import { useDraggable } from "vue-draggable-plus";

defineProps<{
  items: ShoplistItem[];
}>();

const emit = defineEmits<{
  "add": [name: string];
  "delete": [itemId: string];
  "update:itemName": [itemId: string, name: string];
  "reorder": [itemId: string, position: number];
}>();

const editList = ref<HTMLElement | null>(null);
const { start, destroy } = useDraggable(editList, {
  immediate: false,
  handle: ".drag-handle",
  animation: 150,
  onUpdate(event) {
    const itemId = event.item.dataset?.itemId;
    if (itemId == null || event.newIndex == null)
      return;
    emit("reorder", itemId, event.newIndex + 1);
  },
});

// Start drag-and-drop once the list ref is available, tear down on unmount
watch(editList, (el) => {
  if (el)
    start();
}, { flush: "post" });

onUnmounted(() => {
  destroy();
});
</script>

<template>
  <section aria-label="Edit list items">
    <ul ref="editList" class="item-list">
      <li v-for="item in items" :key="item.id" :data-item-id="item.id">
        <ShoplistEditItem
          :item="item"
          @update:name="name => emit('update:itemName', item.id, name)"
          @delete="emit('delete', item.id)"
        />
      </li>
    </ul>
    <ShoplistAddItem @add="name => emit('add', name)" />
  </section>
</template>

<style scoped>
.item-list {
  list-style: none;
  padding: 0;
}
</style>
