<script setup lang="ts">
const props = defineProps<{
  listName: string;
  isDeletingList: boolean;
}>();

const emit = defineEmits<{
  close: [];
  delete: [];
}>();
</script>

<template>
  <Dialog
    visible
    modal
    header="Delete list"
    :style="{ width: '24rem' }"
    @update:visible="emit('close')"
  >
    <p>Are you sure you want to delete the list <strong>{{ props.listName }}</strong>? This action cannot be undone.</p>
    <template #footer>
      <div class="footer-actions">
        <Button
          v-if="!isDeletingList"
          autofocus
          label="Cancel"
          severity="secondary"
          variant="text"
          @click="emit('close')"
        />
        <Button
          label="Delete"
          icon="pi pi-trash"
          severity="danger"
          :loading="isDeletingList"
          @click="emit('delete')"
        />
      </div>
    </template>
  </Dialog>
</template>

<style scoped>
.footer-actions {
  display: flex;
  flex-direction: row;
  gap: var(--default-spacing);
}
</style>
