# BFF proxy design and security measures

**Status**: Decided

## Context

The frontend needs to call the backend API with an access token, but the token must never reach the
browser. `nuxt-oidc-auth` stores tokens encrypted in Valkey but has no way to return the access
token server-side without also exposing it to the browser via the `UserSession` object.

## Decision

A Nitro catch-all route (`server/api/[...path].ts`) acts as a BFF proxy. It reads the encrypted
token from Valkey server-side and forwards it as a Bearer header to the backend.

### Request flow

1. Browser calls `/api/<path>` with `x-csrf: 1` header
2. BFF validates CSRF header, validates/refreshes session, extracts access token from Valkey
3. BFF forwards to backend with `Authorization: Bearer <token>`

### Security measures (all required — do not remove without discussion)

1. **CSRF header** (`x-csrf: 1`): A custom header forces a CORS preflight, restricting callers to
   same-origin. Protects against both state-changing CSRF and cross-origin data theft via GET.
   Aligned with [Duende BFF guidance](https://docs.duendesoftware.com/bff/#csrf-attacks). The API
   client sets this globally.

2. **Origin check**: When an `Origin` header is present, it must match the request URL origin.
   Active same-origin backstop independent of CORS configuration. Same-origin GETs that omit
   `Origin` are allowed — only *mismatched* origins are rejected.

3. **Session lifecycle**: `getUserSession(event)` is called before token extraction. This triggers
   expiration checks and automatic token refresh per the OIDC config. **Without this call,
   `automaticRefresh: true` / `expirationCheck: true` config is dead code** — expired tokens would
   be silently forwarded to the backend.

4. **Cookie stripping**: Outgoing requests to backend set `cookie: ""`. The backend authenticates
   via Bearer only; forwarding session cookies is unnecessary data leakage.

5. **No redirect following**: `redirect: "manual"` prevents silently following 3xx responses
   (e.g., to login pages).

### CORS is intentionally disabled

`security.corsHandler: false` in `nuxt.config.ts`. If CORS allowed a foreign origin to send the
`x-csrf` header, the preflight-based CSRF defense collapses for that origin. The Origin check is
an active backstop, but the primary defense is the absence of permissive CORS headers.
