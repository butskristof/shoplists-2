<script setup lang="ts">
throw createError({
  statusCode: 500,
  statusMessage: "Internal Server Error",
  message: "Database connection failed: ECONNREFUSED 127.0.0.1:5432",
  fatal: true,
});
</script>

<template>
  <div />
</template>
