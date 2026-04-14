# Handler file conventions and nullable request properties

**Status**: Decided

## Context

Need a consistent pattern for use case handlers that works with FluentValidation pipeline behaviors
and produces uniform error responses.

## Decision

Each use case is a single static class with nested `Request`, `Validator`, and `Handler` types.
The static class scopes type names to avoid collisions (e.g., `CreateShoplist.Request` vs
`GetShoplist.Request`).

### The nullable properties trade-off

**Request properties are nullable** (`string?` not `string`), with FluentValidation enforcing
non-null via `NotNullOrEmptyWithErrorCode()` and similar rules.

**Why**: If properties were non-nullable, ASP.NET Core's model binding would reject missing fields
*before* FluentValidation runs, producing inconsistent error responses — some errors would come
from the framework (different format) and some from FluentValidation (`ValidationProblemDetails`).
Making everything nullable ensures all validation flows through the same pipeline and produces
uniform `ValidationProblemDetails` responses.

**Trade-off**: OpenAPI schema generates nullable types → `openapi-typescript` produces nullable
TypeScript fields → frontend code deals with `string | undefined` even for required fields.
Accepted for now. Potential future fix: integrate FluentValidation rules into OpenAPI schema
generation or use derived "validated" types.

### Visibility

- `Request` (and `Response` if applicable): **public** — referenced by the API layer
- `Validator` and `Handler`: **internal** — implementation details
- Primary constructors for DI, enforced readonly via Meziantou.Analyzer MA0143
