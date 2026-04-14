# Use ErrorOr for result/error handling

**Status**: Decided

## Context

Handlers need a consistent way to return success values or typed errors without throwing exceptions
for expected failures. The pattern must be entry-point agnostic — the same handler should work
whether called from an API endpoint, a background job, or a message consumer.

## Decision

[ErrorOr](https://github.com/amantinband/error-or) — provides `ErrorOr<T>` with built-in error
types (NotFound, Validation, Conflict, Unauthorized, etc.) that carry no HTTP coupling.

Mapping to HTTP status codes and ProblemDetails happens exclusively at the API boundary via
`ToHttpResult()` extension methods in `Api/Extensions/ErrorOrExtensions.cs`.

## Key design details

- Extension on `ValueTask<ErrorOr<T>>` enables `await sender.Send(req).ToHttpResult()` without
  awkward parenthesizing.
- Default success: `Ok(value)`. Override via `onSuccess` for `Created` / `NoContent`.
- Multiple validation errors grouped by `Error.Code` into `ValidationProblemDetails`.
- Unexpected exceptions handled separately by `UseExceptionHandler()` middleware → 500 ProblemDetails.
  `UseStatusCodePages()` covers additional non-exception failures (404, 405) with empty bodies.
