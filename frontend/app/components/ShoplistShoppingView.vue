<script setup lang="ts">
import type { ShoplistItem } from "~/types/shoplist";

const props = defineProps<{
  items: ShoplistItem[];
}>();

const emit = defineEmits<{
  toggleItem: [itemId: string];
  enterEditMode: [];
}>();

const itemsToGet = computed(() => props.items.filter(item => !item.isFulfilled));
const fulfilledItems = computed(() => props.items.filter(item => item.isFulfilled));
</script>

<template>
  <StatePanel
    v-if="items.length === 0"
    icon="pi pi-check-circle"
    message="Ready to go! Add some items to your list to get started."
  >
    <template #action>
      <Button
        label="Add items"
        icon="pi pi-pencil"
        @click="emit('enterEditMode')"
      />
    </template>
  </StatePanel>

  <div v-else class="list-content">
    <section class="items-to-get-section" aria-label="Items to get">
      <h2 class="section-heading">
        Items to get
      </h2>
      <StatePanel
        v-if="itemsToGet.length === 0"
        icon="pi pi-check-circle"
        message="All done! Nothing left to get."
      >
        <template #action>
          <Button
            label="Add more items"
            icon="pi pi-pencil"
            @click="emit('enterEditMode')"
          />
        </template>
      </StatePanel>
      <ul v-else class="item-list">
        <li v-for="item in itemsToGet" :key="item.id">
          <ShoplistShoppingItem
            :item="item"
            @toggle="emit('toggleItem', item.id)"
          />
        </li>
      </ul>
    </section>

    <section v-if="fulfilledItems.length > 0" class="fulfilled-section" aria-label="Fulfilled items">
      <h2 class="section-heading">
        Fulfilled
      </h2>
      <ul class="item-list">
        <li v-for="item in fulfilledItems" :key="item.id">
          <ShoplistShoppingItem
            :item="item"
            @toggle="emit('toggleItem', item.id)"
          />
        </li>
      </ul>
    </section>
  </div>
</template>

<style scoped>
.list-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

.section-heading {
  font-size: var(--font-size-base);
  line-height: var(--font-size-base--line-height);
  font-weight: var(--weight-semibold);
  color: var(--p-text-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: var(--spacing-sm);
}

.item-list {
  list-style: none;
  padding: 0;
}
</style>
