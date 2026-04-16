<script setup lang="ts">
const props = defineProps<{
  name: string;
  hideActions: boolean;
}>();

// name has separate prop/emit because it calls a mutation and receives the value from query state
const emit = defineEmits<{
  "update:name": [name: string];
}>();

// editMode on the other hand is local state we can manipulate directly with v-model
const editMode = defineModel<boolean>("editMode", { required: true });

const isEditingName = ref(false);
const editingName = ref("");
const trimmedName = computed(() => editingName.value.trim());
const nameInputInvalid = computed(() => !trimmedName.value);

watch(editMode, (editing) => {
  if (!editing)
    isEditingName.value = false;
});

function startEditingName() {
  editingName.value = props.name;
  isEditingName.value = true;
}

function saveName() {
  if (!trimmedName.value)
    return;
  emit("update:name", trimmedName.value);
  isEditingName.value = false;
}
</script>

<template>
  <div class="list-header">
    <form v-if="isEditingName" class="list-name-form" @submit.prevent="saveName">
      <InputText
        v-model="editingName"
        :invalid="nameInputInvalid"
        autofocus
        fluid
        class="list-name-input"
        @keydown.escape="isEditingName = false"
      />
      <div class="list-name-actions">
        <Button
          v-tooltip.top="'Save'"
          type="submit"
          icon="pi pi-check"
          variant="text"
          aria-label="Save list name"
        />
        <Button
          v-tooltip.top="'Cancel'"
          icon="pi pi-times"
          variant="text"
          severity="secondary"
          aria-label="Cancel renaming"
          @click="isEditingName = false"
        />
      </div>
    </form>
    <template v-else>
      <div class="list-name">
        <h1>
          {{ name }}
        </h1>
      </div>
      <div v-if="editMode" class="list-name-actions">
        <Button
          v-tooltip.top="'Rename list'"
          icon="pi pi-pencil"
          variant="text"
          aria-label="Rename list"
          @click="startEditingName"
        />
      </div>
    </template>
    <div class="actions">
      <template v-if="editMode">
        <Button
          icon="pi pi-times"
          aria-label="Exit edit mode"
          label="Exit edit mode"
          @click="editMode = false"
        />
      </template>
      <template v-else>
        <Button
          v-if="!hideActions"
          icon="pi pi-pencil"
          aria-label="Edit list"
          label="Edit list"
          @click="editMode = true"
        />
      </template>
    </div>
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
      /* match line height to input text height so UI stays stable when toggling edit mode */
      line-height: 50px;
    }
  }

  .list-name-form {
    display: flex;
    flex: 1 1 auto;
    min-width: 0;
    gap: var(--default-spacing);
    align-items: center;
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
</style>
