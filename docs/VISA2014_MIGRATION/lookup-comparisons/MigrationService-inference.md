# MigrationService inference — App_Reg_Check_In null department (Çalik / VISA2015)

**Status:** **preview** (2026-06-30) — human review before second-pass PATCH  
**Scope:** `App_Reg_Check_In` only (`E:2:na:na:na`, `F:2:na:na:na`) where `Application.DepartmentForRegistration` IS NULL  
**Legacy source:** `--legacy-source calik-energi`  
**Machine rules:** [`migration-service-inference.yaml`](../migration-service-inference.yaml)  
**Preview Excel:** `Visa2026.DataImporter/legacy/visa2014/preview-export/ApplicationMigrationServiceInference-preview.calik-energi.xlsx`  
**Primary lookup doc:** [MigrationService.md](./MigrationService.md) (FK-based mapping — approved)

---

## Problem

On Çalik VISA2015, **58** active `App_Reg_Check_In` applications have **null** `DepartmentForRegistration` while `ShowMigrationService` is true on the target type. The approved FK mapping in [MigrationService.md](./MigrationService.md) covers **6,840** apps with a non-null legacy department; these **58** need a separate inference path.

**Approved approach (2026-06-30):** infer `MigrationService` from the applicant's **current address of residence** at `Application.ApplicationDate`, mirroring `PersonCurrentItems.GetCurrentAddressOfResidence`. **Preview + docs only** until Excel is reviewed; **no live PATCH** yet.

---

## Data path

```
Application (null DepartmentForRegistration, subtype 2)
  → PersonInApplication (Employee or FamilyMember)
  → Person
  → AddressOfResidence (as-of ApplicationDate)
  → Region.mgCode / ŞeherEtrap.mgCode
  → proposed MigrationService NameTm
```

Prior SQL inventory (same backup):

| Metric | Count |
|--------|------:|
| Target applications | **58** |
| With person OID in PersonInApplication | **58** |
| With ≥1 AddressOfResidence | **53** |
| Inferable (high + medium + low) | **51** (preview: 7 high + 44 medium + 0 low) |
| Gaps (confidence `none`) | **7** |

---

## Region → regional MigrationService (medium confidence)

When no **city override** applies, map `Region.mgCode` to the regional müdirlik row in [`migration-service.json`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/migration-service.json).

| Region mgCode | Region NameTm (AddressOfResidence) | MigrationService `LocalizationKey` | Target `NameTm` (abbrev.) |
|---------------|-----------------------------------|------------------------------------|---------------------------|
| **AS** | Aşgabat şäheri | `tdmgas` | Aşgabat şäher müdirine |
| **AH** | Ahal welaýaty | `tdmgah` | Ahal welaýaty müdirine |
| **MR** | Mary welaýaty | `tdmgmr` | Mary welaýaty müdirine |
| **BN** | Balkan welaýaty | `tdmgbn` | Balkan welaýaty müdirine |
| **LB** | Lebap welaýaty | `tdmg-lb` | Lebap welaýaty müdirine |
| **DZ** | Daşoguz welaýaty | `tdmg-dz` | Daşoguz welaýaty müdirligine |

**Confidence:** `medium` when region resolves and city is absent or is a non-branch etrap/şäher within that welaýat. Downgrade to `low` when only an **expired** address is available (expired-only fallback in address picker).

---

## City overrides → branch MigrationService (high confidence)

Branch offices share `Code=TDMG` in the seed; resolve by full **`NameTm`** at PATCH time (same as FK mapping).

| City mgCode(s) | Branch | `LocalizationKey` | Notes |
|----------------|--------|-------------------|-------|
| **MR23** | Serhetabat şäheri | `tdmg-serh` | Mary welaýaty branch |
| **MR11** | Ýolöten etraby | `tdmg-yl` | Mary welaýaty branch |
| **BN15**, **BN5** | Türkmenbaşy etraby / şäheri | `tdmg-tb` | Balkan welaýaty branch |
| **BN9** | Gyzylarbat (etraby) | `tdmg-gyz` | Balkan welaýaty branch |
| **LB67**, **LB68** | Atamyrat şäheri / etraby | `tdmg-atyr` | Lebap welaýaty branch |
| *(city name contains **kerki**)* | Kerki | `tdmgkr` | When `mgCode` null; fold via `CatalogMatchHelper` |

---

## Confidence rules (preview column)

| Level | When |
|-------|------|
| **high** | City override matched (`cityMgCode` or `cityNameContains`) and address valid on `ApplicationDate` |
| **medium** | Regional office from `regionMgCode` only, or city override with expired-only address fallback |
| **low** | Regional mapping with expired-only address fallback |
| **none** | No person, no address, missing region, or unknown region |

---

## Known gaps

1. **Null region** on current address — cannot map (common on patent/hotel-only lines without Region FK).
2. **No address** — apps without `AddressOfResidence` for the linked person.
3. **Kerki without mgCode** — relies on city **name** substring; verify in preview before PATCH.
4. **Gyzylarbat** — only **BN9** in VISA2015 `ŞeherEtrap`; addresses citing Gyzylarbat in free text without FK stay regional Balkan or `none`.

---

## Out of scope

- Applications that already had `DepartmentForRegistration` FK (see [MigrationService.md](./MigrationService.md) PATCH).
- Other `ShowMigrationService` types with null FK (expected for visa/invitation types).
- `App_Reg_Check_In_Internal` and non–Check_In registration types.
- OData PATCH / re-import of `Application` — **deferred** until preview approved and `approvedForPatch: true` in YAML.

---

## Next steps

1. Review preview Excel (`Confidence`, `Reason`, `ProposedMigrationService`).
2. Lock any manual overrides for `none` / `low` rows.
3. Set `approvedForPatch: true` in `migration-service-inference.yaml`.
4. Implement second-pass `--patch-visa2014-application-migration-service-inference` (separate from FK PATCH).

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-30 | Initial proposal + preview export entity `ApplicationMigrationServiceInference` |
