import { minimal2023Preset as basePreset, defineConfig } from "@vite-pwa/assets-generator/config";

// Background applied around the source in the generator's padding area. Must match the
// rect fill in pwa-source.svg so padding blends seamlessly with the source tile, and the
// PWA manifest's background_color so the iOS splash transitions cleanly into the icon.
const iconBackground = "#F7F8F6";

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
      resizeOptions: { fit: "contain", background: iconBackground },
    },
    apple: {
      ...basePreset.apple,
      padding: 0.1,
      resizeOptions: { fit: "contain", background: iconBackground },
    },
  },
  images: ["public/pwa-source.svg"],
});
