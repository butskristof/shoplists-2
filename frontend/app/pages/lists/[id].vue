<script setup lang="ts">
interface ShoplistItem {
  id: string;
  name: string;
  done: boolean;
}

const listName = "Weekly groceries";

const items = ref<ShoplistItem[]>([
  { id: "1", name: "Bananas", done: false },
  { id: "2", name: "Whole milk", done: false },
  { id: "3", name: "Sourdough bread", done: false },
  { id: "4", name: "Eggs", done: false },
  { id: "5", name: "Olive oil", done: true },
  { id: "6", name: "Cherry tomatoes", done: false },
  { id: "7", name: "Chicken thighs", done: false },
  { id: "8", name: "Greek yogurt", done: true },
]);

const itemsToGet = computed(() => items.value.filter(item => !item.done));
const doneItems = computed(() => items.value.filter(item => item.done));
</script>

<template>
  <div class="list-detail app-container">
    <h1 class="list-title">
      {{ listName }}
    </h1>

    <div class="lists">
      <section v-if="itemsToGet.length > 0" class="todo-section" aria-label="Items to get">
        <h2 class="todo-heading">
          Items to get
        </h2>
        <ul class="item-list">
          <li v-for="item in itemsToGet" :key="item.id">
            <label :for="`item-${item.id}`" class="item-row">
              <Checkbox
                v-model="item.done"
                :input-id="`item-${item.id}`"
                binary
                size="large"
              />
              <span class="item-name">{{ item.name }}</span>
            </label>
          </li>
        </ul>
      </section>

      <section v-if="doneItems.length > 0" class="done-section" aria-label="Done items">
        <h2 class="done-heading">
          Done
        </h2>
        <ul class="item-list">
          <li v-for="item in doneItems" :key="item.id">
            <label :for="`item-${item.id}`" class="item-row item-row-done">
              <Checkbox
                v-model="item.done"
                :input-id="`item-${item.id}`"
                binary
                size="large"
              />
              <span class="item-name">{{ item.name }}</span>
            </label>
          </li>
        </ul>
      </section>
    </div>
  </div>
</template>

<style scoped>
.list-detail {
  display: flex;
  flex-direction: column;
  gap: var(--default-spacing);
}

.lists {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

.item-list {
  list-style: none;
  padding: 0;
}

.item-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) var(--spacing-md);
  border-bottom: 1px solid var(--p-surface-200);
  cursor: pointer;
  user-select: none;
}

.item-row-done .item-name {
  text-decoration: line-through;
  opacity: 0.5;
}

.done-heading,
.todo-heading {
  font-size: var(--font-size-base);
  line-height: var(--font-size-base--line-height);
  font-weight: var(--weight-semibold);
  color: var(--p-surface-500);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: var(--spacing-sm);
}

@media (hover: hover) {
  /* only apply to mouse/trackpad, not touch devices */
  .item-row:hover {
    background: var(--p-surface-100);
  }
}
</style>
