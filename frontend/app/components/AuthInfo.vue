<script setup lang="ts">
const { loggedIn, user } = useOidcAuth();
</script>

<template>
  <div class="auth-info">
    <p v-if="loggedIn">
      Logged in as {{ user!.userInfo!.preferred_username }} ({{ user!.userInfo!.name }})
    </p>
  </div>
</template>

<style scoped>

</style>
