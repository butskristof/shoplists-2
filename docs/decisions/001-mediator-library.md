# Use Mediator (martinothamar) for in-process mediation

**Status**: Decided

## Context

The application layer uses the mediator pattern to decouple request handling from the API host.
Needed: source-generated or lightweight, MediatR-compatible API shape, pipeline behavior support
(for validation and logging), MIT-licensed.

## Decision

[Mediator](https://github.com/martinothamar/Mediator) by martinothamar — source-generated,
MediatR-compatible API shape, MIT-licensed.

## Alternatives considered

- **MediatR**: Industry standard but moved to a commercial license. Not acceptable for this project.
- **Wolverine**: Full-featured but overkill — designed for messaging/event-driven systems, not just
  in-process mediation. Pulls in more infrastructure than needed.
- **Immediate.Handlers**: Promising but too immature at the time of evaluation. Insufficient
  ecosystem and community validation.
