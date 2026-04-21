# Shoplists

A grocery shopping web app. MVP: users own lists of items that can be ticked on/off. Installable
as a PWA on mobile and desktop.

## Tech stack

**Frontend**
- Nuxt 4 (Vue 3, TypeScript), hybrid SSR → SPA
- PrimeVue v4 (Aura preset) + PrimeIcons, native CSS (no Tailwind)
- Vue Query for server state, optimistic updates for single-resource mutations
- `openapi-fetch` with types generated from the backend OpenAPI spec
- `nuxt-oidc-auth` for auth — tokens kept server-side, never exposed to the browser
- `@vite-pwa/nuxt` for installability + service worker

**Backend**
- .NET 10 Minimal APIs, Entity Framework Core, PostgreSQL
- Clean architecture with a feature-organized application layer
- [Mediator](https://github.com/martinothamar/Mediator) (source-generated) for request handlers
- [ErrorOr](https://github.com/amantinband/error-or) result pattern
- FluentValidation via mediator pipeline behavior
- OpenAPI + Scalar docs UI (dev)

**Infrastructure**
- Aspire orchestrates the full local stack
- Valkey for encrypted session storage
- OpenTelemetry across frontend and backend (logs, traces, metrics)
- Multi-arch container images published to GHCR via GitHub Actions

## Architecture

Clean-architecture layering on the backend (Domain → Application → Infrastructure / Persistence →
Hosts) with a BFF proxy in the Nuxt Nitro server that handles OIDC tokens server-side and enforces
same-origin protection.

Architectural decisions are recorded as lightweight ADRs under [`docs/decisions/`](docs/decisions/).
Working documents for in-flight projects live in [`docs/projects/`](docs/projects/).

## Running locally

From the repo root:

```
aspire run
```

Aspire orchestrates Postgres, Valkey, the database migrator, the API, and the frontend, and assigns
ports dynamically — the dashboard URL is printed to the console.

Authentication requires an external OIDC provider. See the Aspire host and
[`docker-compose/docker-compose.yaml`](docker-compose/docker-compose.yaml) for the required
configuration shape.

## Deployment

Container images for API, DatabaseMigrator, and Frontend are published to GHCR on every push to
`main` (multi-arch: `linux/amd64`, `linux/arm64`). A reference
[`docker-compose.yaml`](docker-compose/docker-compose.yaml) is provided as a starting point.

Everything beyond that — TLS, rate limiting, secrets management, scaling, backups — is left to the
host environment.

## Ideas for future improvements

- Sharing lists between users
- Item metadata (images, price guidance, unit)
- Realtime updates across devices
- Purchase history and analysis (receipts, price/unit tracking, aggregate views over time)
- Predictive suggestions for when an item is likely due for repurchase
- Price comparison across stores

## License

See [`LICENSE`](LICENSE).
