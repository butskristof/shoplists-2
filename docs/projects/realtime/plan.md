# Realtime — Planning Notes

Living document. Captures intent, design space, and open questions for pushing live updates from
the backend to connected clients so that multiple devices (and, eventually, multiple users sharing
a list) see changes without manual refresh.

When approach decisions stabilize (transport, BFF routing, conflict semantics), the headline ones
should be captured as ADRs under `docs/decisions/`. This file is the working plan, not a decision
record.

---

## Goals

- **Multi-device sync for a single user.** If the same account is open on phone and desktop, edits
  on one device appear on the other within a couple of seconds — no pull-to-refresh.
- **Foundation for shared lists.** When list-sharing (separate project) ships, collaborators see
  each other's edits live. The transport and reconciliation work should be done once and reused.
- **Honest conflict story.** Concurrent edits do not silently overwrite each other. The server
  remains the source of truth; clients reconcile against it explicitly.
- **No regression to the existing optimistic-UI flow.** Vue Query optimistic updates
  ([see `useShoplist.ts`](../../../frontend/app/composables/useShoplist.ts)) continue to feel
  instant; realtime layers on top, not under.

## Non-Goals (for now)

- **Presence / "user X is editing" indicators.** Real product value is low for a grocery-list app;
  cost is non-trivial.
- **OT / CRDT-based collaborative editing.** Items are short, structured, and rarely co-edited at
  the character level. A last-writer / version-stamp model is sufficient.
- **Multi-instance backplane.** Single API instance is the current deployment shape. Redis/Valkey
  backplane (already available locally via Aspire) is a future concern, not part of this project.
- **Native push notifications.** Out of scope; a separate concern that can sit on top of realtime
  if/when it lands.

---

## Design space — open considerations

### Transport

- **SignalR over WebSockets** — first-class .NET fit (Hub abstraction, connection lifecycle,
  reconnection, group membership). Idiomatic on the backend, well-supported. Cost: extra wire
  protocol layer, larger client lib, BFF routing concerns (below).
- **Server-Sent Events** — simpler, unidirectional (server → client). Sufficient if clients never
  need to push events over the same channel (we don't — mutations go via REST). Easier to proxy
  through Nitro. Reconnect is built into the browser EventSource API.
- **Raw WebSocket** — most control, most rope. Probably unnecessary.

Lean: SignalR if we want the Group/Hub ergonomics for per-list channels and future expansion.
SSE if we keep this lean and one-way.

### Connection through the BFF vs direct to the backend

The existing BFF proxy ([ADR 006](../../decisions/006-bff-proxy-security.md)) terminates browser
calls at Nitro, injects the access token server-side, and never exposes the token to the browser.
A realtime connection has to honour that boundary:

- **Tunnel through Nitro.** Nitro proxies the WebSocket / SSE stream, injects auth same as REST.
  Adds complexity around connection lifecycle, sticky routing, and reconnect semantics — but
  keeps the BFF security model intact.
- **Direct from browser to backend.** Requires browser to hold a usable credential (cookie or
  short-lived token). Significant security model shift; would need its own ADR.

Lean: tunnel through Nitro. The BFF is load-bearing security; we are not weakening it.

### Update granularity — payloads vs invalidations

- **Invalidation events** — server broadcasts "shoplist X changed"; client invalidates the
  corresponding Vue Query key, which triggers a refetch through the existing pipeline. Cheap,
  simple, reuses every existing handler/endpoint and the optimistic UI machinery. Cost: one extra
  HTTP round-trip per event.
- **Payload events** — server broadcasts the new state. Saves the refetch. Cost: more cache logic
  in the client, more surface for "what is the canonical state" bugs.

Lean: invalidation events first. Add payloads only where the refetch round-trip measurably hurts UX.

### Optimistic UI reconciliation

The current pattern (optimistic update → mutate → invalidate on settle) needs a story for "what
if a realtime event arrives mid-mutation". Specifically:

- **Echo suppression.** When my own mutation triggers a broadcast, I should not roll my own state
  back to the server's version and immediately re-apply my optimistic update. Options:
  - Tag mutations with a client-generated ID; server includes it in the broadcast; clients ignore
    events they originated.
  - Skip the broadcast on the originating connection (SignalR Hub `Clients.OthersInGroup(...)`).
- **Stale-while-pending reconciliation.** If a remote event arrives while my mutation is in
  flight, do I:
  - Discard the remote update (loses real changes)?
  - Apply on top of my optimistic state (may show inconsistent intermediate state)?
  - Queue and replay after my mutation settles (safest, more bookkeeping)?

### Versioning / concurrency

EF Core concurrency tokens (`xmin` on Postgres, or an explicit `RowVersion`) on the aggregate root
let us:

- Detect conflicting updates on the server and return `Error.Conflict` from handlers (existing
  `ErrorOr` pattern already maps to HTTP 409).
- Stamp broadcasts with the new version so clients can sequence and detect missed events.

Worth deciding once and applying everywhere; touches every aggregate.

### Channel topology

- **Group per shoplist.** Client joins when it has access; leaves on unsubscribe. Maps cleanly to
  the existing per-user authorization (`CurrentUserShoplists()`).
- **Group per user.** Simpler for the single-user multi-device case, but doesn't scale to sharing
  without rework.

Lean: group per shoplist from the start — costs roughly the same and avoids a migration later.

### Catch-up after reconnect

What if a client misses events while disconnected (mobile sleeping, transient network drop)?

- **Pull on reconnect.** Simplest — refetch open queries on connect. Works because invalidation
  events already trigger a refetch.
- **Resume from last seen version.** Server replays events since version X. Stronger guarantee,
  needs an event log. Probably overkill for this app.

Strong overlap with the offline-first project — co-design these (see [offline-first plan][1]).

[1]: ../offline-first/plan.md

---

## Co-design with offline-first

Realtime and [offline-first][1] both wrestle with the same problem: optimistic UI + a server that
is the source of truth + multi-source state changes. The reconciliation primitives should be
designed once:

- Version stamps / concurrency tokens belong to both.
- The "outbox" of pending offline mutations and the "replay buffer" of missed realtime events
  share semantics (intent → server applies → canonical state returns).
- Echo suppression for realtime is structurally similar to deduping replayed offline mutations.

Recommend running the design phase of both projects in parallel, even if implementation lands
serially.

---

## Open questions

- **Transport choice**: SignalR vs SSE. Decide at project kickoff.
- **BFF integration**: confirm Nitro can proxy WebSocket / SSE streams cleanly, including reconnect.
- **Concurrency tokens**: where do they live (aggregate root only, or every entity)? How are they
  surfaced in the API contract?
- **Echo suppression strategy**: client-tag vs `OthersInGroup` broadcast.
- **Vue Query integration**: invalidation only, or do we ever push payloads into the cache directly?
- **Reconnect / catch-up**: pull-on-reconnect vs event-replay buffer.
- **What's the first driver?** Solo multi-device adds modest value. Sharing makes it essential.
  Tie this project's timing to the sharing project.

---

## Priority signals (when to actually start)

- Multi-device single-user use is real but tolerable without realtime today. Not a blocker.
- **List sharing is the trigger.** Once two users can edit the same list, manual refresh becomes
  a real UX hole. Land realtime alongside or shortly after sharing.
- Integration tests (see [testing plan](../testing/plan.md)) should be in place before this lands
  — realtime amplifies the cost of silent regressions.
