<script setup lang="ts">
import type { ShoplistItem } from "~/types/shoplist";
import { useConfirm } from "primevue/useconfirm";

const props = defineProps<{
  item: ShoplistItem;
}>();

const emit = defineEmits<{
  "update:name": [name: string];
  "delete": [];
}>();

const confirm = useConfirm();

const isEditing = ref(false);
const editingName = ref("");
const trimmedName = computed(() => editingName.value.trim());
const inputInvalid = computed(() => !trimmedName.value);

function startEditing() {
  editingName.value = props.item.name;
  isEditing.value = true;
}

function save() {
  if (!trimmedName.value)
    return;
  emit("update:name", trimmedName.value);
  isEditing.value = false;
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
  <div
    class="item-row"
    :class="{ editing: isEditing }"
  >
    <span class="drag-handle" aria-hidden="true" title="Drag to reorder">
      <i class="pi pi-ellipsis-v" />
    </span>
    <form v-if="isEditing" class="edit-form" @submit.prevent="save">
      <InputText
        v-model="editingName"
        :invalid="inputInvalid"
        autofocus
        fluid
        @keydown.escape="isEditing = false"
      />
      <div class="actions">
        <Button
          v-tooltip.top="'Save'"
          type="submit"
          icon="pi pi-check"
          rounded
          variant="text"
          aria-label="Save"
        />
        <Button
          v-tooltip.top="'Cancel'"
          icon="pi pi-times"
          rounded
          variant="text"
          severity="secondary"
          aria-label="Cancel"
          @click="isEditing = false"
        />
      </div>
    </form>
    <template v-else>
      <span class="item-name">{{ item.name }}</span>
      <div class="actions">
        <Button
          v-tooltip.top="'Rename'"
          icon="pi pi-pencil"
          rounded
          variant="text"
          aria-label="Rename item"
          @click="startEditing"
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

<style scoped>
.item-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) var(--spacing-md);
  border-bottom: 1px solid var(--p-content-border-color);

  .edit-form {
    display: flex;
    align-items: center;
    gap: var(--spacing-md);
    flex: 1;
    min-width: 0;
  }

  &:not(.editing) {
    .item-name {
      min-width: 0;
      overflow-wrap: break-word;
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

:deep(.p-inputtext) {
  padding: var(--spacing-xs) var(--spacing-sm);
}

@media (hover: hover) {
  .item-row:not(.editing):hover {
    background: var(--p-content-hover-background);
  }
}
</style>
