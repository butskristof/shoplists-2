<script setup lang="ts">
import { useRouteQuery } from "@vueuse/router";
import { useDraggable } from "vue-draggable-plus";

const props = defineProps<{ listId: string }>();

const {
  list,
  sortedItems,
  itemsToGet,
  fulfilledItems,
  updateListName,
  addItem,
  deleteItem,
  updateItemName,
  toggleItem,
  reorderItem,
} = useShoplist(props.listId);
const hasNoItems = computed(() => list.value?.items.length === 0);

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
  onUpdate(event) {
    const itemId = event.item.dataset?.itemId;
    if (itemId == null || event.newIndex == null)
      return;
    reorderItem(itemId, event.newIndex + 1);
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

// List name editing
const isEditingName = ref(false);
const editingName = ref("");
const nameInputInvalid = ref(false);

watch(isEditingName, (editing) => {
  editingName.value = editing ? list.value?.name ?? "" : "";
  nameInputInvalid.value = false;
});

watch(editingName, () => {
  if (nameInputInvalid.value)
    nameInputInvalid.value = false;
});

watch(isEditMode, (editMode) => {
  if (!editMode)
    isEditingName.value = false;
});

function toggleEditMode() {
  isEditMode.value = !isEditMode.value;
}

function saveName() {
  const trimmed = editingName.value.trim();
  nameInputInvalid.value = !trimmed;
  if (nameInputInvalid.value) {
    return;
  }

  updateListName(trimmed);
  isEditingName.value = false;
}
</script>

<template>
  <div class="list-header">
    <div class="list-name">
      <InputText
        v-if="isEditingName"
        v-model="editingName"
        :invalid="nameInputInvalid"
        autofocus
        fluid
        class="list-name-input"
        @keydown.enter="saveName"
        @keydown.escape="isEditingName = false"
      />
      <h1 v-else>
        {{ list.name }}
      </h1>
    </div>
    <div v-if="isEditMode" class="list-name-actions">
      <template v-if="isEditingName">
        <Button
          v-tooltip.top="'Save'"
          icon="pi pi-check"
          rounded
          variant="text"
          aria-label="Save list name"
          @click="saveName"
        />
        <Button
          v-tooltip.top="'Cancel'"
          icon="pi pi-times"
          rounded
          variant="text"
          severity="secondary"
          aria-label="Cancel renaming"
          @click="isEditingName = false"
        />
      </template>
      <Button
        v-else
        v-tooltip.top="'Rename list'"
        icon="pi pi-pencil"
        variant="text"
        aria-label="Rename list"
        @click="isEditingName = true"
      />
    </div>
    <div class="actions">
      <template v-if="isEditMode">
        <Button
          icon="pi pi-times"
          aria-label="Exit edit mode"
          label="Exit edit mode"
          @click="toggleEditMode"
        />
      </template>
      <template v-else>
        <Button
          v-if="!hasNoItems"
          icon="pi pi-pencil"
          aria-label="Edit list"
          label="Edit list"
          @click="toggleEditMode"
        />
      </template>
    </div>
  </div>

  <div class="list-items">
    <!-- Edit mode: single merged list -->
    <template v-if="isEditMode">
      <section aria-label="Edit list items">
        <ul ref="editList" class="item-list">
          <li v-for="item in sortedItems" :key="item.id" :data-item-id="item.id">
            <ShoplistItemRow
              :item="item"
              :is-edit-mode="true"
              @update:name="updateItemName(item.id, $event)"
              @delete="deleteItem(item.id)"
            />
          </li>
        </ul>
        <ShoplistItemRow
          :is-edit-mode="true"
          @add="addItem"
        />
      </section>
    </template>

    <template v-else>
      <StatePanel
        v-if="hasNoItems"
        icon="pi pi-check-circle"
        message="Ready to go! Add some items to your list to get started."
      >
        <template #action>
          <Button
            label="Add items"
            icon="pi pi-pencil"
            @click="toggleEditMode"
          />
        </template>
      </StatePanel>

      <!-- Normal mode: shopping view -->
      <template v-else>
        <section class="items-to-get-section" aria-label="Items to get">
          <h2 class="normal-mode-heading">
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
                @click="toggleEditMode"
              />
            </template>
          </StatePanel>
          <ul v-else class="item-list">
            <li v-for="item in itemsToGet" :key="item.id">
              <ShoplistItemRow
                :item="item"
                :is-edit-mode="false"
                @toggle="toggleItem(item.id)"
              />
            </li>
          </ul>
        </section>

        <section v-if="fulfilledItems.length > 0" class="fulfilled-section" aria-label="Fulfilled items">
          <h2 class="normal-mode-heading">
            Fulfilled
          </h2>
          <ul class="item-list">
            <li v-for="item in fulfilledItems" :key="item.id">
              <ShoplistItemRow
                :item="item"
                :is-edit-mode="false"
                @toggle="toggleItem(item.id)"
              />
            </li>
          </ul>
        </section>
      </template>
    </template>
  </div>
</template>

<style scoped>
.list-header {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: var(--default-spacing);
  min-width: 0;

  margin-bottom: var(--default-spacing);

  .list-name {
    flex: 1 1 auto;
    min-width: min-content;

    h1 {
      overflow-wrap: break-word;
    }
  }

  .list-name-input {
    min-width: 200px;
    flex: 1;
    font-size: var(--font-size-2xl);
    line-height: var(--font-size-2xl--line-height);
    font-weight: var(--weight-bold);
  }

  .list-name-actions {
    flex-shrink: 0;
  }

  .actions {
    flex-shrink: 0;
  }
}

.list-items {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

.item-list {
  list-style: none;
  padding: 0;
}

.normal-mode-heading {
  font-size: var(--font-size-base);
  line-height: var(--font-size-base--line-height);
  font-weight: var(--weight-semibold);
  color: var(--p-text-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: var(--spacing-sm);
}
</style>
