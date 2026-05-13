# Word reports — review status tracker

Tracks **Resminamalar** templates under **`Visa2026.Module/Resources/*.docx`** through refactor/redesign. Update this file whenever a batch moves—do not rely on chat memory alone.

## Status values

| Status | Meaning |
|--------|---------|
| **Pending** | Not yet run through the **Review** checklist in **`SKILL.md`**. |
| **In review** | Batch actively in progress (agent or human). |
| **Completed** | Checklist done: trace, family, scan parity, binding audit, preview preset, user sign-off (or explicit waive). |
| **Blocked** | Waiting on scan, map approval, BO field, or product decision—note in **Notes**. |

## How to update

1. **New template:** Add a row (template name, `*ReportDef`, tentative **Family**, **Pending**).
2. When starting a batch: set affected rows to **In review**.
3. When finishing: set to **Completed** (or **Blocked** with reason).
4. Set **Last updated** to **YYYY-MM-DD** and keep **Notes** short (PR link, preset name, exception).

## Tracker

| Template | Report definition(s) | Family | Status | Last updated | Notes |
|----------|------------------------|--------|--------|--------------|-------|
| `BusinessTrip_Sanawy.docx` | `BusinessTripSanawyReportDef` | T1 | Pending | — | |
| `BusinessTrip_Arrival_Letter.docx` | `BusinessTripArrivalLetterReportDef` | L3 | Pending | — | |
| `BusinessTrip_Departure_Letter.docx` | `BusinessTripDepartureLetterReportDef` | L3 | Pending | — | |
| `App_Sanawy_Letter.docx` | `AppSanawyLetterReportDefBase` (+ subclasses in `AppSanawyLetterReportDefs.cs`) | T1 | Pending | — | One template, many application types |
| `App_Reg_Check_In_Letter.docx` | `AppRegCheckInLetterReportDef` | L1 | Pending | — | |
| `App_Reg_Check_In_Internal_Letter.docx` | `AppRegCheckInInternalLetterReportDef` | L1 | Pending | — | |
| `App_Reg_Check_Out_Letter.docx` | `AppRegCheckOutLetterReportDef` | L1 | Pending | — | |
| `App_Reg_Check_Out_Internal_Letter.docx` | `AppRegCheckOutInternalLetterReportDef` | L1 | Pending | — | |
| `App_Reg_Ext_Letter.docx` | `AppRegExtLetterReportDef` | L1 | Pending | — | |
| `App_Reg_Info_Change_Address_Letter.docx` | `AppRegInfoChangeAddressLetterReportDef` | L1 | Pending | — | |
| `App_Reg_Info_Change_Passport_Letter.docx` | `AppRegInfoChangePassportLetterReportDef` | L1 | Pending | — | |
| `App_Inv_Letter.docx` | `AppInvLetterReportDef` | L2 | Pending | — | |
| `App_Inv_And_WP_Letter.docx` | `AppInvAndWPLetterReportDef` | L2 | **In review** | 2026-05-13 | `MakeAppInvAndWPLetterTemplate`: scan-aligned Goşundy block (2-pasport kopiýalary + `TotalPersonCount`-daşary maglumaty, two `<w:p>`); preview `inv-and-wp-letter` uses full GT-15 `ProjectContract_Description` sample. Letterhead/footer graphics not in OpenXml yet. |
| `App_Inv_FM_Letter.docx` | `AppInvFMLetterReportDef` | L2 | Pending | — | |
| `App_Cancel_Visa_Letter.docx` | `AppCancelVisaLetterReportDef` | L1 | Pending | — | |
| `App_Cancel_Visa_And_WP_Letter.docx` | `AppCancelVisaAndWPLetterReportDef` | L1 | Pending | — | |
| `App_Cancel_Inv_WP_Letter.docx` | `AppCancelInvWPLetterReportDef` | L1 | Pending | — | |
| `App_Change_Passport_Letter.docx` | `AppChangePassportLetterReportDef` | L1 | Pending | — | |
| `App_Exit_Visa_Letter.docx` | `AppExitVisaLetterReportDef` | L3 | Pending | — | |
| `App_Additional_WP_Location_Letter.docx` | `AppAdditionalWPLocationLetterReportDef` | L1 | Pending | — | |
| `App_Border_Zone_Permission_Letter.docx` | `AppBorderZonePermissionLetterReportDef` | L1 | Pending | — | |
| `App_Change_Inv_Letter.docx` | `AppChangeInvLetterReportDef` | L2 | Pending | — | |
| `App_Visa_And_WP_Ext_Letter.docx` | `AppVisaAndWPExtLetterReportDef` | L1 | Pending | — | |
| `App_Visa_Ext_FM_Letter.docx` | `AppVisaExtFMLetterReportDef` | L2 | Pending | — | |
| `App_Cancel_Inv_WP_Item.docx` | `AppCancelInvWPItemReportDef` | T2 | Pending | — | `AppItemSanawyReportDefs.cs` |
| `App_Cancel_Visa_And_WP_Item.docx` | `AppCancelVisaAndWPItemReportDef` | T2 | Pending | — | |
| `App_Change_Inv_Item.docx` | `AppChangeInvItemReportDef` | T2 | Pending | — | |
| `App_Border_Zone_Permission_Item.docx` | `AppBorderZonePermissionItemReportDef` | T2 | Pending | — | |
| `App_Inv_And_WP_Borcnama_Item.docx` | `AppInvAndWPBorcnamaItemReportDef` | F1 | **Completed** | 2026-05-12 | First Word report design signed off: `MakeBorcnamaTemplate`, FormTemplates scan parity, one A4 page, `PreviewWordReports` preset `borcnama`. |
| `App_Labor_Contract_Item.docx` | `AppLaborContractItemReportDef` | F2 | Pending | — | |

**Not in this table:** `App_Reg_Check_In.docx` (and any other non–Word-pipeline `.docx` in `Resources`)—mail-merge / legacy; track separately if still maintained.

## Summary counts

**Current counts:** Completed **1**, Pending **29** (30 Resminamalar templates in table). Re-count after edits.
