import { getUserSession } from "nuxt-oidc-auth/runtime/server/utils/session.js";
import { joinURL } from "ufo";
import { getAccessToken } from "../utils/auth";

/**
 * BFF catch-all proxy: forwards /api/* requests to the backend API with
 * the user's access token attached as a Bearer header.
 *
 * Security measures:
 * - **CSRF protection**: a custom `x-csrf` header is required on every
 *   request. Custom headers trigger a CORS preflight, restricting callers
 *   to same-origin (or explicitly allowed origins). This guards against
 *   both state-changing CSRF and cross-origin data theft via GET.
 *   See https://docs.duendesoftware.com/bff/#csrf-attacks
 * - **Token isolation**: the access token is extracted from the encrypted
 *   OIDC session in Valkey — it never reaches the browser.
 * - **Cookie stripping**: auth cookies are removed from the proxied
 *   request so they never reach the backend API.
 * - **No redirect following**: 3xx responses are passed back to the
 *   client so the SPA can handle navigation.
 */
export default defineEventHandler(async (event) => {
  // Reject requests without the CSRF header. The browser-side API client
  // sets this on every request; because it is a non-standard header the
  // browser issues a CORS preflight — effectively enforcing same-origin.
  if (event.headers.get("x-csrf") !== "1") {
    throw createError({
      statusCode: 400,
      message: "Missing CSRF protection header",
    });
  }

  // handle authentication
  // since nuxt-oidc-auth in its current version cannot return the access tokens in the UserSession
  // object without exposing them to the client as well, we hack our way around this:
  // 1. use the getUserSession function as described in the docs: this takes care of expired
  //    sessions, refreshing, ...
  //    it throws if it cannot provide a UserSession object, we catch and rethrow with just
  //    401 Unauthorized instead of the detailed error we get from the library
  // 2. use a utility function to extract the access token from the persistent session storage
  //    we already know there is a user session at this point, and since we're using refresh tokens,
  //    the access token will have been saved to the storage
  //    the code itself is lifted from the library, where it is behind a configuration check for
  //    exposeAccessToken but no differentiation for calls from the client client (composable in o
  //    the app) or server. We copied the code and keep it in here so it'll only ever run on the
  //    server
  //    !! MAKE SURE TO NEVER EXPOSE THE ACCESS TOKEN IN THE RESPONSE !!

  // Validate the session and trigger automatic token refresh if the
  // access token is expired. getUserSession handles expiration checks
  // and refresh-token flows based on the nuxt-oidc-auth configuration.
  // Without this call we could forward expired tokens to the backend.
  await getUserSession(event);

  // Extract the (potentially refreshed) access token from encrypted
  // persistent storage. Throws 401 if no token is available.
  const accessToken = await getAccessToken(event);

  const backendApiUrl = useRuntimeConfig().backendApiUrl;
  const path = getRouterParam(event, "path");

  // joinURL (from ufo, shipped with Nitro) normalises trailing/leading
  // slashes to avoid double-slash issues in the target URL.
  return proxyRequest(event, joinURL(backendApiUrl, path ?? ""), {
    headers: {
      Authorization: `Bearer ${accessToken}`,
      // Strip cookies — the backend authenticates via Bearer token only.
      // Without this, proxyRequest forwards the original request headers
      // including the session cookie, which is unnecessary data leakage.
      cookie: "",
    },
    fetchOptions: {
      // Do not follow redirects (e.g. to login pages). Pass the 3xx
      // back so the SPA can handle navigation appropriately.
      redirect: "manual",
    },
  });
});
