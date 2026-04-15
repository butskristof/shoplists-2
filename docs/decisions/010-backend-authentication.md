# Backend authentication approach

**Status**: Decided

## Context

The API needs to validate access tokens issued by an external OIDC provider. The frontend BFF
handles the OIDC flow and forwards Bearer tokens — the API only needs to validate them.

## Decision

JWT Bearer validation via `Microsoft.AspNetCore.Authentication.JwtBearer`. The handler
auto-discovers signing keys at `{Authority}/.well-known/openid-configuration`.

### `MapInboundClaims = false` — critical setting

Without this, ASP.NET Core remaps OIDC-standard claim types like `"sub"` and `"name"` to long XML
namespace URIs (e.g., `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`).
With `MapInboundClaims = false`, claim lookups use the OIDC names directly.

This means **claim lookups must use `"sub"` directly, not `ClaimTypes.NameIdentifier`**.

### Configuration

`AuthenticationSettings` (`Authority`, `Audience`) bound from Aspire parameters
(`oidc-authority`, `oidc-audience`) injected as environment variables
(`Authentication__Authority`, `Authentication__Audience`).

### `ICurrentUser` implementation

`HttpContextCurrentUser` reads the `"sub"` claim from `HttpContext.User` via
`IHttpContextAccessor`, wraps it in `UserId` (string-backed strongly-typed ID), throws
`InvalidOperationException` if missing — this is a configuration error since all API endpoints
require authentication.

### Authorization strategy: group-level vs FallbackPolicy

Chose `.RequireAuthorization()` on the `/api` route group over a global `FallbackPolicy` because
all real endpoints are already centralized under `/api`. OpenAPI, Scalar, and health checks live
outside `/api` and are unaffected — no need for explicit `[AllowAnonymous]` on those.

### OpenAPI security

`BearerSecuritySchemeTransformer` (`Api/OpenApi/`) declares Bearer as a document-level security
requirement. Standard OpenAPI pattern — individual operations can override with empty security
array if needed. Makes Scalar show the Bearer token input for authenticated testing.
