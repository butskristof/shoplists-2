# Offline-first — Planning Notes

Living document. Captures intent, design space, and open questions for making the app usable
without network — including reading cached lists and queueing mutations for later sync. Split out
from [`pwa/plan.md`][pwa] (which covers app *delivery* offline) because the *data layer* offline
story is its own design problem and deeply entangled with the [realtime project][realtime].

[pwa]: ../pwa/plan.md
[realtime]: ../realtime/plan.md

When approach decisions stabilize (storage, conflict policy, outbox shape), the headline ones
should be captured as ADRs under `docs/decisions/`. This file is the working plan, not a decision
record.

---

## Goals

- **Read while offline.** If a user opens the app in a low-signal grocery store, their lists and
  items are visible from cached state.
- **Mutate while offline.** Tick items off, add items, rename — all without network. Changes queue
  locally and replay on reconnect.
- **No silent data loss.** A queued mutation that ultimately conflicts with server state surfaces
  to the user; it is not dropped.
- **Same UX feel.** Optimistic UI continues to dominate the perceived response time. Offline mode
  is a graceful degradation, not a different app.
- **No regression to current online behaviour.** Vue Query + BFF flow stays unchanged for online
  users.

## Non-Goals (for now)

- **Multi-week / multi-month offline operation.** Realistic target: minutes to a few hours
  ("walking around a grocery store with patchy signal"). Anything longer is a different problem
  (stale auth, schema drift, etc.).
- **Peer-to-peer sync without server.** Server remains source of truth.
- **Offline auth / re-login while offline.** OIDC flow requires connectivity. Existing session
  cookie can ride through transient disconnects, but full re-auth is online-only.
- **Background sync APIs / periodic background sync.** Service worker background sync is a
  separate phase if/when it's needed.
- **Full CRDT data model.** See realtime plan — same reasoning.

---

## Design space — open considerations

### Cache persistence

Vue Query already caches query data in-memory. To survive a reload offline we need:

- **`@tanstack/vue-query-persist-client`** — official persister, plugs into the existing client.
  Trade-off: ergonomic but ties cache schema to library internals; migration story is
  library-controlled.
- **Custom IDB layer** — full control, more code. Could share storage with the mutation outbox.

Lean: VQ persister first. Drop down to custom only if it limits us.

### IndexedDB access layer

- **Raw IDB** — no dep, painful API.
- **`idb` (small wrapper)** — official small Promise wrapper from Jake Archibald. Minimal.
- **Dexie** — richer, querying, hooks. Overkill if we only persist a couple of stores (cache +
  outbox).

Lean: `idb` if we end up writing the outbox by hand. None if VQ persister covers everything.

### Mutation outbox

Two shapes to consider:

- **Implicit (via VQ mutation persistence)** — VQ's mutation cache is persistable. Pending
  mutations replay on resume.
  - Pro: piggybacks on the existing mutation machinery (optimistic update + onError rollback +
    onSettled invalidate). Less code.
  - Con: mutation shape is tied to client function references — harder to evolve across app
    versions (if a mutation key disappears in a new build, persisted mutations break).
- **Explicit outbox table** — we persist *intents* (typed events: `AddItem`, `ToggleFulfilled`,
  etc.) keyed by a client-generated ID. Replayer hits the REST API.
  - Pro: durable, app-version-agnostic, naturally typed at the boundary.
  - Con: parallel pipeline to VQ; need to keep them in sync.

Lean: explicit outbox once we go beyond "tick a checkbox while offline" — the typed event log is
the right abstraction for non-trivial offline.

### Client-side identity

We already self-generate strongly-typed IDs on the server side (see
[ADR 016](../../decisions/016-self-generated-keys-and-ef-core.md)). The same primitive applies
client-side: an item created offline gets its final ID at creation time, with no "temp ID → real
ID" rewrite on sync.

This is a large win we've already paid for. Reuse it.

### Conflict resolution policy

When a queued mutation arrives at the server and the server's state has changed underneath it:

- **Last-writer-wins** — simplest, lossy. Acceptable for items where the latest state is what
  matters (toggle fulfilled, rename).
- **Server-wins** — silently drop the client's intent. Frustrating; rejected.
- **Field-level merge** — for richer entities (item metadata project will bring more fields).
  Reasonable for non-overlapping fields.
- **User-prompted resolution** — show the user "you set X to A while offline; meanwhile Y set it
  to B. Keep yours or theirs?" Highest fidelity, biggest UX cost.

Lean: per-operation policy table, not a single global rule. Toggle-fulfilled = LWW; rename =
LWW; delete-while-renamed = surface to user; add-on-deleted-parent = abort with toast.

### Replay semantics

- **Idempotency.** Each outbox entry gets a client-generated event ID. Server should be idempotent
  on replay (no double-applies if a network retry succeeds but the client never saw the response).
- **Ordering.** Replay in submission order per aggregate to preserve user intent (`add A`, `add B`,
  `delete A` cannot be reordered without changing meaning).
- **Failure handling.** Permanent failure (validation, conflict surfaced to user, deleted parent)
  → flag the outbox entry; surface in UI; user resolves explicitly. Transient failure (network
  blip) → retry with backoff.

### UI surface

- **Offline banner** — already planned in [PWA Phase 2][pwa-phase-2]. Persistent, top-anchored.
- **Per-item sync indicator** — small dot / icon showing "this change hasn't synced yet". Useful
  feedback once the outbox exists.
- **Conflict resolution dialog** — only when policy dictates user intervention. Avoid for the
  common case.
- **"X changes pending" badge** — useful for a confidence check before walking out of signal range.

[pwa-phase-2]: ../pwa/plan.md#phase-2--offline-ux--update-prompt-ux-layer-on-top-of-existing-sw

### Service worker boundary

The PWA SW already caches the *app shell*. It explicitly does **not** cache `/api/*`
([PWA plan][pwa]). The offline-data layer therefore lives in the *page* (IDB + VQ), not the SW.
This separation is deliberate: SW concerns delivery, page concerns data. No conflict.

---

## Co-design with realtime

[Realtime][realtime] and offline-first share the same hard problem: optimistic UI + canonical
server state + concurrent change sources. Concretely:

- **Version stamps / concurrency tokens.** Realtime needs them to broadcast "new version X";
  offline replay needs them to detect "the world moved while I was offline". Same field, same
  semantics.
- **Idempotency keys.** Realtime needs them for echo suppression; offline replay needs them for
  safe retry.
- **Replay vs catch-up.** A reconnecting client may have unsent offline mutations *and* missed
  realtime events. Both must reconcile in a consistent order. The pipelines should not be
  designed in isolation.

Recommend overlapping the design phase of both projects, even if implementation lands serially.

---

## Phased rollout (sketch)

- **Phase 0** — Decide outbox shape (VQ persister vs explicit) and storage layer. Write the ADR.
- **Phase 1** — Cache persistence only. Survive offline reload; reads work; mutations still
  require network (existing VQ default `networkMode: 'online'` already pauses them).
- **Phase 2** — Outbox for write operations. Toggle / add / rename queue and replay. Pick
  the smallest useful set; expand later.
- **Phase 3** — Conflict surface UI + per-item sync indicators.
- **Phase 4** — Hardening: idempotency, retry/backoff, outbox eviction policy, schema versioning
  for stored intents.

---

## Open questions

- **Outbox shape**: VQ persister or explicit.
- **Storage**: VQ persister-managed only, or shared IDB layer for cache + outbox.
- **Conflict policy table**: which operations are LWW, which surface to the user, which merge.
- **Storage budget**: cache eviction policy, outbox max-age.
- **Schema versioning**: how do we handle a user with v1 stored mutations replaying against a v2
  API contract?
- **Auth lifecycle offline**: session cookie expiry mid-offline-session — what happens to the
  outbox? Block at replay? Drop entries?
- **Mobile-specific concerns**: iOS Safari IDB quota, eviction under storage pressure.

---

## Priority signals (when to actually start)

- The current online-only flow is acceptable for the immediate use case. Not blocking.
- **Driver: mobile usability in poor-signal locations** — when the app feels broken in a real
  grocery store, this project is overdue.
- Integration tests (see [testing plan](../testing/plan.md)) must be in place. The replay-on-
  reconnect path is exactly the kind of thing silent regressions love.
- Best co-designed with [realtime][realtime].
