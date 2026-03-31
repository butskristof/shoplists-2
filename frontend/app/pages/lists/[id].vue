<script setup lang="ts">
import type { ShoplistItem } from "~/types/shoplist";

const listName = "Weekly groceries with a very long name for the list";

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

const isEditMode = ref(false);

const itemsToGet = computed(() => items.value.filter(item => !item.done));
const doneItems = computed(() => items.value.filter(item => item.done));

function toggleEditMode() {
  isEditMode.value = !isEditMode.value;
}

function updateItemName(id: string, newName: string) {
  const item = items.value.find(i => i.id === id);
  if (item)
    item.name = newName;
}

function toggleItem(id: string) {
  const item = items.value.find(i => i.id === id);
  if (item)
    item.done = !item.done;
}

function deleteItem(id: string) {
  items.value = items.value.filter(i => i.id !== id);
}

function addItem(name: string) {
  items.value.push({
    id: crypto.randomUUID(),
    name,
    done: false,
  });
}
</script>

<template>
  <div class="list-detail app-container">
    <div class="list-header">
      <h1 class="list-name">
        {{ listName }}
      </h1>
      <div class="actions">
        <Button
          :icon="isEditMode ? 'pi pi-times' : 'pi pi-pencil'"
          :aria-label="isEditMode ? 'Exit edit mode' : 'Edit list'"
          :label="isEditMode ? 'Exit edit mode' : 'Edit list'"
          variant="outlined"
          @click="toggleEditMode"
        />
      </div>
    </div>

    <div class="lists">
      <!-- Edit mode: single merged list -->
      <template v-if="isEditMode">
        <section aria-label="Edit list items">
          <ul class="item-list">
            <li v-for="item in items" :key="item.id">
              <ShoplistItemRow
                :item="item"
                :is-edit-mode="true"
                @update:name="updateItemName(item.id, $event)"
                @delete="deleteItem(item.id)"
              />
            </li>
            <li>
              <ShoplistItemRow
                :is-edit-mode="true"
                @add="addItem"
              />
            </li>
          </ul>
        </section>
      </template>

      <!-- Normal mode: shopping view -->
      <template v-else>
        <section class="todo-section" aria-label="Items to get">
          <h2 class="todo-heading">
            Items to get
          </h2>
          <ul v-if="itemsToGet.length > 0" class="item-list">
            <li v-for="item in itemsToGet" :key="item.id">
              <ShoplistItemRow
                :item="item"
                :is-edit-mode="false"
                @toggle="toggleItem(item.id)"
              />
            </li>
          </ul>
          <div v-else class="empty-state">
            <p>All done! Nothing left to get.</p>
          </div>
        </section>

        <section v-if="doneItems.length > 0" class="done-section" aria-label="Done items">
          <h2 class="done-heading">
            Done
          </h2>
          <ul class="item-list">
            <li v-for="item in doneItems" :key="item.id">
              <ShoplistItemRow
                :item="item"
                :is-edit-mode="false"
                @toggle="toggleItem(item.id)"
              />
            </li>
          </ul>
        </section>
      </template>
    </div>
  </div>
</template>

<style scoped>
.list-detail {
  display: flex;
  flex-direction: column;
  gap: var(--default-spacing);
}

.list-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
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

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-lg) var(--spacing-md);
  color: var(--p-surface-500);
}
</style>
