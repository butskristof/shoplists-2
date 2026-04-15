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
- Sync `onSuccess` overload (`Func<T, IResult>?`) for simple mappings (Created, NoContent).
- Async `onSuccess` overload (`Func<T, Task<IResult>>`) for success paths needing async work.
- Default success: `Ok(value)`. Override via `onSuccess` for `Created` / `NoContent`.
- Multiple validation errors grouped by `Error.Code` into `ValidationProblemDetails`.
- Non-validation errors use the first error to determine status code.

## ErrorType → HTTP status mapping

| `ErrorOr` `ErrorType` | HTTP response |
|---|---|
| `Validation` | 400 + `ValidationProblemDetails` |
| `NotFound` | 404 + `ProblemDetails` |
| `Conflict` | 409 + `ProblemDetails` |
| `Unauthorized` | 403 + `ProblemDetails` |
| (others) | mapped via `ToHttpResult()` extensions |

## Middleware stack

`ToHttpResult()` only handles expected `ErrorOr` failures. Other failure modes are covered by
ASP.NET Core middleware:

- `UseExceptionHandler()` — unexpected exceptions → 500 + `ProblemDetails`
- `UseStatusCodePages()` — empty-body failures (404 from routing, 405 from method mismatch)
- `AddProblemDetails()` registered in DI — enables automatic `ProblemDetails` formatting for
  both middlewares above
