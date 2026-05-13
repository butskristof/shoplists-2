# Item metadata — Planning Notes

Living document. Captures intent and design space for enriching the current minimal item model
(name + fulfilled-flag) with metadata: images, units, optional price observations, and eventually
purchase history. Lays the groundwork for downstream features that depend on item identity
across lists (predictive suggestions, price guidance — see README "Ideas for future improvements").

When approach decisions stabilize (catalog vs per-list-item identity, image storage, price model),
the headline ones should be captured as ADRs under `docs/decisions/`.

---

## Goals

- **Richer item model.** Optional metadata fields — image, unit (kg, L, count), notes — so an
  item can carry more than just a string name.
- **Price observations over time.** Each "I bought this" event can record a price; the item
  surfaces an aggregate ("usually around €X") to inform the next shop.
- **Purchase history.** A modest log of when an item was last fulfilled across lists, which is the
  raw material for both price guidance and (eventually) predictive repurchase suggestions.
- **Pleasant input UX.** Adding metadata cannot feel like filling a form. Default UX must stay
  "type name, hit enter"; metadata is an enhancement, never a barrier.

## Non-Goals (for now)

- **Receipt scanning / OCR.** Big project of its own; not part of this scope.
- **Multi-store price comparison.** README mentions it as a future idea — separate project,
  depends on metadata existing first.
- **Recipes, nutrition, brand catalogs.** Different domain entirely.
- **Predictive repurchase suggestions.** Stretches into ML territory; the data model should not
  *block* this future, but building it is out of scope here.
- **Public / shared catalog of items.** Each user's metadata is private until sharing changes
  that calculus.

---

## Headline architectural question — Catalog vs per-list-item identity

The single biggest decision in this project, and the one to settle before writing any schema:

**Today**, every `ShoplistItem` is independent. "Milk" on list A has no relationship to "milk" on
list B; the same name typed twice is two unrelated rows.

For metadata to be useful — particularly price history — items need *identity across lists*. If
yesterday's "milk" purchase on list A informs today's price estimate on list B, there must be a
concept connecting them.

Two shapes:

- **A) Catalog entity.** Introduce a per-user `CatalogItem` (Name, optional Image, Unit, etc.).
  `ShoplistItem` references `CatalogItem` by FK. Price observations and purchase history attach
  to the catalog entry, not the list item. Adding "milk" to a list either picks an existing
  catalog entry or creates a new one.
  - Pro: clean separation of "what this is" from "where it appears". Price history works
    naturally. Sharing-friendly later (a shared list can reference a shared or per-user catalog).
  - Con: more schema surface. UX has to handle the "this is a new item vs an existing one"
    distinction without nagging.

- **B) Per-list-item with fuzzy linking.** Keep `ShoplistItem` independent; rely on name matching
  (normalized lowercase, maybe trigram similarity) to associate observations across lists.
  - Pro: minimal schema change. Existing data structure is preserved.
  - Con: every aggregation depends on string normalization; renaming an item de-links its history;
    no clean home for images and unit (does each `ShoplistItem` carry its own image, even when
    it's the same item appearing on 12 lists?).

Lean: A) Catalog entity. The cost is paid once; B) leaks across every downstream feature.

Sub-questions if we go with A:
- Catalog ownership: per-user, or per-aggregate-root (the Shoplist itself owns its catalog of
  ever-added items)? Per-user generalises better.
- Migration story for existing items: backfill into a catalog on first run, deduping by
  normalized name? Or leave existing items uncategorized and only new items go through the
  catalog?
- Catalog discoverability in the UI: autocomplete dropdown on add-item? Inline "did you mean…"?

---

## Design space — other open considerations

### Image storage

- **Blob columns in Postgres** — rejected. Doesn't scale, bloats backups.
- **Filesystem volume** — viable for self-hosted single-instance deploy, breaks multi-instance.
- **S3-compatible object storage** — MinIO locally (Aspire can orchestrate), Backblaze B2 /
  Cloudflare R2 / Tigris in production. Adds a dependency but is the standard answer.
- **Cloudflare Images / imgproxy** — image-specific service, handles resizing and format
  conversion (AVIF, WebP). Heavier but solves variants for free.

Lean: S3-compatible (MinIO local, R2 or B2 in prod). Image variants generated at upload time
via Aspire-orchestrated worker or on-demand via imgproxy.

### Image processing

- **Upload-time variants** — resize on upload, store thumbnail + full. Predictable cost,
  storage doubles, no runtime overhead.
- **On-demand via imgproxy** — small Go service that serves on-the-fly variants from origin
  storage. Cleaner storage model, adds a runtime hop.

### Units of measurement

- Free-text vs enumerated. Free-text is honest about the long tail ("bunch of bananas").
  Enumerated is friendlier for aggregation (price-per-unit).
- Pragmatic middle ground: small enum (`Count`, `Kg`, `G`, `L`, `Ml`, `Other`), with a free-text
  field when `Other`.

### Price model

Each price observation is its own row:
- `Amount` (decimal — money type, never float)
- `Currency` (single currency MVP — likely EUR — with a domain assumption it doesn't change per
  user; revisit if multi-currency users appear)
- `ObservedAt` (timestamp)
- Source: tied to a `ShoplistItem.Fulfilled` event? Manually entered? Both?
- Optional: store / location / notes

Aggregates surfaced in UI:
- "Last seen at €X"
- "Average over last N observations"
- Trend indicator (price rising / stable / falling)

### Purchase history

A `Purchase` (or `Fulfillment`) event captures: `CatalogItem`, `ShoplistId`, `FulfilledAt`,
optional `PriceObservation`. This is the table that grows fastest. Retention policy: keep
indefinitely until volume becomes an issue. Index on `(CatalogItemId, FulfilledAt DESC)` for the
"last bought" query that the UI will hit constantly.

### API surface

- Catalog CRUD (create, rename, update image/unit).
- Item add-to-list now takes a `CatalogItemId` OR a new name that creates one.
- Purchase event emitted on fulfilment toggle (server-side, not client-controlled).
- Price observation: separate endpoint, or piggyback on fulfilment? Probably separate — user may
  fulfil first and add the price later.

### UX surface

- Add-item input: typeahead against catalog, with "create new" fallback.
- Item detail / edit screen: where image, unit, notes, history live. Today there's no such
  screen — this project is its first real driver.
- Price display on item rows: subtle, not always-on. "€2.49 (avg)" tucked at the end of the row.
- Bulk catalog management: probably out of scope for v1; revisit if the catalog gets messy.

---

## Co-design with other projects

- **Sharing.** If lists are shared, does the catalog follow? Most natural answer: catalog stays
  per-user (so "my prices" remain private), but shared list items may reference any
  collaborator's catalog or fall back to "uncategorized name". Decide alongside the sharing
  project — both touch the same authorization boundary.
- **Offline-first.** Image upload while offline is its own beast: queue uploads as a separate
  outbox track (different size/retry profile from JSON mutations). Maintain a local blob URL
  until sync confirms.
- **Realtime.** Catalog and price-observation events flow through the same broadcast channel as
  list edits. Reuse, don't duplicate.

---

## Open questions

- **Catalog vs per-item identity.** The headline question. Resolve first.
- **Image storage**: S3-compatible (which provider for prod?) vs filesystem (single-instance
  acceptable?).
- **Image variants**: upload-time vs on-demand.
- **Single-currency assumption**: hold the line on EUR-only MVP?
- **Catalog scope**: per-user from day one; revisit on sharing.
- **Migration of existing items**: backfill into catalog, or leave them legacy?
- **Add-item UX**: typeahead against catalog vs explicit "pick existing or create new" flow.
- **Predictive suggestions later**: does the price/purchase schema need to support them now, or
  is it fine to add aggregation tables/materialized views later?

---

## Priority signals (when to actually start)

- This is the **highest-stake schema-touching project** in the roadmap. Integration tests
  (see [testing plan](../testing/plan.md)) should be live before this lands. The testing plan
  already names this project as the canonical trigger.
- Sharing wants to know the catalog shape (does a collaborator see my catalog entries?). Either
  do this first, or align design upfront so sharing doesn't have to retrofit.
- Image storage adds a real new dependency (S3-compatible). Worth the cost only when there's a
  concrete user-facing feature ready to consume it — i.e. don't stand up MinIO until the rest of
  the project is shaped.
