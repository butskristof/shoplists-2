<script setup lang="ts">
useHead({ title: "Your lists" });

const { lists, isPending, isError } = useShoplists();
const hasNoLists = computed(() => lists.value?.length === 0);

const showCreateDialog = ref(false);

async function handleCreated(id: string) {
  await navigateTo({ name: "lists-id", params: { id } });
}
</script>

<template>
  <div class="list-overview app-container">
    <div class="page-header">
      <h1>Your lists</h1>
      <Button
        v-if="lists"
        label="New list"
        icon="pi pi-plus"
        @click="showCreateDialog = true"
      />
    </div>

    <CreateShoplistDialog
      v-if="showCreateDialog"
      @close="showCreateDialog = false"
      @created="handleCreated"
    />

    <StatePanel
      v-if="isError"
      variant="error"
      icon="pi pi-exclamation-circle"
      message="Something went wrong loading your lists."
    />

    <LoadingPanel v-else-if="isPending" />

    <ul v-else-if="lists && lists.length > 0" class="list">
      <li v-for="item in lists" :key="item.id">
        <NuxtLink
          :to="{ name: 'lists-id', params: { id: item.id } }"
          class="list-row"
        >
          <div class="content">
            <span class="name">{{ item.name }}</span>
            <span class="meta">
              <template v-if="item.items.total === 0">No items</template>
              <template v-else>{{ item.items.fulfilled }}/{{ item.items.total }} fulfilled</template>
            </span>
          </div>
          <i class="pi pi-chevron-right chevron" />
        </NuxtLink>
      </li>
    </ul>

    <StatePanel
      v-else-if="hasNoLists"
      icon="pi pi-list"
      title="No lists yet"
      message="Create a list to get started"
    >
      <template #action>
        <Button
          label="Create list"
          icon="pi pi-plus"
          @click="showCreateDialog = true"
        />
      </template>
    </StatePanel>
  </div>
</template>

<style scoped>
.list-overview {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--default-spacing);
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: var(--default-spacing);
}

.list {
  list-style: none;
  padding: 0;
}

.list-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
  border-bottom: 1px solid var(--p-content-border-color);
  text-decoration: none;
  color: inherit;

  .content {
    display: flex;
    flex-direction: column;
    gap: var(--spacing-xs);
    min-width: 0;

    .name {
      font-weight: var(--weight-medium);
      font-size: var(--font-size-lg);
      line-height: var(--font-size-lg--line-height);
      //overflow: hidden;
      //text-overflow: ellipsis;
      //white-space: nowrap;
    }

    .meta {
      font-size: var(--font-size-sm);
      line-height: var(--font-size-sm--line-height);
      color: var(--p-text-muted-color);
    }
  }

  .chevron {
    margin-left: auto;
    color: var(--p-surface-400);
    flex-shrink: 0;
    font-size: var(--font-size-sm);
    line-height: var(--font-size-sm--line-height);
  }
}

@media (hover: hover) {
  .list-row:hover {
    background: var(--p-content-hover-background);
  }
}
</style>
