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
const inputInvalid = ref(false);
const inputRef = useTemplateRef<{ $el: HTMLInputElement }>("inputRef");

const isAddRow = computed(() => !props.item);

watch(isEditing, (editing) => {
  if (editing) {
    editingName.value = props.item?.name ?? "";
    inputInvalid.value = false;
    nextTick(() => inputRef.value?.$el?.focus());
  }
  else {
    editingName.value = "";
    inputInvalid.value = false;
  }
});

watch(editingName, () => {
  if (inputInvalid.value)
    inputInvalid.value = false;
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
  if (!trimmed) {
    inputInvalid.value = true;
    return;
  }
  inputInvalid.value = false;
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
      class="item-row item-row--edit-mode"
      :class="{ 'item-row--editing': isEditing || isAddRow }"
    >
      <span
        class="drag-handle"
        aria-hidden="true"
        :title="!isAddRow ? 'Drag to reorder' : undefined"
      >
        <i v-if="!isAddRow" class="pi pi-ellipsis-v" />
      </span>
      <template v-if="isEditing || isAddRow">
        <InputText
          ref="inputRef"
          v-model="editingName"
          :placeholder="isAddRow ? 'Add a new item...' : ''"
          :invalid="inputInvalid"
          fluid
          @keydown.enter="save"
          @keydown.escape="cancelEdit"
        />
        <div class="actions">
          <template v-if="isEditing">
            <Button
              v-tooltip.top="'Save'"
              icon="pi pi-check"
              rounded
              variant="text"
              aria-label="Save"
              @click="save"
            />
            <Button
              v-tooltip.top="'Cancel'"
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
              v-tooltip.top="'Add item'"
              icon="pi pi-plus"
              rounded
              variant="text"
              aria-label="Add item"
              @click="save"
            />
          </template>
        </div>
      </template>
      <template v-else-if="item">
        <span class="item-name">{{ item.name }}</span>
        <div class="actions">
          <Button
            v-tooltip.top="'Rename'"
            icon="pi pi-pencil"
            rounded
            variant="text"
            aria-label="Rename item"
            @click="startEdit"
          />
          <Button
            v-tooltip.top="'Delete'"
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
  <template v-else-if="item">
    <label
      :for="`item-${item.id}`"
      class="item-row item-row--normal-mode"
      :class="{ done: item.done }"
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
  border-bottom: 1px solid var(--p-content-border-color);

  .item-name {
    min-width: 0;
    overflow-wrap: break-word;
  }
}

.item-row--normal-mode {
  cursor: pointer;
  user-select: none;

  .done .item-name {
    text-decoration: line-through;
    opacity: 0.5;
  }
}

.item-row--edit-mode {
  &:not(.item-row--editing) {
    .item-name {
      min-height: 34px;
      display: flex;
      align-items: center;
    }
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

    &:active {
      cursor: grabbing;
    }
  }

  .actions {
    display: flex;
    gap: var(--spacing-sm);
    margin-left: auto;
  }

  .actions :deep(.p-button-icon-only) {
    --p-button-icon-only-width: 30px;
  }
}

.item-row--editing {
}

:deep(.p-inputtext) {
  padding: var(--spacing-xs) var(--spacing-sm);
}
@media (hover: hover) {
  .item-row:not(.item-row--editing):hover {
    background: var(--p-content-hover-background);
  }
}
</style>
