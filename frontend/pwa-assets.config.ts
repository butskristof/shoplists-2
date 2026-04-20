import { minimal2023Preset as basePreset, defineConfig } from "@vite-pwa/assets-generator/config";

// Background applied around the source in the generator's padding area. Matches --p-surface-950
// (zinc.950, app dark-mode background) so the padding blends with our built-in source tile.
const darkSurface = "#09090b";

export default defineConfig({
  headLinkOptions: {
    preset: "2023",
    // Default resolves the SVG favicon link to the source image filename
    // (`/pwa-source.svg`). We ship a separate simplified browser-tab icon at
    // `/favicon.svg`, so point the head link at that instead.
    resolveSvgName: () => "favicon.svg",
  },
  preset: {
    ...basePreset,
    // Override padding: preset defaults to 0.3 for both. That stacks with the source SVG's own
    // safe-zone and leaves the cart uncomfortably small on home screens and tabs.
    maskable: {
      ...basePreset.maskable,
      padding: 0.15,
      resizeOptions: { fit: "contain", background: darkSurface },
    },
    apple: {
      ...basePreset.apple,
      padding: 0.1,
      resizeOptions: { fit: "contain", background: darkSurface },
    },
  },
  images: ["public/pwa-source.svg"],
});
