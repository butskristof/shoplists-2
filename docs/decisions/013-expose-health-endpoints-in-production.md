# Expose health endpoints in all environments

**Status**: Decided

## Context

The Aspire `aspire-servicedefaults` template gates `MapHealthChecks` behind
`app.Environment.IsDevelopment()` and links to
[aka.ms/dotnet/aspire/healthchecks](https://aka.ms/dotnet/aspire/healthchecks) with a warning about
security implications of exposing health endpoints in production.

Docker Compose deployments need `/health/ready` reachable so the container `HEALTHCHECK`
(`curl --fail http://localhost:8080/health/ready`) can gate `depends_on: service_healthy` for the
frontend. With the template default, the endpoints return 404 in Production and the frontend never
starts.

## Decision

Remove the `IsDevelopment()` guard in `ServiceDefaults/Extensions.cs`. Map both `/health/ready`
and `/health/live` unconditionally.

## Why the Aspire default doesn't apply here

The template's concerns, and why each is mitigated by the current deployment topology:

1. **Information disclosure via check names.** The default response writer emits the name and
   status of every registered check (Postgres, Valkey, etc.), which is useful recon for an
   attacker. *Mitigation here*: the API has no `ports:` mapping in `docker-compose.yaml` — port
   8080 is only reachable on the internal Docker network. The BFF proxy
   (`frontend/server/api/[...path].ts`) only forwards `/api/*`, so external traffic cannot reach
   `/health/*`.

2. **DoS amplification via unauthenticated dependency I/O.** `/health/ready` executes every
   registered check (DB ping, cache ping, HTTP probes) on every request, which is cheap to abuse
   from an unauthenticated endpoint. *Mitigation here*: same as above — the endpoint isn't
   reachable from outside the Docker network, so there's no unauthenticated caller path.

3. **Assumed orchestrator topology.** The Aspire default assumes production = Kubernetes / Azure
   Container Apps / App Service, where the orchestrator probes over the pod network and no
   public-facing endpoint is needed. *Mitigation here*: not applicable — we use plain Docker
   Compose and rely on the container-level `HEALTHCHECK`, which requires the endpoint to exist.

Liveness (`/health/live`) runs only the trivial `self` check, so it carries none of these
concerns regardless of exposure.

## Alternatives considered

- **Keep the guard, add `.RequireHost("localhost")`**: defense-in-depth — the endpoint only
  responds when the `Host:` header is `localhost`, as sent by curl inside the container. Rejected:
  adds a second line of protection against a threat model that doesn't apply, and surprises future
  readers who see a non-obvious `RequireHost` call.
- **Bind health endpoints to a separate Kestrel port** (e.g. 8081) that no service exposes.
  Rejected: significant Kestrel/Aspire config overhead; no real benefit given the endpoints are
  already network-internal.
- **Keep the guard, set `ASPNETCORE_ENVIRONMENT=Development` in the compose file**. Rejected:
  changes far more behavior than health endpoint registration (dev-only middleware, Scalar UI,
  detailed error pages) and misrepresents the deployment environment.

## Re-evaluation triggers

Revisit this decision if any of the following change:

- The API service gets a `ports:` mapping that publishes 8080 (or any port) on the host.
- The BFF proxy is modified to forward paths other than `/api/*`.
- The deployment moves to an orchestrator (Kubernetes, Azure Container Apps, App Service) that
  provides its own health probe mechanism — the endpoints may need additional filtering or a
  dedicated binding.
