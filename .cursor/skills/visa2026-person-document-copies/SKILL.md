---
name: visa2026-person-document-copies
description: >-
  Person-scoped document copies in the global preview slot: sectioned catalog of
  child BO attachments, DetailView toolbar + ListView row column, per-record Preview.
  Phases 1-2 shipped; phases 3-4 deferred. Not ministry PdfGenerationBatch —
  use visa2026-document-copies for ApplicationItem ZIP. Shell UX via visa2026-preview-slot.
disable-model-invocation: false
---

# Visa2026 — Person document copies

**User prompts:** [prompts.md](./prompts.md) (`@visa2026-person-document-copies`).

**Implementation status:** **Phases 1–2 shipped.** **Phases 3–4 deferred** until product approves (cross-link to ApplicationItem copies; optional person ZIP). Do not implement Phase 3/4 from this skill without an explicit user request.

## Agent workflow (when implementing)

1. **Read** [`docs/PERSON_DOCUMENT_COPIES.md`](../../../docs/PERSON_DOCUMENT_COPIES.md) and [learnings.md](./learnings.md).
2. **Classify** — Person catalog/resolver (**this skill**) vs ApplicationItem ministry ZIP (**[document-copies](../visa2026-document-copies/SKILL.md)**) vs slot layout (**[preview-slot](../visa2026-preview-slot/SKILL.md)**).
3. **Implement** in **Module** (resolver) + **Blazor** (component, slot panel); keep ApplicationItem paths unchanged.
4. **Verify** — `dotnet build Visa2026.slnx -c Debug`; Person DetailView → catalog → Preview → full-width viewer.
5. **Record** — append [learnings.md](./learnings.md) after verified behaviour ([MATURITY.md](./MATURITY.md)).

## Canonical doc

**[`docs/PERSON_DOCUMENT_COPIES.md`](../../../docs/PERSON_DOCUMENT_COPIES.md)**

**Related skills:**

| Topic | Skill |
|-------|--------|
| ApplicationItem scans + ministry ZIP + application form | [visa2026-document-copies](../visa2026-document-copies/SKILL.md) |
| `#visa-preview-slot` shell, catalog card CSS, new occupant | [visa2026-preview-slot](../visa2026-preview-slot/SKILL.md) |
| Person BO collections, `IsEmployee` / family member | [`Person.md`](../../../Visa2026.Module/BusinessObjects/Person.md) |
| `*Document` types in ministry ZIP | [`APPLICATION_DIPLOMA_PACKAGE_PLAN.md`](../../../docs/APPLICATION_DIPLOMA_PACKAGE_PLAN.md) |
| Person dossier page / director export ZIP | [visa2026-person-dossier](../visa2026-person-dossier/SKILL.md) |

**Reference (planned file map):** [reference.md](./reference.md)

---

## Scope (this skill)

| In scope | Out of scope |
|----------|----------------|
| `PersonLinkedDocumentsResolver` + sectioned catalog | `ApplicationItemLinkedDocumentsResolver` changes |
| `PersonDocumentCopiesComponent` + slot panel | Resminamalar / Word reports |
| Per-record Preview (reuse merge helpers) | `PdfGenerationBatch` / application form (v1) |
| `PersonDocumentCopiesController` DetailView toolbar (ListView = row column only) | XFA field mapping ([pdf-form-mapping](../visa2026-pdf-form-mapping/SKILL.md)) |
| Role-based section visibility (employee vs family) | `FamilyMemberImage` byte[] preview unless product adds |

---

## Design rules (do not violate)

1. **Person ≠ ApplicationItem** — index live `Person` collections, not `ApplicationItem` FK snapshots.
2. **Separate slot occupant** — `PersonDocumentCopies` mode; do not overload `DocumentCopiesSlotRequest` without an explicit scope discriminator.
3. **Preview-first v1** — no ministry package enqueue in first release.
4. **Catalog vs preview** — catalog card CSS only in catalog mode; viewer full slot width ([preview-slot](../visa2026-preview-slot/SKILL.md)).
5. **Reuse merge, not merger** — share PDF preview builder; do not use `ApplicationItemLinkedDocumentsMerger` for Person rows.
6. **Shared catalog chrome** — .doc-copies-catalog* is the visual source of truth for all document-copies occupants ([PREVIEW_SLOT.md](../../../docs/PREVIEW_SLOT.md) § Document-copies catalog chrome). Person catalog defines the look; AppItem/Header must match.
7. **Dedicated brand mark** — smiling paperclip + pill (.doc-copies-brand, DocumentCopies ImageName, DocumentCopiesBrandMark). Use for toolbar actions, ListView Copies cells, dossier button, and slot titles — not generic BO_FileAttachment.

---

## Phased delivery

| Phase | Focus | Status |
|-------|--------|--------|
| **1** | Resolver, DetailView action, slot panel, catalog, Preview, Refresh | **Shipped** |
| **2** | ListView **Copies** column (no toolbar), gear details, current badges | **Shipped** |
| **3** | Cross-link to ApplicationItem document copies | **Deferred** |
| **4** | Optional person ZIP export (new service, not `PdfGenerationBatch`) | **Deferred** |

For ministry ZIP / application form, officers use **[document-copies](../visa2026-document-copies/SKILL.md)** on **ApplicationItem** — not Person catalog.

---

## UX / implementation checklist (phase 1)

1. **Module:** `PersonLinkedDocumentsResolver`, DTOs, `PersonDocumentCopiesController`.
2. **Preview slot:** `PersonDocumentCopiesSlotRequest`, `VisaPreviewSlotMode.PersonDocumentCopies`, occupant key, `OpenPersonDocumentCopiesAsync`.
3. **Blazor:** `PersonDocumentCopiesSlotPanel`, `PersonDocumentCopiesComponent` (`UseInlinePreview`), file access service.
4. **CSS:** reuse `.resminamalar-slot-panel` + `.app-item-doc-copies--inline-slot` ([preview-slot](../visa2026-preview-slot/SKILL.md)).
5. **Localization:** `PersonDocumentCopies.*` in `UiStrings.messages.json`; action in model.
6. **Docs:** update [`PERSON_DOCUMENT_COPIES.md`](../../../docs/PERSON_DOCUMENT_COPIES.md) status when shipped.

---

## Build / verify

```powershell
dotnet build Visa2026.slnx -c Debug
```

Manual (when implemented): employee Person with scans → Document copies → section rows → Preview → Close.

---

## Recording experience

| After verified fix | Action |
|--------------------|--------|
| Person resolver, catalog, preview, Person entry points | Append [learnings.md](./learnings.md) |
| Slot shell / CSS / occupant | [preview-slot/learnings.md](../visa2026-preview-slot/learnings.md) |
| ApplicationItem ZIP / scans on lines | [document-copies/learnings.md](../visa2026-document-copies/learnings.md) |
