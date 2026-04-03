import redisDriver from "unstorage/drivers/redis";

/**
 * Mounts a Redis-compatible storage driver on the "oidc" namespace used by nuxt-oidc-auth
 * for persisting encrypted tokens (access, refresh, ID) across server restarts.
 *
 * By default, nuxt-oidc-auth stores these in-memory, which is lost on restart.
 * Mounting a persistent driver on the "oidc" namespace replaces this.
 *
 * Works with any RESP-compatible server (Redis, Valkey, Garnet, etc.) via ioredis.
 *
 * This uses the Nitro runtime plugin pattern for dynamic storage mounting, since
 * the connection details are injected at runtime by Aspire (not known at build time).
 * See: https://nitro.build/guide/storage#runtime-configuration
 * See: https://nuxt-oidc-auth.pages.dev/getting-started/security (server-side storage)
 */
export default defineNitroPlugin(() => {
  const { host, port, password, tls } = useRuntimeConfig().redis;

  if (!host || !port) {
    throw new Error("[oidc-storage] Redis-compatible server not configured. NUXT_REDIS_HOST and NUXT_REDIS_PORT are required for OIDC token storage.");
  }

  const useTls = tls === "true";

  const driver = redisDriver({
    base: "oidc",
    host,
    port: Number(port),
    password: password || undefined,
    // TLS is determined by the hosting environment (Aspire sets NUXT_REDIS_TLS).
    // When enabled, an empty tls options object tells ioredis to use TLS with
    // default settings. Aspire injects NODE_EXTRA_CA_CERTS for self-signed CAs.
    ...(useTls && { tls: {} }),
  });

  useStorage().mount("oidc", driver);
});
