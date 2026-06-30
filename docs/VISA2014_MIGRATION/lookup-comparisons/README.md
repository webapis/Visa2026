# VISA2014 → Visa2026 — lookup comparison previews

Side-by-side review of **legacy lookup values actually used in production** vs **Visa2026 seeded catalogs** (no legacy lookup import).

**Purpose:** Decide layer-3 mappings, gaps, and import policy before updating `lookup-translations.yaml`.

---

## Table columns

| Column | Meaning |
|--------|---------|
| **Legacy (VISA2015)** | Value as stored / displayed in old DB (what officers saw on Person rows) |
| **Visa2026 (target)** | Matching catalog row (`Code` / `NameTm` / `LocalizationKey`) from `LookupCatalogs/*.json` |
| **Person rows** | Active `Person` rows referencing this legacy shape (if applicable) |
| **Match** | `exact` · `proposed` · `mismatch` · `legacy_only` · `target_only` · `blocked` |
| **Import note** | Recommended action for migration |

**Do not import** legacy lookup table rows. Only translate Person FKs to existing Visa2026 catalog entries (or `allow_null` / custom field).

---

## Files

| Catalog | Comparison |
|---------|------------|
| **Review queue** | [`lookup-review-queue.yaml`](./lookup-review-queue.yaml) — person-wave / application-wave gate |
| MaritalStatus | [MaritalStatus.md](./MaritalStatus.md) · [MaritalStatus.yaml](./MaritalStatus.yaml) (**approved** 2026-06-21) |
| Relationship | [Relationship.md](./Relationship.md) · [Relationship.yaml](./Relationship.yaml) (**approved** 2026-06-21) |
| ProjectContract | [ProjectContract.md](./ProjectContract.md) · [ProjectContract.yaml](./ProjectContract.yaml) (**approved** 2026-06-21 — GT-15 pilot) |
| Country (Person) | Done in `lookup-translations.yaml` audit (64 identity codes) |
| Gender | Done in `lookup-translations.yaml` (2 values) |
| **MigrationService** | [MigrationService.md](./MigrationService.md) · [MigrationService.yaml](./MigrationService.yaml) (**approved** 2026-06-30) |

Add one `{Catalog}.md` + `{Catalog}.yaml` per catalog audit.

---

## Workflow

1. SQL DISTINCT legacy values **used on transactional data** (not whole lookup table).
2. Load Visa2026 JSON catalog (+ optional OData GET on dev DB to confirm runtime).
3. Fill comparison table; mark `Match` and `Import note`.
4. Update `lookup-translations.yaml` when mapping is approved.
5. Link from dossier / `migration-status.yaml` `lookupCatalogAudit`.

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-30 | MigrationService draft — DepartmentForRegistration 9 codes on 6,840 apps; TDMG başlygyna + Kerki gaps |
| 2026-06-21 | ProjectContract approved — all legacy codes → GT-15 (Gap Insaat / Calik Energi pilot) |
| 2026-06-21 | Relationship comparison (147/159 proposed; ejesi + giyewisi gaps) |
| 2026-06-21 | Added lookup-review-queue.yaml; MaritalStatus approved |
| 2026-06-21 | Initial format; MaritalStatus comparison |
