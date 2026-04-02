<script setup lang="ts">

</script>

<template>
  <div class="app-footer">
    <div class="app-container footer-content">
      <div class="actions">
        <ColorModeToggle />
      </div>
    </div>
  </div>
</template>

<style scoped>
.footer-content {
  padding: var(--default-spacing);
  @media (min-width: 768px) {
    padding-block: var(--spacing-md);
    padding-inline: var(--spacing-lg);
  }

  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;

  //border-top: 1px solid var(--p-content-border-color);

  .actions {
    margin-right: calc(-1 * var(--p-button-padding-x));
  }
}
</style>
