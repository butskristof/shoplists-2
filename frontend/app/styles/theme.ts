import { definePreset, palette } from "@primeuix/themes";
import Aura from "@primeuix/themes/aura";

export const Market = definePreset(Aura, {
  primitive: {
    // Repurpose Aura's danger family (Button severity="danger", error states) to the
    // Market berry hue. One override = all destructive components updated.
    red: palette("#B03B2E"),
  },
  semantic: {
    // Forest-green primary ramp used by the light scheme (Aura derives
    // primary.color from {primary.500}, hover from {primary.600}, etc.).
    // Dark replaces this with its own mint ramp inside colorScheme.dark.
    primary: palette("#0F6B50"),
    colorScheme: {
      light: {
        // Near-neutral surface ramp with a faint green cast (R < G > B across the
        // ramp). 50 = app bg, 100 = surface alt, 200 = rule/divider — anchored on
        // the Market design baseline. Drives content backgrounds, borders, hovers,
        // form fills, and text color through Aura's existing semantic chains.
        surface: {
          0: "#FFFFFF",
          50: "#F7F8F6",
          100: "#EEF1ED",
          200: "#E4E8E3",
          300: "#C9D0C7",
          400: "#97A095",
          500: "#6E776C",
          600: "#525B50",
          700: "#3A4239",
          800: "#262C24",
          900: "#181D17",
          950: "#0E120D",
        },
        // Custom semantic group → emitted as --p-accent-*. Reserved for a future
        // <AccentButton> variant; usable today via var(--p-accent-color) in any CSS.
        accent: {
          color: "#D9603E",
          contrastColor: "#FFFFFF",
          hoverColor: "#C84F2E",
          activeColor: "#A8421F",
        },
        // Use var(--p-shadow-card) for elevated surfaces (list cards, home tiles).
        // Aura's overlay.* shadows (Dialog/Popover) stay on defaults.
        shadow: {
          card: "0 1px 0 rgba(15, 23, 18, 0.04), 0 12px 28px -16px rgba(15, 23, 18, 0.12)",
        },
      },
      dark: {
        // Green-cast near-black ramp. surface.900 = card, .950 = app bg, .800 = lifted.
        // Drop shadows are invisible on dark surfaces — depth comes from these steps
        // plus content.borderColor (= surface.700).
        surface: {
          0: "#FFFFFF",
          50: "#E5E8E3",
          100: "#CACEC7",
          200: "#A6ABA4",
          300: "#7E847C",
          400: "#5C615A",
          500: "#444842",
          600: "#353932",
          700: "#282C26",
          800: "#1F2421",
          900: "#171D1A",
          950: "#0E1411",
        },
        // Dark deliberately shifts to a mint family for legibility on the
        // green-black bg (forest-derived {primary.400} is too dark). Spreading
        // palette('#4FB896') gives dark its own 50–950 ramp so highlight tints
        // and any other {primary.X} references in this scheme stay in the same
        // hue family as the explicit primary.color override. Light continues to
        // use the top-level forest palette.
        primary: {
          ...(palette("#4FB896") as Record<string, string>),
          color: "#4FB896",
          contrastColor: "#0B1512",
          hoverColor: "#62C2A4",
          activeColor: "#76CCB2",
        },
        accent: {
          color: "#E88066",
          contrastColor: "#0B1512",
          hoverColor: "#EE8E76",
          activeColor: "#F2A088",
        },
        // No drop shadow on dark; rely on surface stepping for depth.
        shadow: {
          card: "none",
        },
      },
    },
  },
});
