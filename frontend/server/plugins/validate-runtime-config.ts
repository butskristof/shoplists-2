/**
 * Fail fast at startup if critical runtime config is missing.
 *
 * The BFF (server/api/[...path].ts) depends on `backendApiUrl` to proxy
 * requests to the backend API. Without it, every request would silently
 * target a malformed URL and fail with cryptic errors — validating at
 * startup surfaces the misconfiguration immediately in the Nitro logs.
 *
 * OIDC secrets are validated by nuxt-oidc-auth itself; Redis config is
 * validated in oidc-storage.ts. This plugin covers the BFF-specific
 * config that no other module enforces.
 */
export default defineNitroPlugin(() => {
  const { backendApiUrl } = useRuntimeConfig();

  if (!backendApiUrl) {
    throw new Error(
      "[validate-runtime-config] NUXT_BACKEND_API_URL is required for the BFF to proxy requests to the backend API.",
    );
  }
});
