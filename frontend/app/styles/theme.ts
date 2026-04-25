import { definePreset, palette } from "@primeuix/themes";
import Aura from "@primeuix/themes/aura";

export const Market = definePreset(Aura, {
  primitive: {
    red: palette("#B03B2E"),
  },
  semantic: {
    primary: palette("#0F6B50"),
  },
  // semantic: {
  //   primary: {
  //     50: "{indigo.50}",
  //     100: "{indigo.100}",
  //     200: "{indigo.200}",
  //     300: "{indigo.300}",
  //     400: "{indigo.400}",
  //     500: "{indigo.500}",
  //     600: "{indigo.600}",
  //     700: "{indigo.700}",
  //     800: "{indigo.800}",
  //     900: "{indigo.900}",
  //     950: "{indigo.950}",
  //   },
  // },
});
