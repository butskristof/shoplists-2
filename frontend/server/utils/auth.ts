/* eslint-disable node/prefer-global/process */
import type { H3Event } from "h3";
// @ts-expect-error nuxt-oidc-auth wildcard export not resolved by vue-tsc; .d.ts exists at dist/runtime/server/utils/security.d.ts
import { decryptToken } from "nuxt-oidc-auth/runtime/server/utils/security.js";

const SESSION_NAME = "nuxt-oidc-auth";

/**
 * Reads the access token from the OIDC persistent session storage.
 * Tokens are stored encrypted in Valkey by nuxt-oidc-auth.
 * We decrypt server-side for BFF proxying — the token is never exposed to the client
 * (exposeAccessToken is false in the OIDC provider config).
 */
export async function getAccessToken(event: H3Event): Promise<string> {
  const session = await useSession(event, {
    name: SESSION_NAME,
    password: process.env.NUXT_OIDC_SESSION_SECRET!,
  });

  if (!session.id || Object.keys(session.data).length === 0) {
    throw createError({ statusCode: 401, message: "Not authenticated" });
  }

  const persistentSession = (await useStorage("oidc").getItem(session.id)) as {
    accessToken?: string;
  } | null;
  if (!persistentSession?.accessToken) {
    throw createError({
      statusCode: 401,
      message: "No access token available",
    });
  }

  return await decryptToken(
    persistentSession.accessToken,
    process.env.NUXT_OIDC_TOKEN_KEY!,
  );
}
