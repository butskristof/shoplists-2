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
 *   both state-changing CSRF and cross-origin data theft via GET. Matches
 *   Duende BFF's antiforgery convention; returns 401 when absent.
 *   See https://docs.duendesoftware.com/bff/#csrf-attacks
 * - **Origin check**: when an `Origin` header is present, it must match
 *   the request's own origin. Active same-origin backstop that does not
 *   depend on the absence of CORS headers elsewhere in the stack.
 * - **Token isolation**: the access token is extracted from the encrypted
 *   OIDC session in Valkey — it never reaches the browser.
 *   !! NEVER expose the access token in the response body !!
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
      statusCode: 401,
      message: "Missing CSRF protection header",
    });
  }

  // Active cross-origin rejection. Browsers send `Origin` on cross-origin
  // requests (and same-origin non-GET). When present, it must match the
  // request's own origin — a foreign origin is never allowed to reach the
  // BFF, regardless of any CORS config elsewhere in the stack.
  // Same-origin GETs typically omit `Origin` entirely, which is fine:
  // we only reject when it IS present and mismatched.
  const requestOrigin = event.headers.get("origin");
  if (requestOrigin && requestOrigin !== getRequestURL(event).origin) {
    throw createError({
      statusCode: 403,
      message: "Cross-origin requests not allowed",
    });
  }

  // Validate the session and trigger automatic token refresh if the
  // access token is expired. getUserSession handles expiration checks
  // and refresh-token flows based on the nuxt-oidc-auth configuration.
  // Without this call we could forward expired tokens to the backend.
  // Wrap in try/catch to return a clean 401 instead of leaking the
  // library's internal error details (decryption failures, etc.).
  try {
    await getUserSession(event);
  }
  catch {
    throw createError({
      statusCode: 401,
      message: "Session invalid",
    });
  }

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
