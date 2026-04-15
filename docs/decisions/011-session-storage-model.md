# Frontend session storage model

**Status**: Decided

## Context

`nuxt-oidc-auth` needs to store OIDC session state (provider info, expiry) AND the actual access /
refresh / ID tokens. These have very different security and lifecycle requirements.

## Decision

`nuxt-oidc-auth` splits session data across two tiers — we accept and rely on this split:

1. **h3 cookie session** (httpOnly, `sameSite: lax`) — session ID, provider name, expiry metadata.
   Lightweight, sent on every request.
2. **Persistent session in Valkey** — encrypted access / refresh / ID tokens. Only read server-side
   when needed.

## Implications for our code

- `server/utils/auth.ts::getAccessToken()` reads tier 2 via `getUserSessionId()` and decrypts the
  access token. **It assumes the session has already been validated by `getUserSession`** — it
  does NOT trigger refresh itself.
- **Always call `getUserSession(event)` first** before extracting tokens. This is what triggers
  expiration checks and the configured `automaticRefresh: true` behavior. Skipping it means
  expired tokens get silently forwarded.
- Startup config validation (`server/plugins/oidc-storage.ts`) verifies the Redis connection at
  boot — misconfiguration surfaces immediately in the Aspire dashboard rather than as cryptic
  per-request errors.
