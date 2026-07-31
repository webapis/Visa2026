---
name: visa2026-invitation-work-permit-document-copies
description: >-
  Header and item document copies in the global preview slot for Invitation,
  WorkPermit, Rejection, and BorderZone. BorderZone Phase 0 (BorderZoneDocument +
  Documents like Invitation/WorkPermit) ships in the same release as preview.
  Planned — not implemented.
disable-model-invocation: false
---

# Visa2026 — Header & item document copies

**Families:** `Invitation` / `InvitationItem`, `WorkPermit` / `WorkPermitItem`, `Rejection` / `RejectionItem`, `BorderZone` / `BorderZoneItem`.

**Implementation status:** **Planned.**

## Border zone (resolved)

**Same release:** add `BorderZoneDocument` + `BorderZone.Documents` + DetailView **Documents** tab (mirror `Invitation` / `WorkPermit`) **before** Border zone preview slot. Not empty-state-only UI.

## Canonical doc

**[`docs/INVITATION_WORK_PERMIT_DOCUMENT_COPIES.md`](../../../docs/INVITATION_WORK_PERMIT_DOCUMENT_COPIES.md)**

**Catalog chrome:** Header copies use shared `.doc-copies-catalog*` ([`PREVIEW_SLOT.md`](../../../docs/PREVIEW_SLOT.md) § Document-copies catalog chrome) — same sectioned look as Person copies. Do not reintroduce flat `__group` cards.

## Phases

| Phase | Focus |
|-------|--------|
| **0** | `BorderZone` file storage parity with Invitation/WorkPermit |
| **1** | Preview slot all four families (Border zone after step 0, same release) |
| **2** | ListView **Copies** columns |
| **3–4** | Cross-links, images — **deferred** |
