<script setup lang="ts">
const colorMode = useColorMode();

const colorModeConfig = computed(() => {
  switch (colorMode.preference) {
    case "light":
      return { icon: "pi pi-sun", label: "Light mode" };
    case "dark":
      return { icon: "pi pi-moon", label: "Dark mode" };
    default:
      return { icon: "pi pi-desktop", label: "System mode" };
  }
});

function cycleColorMode() {
  const modes = ["system", "light", "dark"] as const;
  const currentIndex = modes.indexOf(colorMode.preference as typeof modes[number]);
  colorMode.preference = modes[(currentIndex + 1) % modes.length]!;
}
</script>

<template>
  <Button
    :icon="colorModeConfig.icon"
    :aria-label="colorModeConfig.label"
    text
    rounded
    severity="secondary"
    @click="cycleColorMode"
  />
</template>
