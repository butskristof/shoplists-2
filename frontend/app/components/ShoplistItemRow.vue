<script setup lang="ts">
import type { ShoplistItem } from "~/types/shoplist";
import { useConfirm } from "primevue/useconfirm";

const props = defineProps<{
  item?: ShoplistItem;
  isEditMode: boolean;
}>();

const emit = defineEmits<{
  "update:name": [name: string];
  "toggle": [];
  "delete": [];
  "add": [name: string];
}>();

const confirm = useConfirm();

const isEditing = ref(false);
const editingName = ref("");
const inputRef = useTemplateRef<{ $el: HTMLInputElement }>("inputRef");

const isAddRow = computed(() => !props.item);

watch(isEditing, (editing) => {
  if (editing) {
    editingName.value = props.item?.name ?? "";
    nextTick(() => inputRef.value?.$el?.focus());
  }
  else {
    editingName.value = "";
  }
});

watch(() => props.isEditMode, (editMode) => {
  if (!editMode)
    isEditing.value = false;
});

function startEdit() {
  if (!props.item)
    return;
  isEditing.value = true;
}

function cancelEdit() {
  if (isAddRow.value)
    return;
  isEditing.value = false;
}

function save() {
  const trimmed = editingName.value.trim();
  if (!trimmed)
    return;
  if (isAddRow.value) {
    emit("add", trimmed);
    editingName.value = "";
    nextTick(() => inputRef.value?.$el?.focus());
  }
  else {
    emit("update:name", trimmed);
    isEditing.value = false;
  }
}

function confirmDelete(event: Event) {
  confirm.require({
    target: event.currentTarget as HTMLElement,
    message: "Delete this item from your list?",
    acceptProps: { label: "Delete", severity: "danger" },
    rejectProps: { label: "Cancel", severity: "secondary", outlined: true },
    accept: () => emit("delete"),
  });
}
</script>

<template>
  <!-- Edit mode -->
  <template v-if="isEditMode">
    <div
      class="item-row" :class="{ 'item-row--editing': isEditing || isAddRow }"
    >
      <span class="drag-handle" aria-hidden="true">
        <i v-if="!isAddRow" class="pi pi-ellipsis-v" />
      </span>
      <template v-if="isEditing || isAddRow">
        <InputText
          ref="inputRef"
          v-model="editingName"
          :placeholder="isAddRow ? 'Add new item...' : ''"
          fluid
          @keydown.enter="save"
          @keydown.escape="cancelEdit"
        />
        <div class="item-row__actions">
          <template v-if="isEditing">
            <Button
              icon="pi pi-save"
              rounded
              variant="text"
              aria-label="Save"
              @click="save"
            />
            <Button
              icon="pi pi-times"
              rounded
              variant="text"
              severity="secondary"
              aria-label="Cancel"
              @click="cancelEdit"
            />
          </template>
          <template v-if="isAddRow">
            <Button
              icon="pi pi-plus"
              rounded
              variant="text"
              aria-label="Add item"
              @click="save"
            />
          </template>
        </div>
      </template>
      <template v-else>
        <span class="item-name">{{ item!.name }}</span>
        <div class="item-row__actions">
          <Button
            icon="pi pi-pencil"
            rounded
            variant="text"
            aria-label="Edit item"
            @click="startEdit"
          />
          <Button
            icon="pi pi-trash"
            rounded
            variant="text"
            severity="danger"
            aria-label="Delete item"
            @click="confirmDelete"
          />
        </div>
      </template>
    </div>
  </template>

  <!-- Normal mode: shopping view -->
  <template v-else>
    <label
      v-if="item"
      :for="`item-${item.id}`"
      class="item-row item-row--normal-mode"
      :class="{ 'item-row--done': item.done }"
    >
      <Checkbox
        :model-value="item.done"
        :input-id="`item-${item.id}`"
        binary
        size="large"
        @update:model-value="emit('toggle')"
      />
      <span class="item-name">{{ item.name }}</span>
    </label>
  </template>
</template>

<style scoped>
.item-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) var(--spacing-md);
  border-bottom: 1px solid var(--p-surface-200);
}

.drag-handle {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  cursor: grab;
  color: var(--p-surface-500);
  flex-shrink: 0;
}

.drag-handle:active {
  cursor: grabbing;
}

.item-row--normal-mode {
  cursor: pointer;
  user-select: none;
}

.item-row--done .item-name {
  text-decoration: line-through;
  opacity: 0.5;
}

.item-row__actions {
  display: flex;
  gap: var(--spacing-sm);
  margin-left: auto;
}

.item-row__actions :deep(.p-button) {
  --p-button-icon-only-width: 30px;
  //padding: 0;
  //margin: 0;
}

:deep(.p-inputtext) {
  padding: var(--spacing-xs) var(--spacing-sm);
}

@media (hover: hover) {
  .item-row:not(.item-row--editing):hover {
    background: var(--p-surface-100);

    .dark-mode & {
      background-color: var(--p-surface-800);
    }
  }
}
</style>
