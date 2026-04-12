import type { paths } from "~/generated/api";
import createClient from "openapi-fetch";

export const api = createClient<paths>({
  // All requests target the BFF proxy at /api, which extracts the access
  // token from the encrypted OIDC session and forwards the request to the
  // backend with a Bearer header. No auth handling needed client-side.
  baseUrl: "/api",
  headers: {
    // CSRF protection: the BFF proxy rejects requests without this header.
    // A non-standard header forces a CORS preflight on every request,
    // effectively restricting callers to same-origin.
    // See server/api/[...path].ts for the server-side check.
    "x-csrf": "1",
  },
});
