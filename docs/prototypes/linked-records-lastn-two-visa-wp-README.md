# Linked records Last-N (visa + work permit) — prototypes (2026-11-07)

For **cancel visa + work permit** (`cancel_visa_wp`) and other profiles with `PersonVisaLastCount` / `PersonWorkPermitItemLastCount` = 2.

## Decision (proposed)

| Surface | Pattern |
|---------|---------|
| **Linked records overview** | Separate rows: two **Visa**, two **Work permit** (same labels, different values). Optional muted **1 of 2** / **2 of 2**. Eye per row. **Join** pins all visible Visa + WP rows up to Last-N. |
| **People & links list** | Keep **one person row**; Visa / Work permit cells stack or comma-join both numbers (do not duplicate the person). |

Do **not** collapse two visas into one dialog row — cancellation needs each document pinned.

## Images

| File | Shows |
|------|--------|
| [linked-records-lastn-two-visa-wp-overview.jpg](./linked-records-lastn-two-visa-wp-overview.jpg) | Dialog overview with 2× Visa + 2× Work permit |
| [people-links-lastn-two-visa-wp-row.jpg](./people-links-lastn-two-visa-wp-row.jpg) | People & links row with both numbers in cells |

## Implementation (shipped 2026-11-07)

`PersonLinkReview.Build` expands Last-N kinds into separate overview rows (`SlotIndex`/`SlotCount`). **Join** calls `SyncResolvedLinksForKind` so all visible Visa/WP ids are pinned. People & links stacks numbers via `JoinCellsForPerson` (+ Work permit column).