<script setup lang="ts">
const emit = defineEmits<{
  add: [name: string];
}>();

const editingName = ref("");
const hasAttemptedSave = ref(false);
const trimmedName = computed(() => editingName.value.trim());
const inputInvalid = computed(() => hasAttemptedSave.value && !trimmedName.value);

function save() {
  hasAttemptedSave.value = true;
  if (!trimmedName.value)
    return;
  emit("add", trimmedName.value);
  editingName.value = "";
  hasAttemptedSave.value = false;
}
</script>

<template>
  <form class="item-row" @submit.prevent="save">
    <span class="drag-handle-spacer" aria-hidden="true" />
    <InputText
      v-model="editingName"
      placeholder="Add a new item..."
      :invalid="inputInvalid"
      fluid
    />
    <div class="actions">
      <Button
        v-tooltip.top="'Add item'"
        type="submit"
        icon="pi pi-plus"
        rounded
        variant="text"
        aria-label="Add item"
      />
    </div>
  </form>
</template>

<style scoped>
.item-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) var(--spacing-md);

  .drag-handle-spacer {
    width: 22px;
    flex-shrink: 0;
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
</style>
