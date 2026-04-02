<script setup lang="ts">
import { useRouteQuery } from "@vueuse/router";
import { useToast } from "primevue/usetoast";
import { useDraggable } from "vue-draggable-plus";

const route = useRoute();
const listId = route.params.id as string;
const toast = useToast();

const {
  list,
  sortedItems,
  itemsToGet,
  doneItems,
  toggleItem,
  addItem,
  deleteItem,
  updateItemName,
  reorderItem,
} = useShoplist(listId);

useHead({
  title: computed(() => list.value?.name),
});

async function handleToggle(itemId: string) {
  await toggleItem(itemId);
}

async function handleAdd(name: string) {
  await addItem(name);
}

async function handleDelete(itemId: string) {
  const success = await deleteItem(itemId);
  if (success)
    toast.add({ severity: "info", summary: "Item removed", life: 2000 });
}

async function handleUpdateName(itemId: string, name: string) {
  await updateItemName(itemId, name);
}

const isEditMode = useRouteQuery("edit", String(false), {
  transform: {
    get: v => v === String(true),
    set: v => String(v),
  },
  mode: "replace",
});
const editList = ref<HTMLElement | null>(null);
const { start, destroy } = useDraggable(editList, {
  immediate: false,
  handle: ".drag-handle",
  animation: 150,
  onUpdate(evt) {
    const itemId = evt.item.dataset?.itemId;
    if (itemId == null || evt.newIndex == null)
      return;
    reorderItem(itemId, evt.newIndex + 1);
  },
});

watch([isEditMode, editList], ([editMode, el]) => {
  if (editMode && el)
    start();
  else
    destroy();
  // immediate true: run on first page load (if starting in edit mode already)
  // flush post: watcher runs after DOM changes, so editList ref should already be populated
  // prevents the watcher running twice: once for isEditMode, then for the editList
  // being populated as a result of that changing
}, { flush: "post", immediate: true });

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
            @click="toggleEditMode"
          />
        </div>
      </div>

      <div class="lists">
        <!-- Edit mode: single merged list -->
        <template v-if="isEditMode">
          <section aria-label="Edit list items">
            <ul ref="editList" class="item-list">
              <li v-for="item in sortedItems" :key="item.id" :data-item-id="item.id">
                <ShoplistItemRow
                  :item="item"
                  :is-edit-mode="true"
                  @update:name="handleUpdateName(item.id, $event)"
                  @delete="handleDelete(item.id)"
                />
              </li>
            </ul>
            <ShoplistItemRow
              :is-edit-mode="true"
              @add="handleAdd"
            />
          </section>
        </template>

        <!-- Normal mode: shopping view -->
        <template v-else-if="list.items.length > 0">
          <section class="todo-section" aria-label="Items to get">
            <h2 class="todo-heading">
              Items to get
            </h2>
            <ul v-if="itemsToGet.length > 0" class="item-list">
              <li v-for="item in itemsToGet" :key="item.id">
                <ShoplistItemRow
                  :item="item"
                  :is-edit-mode="false"
                  @toggle="handleToggle(item.id)"
                />
              </li>
            </ul>
            <div v-else class="empty-state">
              <i class="pi pi-check-circle icon" />
              <p>All done! Nothing left to get.</p>
              <Button
                label="Add more items"
                icon="pi pi-pencil"
                variant="outlined"
                size="small"
                @click="toggleEditMode"
              />
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
                  @toggle="handleToggle(item.id)"
                />
              </li>
            </ul>
          </section>
        </template>

        <div v-else class="empty-state">
          <i class="pi pi-check-circle icon" />
          <p>Ready to go! Add some items to your list to get started.</p>
          <Button
            label="Add items"
            icon="pi pi-pencil"
            variant="outlined"
            size="small"
            @click="toggleEditMode"
          />
        </div>
      </div>
    </template>

    <div v-else class="not-found">
      <p>This list could not be found.</p>
    </div>
  </div>
</template>

<style scoped>
.list-detail {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--default-spacing);

  @media (min-width: 640px) {
    max-width: 600px;
  }
  @media (min-width: 768px) {
    max-width: 720px;
  }
}

.list-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: var(--default-spacing);
  min-width: 0;

  margin-bottom: var(--default-spacing);
}

.list-name {
  min-width: 0;
  overflow-wrap: break-word;
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
  color: var(--p-text-muted-color);
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
  text-align: center;

  .icon {
    font-size: var(--font-size-4xl);
    line-height: var(--font-size-4xl--line-height);
  }
}
</style>
