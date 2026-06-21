# VISA2014 → Visa2026 — migration status

**Last updated:** 2026-06-21  
**Machine-readable:** [`migration-status.yaml`](migration-status.yaml) — **update this file first**, then refresh this dashboard if summaries drift.

**Quick links:** [Migration plan](../VISA2014_MIGRATION.md) · [Order](../../Visa2026.DataImporter/legacy/visa2014/order.yaml) · [Import strategy](../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml) · [Lookup strategy](LOOKUP_RESOLUTION_STRATEGY.md) · [File/image import](FILE_AND_IMAGE_IMPORT.md)

---

## Current focus

1. **Person pilot OData import** — `--import-visa2014` for Person (importConfirmed set 2026-06-21)
2. **ApplicationType lookup comparison** → Application discovery
3. **Passport** in `order.yaml` (ISS-005) before ApplicationItem

---

## Workstreams

| Workstream | Status | Artifact |
|------------|--------|----------|
| Schema bootstrap | **Done** | [`schema-snapshot.md`](schema-snapshot.md) |
| Lookup resolution strategy | **Done** | [`LOOKUP_RESOLUTION_STRATEGY.md`](LOOKUP_RESOLUTION_STRATEGY.md) |
| Import strategy approval | **Done** | [`import-strategy.yaml`](../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml) (`approved` 2026-06-21) |
| Layer 3 lookup audit | **Done** (person-wave) | [`lookup-translations.yaml`](lookup-translations.yaml) |
| Phase 1 discovery | **In progress** | [`discovery/`](discovery/) |
| Excel preview export | **Done** (Person) | [`EXCEL_PREVIEW_EXPORT.md`](EXCEL_PREVIEW_EXPORT.md) |
| File/image import | **In progress** | [`FILE_AND_IMAGE_IMPORT.md`](FILE_AND_IMAGE_IMPORT.md) |
| Import implementation | Not started | `Visa2026.DataImporter` |
| Pilot OData load | Not started | `Visa2026DbDev` |

---

## Entities (discovery + import)

Canonical flags: [`order.yaml`](../../Visa2026.DataImporter/legacy/visa2014/order.yaml). Summary: [`entity-inventory.yaml`](entity-inventory.yaml).

| Entity | Discovery | Import confirmed | Import run | Notes |
|--------|-----------|------------------|------------|-------|
| **Person** | **Complete** | **Yes** (2026-06-21) | Pending | Pilot; dossier [`discovery/Person.yaml`](discovery/Person.yaml) |
| **Application** | Not started | No | Pending | Blocked on Person review; needs ApplicationType lookup audit |
| **ApplicationItem** | Not started | No | Pending | Depends on Person + Application |

**Not in order yet:** Passport (child of Person — likely needed before ApplicationItem).

---

## Lookup catalog audit

Do **not** import legacy lookup tables. Track translation coverage here; detail in [`lookup-translations.yaml`](lookup-translations.yaml).

| Catalog | Used by | Audit | Mapped values | Policy |
|---------|---------|-------|---------------|--------|
| Gender | Person | **Done** | 2 | allow_null |
| Country | Person | **Done** | 64 | block_row |
| MaritalStatus | Person | **Approved** | 6 | allow_null |
| Relationship | Person | **Approved** | 8 | allow_null |
| ProjectContract | Person, Application | **Approved** | 15 → GT-15 | allow_null |
| ApplicationType | Application, ApplicationItem | Not started | — | — |

---

## Open issues

| ID | Severity | Title | Status |
|----|----------|-------|--------|
| [ISS-004](migration-status.yaml) | Medium | 5 PersonalNumber duplicate pairs | Open |
| [ISS-005](migration-status.yaml) | Medium | Passport not in order.yaml | Open |
| [ISS-008](migration-status.yaml) | Medium | Application discovery not started | Open |
| [ISS-009](migration-status.yaml) | Medium | Open strategic decisions | Open |
| [ISS-010](migration-status.yaml) | Low | Legacy lookup duplicates / unused rows | In progress |
| [ISS-011](migration-status.yaml) | Medium | Binary fields separate from Excel preview | In progress |

**Resolved:** ISS-001, ISS-002, ISS-003, ISS-006, ISS-007, ISS-012, ISS-013, ISS-014.

Full detail (notes, owners, dates): [`migration-status.yaml`](migration-status.yaml) → `issues[]`.

---

## Done (recent)

- [x] VISA2015 schema bootstrap — 94 tables ([`schema-snapshot.md`](schema-snapshot.md))
- [x] Migration scaffolding + three-layer mapping layout
- [x] **Person** Phase 1 discovery complete ([`discovery/Person.yaml`](discovery/Person.yaml))
- [x] Person field-map + property gap registry
- [x] Gender lookup translation (Ayal → Aýal)
- [x] **Country lookup audit (Person scope)** — 64 codes, identity map to `Country.Code` ([`lookup-translations.yaml`](lookup-translations.yaml))
- [x] Lookup resolution strategy documented ([`LOOKUP_RESOLUTION_STRATEGY.md`](LOOKUP_RESOLUTION_STRATEGY.md))
- [x] Import strategy **approved** (2026-06-21) — `import-strategy.yaml`
- [x] **MaritalStatus lookup approved** — Status int 0–5 → Visa2026 Code ([`lookup-translations.yaml`](lookup-translations.yaml), [`MaritalStatus.md`](lookup-comparisons/MaritalStatus.md))
- [x] **Lookup review queue** — person-wave / application-wave gate ([`lookup-review-queue.yaml`](lookup-comparisons/lookup-review-queue.yaml))
- [x] **Person importConfirmed** — 2026-06-21 after Person-preview.xlsx review ([`discovery/Person.yaml`](discovery/Person.yaml))
- [x] **Relationship lookup approved** — Mother + BrotherInLaw; layer 3 ([`Relationship.md`](lookup-comparisons/Relationship.md))
- [x] **ProjectContract approved (pilot)** — all legacy codes → GT-15 ([`ProjectContract.md`](lookup-comparisons/ProjectContract.md))
- [x] **Person-wave lookup gate complete** ([`lookup-review-queue.yaml`](lookup-comparisons/lookup-review-queue.yaml))

---

## How to update

1. Edit **`migration-status.yaml`** — set `lastUpdated`, `updatedBy`, and the relevant `workstreams` / `lookupCatalogAudit` / `issues` entry.
2. For **entity** discovery or import flags, update **`order.yaml`** + dossier + **`entity-inventory.yaml`** (keep in sync).
3. Refresh **`STATUS.md`** tables if the yaml changed materially (or rely on yaml as source of truth for agents).
4. Append session notes to [`.cursor/skills/visa2014-to-visa2026-import/learnings.md`](../../.cursor/skills/visa2014-to-visa2026-import/learnings.md) when a dossier closes or an issue is resolved.

**Issue lifecycle:** `open` → `in_progress` → `resolved` (set `resolvedAt`; move id to `resolvedIssues` when closing). Use `deferred` / `wont_fix` when explicitly parked.

**Workstream lifecycle:** `not_started` → `in_progress` → `done` (or `blocked` with `blockedBy` / issue link).
