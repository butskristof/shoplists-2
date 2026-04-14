# SSR-aware API client design (useApi composable)

**Status**: Decided

## Context

The `openapi-fetch` client must work in two very different environments from the same call site:
browser (client-side navigation) and Node.js (SSR / `onServerPrefetch`). Getting this wrong
produces subtle hydration mismatches that fail silently.

## Decision

The `useApi()` composable (`app/composables/useApi.ts`) returns a client configured for the current
rendering context:

- **Client-side**: relative `baseUrl: "/api"`, browser handles cookies automatically.
- **SSR**: absolute `baseUrl: "${origin}/api"` (Node's undici fetch rejects relative URLs) with a
  custom `fetch` wrapper that forwards the incoming request's `cookie` header.

### The hydration mismatch problem

Without cookie forwarding during SSR, the BFF proxy can't find the OIDC session → returns 401 →
the SSR query fails → Vue Query's `dehydrate` drops the failed query → the client hydrates with
empty data → hydration mismatch. The failure is silent — the page renders, but with a client-side
refetch instead of server-rendered data.

### The headers gotcha

The server `fetch` wrapper must seed its `Headers` from the incoming `Request.headers`, not from
`init.headers`. `openapi-fetch` constructs a `Request` object internally and per the fetch spec,
`init.headers` *replaces* the Request's headers wholesale. Passing a partial header set would strip
the `x-csrf` header that `openapi-fetch` already set → BFF returns 400.

### Setup context requirement

`useApi()` must be called synchronously in setup context, before any `await`, because
`useRequestURL` and `useRequestEvent` rely on the active component instance.

Similarly, SSR-aware composables (`useShoplists`, `useShoplist`) that register `onServerPrefetch`
must be called at the top of `<script setup>` before any `await`. `onServerPrefetch` binds to the
current component instance — after an `await`, the instance may no longer be tracked. Getting this
wrong fails silently: SSR no longer prefetches, producing client-side refetches instead.
