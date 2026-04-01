<script setup lang="ts">
const { lists } = useShoplists();
</script>

<template>
  <div class="list-overview app-container">
    <h1>Your lists</h1>

    <ul v-if="lists.length > 0" class="list">
      <li v-for="item in lists" :key="item.id">
        <NuxtLink
          :to="{ name: 'lists-id', params: { id: item.id } }"
          class="list-row"
        >
          <div class="list-row__content">
            <span class="list-row__name">{{ item.name }}</span>
            <span class="list-row__meta">
              <template v-if="item.itemCount === 0">No items</template>
              <template v-else>{{ item.doneCount }}/{{ item.itemCount }} done</template>
            </span>
          </div>
          <i class="pi pi-chevron-right list-row__chevron" />
        </NuxtLink>
      </li>
    </ul>

    <div v-else class="empty-state">
      <p>No lists yet.</p>
    </div>
  </div>
</template>

<style scoped>
.list-overview {
  display: flex;
  flex-direction: column;
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
}

.list-row__content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
  min-width: 0;
}

.list-row__name {
  font-weight: var(--weight-medium);
  font-size: var(--font-size-lg);
  line-height: var(--font-size-lg--line-height);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.list-row__meta {
  font-size: var(--font-size-sm);
  line-height: var(--font-size-sm--line-height);
  color: var(--p-surface-500);
}

.list-row__chevron {
  margin-left: auto;
  color: var(--p-surface-400);
  flex-shrink: 0;
  font-size: var(--font-size-sm);
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-lg) var(--spacing-md);
  color: var(--p-surface-500);
}

@media (hover: hover) {
  .list-row:hover {
    background: var(--p-surface-100);

    .dark-mode & {
      background-color: var(--p-surface-800);
    }
  }
}
</style>
