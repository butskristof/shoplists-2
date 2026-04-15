/**
 * Readiness probe: verifies the server can serve real traffic.
 *
 * Checks that Valkey (OIDC session storage) is reachable. Without it,
 * getUserSession fails and every authenticated request returns 401.
 *
 * The backend API is intentionally not checked here — it has its own
 * health checks, and a backend outage should not pull frontends out
 * of rotation.
 */
export default defineEventHandler(async (event) => {
  try {
    // Read-only check against the mounted oidc storage driver.
    // Returns false (key doesn't exist) if Valkey is reachable, throws if not.
    await useStorage("oidc").hasItem("_health:ready");

    return { status: "healthy" };
  }
  catch {
    setResponseStatus(event, 503);
    return { status: "unhealthy", reason: "session storage unreachable" };
  }
});
