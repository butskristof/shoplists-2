<script setup lang="ts">
const emit = defineEmits<{
  add: [name: string];
}>();

const editingName = ref("");
const inputInvalid = ref(false);
const inputRef = useTemplateRef<{ $el: HTMLInputElement }>("inputRef");

watch(editingName, () => {
  if (inputInvalid.value)
    inputInvalid.value = false;
});

function save() {
  const trimmed = editingName.value.trim();
  if (!trimmed) {
    inputInvalid.value = true;
    return;
  }
  emit("add", trimmed);
  editingName.value = "";
  nextTick(() => inputRef.value?.$el?.focus());
}
</script>

<template>
  <div class="item-row">
    <span class="drag-handle-spacer" aria-hidden="true" />
    <InputText
      ref="inputRef"
      v-model="editingName"
      placeholder="Add a new item..."
      :invalid="inputInvalid"
      fluid
      @keydown.enter="save"
    />
    <div class="actions">
      <Button
        v-tooltip.top="'Add item'"
        icon="pi pi-plus"
        rounded
        variant="text"
        aria-label="Add item"
        @click="save"
      />
    </div>
  </div>
</template>

<style scoped>
.item-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) var(--spacing-md);
  border-bottom: 1px solid var(--p-content-border-color);

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
