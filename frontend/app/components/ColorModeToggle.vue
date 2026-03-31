<script setup lang="ts">
const colorMode = useColorMode();

const colorModeIcon = computed(() => {
  switch (colorMode.preference) {
    case "light":
      return "pi pi-sun";
    case "dark":
      return "pi pi-moon";
    default:
      return "pi pi-desktop";
  }
});

const colorModeLabel = computed(() => {
  switch (colorMode.preference) {
    case "light":
      return "Light mode";
    case "dark":
      return "Dark mode";
    default:
      return "System mode";
  }
});
function cycleColorMode() {
  const modes = ["system", "light", "dark"] as const;
  const currentIndex = modes.indexOf(colorMode.preference as typeof modes[number]);
  colorMode.preference = modes[(currentIndex + 1) % modes.length];
}
</script>

<template>
  <Button
    :icon="colorModeIcon"
    :aria-label="colorModeLabel"
    text
    rounded
    severity="secondary"
    @click="cycleColorMode"
  />
</template>

<style scoped>

</style>
