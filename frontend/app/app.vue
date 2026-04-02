<script setup lang="ts">
useHead({
  titleTemplate: title => title ? `${title} — Shoplists` : "Shoplists",
});
</script>

<template>
  <NuxtRouteAnnouncer />
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
  <ConfirmPopup />
  <Toast position="bottom-right" />
</template>
