<script setup lang="ts">
useHead({ title: "Your lists" });

const { lists } = useShoplists();
</script>

<template>
  <div class="list-overview app-container">
    <h1>Your lists</h1>

    <AuthInfo />

    <ul v-if="lists.length > 0" class="list">
      <li v-for="item in lists" :key="item.id">
        <NuxtLink
          :to="{ name: 'lists-id', params: { id: item.id } }"
          class="list-row"
        >
          <div class="content">
            <span class="name">{{ item.name }}</span>
            <span class="meta">
              <template v-if="item.itemCount === 0">No items</template>
              <template v-else>{{ item.doneCount }}/{{ item.itemCount }} done</template>
            </span>
          </div>
          <i class="pi pi-chevron-right chevron" />
        </NuxtLink>
      </li>
    </ul>

    <div v-else class="empty-state">
      <i class="pi pi-list icon" />
      <p>No lists yet</p>
      <p class="hint">
        Create a list to start tracking what you need.
      </p>
      <Button
        label="Create list"
        icon="pi pi-plus"
      />
    </div>
  </div>
</template>

<style scoped>
.list-overview {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--default-spacing);

  @media (min-width: 640px) {
    max-width: 600px;
  }
  @media (min-width: 768px) {
    max-width: 720px;
  }
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
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
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
  }
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-lg) var(--spacing-md);
  text-align: center;

  .icon {
    font-size: var(--font-size-4xl);
    line-height: var(--font-size-4xl--line-height);
  }

  .hint {
    font-size: var(--font-size-sm);
    line-height: var(--font-size-sm--line-height);
    margin-bottom: var(--default-spacing);
  }
}

@media (hover: hover) {
  .list-row:hover {
    background: var(--p-content-hover-background);
  }
}
</style>
