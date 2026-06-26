# VISA2014 â†’ Visa2026 â€” migration status

**Last updated:** 2026-06-26  
**Machine-readable:** [`migration-status.yaml`](migration-status.yaml) â€” **update this file first**, then refresh this dashboard if summaries drift.

**Quick links:** [Migration plan](../VISA2014_MIGRATION.md) Â· [Multi-company sources](MULTI_COMPANY_LEGACY_SOURCES.md) Â· [Order](../../Visa2026.DataImporter/legacy/visa2014/order.yaml) Â· [Import strategy](../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml) Â· [Lookup strategy](LOOKUP_RESOLUTION_STRATEGY.md)

---

## Current focus

1. **Person pilot OData import (live)** — start Blazor :5001, then `--import-visa2014 --entity Person --legacy-source calik-energi --max-rows 10`
2. **Full Person import** (~2924 rows) after spot-check
3. **ApplicationType** lookup comparison → Application discovery
4. **Passport** in `order.yaml` (ISS-005)

**Person `importConfirmed: true`** â€” 2026-06-26 (Ã‡alik `Person-preview.calik-energi.xlsx`).

---

## Workstreams

| Workstream | Status | Artifact |
|------------|--------|----------|
| Schema bootstrap | **Done** | [`schema-snapshot.md`](schema-snapshot.md) |
| Lookup resolution strategy | **Done** | [`LOOKUP_RESOLUTION_STRATEGY.md`](LOOKUP_RESOLUTION_STRATEGY.md) |
| Import strategy approval | **Done** | [`import-strategy.yaml`](../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml) (`approved` 2026-06-21) |
| Multi-company legacy | **Done** | [`MULTI_COMPANY_LEGACY_SOURCES.md`](MULTI_COMPANY_LEGACY_SOURCES.md) |
| Layer 3 lookup audit | **In progress** | Shared catalogs done; ProjectContract per company |
| Phase 1 discovery | **In progress** | [`discovery/`](discovery/) |
| Excel preview export | **Done** (Ã‡alik Person) | `Person-preview.calik-energi.xlsx` reviewed 2026-06-26 |
| File/image import | **In progress** | [`FILE_AND_IMAGE_IMPORT.md`](FILE_AND_IMAGE_IMPORT.md) |
| Import implementation | **In progress** | `--legacy-source` + Person OData importer |
| Pilot OData load | Not started | LocalDB `Visa2026` (Ã‡alik) |

---

## Entities (discovery + import)

Canonical flags: [`order.yaml`](../../Visa2026.DataImporter/legacy/visa2014/order.yaml). Summary: [`entity-inventory.yaml`](entity-inventory.yaml).

| Entity | Discovery | Import confirmed | Import run | Notes |
|--------|-----------|------------------|------------|-------|
| **Person** | **Complete** | **Yes** (2026-06-26, Ã‡alik) | Pending | `legacySource: calik-energi`; pilot OData next |
| **Application** | Not started | No | Pending | Blocked on Person review; needs ApplicationType lookup audit |
| **ApplicationItem** | Not started | No | Pending | Depends on Person + Application |

**Not in order yet:** Passport (child of Person â€” likely needed before ApplicationItem).

---

## Lookup catalog audit

Do **not** import legacy lookup tables. Track translation coverage here; detail in [`lookup-translations.yaml`](lookup-translations.yaml).

| Catalog | Used by | Audit | Mapped values | Policy |
|---------|---------|-------|---------------|--------|
| Gender | Person | **Done** | 2 | allow_null |
| Country | Person | **Done** | 64 | block_row |
| MaritalStatus | Person | **Approved** | 6 | allow_null |
| Relationship | Person | **Approved** | 8 | allow_null |
| ProjectContract | Person, Application | **Complete** (Çalik) | 73 identity pass-through | allow_null |
| ApplicationType | Application, ApplicationItem | Not started | â€” | â€” |

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

**Resolved:** ISS-001, ISS-002, ISS-003, ISS-006, ISS-007, ISS-012, ISS-013, ISS-014, ISS-015, ISS-016.

Full detail (notes, owners, dates): [`migration-status.yaml`](migration-status.yaml) â†’ `issues[]`.

---

## Done (recent)

- [x] VISA2015 schema bootstrap â€” 94 tables ([`schema-snapshot.md`](schema-snapshot.md))
- [x] Migration scaffolding + three-layer mapping layout
- [x] **Person** Phase 1 discovery complete ([`discovery/Person.yaml`](discovery/Person.yaml))
- [x] Person field-map + property gap registry
- [x] Gender lookup translation (Ayal â†’ AÃ½al)
- [x] **Country lookup audit (Person scope)** â€” 64 codes, identity map to `Country.Code` ([`lookup-translations.yaml`](lookup-translations.yaml))
- [x] Lookup resolution strategy documented ([`LOOKUP_RESOLUTION_STRATEGY.md`](LOOKUP_RESOLUTION_STRATEGY.md))
- [x] Import strategy **approved** (2026-06-21) â€” `import-strategy.yaml`
- [x] **MaritalStatus lookup approved** â€” Status int 0â€“5 â†’ Visa2026 Code ([`lookup-translations.yaml`](lookup-translations.yaml), [`MaritalStatus.md`](lookup-comparisons/MaritalStatus.md))
- [x] **Lookup review queue** â€” person-wave / application-wave gate ([`lookup-review-queue.yaml`](lookup-comparisons/lookup-review-queue.yaml))
- [x] **Person importConfirmed** â€” 2026-06-21 after Person-preview.xlsx review ([`discovery/Person.yaml`](discovery/Person.yaml))
- [x] **Relationship lookup approved** â€” Mother + BrotherInLaw; layer 3 ([`Relationship.md`](lookup-comparisons/Relationship.md))
- [x] **ProjectContract approved (Gap pilot)** — 15 codes → GT-15 ([`ProjectContract.md`](lookup-comparisons/ProjectContract.md))
- [x] **ProjectContract Çalik audit** — 73 union codes; identity pass-through ([`ProjectContract.calik-energi.md`](lookup-comparisons/ProjectContract.calik-energi.md))
- [x] **ProjectContract Çalik tenant catalog** — `project-contract.calik-energi.json` (73 rows, ISS-016 resolved 2026-06-26)
- [x] **Person-wave lookup gate complete** ([`lookup-review-queue.yaml`](lookup-comparisons/lookup-review-queue.yaml))

---

## How to update

1. Edit **`migration-status.yaml`** â€” set `lastUpdated`, `updatedBy`, and the relevant `workstreams` / `lookupCatalogAudit` / `issues` entry.
2. For **entity** discovery or import flags, update **`order.yaml`** + dossier + **`entity-inventory.yaml`** (keep in sync).
3. Refresh **`STATUS.md`** tables if the yaml changed materially (or rely on yaml as source of truth for agents).
4. Append session notes to [`.cursor/skills/visa2014-to-visa2026-import/learnings.md`](../../.cursor/skills/visa2014-to-visa2026-import/learnings.md) when a dossier closes or an issue is resolved.

**Issue lifecycle:** `open` â†’ `in_progress` â†’ `resolved` (set `resolvedAt`; move id to `resolvedIssues` when closing). Use `deferred` / `wont_fix` when explicitly parked.

**Workstream lifecycle:** `not_started` â†’ `in_progress` â†’ `done` (or `blocked` with `blockedBy` / issue link).
