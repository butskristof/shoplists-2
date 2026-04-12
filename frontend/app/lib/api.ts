import type { paths } from "~/generated/api";
import createClient from "openapi-fetch";

export const api = createClient<paths>({
  baseUrl: "/",
});
