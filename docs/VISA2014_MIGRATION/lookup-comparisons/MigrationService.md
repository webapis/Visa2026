# Lookup comparison — MigrationService (Çalik Energi / VISA2015)

**Status:** **approved** (2026-06-30)  
**Legacy source:** `--legacy-source calik-energi`  
**Legacy table:** `dbo.DepartmentForRegistration` (`TitleOfDepartmentForRegistration`, `DepartmentForRegistrationL`)  
**FK usage:** `dbo.Application.DepartmentForRegistration`  
**Target:** [`migration-service.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/migration-service.json) (**9** rows)  
**OData resolve (planned):** `NameTm` via `LookupCatalogMatchHelper.NormalizeKey` (not `Code` alone — six target rows share `TDMG`)

**Related:** Application field-map `DepartmentForRegistration` → `Application.MigrationService` when `ApplicationType.ShowMigrationService`.

---

## Summary

| Metric | Value |
|--------|------:|
| Active `Application` rows | **12,237** |
| With `DepartmentForRegistration` FK | **6,840** |
| Without FK (expected for non-registration types) | **5,397** |
| Legacy catalog rows (active) | **9** (all **9** used on data) |
| Distinct legacy codes on data | **9** |
| Visa2026 catalog rows | **9** |
| **Proposed 1:1 maps** | **9** codes (all mapped) |
| **Pending human decision** | **0** |

### Verdict

**Approved** — map legacy `TitleOfDepartmentForRegistration` → target `NameTm`. PATCH imported Applications via id-map.

---

## Legacy values used on Application (Çalik / VISA2015)

| Legacy code | Legacy `DepartmentForRegistrationL` (abbrev.) | App rows | Visa2026 `Code` | Visa2026 `NameTm` (abbrev.) | Match | Import note |
|-------------|-----------------------------------------------|----------|-----------------|----------------------------|-------|-------------|
| **TDMGMR** | Mary welaýaty müdirliginiň **müdirine** | 1,961 | TDMGMR | Mary welaýaty müdirine | ✓ exact | Map by code |
| **TDMGAH** | Ahal welaýaty müdirliginiň müdirine | 1,353 | TDMGAH | Ahal welaýaty müdirine | ✓ exact | Map by code |
| **TDMGAS** | Aşgabat şäher müdirliginiň **müdirine** | 472 | TDMGAS | Aşgabat şäher müdirine | ✓ exact | Map by code |
| **TDMGBN** | Balkan welaýaty müdirligine | 1,235 | TDMG | Balkan welaýaty müdirine | proposed | Legacy ends *müdirligine*; same office as target row |
| **TDMGSERH** | Mary … Serhetabat etrap bölümi | 42 | TDMG | Mary … Serhetabat şäher bölümi | proposed | Normalize label → target `NameTm` |
| **TDMGYL** | Mary … Ýolöten etrap bölümi | 23 | TDMG | Mary … Ýolöten etrap bölümi | proposed | Normalize label → target `NameTm` |
| **TDMGLB** | Lebap welaýaty müdirligine | 95 | TDMG | Lebap welaýaty müdirine | proposed | Regional office (not Kerki branch) |
| **TDMG** | Aşgabat şäher müdirliginiň **başlygyna** | 1,433 | TDMGAS | Aşgabat şäher müdirine | ✓ approved | Map başlygyna → **TDMGAS** (2026-06-30) |
| **TDMGLBA** | Lebap … **Kerki** şäher bölümi | 98 | TDMGKR | Lebap … Kerki şäher bölümi | ✓ approved | **New seed row** `TDMGKR` |

**Example (screenshot V-1039):** legacy `6/-1039` → **TDMGBN** → proposed target *Balkan welaýaty müdirine* (`Code=TDMG`).

---

## Visa2026 catalog (target)

| Code | NameTm | Used by Çalik legacy? |
|------|--------|----------------------|
| TDMGAS | Aşgabat şäher müdirine | ✓ TDMGAS + (proposed) TDMG başlygyna |
| TDMGAH | Ahal müdirine | ✓ |
| TDMGMR | Mary müdirine | ✓ |
| TDMG | Mary Serhetabat şäher bölümi | ✓ TDMGSERH |
| TDMG | Mary Ýolöten etrap bölümi | ✓ TDMGYL |
| TDMG | Balkan Gyzylarbat etrap bölümi | target_only (0 Çalik apps) |
| TDMG | Balkan Türkmenbaşy şäher bölümi | target_only (0 Çalik apps) |
| TDMG | Balkan welaýaty müdirine | ✓ TDMGBN |
| TDMG | Lebap welaýaty müdirine | ✓ TDMGLB (proposed) |
| TDMGKR | Lebap Kerki şäher bölümi | ✓ TDMGLBA (new 2026-06-30) |
| TDMG | Daşoguz welaýaty müdirligine | inference_only (DZ region — no Çalik legacy dept) |

---

## Approved decisions (2026-06-30)

1. **TDMG → başlygyna (1,433 apps):** map to **TDMGAS** (*müdirine*).
2. **TDMGLBA → Kerki (98 apps):** new catalog row `Code=TDMGKR`, Kerki şäher bölümi `NameTm`.

---

## Null FK on Application (5,397 rows)

Not all application types show **Migration service** (`ShowMigrationService=false` on visa / invitation / border-zone types). Null `DepartmentForRegistration` on those rows is **expected** — do not PATCH.

PATCH scope: only imported applications where legacy had a non-null `DepartmentForRegistration` FK (**6,840** rows).

---

## Address-based inference (Check_In null department)

**58** Çalik `App_Reg_Check_In` apps have null `DepartmentForRegistration` but require `MigrationService` on import. FK PATCH does not cover them.

| Artifact | Path |
|----------|------|
| Proposal + mapping table | [MigrationService-inference.md](./MigrationService-inference.md) |
| Machine rules | [migration-service-inference.yaml](../migration-service-inference.yaml) |
| Preview Excel | `Visa2026.DataImporter/legacy/visa2014/preview-export/ApplicationMigrationServiceInference-preview.calik-energi.xlsx` |

**Status (2026-06-30):** preview approved; second-pass PATCH via `--patch-visa2014-application-migration-service-inference` (51 patchable rows; 7 `none` skipped).

---

## Next steps

1. **DB sync** — restart Blazor or `--updateDatabase` once so Kerki row appears in `MigrationServices`.
2. **Phase 2:** Application transform preview (`MigrationService` column) → re-export Excel.
3. **Phase 3:** OData PATCH `--patch-visa2014-application-migration-service`.

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-30 | Daşoguz seed `tdmg-dz` for address inference (DZ region) |
| 2026-06-30 | Approved: TDMG→TDMGAS; TDMGLBA→Kerki seed (TDMGKR); 9/9 codes mapped |
| 2026-06-30 | Initial SQL inventory (9/9 legacy codes used; 2 gaps flagged) |
