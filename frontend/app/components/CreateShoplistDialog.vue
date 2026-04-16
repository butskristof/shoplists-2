<script setup lang="ts">
const emit = defineEmits<{
  close: [];
  created: [id: string];
}>();

const { createList, isCreatingList } = useShoplists();

const name = ref("");
const hasAttemptedCreate = ref(false);
const trimmedName = computed(() => name.value.trim());
const nameInvalid = computed(() => hasAttemptedCreate.value && !trimmedName.value);

async function handleCreate() {
  hasAttemptedCreate.value = true;
  if (!trimmedName.value)
    return;

  const id = await createList(trimmedName.value);
  emit("close");
  emit("created", id);
  // Failure toast is surfaced by the mutation's onError handler.
}
</script>

<template>
  <Dialog
    visible
    modal
    header="Create new list"
    :style="{ width: '24rem' }"
    @update:visible="emit('close')"
  >
    <form
      id="create-list-form"
      class="create-list-form"
      @submit.prevent="handleCreate"
    >
      <div class="form-field">
        <label for="new-list-name">Name</label>
        <InputText
          id="new-list-name"
          v-model="name"
          :invalid="nameInvalid"
          autofocus
          fluid
          placeholder="e.g. Weekly groceries"
        />
      </div>
    </form>
    <template #footer>
      <div class="footer-actions">
        <Button
          v-if="!isCreatingList"
          label="Cancel"
          severity="secondary"
          variant="text"
          @click="emit('close')"
        />
        <Button
          label="Create"
          icon="pi pi-plus"
          type="submit"
          form="create-list-form"
          :loading="isCreatingList"
        />
      </div>
    </template>
  </Dialog>
</template>

<style scoped>
.form-field {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.footer-actions {
  display: flex;
  flex-direction: row;
  gap: var(--default-spacing);
}
</style>
