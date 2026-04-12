import { getAccessToken } from "../utils/auth";

/**
 * BFF catch-all proxy: forwards all /api/* requests to the backend API
 * with the user's access token attached as a Bearer header.
 *
 * The access token is extracted from the encrypted OIDC session stored
 * in Valkey — it never reaches the client.
 */
export default defineEventHandler(async (event) => {
  const accessToken = await getAccessToken(event);
  const backendApiUrl = useRuntimeConfig().backendApiUrl;
  const path = getRouterParam(event, "path");

  return proxyRequest(event, `${backendApiUrl}/api/${path}`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  });
});
