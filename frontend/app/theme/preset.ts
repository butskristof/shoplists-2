import { definePreset, palette } from "@primeuix/themes";
import Aura from "@primeuix/themes/aura";

// Market-direction preset. Built incrementally per phases in
// docs/projects/visual-identity/plan.md; target values come from
// docs/projects/visual-identity/design-tokens.md.
//
// Structure intentionally mirrors Aura (see
// node_modules/@primeuix/themes/dist/aura/base/index.mjs for the source)
// because overrides that don't match Aura's colorScheme placement are
// silently ignored — see primevue-theming.md §2.

// Light surface ramp — green-tinted near-whites through to ink. 0/50/100/200
// land on the design's surface/bg/surfaceAlt/rule; 500 is the design's muted;
// 900 is ink. Mid shades interpolate; 300-400 ≈ form-field borders.
const lightSurface = {
  0: "#FFFFFF",
  50: "#F7F8F6",
  100: "#EEF1ED",
  200: "#E4E8E3",
  300: "#D0D5CF",
  400: "#A5ACA7",
  500: "#8A928C",
  600: "#606A63",
  700: "#3D453F",
  800: "#222825",
  900: "#111714",
  950: "#0A0F0C",
};

// Dark surface ramp — mirror of light. 800/900/950 land on design
// surfaceAlt/surface/bg; 50 is ink; 400 is muted; 200 tracks inkSoft.
const darkSurface = {
  0: "#FFFFFF",
  50: "#ECEFEA",
  100: "#D8DDD5",
  200: "#B4BAB5",
  300: "#8C928E",
  400: "#7C847F",
  500: "#5D645F",
  600: "#434A46",
  700: "#2E3430",
  800: "#202825",
  900: "#161D1A",
  950: "#0D1311",
};

// Hand-tuned red→berry ramp. Design berry is #B03B2E (light) and #E56A5E
// (dark); Aura's danger components reference {red.500} in light and
// {red.400} in dark, so those slots are pinned to the exact design values.
// Non-anchored shades are interpolated for a coherent gradient. Knock-on
// effect (decision #4 in open-questions.md): every `--p-red-*` reference
// in the app — including StatePanel.vue's error variant — lands on the
// warmer ramp automatically.
const berryRamp = {
  50: "#FDF4F2",
  100: "#FAE5E1",
  200: "#F4BCB2",
  300: "#EC9486",
  400: "#E56A5E", // dark danger bg (Aura `{red.400}`)
  500: "#B03B2E", // light danger bg (Aura `{red.500}`) — design berry
  600: "#972E22",
  700: "#7D2518",
  800: "#641D11",
  900: "#4F150C",
  950: "#2A0905", // contrast ink on dark danger buttons (Aura `{red.950}`)
};

export const MarketPreset = definePreset(Aura, {
  primitive: {
    red: berryRamp,
  },

  semantic: {
    primary: palette("#0F6B50"),

    // Design's `r` (radius-sm) = 4px. Aura's form.field.border.radius
    // defaults to border.radius.md (6px). Overriding here tightens both
    // inputs AND buttons — button.root.borderRadius references the same
    // semantic token. Checkbox has its own override below (design asks
    // for 5px — a touch rounder than form fields).
    formField: {
      borderRadius: "4px",
    },

    focusRing: {
      width: "2px",
      style: "solid",
      color: "{primary.color}",
      offset: "2px",
      shadow: "none",
    },

    // `content.borderRadius` is the semantic token used by card-shaped
    // surfaces that don't have their own component override (e.g. our
    // future `.app-card` utility). Aura default is border.radius.md (6px)
    // — promote to design's `rLg` (10px).
    content: {
      borderRadius: "10px",
    },

    colorScheme: {
      light: {
        surface: lightSurface,
        primary: {
          color: "#0F6B50",
          contrastColor: "#FFFFFF",
          hoverColor: "{primary.600}",
          activeColor: "{primary.700}",
        },
        // Exact design values — don't thread these through the surface
        // ramp. Mid-ramp shades drive form-field borders etc. and have
        // no direct design equivalent, so letting them float with the
        // ramp is safer than pinning each to a semantic.
        content: {
          background: "#FFFFFF",
          hoverBackground: "#EEF1ED",
          borderColor: "#E4E8E3",
          color: "{text.color}",
          hoverColor: "{text.hover.color}",
        },
        text: {
          color: "#111714",
          hoverColor: "#111714",
          mutedColor: "#8A928C",
          hoverMutedColor: "#606A63",
        },
      },
      dark: {
        surface: darkSurface,
        primary: {
          color: "#4FB896",
          contrastColor: "#0B1512",
          hoverColor: "{primary.300}",
          activeColor: "{primary.200}",
        },
        content: {
          background: "#161D1A",
          hoverBackground: "#202825",
          // Design's rule is low-alpha white on dark — can't be
          // expressed as a surface shade without killing the
          // transparency over elevated cards.
          borderColor: "rgba(255, 255, 255, 0.08)",
          color: "{text.color}",
          hoverColor: "{text.hover.color}",
        },
        text: {
          color: "#ECEFEA",
          hoverColor: "#ECEFEA",
          mutedColor: "#7C847F",
          hoverMutedColor: "#B4BAB5",
        },
      },
    },
  },

  // Component tokens — last resort per PrimeVue best-practice, used here
  // because these values don't map cleanly to a shared semantic.
  components: {
    // 22×22, radius 5 (slightly rounder than form fields) per design.
    // Size tokens ship with sm/lg variants too; we leave those at Aura
    // defaults — `size="large"` usage in ShoplistShoppingItem renders a
    // bit bigger than 22 and will be revisited in Phase 6.
    checkbox: {
      root: {
        width: "22px",
        height: "22px",
        borderRadius: "5px",
      },
    },
    // Dialog radius pinned to 10px (rLg) — Aura default references
    // overlay.modal.borderRadius which is border.radius.xl (12px).
    dialog: {
      root: {
        borderRadius: "10px",
      },
    },
  },
});
