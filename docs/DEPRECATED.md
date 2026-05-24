# Deprecated business objects and properties

Human-readable registry for **deprecated or legacy** domain types and members in `Visa2026.Module`. Use this when refactoring, writing updaters, or answering “can we delete this?”

**Related:** state lifecycle deprecations live in [`docs/STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md) and [`docs/states/`](states/). Lookup seeding “no longer used” paths are in [`docs/LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md).

---

## When to update this file

Add or extend a row **in the same PR** when you:

- Mark a type or member `[Obsolete]` or hide it from the UI as legacy-only.
- Stop seeding or syncing a lookup catalog while the table/column remains.
- Drop a column or table via a `ModuleUpdater` (move the row to **Removed** with updater name).

Do **not** remove EF types or DB columns until a row exists here (or in a linked feature doc) and migration impact is understood.

### Row fields

| Column | Meaning |
|--------|---------|
| **Status** | `Deprecated` — do not use in new code/UI. `Retained` — DB/import only, hidden in app. `Removed` — no longer in schema (historical). |
| **Replacement** | What to use instead (type, property, or workflow). |
| **Schema** | `Keep table`, `Keep column`, `Dropped by <Updater>`, etc. |
| **Since** | Approx. release or PR theme (optional). |

In C#, prefer `[Obsolete("…")]` with the same replacement text when the compiler allows it; for hidden legacy columns, use an XML `<summary>Legacy …</summary>` on the property.

---

## Business objects and enums

| Name | Status | Replacement | Schema | Notes |
|------|--------|-------------|--------|-------|
| **ApplicationTypeFilter** | Deprecated | `ApplicationType.SelectionCode` + `ApplicationTypeCodePickerHelper` (hundreds grouping) | Table retained; **not** in `LookupCatalogs/manifest.json` | Still exposed read-only in security/Web API for existing FKs. See [`docs/APPLICATION_BO_TYPE_SELECTION_REFACTOR.md`](APPLICATION_BO_TYPE_SELECTION_REFACTOR.md). |
| **ApplicabilityMode** (enum) | Deprecated | `UserReportTemplate.ApplicableTypeLinks`, `ApplicableProjectContractLinks`, `VisibilityCriteria` | Enum column on `UserReportTemplates` retained | `[Obsolete]` on enum and `UserReportTemplate.ApplicabilityMode`. |
| **ApplicationStatus** (enum) | Deprecated | `ApplicationProgress` + `Application.CurrentState`; locations via **ApplicationLocation** catalog | Enum unused on `Application` BO; may remain in old import models | Docs in [`docs/BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) §8b still describe the old enum — prefer §8c progress model for new work. |
| **Company** | Deprecated (Phase 3) | `CompanyProfile` + `SystemSettings` (app numbering) | Table retained | Hidden from navigation (`NavigationItem(false)`). See [`docs/ORGANIZATION_SINGLETON_REFACTOR_PLAN.md`](ORGANIZATION_SINGLETON_REFACTOR_PLAN.md). |
| **CompanyHead** | Deprecated (Phase 1+) | `AuthorizedSignatory` | Table retained | Flat signatory fields; no `LocalEmployee` / `Person` link on replacement. |
| **Representative** | Deprecated (Phase 1+) | `AuthorizedRepresentative` | Table retained | Flat representative fields. |
| **LocalEmployee** | Deprecated (Phase 1+) | Folded into signatory/representative singletons (no replacement BO) | Table retained | Roster not used after refactor. |

### Lookups: seeding vs UI-only (not always “deprecated”)

| Name | Status | Replacement | Schema | Notes |
|------|--------|-------------|--------|-------|
| **ApplicationLocation** | Active (seeded) | — | `LookupCatalogs/application-location.json` | Used on `ApplicationProgress.Location`. Layer B strings in `LookupCatalogStrings.json`. |
| **BorderZoneLocation** | Retained (UI catalog) | Comma-separated **`BorderZoneName`** on `ApplicationItem` / string on `Visa` | BO + table retained; **no** JSON catalog | Long labels for item multi-select; see [`docs/COMMA_SEPARATED_MULTI_SELECT.md`](COMMA_SEPARATED_MULTI_SELECT.md). Do not confuse with **ApplicationLocation**. |
| **MovementPermitLocation** | Retained (UI catalog) | Per-deployment rows in lookup UI | Table retained; excluded from manifest | See [`docs/LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md). |

---

## Properties on active business objects

| Business object | Property | Status | Replacement | Schema |
|-----------------|----------|--------|-------------|--------|
| **UserReportTemplate** | `ApplicabilityMode` | Deprecated | Applicable type/contract links + `VisibilityCriteria` | Column retained |
| **ApplicationType** | `ApplicationTypeFilter`, `ApplicationTypeFilterNames` | Deprecated | `SelectionCode`, quick-code picker | FK / string retained |
| **ProjectContract** | `Description`, `Company`, `Ministry`, `Images`, `Documents` | Retained (legacy) | `Name` / `NameTm` / `Code` on contract; `CompanyProfile`; Word static text for letters | Columns retained; UI hidden |
| **Application** | `Company`, `CompanyHead`, `Representative` | Removed (Phase 3) | `Application_Company_*` / `Application_CompanyHead_*` aliases + singletons | DB columns retained until Phase 5 |
| **Person** | `Company` | Removed (Phase 3) | Single-tenant: no per-person company FK | DB column retained until Phase 5 |
| **Passport** | `PersonalNumber` | Retained (legacy) | `Person.PersonalNumber` | Column retained; hidden in UI |

---

## Removed schema (historical)

| Artifact | Removed by | Replacement |
|----------|------------|-------------|
| `Visas.HasBorderZonePermit` | `VisaBorderZoneLocationStringUpdater` | `Visa.BorderZoneLocation` string + **BorderZoneName** catalog |
| `Visa` ↔ `City` link table | `VisaBorderZoneLocationStringUpdater` | `Visa.BorderZoneLocation` |
| `WorkPermitItemPermittedCity` / link table | `WorkPermitItemPermittedLocationsStringUpdater` | `WorkPermitItem.WorkPermittedLocations` + **WorkPermittedLocation** catalog |
| `lookup.xlsm` as runtime seed | Lookup catalog sync on deploy | `LookupCatalogs/*.json` + ApplicationType C# seeds — see [`docs/LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md) |

---

## Tooling and docs (non-BO)

| Item | Status | Replacement |
|------|--------|-------------|
| `Visa2026.DataImporter --seed-lookups-only` | Deprecated | App startup `LookupCatalogSyncUpdater` |
| `LOOKUPS.md` as source of truth | Deprecated | JSON catalogs in git; human reference only |
| `BusinessTripWordController` | Removed | `WordReportsController` + `BusinessTripSanawyReportDef` — see [`docs/WORD_REPORT_GENERATION_IDEA.md`](WORD_REPORT_GENERATION_IDEA.md) |

---

## Change log

| Date | Change |
|------|--------|
| 2026-05-24 | Initial registry; ApplicationLocation JSON seed called out vs BorderZoneLocation UI catalog. |
