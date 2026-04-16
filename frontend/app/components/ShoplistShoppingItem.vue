<script setup lang="ts">
import type { ShoplistItem } from "~/types/shoplist";

defineProps<{
  item: ShoplistItem;
}>();

const emit = defineEmits<{
  toggle: [];
}>();
</script>

<template>
  <label
    :for="`item-${item.id}`"
    class="item-row"
    :class="{ fulfilled: item.isFulfilled }"
  >
    <Checkbox
      :model-value="item.isFulfilled"
      :input-id="`item-${item.id}`"
      binary
      size="large"
      @update:model-value="emit('toggle')"
    />
    <span class="item-name">{{ item.name }}</span>
  </label>
</template>

<style scoped>
.item-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) var(--spacing-md);
  border-bottom: 1px solid var(--p-content-border-color);
  cursor: pointer;
  user-select: none;

  .item-name {
    min-width: 0;
    overflow-wrap: break-word;
  }

  &.fulfilled .item-name {
    //text-decoration: line-through;
    opacity: 0.5;
  }
}

@media (hover: hover) {
  .item-row:hover {
    background: var(--p-content-hover-background);
  }
}
</style>
