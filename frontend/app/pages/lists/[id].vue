<script setup lang="ts">
const route = useRoute();
const listId = route.params.id as string;

const {
  list,
  itemsToGet,
  doneItems,
  toggleItem,
  addItem,
  deleteItem,
  updateItemName,
} = useShoplist(listId);

const isEditMode = ref(false);

function toggleEditMode() {
  isEditMode.value = !isEditMode.value;
}
</script>

<template>
  <div class="list-detail app-container">
    <Button
      v-slot="slotProps"
      class="back-button"
      as-child
      text
    >
      <NuxtLink to="/" class="back-button" :class="slotProps.class">
        <i class="pi pi-arrow-left" />
        Back to lists
      </NuxtLink>
    </Button>

    <template v-if="list">
      <div class="list-header">
        <h1 class="list-name">
          {{ list.name }}
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
              <li v-for="item in list.items" :key="item.id">
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
    </template>

    <div v-else class="not-found">
      <p>This list could not be found.</p>
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

  margin-bottom: var(--default-spacing);
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

.back-button {
  align-self: flex-start;
  margin-left: calc(-1 * var(--p-button-padding-x));
}

.not-found,
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-lg) var(--spacing-md);
  color: var(--p-surface-500);
}
</style>
