/* eslint-disable node/prefer-global/process */
import type { H3Event } from "h3";
import type { PersistentSession } from "nuxt-oidc-auth/runtime/types.js";
import { decryptToken } from "nuxt-oidc-auth/runtime/server/utils/security.js";
import { getUserSessionId } from "nuxt-oidc-auth/runtime/server/utils/session.js";

/**
 * Reads the access token from the OIDC persistent session storage.
 *
 * nuxt-oidc-auth stores session data in two tiers:
 * 1. An h3 cookie session (session ID, provider, expiry metadata)
 * 2. A persistent session in Valkey (encrypted tokens: access, refresh, id)
 *
 * This function reads tier 2 and decrypts the access token for BFF
 * proxying. The token is never exposed to the browser (exposeAccessToken
 * is false in the OIDC provider config).
 *
 * **Important**: call `getUserSession(event)` before this function to
 * ensure the session is valid and tokens have been refreshed if expired.
 * This function only reads the stored token — it does not trigger refresh.
 */
export async function getAccessToken(event: H3Event): Promise<string> {
  const sessionId = await getUserSessionId(event);

  const persistentSession = (await useStorage("oidc").getItem(
    sessionId,
  )) as PersistentSession | null;
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
