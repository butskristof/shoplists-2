import redisDriver from "unstorage/drivers/redis";

export default defineNitroPlugin(() => {
  const storage = useStorage();

  const host = process.env.REDIS_HOST;
  const port = Number(process.env.REDIS_PORT);
  const password = process.env.REDIS_PASSWORD;

  if (!host || !port) {
    console.warn("[oidc-storage] REDIS_HOST or REDIS_PORT not set, OIDC session storage will use default (in-memory).");
    return;
  }

  storage.mount("oidc", redisDriver({
    base: "oidc",
    host,
    port,
    password,
  }));
});
