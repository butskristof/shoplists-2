import redisDriver from "unstorage/drivers/redis";

/**
 * Mounts a Redis-backed storage driver on the "oidc" namespace used by nuxt-oidc-auth
 * for persisting encrypted tokens (access, refresh, ID) across server restarts.
 *
 * By default, nuxt-oidc-auth stores these in-memory, which is lost on restart.
 * Mounting a persistent driver on the "oidc" namespace replaces this.
 *
 * This uses the Nitro runtime plugin pattern for dynamic storage mounting, since
 * the Redis connection details are injected at runtime by Aspire (not known at build time).
 * See: https://nitro.build/guide/storage#runtime-configuration
 * See: https://nuxt-oidc-auth.pages.dev/getting-started/security (server-side storage)
 */
export default defineNitroPlugin(() => {
  const { host, port, password } = useRuntimeConfig().redis;

  if (!host || !port) {
    throw new Error("[oidc-storage] Redis not configured. NUXT_REDIS_HOST and NUXT_REDIS_PORT are required for OIDC token storage.");
  }

  const driver = redisDriver({
    base: "oidc",
    host,
    port: Number(port),
    password: password || undefined,
    // Aspire's Redis resource exposes TLS on its primary port (rediss://).
    // ioredis expects a Node.js tls.ConnectionOptions object to enable TLS;
    // an empty object means "use TLS with default settings." Aspire injects
    // NODE_EXTRA_CA_CERTS so Node trusts the self-signed Aspire CA automatically.
    tls: {},
  });

  useStorage().mount("oidc", driver);
});
