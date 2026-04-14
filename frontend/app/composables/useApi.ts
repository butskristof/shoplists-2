import type { paths } from "~/generated/api";
import createClient from "openapi-fetch";

/**
 * Returns an openapi-fetch client wired correctly for the current rendering
 * context.
 *
 * On the client, the setup is trivial: a relative `/api` baseUrl, with the
 * browser handling cookies automatically.
 *
 * On the server (during SSR / onServerPrefetch), two things have to be done
 * manually that the browser would otherwise handle:
 *  - URLs must be absolute. Node's undici-based fetch rejects relative URLs,
 *    so we resolve `/api` against the incoming request's origin.
 *  - The session cookie must be explicitly forwarded so the BFF's
 *    getUserSession finds the encrypted OIDC session. Without forwarding,
 *    the BFF returns 401, the SSR query fails, dehydrate() drops the failed
 *    query, and the client hydrates empty — producing a hydration mismatch
 *    (server rendered the error branch, client renders the loading branch).
 *
 * Must be called within a Nuxt setup context (composable, <script setup>),
 * synchronously and before any top-level await, because useRequestURL and
 * useRequestEvent rely on the active component instance.
 */
export function useApi() {
  if (import.meta.server) {
    const requestUrl = useRequestURL();
    const event = useRequestEvent();
    const cookie = event?.headers.get("cookie") ?? "";

    const serverFetch: typeof globalThis.fetch = (input, init) => {
      // openapi-fetch passes a `Request` object as `input`. Per the fetch
      // spec, `init.headers` REPLACES the Request's headers wholesale, so we
      // must seed our Headers from `input.headers` to preserve the headers
      // openapi-fetch set (notably the `x-csrf` header the BFF requires).
      const headers = new Headers(input instanceof Request ? input.headers : init?.headers);
      if (cookie) {
        headers.set("cookie", cookie);
      }
      return globalThis.fetch(input, { ...init, headers });
    };

    return createClient<paths>({
      baseUrl: `${requestUrl.origin}/api`,
      headers: { "x-csrf": "1" },
      fetch: serverFetch,
    });
  }

  return createClient<paths>({
    baseUrl: "/api",
    headers: { "x-csrf": "1" },
  });
}
