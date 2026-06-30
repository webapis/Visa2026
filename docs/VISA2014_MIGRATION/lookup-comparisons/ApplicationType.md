# ApplicationType — legacy composite → Visa2026 `ApplicationType.Name`

**Scope:** Application header import (`dbo.Application`).  
**Layer 3:** `lookup-translations.yaml` → `ApplicationType` catalog.  
**Discovery:** `docs/VISA2014_MIGRATION/discovery/Application.yaml` (2026-06-29).

## Composite key

Legacy stores type across several columns; importer derives:

```text
{category}:{subtypeId}:{invWp}:{wizaWp}:{changeInfo}
```

| Segment | Source |
|---------|--------|
| `category` | `ForEmployee` → `E`; `ForFamilyMember` → `F` |
| `subtypeId` | `ApplicationTypeForEmployee.TypeOfApplicationForEmployeeID` or family equivalent |
| `invWp` | `IsInvitationWithWorkPermit.InvitationAndWorkPermitRequired` (0/1) |
| `wizaWp` | `IsWizaWithWorkPermit.WizaAndWorkPermitRequired` (0/1) |
| `changeInfo` | `ChangeInformation` when subtype 5 (info change) |

Subtype IDs come from `TypeOfApplicationForEmployeeID` / `TypeOfApplicationForFamilyMemberID` in `VISA2015`. Some production IDs (**31**, **44**, **45**, **55**) are not in the stock `SubType` enum.  
31 composite keys mapped; **2 keys skipped at import** — approved 2026-06-29 (`unmappedPolicy: skip_row`; 105 headers, 204 ApplicationItem rows).

## Full mapping table (VISA2014 → Visa2026)

English labels from `ApplicationTypeLookupStrings.json`. Legacy = employee **(E)** / family **(F)** + **subtype ID** + optional flags (inv+WP, visa+WP, change info).

### Mapped (Çalik import plan)

| VISA2014 (legacy) | Visa2026 |
|-------------------|----------|
| Employee · Invitation (subtype 0) | `App_Inv` — Obtain invitation |
| Employee · Invitation + work permit (subtype 0, inv+WP) | `App_Inv_And_WP` — Obtain invitation and work permit |
| Family · Invitation (subtype 0) | `App_Inv_FM` — Obtain invitation (family member) |
| Employee · Change invitation (subtype 1) | `App_Change_Inv` — Change invitation |
| Employee · Registration upon arrival (subtype 2) | `App_Reg_Check_In` — Registration (arrival from abroad) |
| Family · Registration upon arrival (subtype 2) | `App_Reg_Check_In` — Registration (arrival from abroad) |
| Employee · Registration extension (subtype 3) | `App_Reg_ext` — Extend registration |
| Family · Registration extension (subtype 3) | `App_Reg_ext` — Extend registration |
| Employee · Register at new location (subtype 4) | `App_Reg_Check_In_Internal` — Registration (arrival from another region) |
| Family · Register at new location (subtype 4) | `App_Reg_Check_In_Internal` — Registration (arrival from another region) |
| Employee · Change registration info — address (subtype 5) | `App_Reg_Info_Change_Address` — Change registration data (address change) |
| Family · Change registration info — address (subtype 5) | `App_Reg_Info_Change_Address` — Change registration data (address change) |
| Employee · Change registration info — passport (subtype 5, change=passport) | `App_Reg_Info_Change_Passport` — Change registration data (passport replacement) |
| Employee · Change registration info — visa (subtype 5, change=visa) | `App_Reg_Info_Change_Visa` — Change registration data (visa replacement) |
| Employee · Strike-off / check-out (subtype 6) | `App_Reg_Check_Out` — Deregistration (departure abroad) |
| Family · Strike-off / check-out (subtype 6) | `App_Reg_Check_Out` — Deregistration (departure abroad) |
| Employee · Visa extension (subtype 7) | `App_Visa_Ext` — Extend visa validity |
| Employee · Visa + work permit extension (subtype 7, visa+WP) | `App_Visa_and_WP_Ext` — Extend visa and work permit |
| Family · Visa extension (subtype 7) | `App_Visa_Ext_FM` — Extend visa validity (family member) |
| Employee · Change visa category (subtype 8) | `App_Change_Visa_Category` — Change visa category |
| Family · Change visa category (subtype 8) | `App_Change_Visa_Category` — Change visa category |
| Employee · Transfer visa to new passport (subtype 9) | `App_Change_Passport` — Transfer visa to new passport |
| Employee · Service passport invitation (subtype 10) | `App_Sevice_Passport` — Obtain invitation (service passport) |
| Family · Service passport invitation (subtype 10) | `App_Sevice_Passport` — Obtain invitation (service passport) |
| Employee · Border zone permission (subtype 11) | `App_Border_Zone_Permission` — Obtain border zone permit |
| Employee · Cancel visa and work permit (subtype 12) | `App_Cancel_Visa_and_WP` — Cancel visa and work permit |
| Employee · Business trip departure (subtype 13) | `App_Business_Trip_Departure` — Business trip departure |
| Employee · Business trip arrival (subtype 14) | `App_Business_Trip_Arrival` — Business trip arrival |
| Employee · Additional work-permitted location (subtype 15) | `App_Additional_WP_location` — Additional work-permitted location |
| Employee · Invitation per work permit (subtype 20) | `App_Inv_According_to_WP` — Obtain invitation per work permit |
| Employee · Cancel work permit (subtype **31**, DB-only ID) | `App_Cancell_WP` — Cancel work permit |
| Employee · Cancel visa (subtype **45**, DB-only ID) | `App_Cancel_Visa` — Cancel visa |
| Family · Cancel visa (subtype **45**) | `App_Cancel_Visa` — Cancel visa |

### Skipped at import (approved `skip_row` 2026-06-29)

| VISA2014 (legacy) | Visa2026 | Count |
|-------------------|----------|-------|
| Employee · subtype **44** (custom base type 7) | — (no mapping) | 92 apps · 187 items |
| Employee · subtype **55** (visa family) | — (no mapping; candidate `App_Exit_Visa` unconfirmed) | 13 apps · 17 items |

Composite keys `E:44:na:na:na` and `E:55:na:na:na` remain in `audit.unmapped`; importer skips parent Application and child ApplicationItem rows.

### Visa2026 only — no Çalik legacy rows

| VISA2014 (legacy) | Visa2026 |
|-------------------|----------|
| — | `App_Reg_Check_Out_Internal` — Deregistration (departure to another region) |
| — | `App_WP_Ext` — Extend work permit |
| — | `App_Visa_Ext_According_to_WP` — Extend visa per work permit |
| — | `App_Exit_Visa` — Exit visa |
| — | `App_Visa_For_New_Born_FM` — Visa for newborn (family member) |
| — | `App_Cancel_BZ` — Cancel border zone permit |
| — | `App_Cancel_App` — Cancel application |
| — | `App_Cancel_Visa_and_WP_Ext` — Cancel visa and work permit extension application |
| — | `App_Cancel_Visa_Ext` — Cancel visa extension application |
| — | `App_Cancel_Inv` — Cancel invitation |
| — | `App_Cancel_Inv_WP` — Cancel invitation and work permit |

Composite keys for each mapped row: `lookup-translations.yaml` → `ApplicationType.values[]`.

## Skipped keys (reference — approved skip, no mapping)

### `E:55:na:na:na` — 13 rows

| Signal | Value |
|--------|-------|
| `TypeOfApplicationForEmployeeID` | 55 (not in `SubType` enum) |
| `TypeOfBaseApplication` | **2** (= `AppType.Visa`) |
| Employee-only | yes |
| Sample numbers | `4/-80`, `7/-271`, `12/-13277`, … |

**Hypothesis (unconfirmed):** post-enum visa variant — candidate **`App_Exit_Visa`** (Visa2026-only type, selection code 703).  
**Decision 2026-06-29:** skip at import; no `values[]` row added.

### `E:44:na:na:na` — 92 rows

| Signal | Value |
|--------|-------|
| `TypeOfApplicationForEmployeeID` | 44 (not in `SubType` enum) |
| `TypeOfBaseApplication` | **7** (exists in DB; not in stock `AppType` 0–6 enum) |
| Employee-only | yes |
| PersonInApplication lines | 187 (employees only; no family members on lines) |
| Sample numbers | `11/-13251`, `2/-11347`, … |

**Hypothesis:** custom / later-added application family (base type 7). No safe Visa2026 `ApplicationType.Name` without domain label.  
**Decision 2026-06-29:** skip at import; no `values[]` row added.

## If mapping is added later

1. Add `values[]` rows to `lookup-translations.yaml` under `ApplicationType`.
2. Set `audit.allMappedToTarget: true` and change `unmappedPolicy` if all/scope changes.
3. Re-run composite DISTINCT SQL from dossier / field-map.
4. Re-export Excel preview for affected Application / ApplicationItem rows.
