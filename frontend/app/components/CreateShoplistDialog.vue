<script setup lang="ts">
const emit = defineEmits<{
  close: [];
  created: [id: string];
}>();

const { createList } = useShoplists();
const toast = useToast();

const name = ref("");
const nameInvalid = ref(false);
const isCreating = ref(false);

watch(name, () => {
  if (nameInvalid.value)
    nameInvalid.value = false;
});

async function handleCreate() {
  const trimmed = name.value.trim();
  if (!trimmed) {
    nameInvalid.value = true;
    return;
  }
  nameInvalid.value = false;
  isCreating.value = true;
  const id = await createList(trimmed);
  isCreating.value = false;
  if (id) {
    emit("close");
    emit("created", id);
  }
  else {
    toast.add({ severity: "error", summary: "Failed to create list", life: 3000 });
  }
}
</script>

<template>
  <Dialog
    visible
    header="Create new list"
    modal
    :style="{ width: '24rem' }"
    @update:visible="emit('close')"
  >
    <div class="create-dialog-content">
      <label for="new-list-name">Name</label>
      <InputText
        id="new-list-name"
        v-model="name"
        :invalid="nameInvalid"
        autofocus
        fluid
        placeholder="e.g. Weekly groceries"
        @keydown.enter="handleCreate"
      />
    </div>
    <template #footer>
      <Button
        label="Cancel"
        severity="secondary"
        variant="text"
        @click="emit('close')"
      />
      <Button
        label="Create"
        icon="pi pi-plus"
        :loading="isCreating"
        @click="handleCreate"
      />
    </template>
  </Dialog>
</template>

<style scoped>
.create-dialog-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}
</style>
