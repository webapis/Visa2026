# Report Generation Guide

This document is the **primary reference for AI-assisted report creation, review, and refactoring** in this project. It maps every known government form to its report class, data type, and asset files. Update this document whenever a new report is created, a form is added, or a report status changes.

For XtraReports technical conventions (page size, fonts, borders, expression bindings, .resx sync), see [REPORTS.md](REPORTS.md).

---

## Completion Dashboard

> Update this section every time a report is implemented or a form image is added. Percentages are calculated from the totals defined in Section 4 (ApplicationType Master List).

### Overall Progress

| Area | Done | Total | % Complete |
|---|---|---|---|
| Report classes (all ApplicationTypes, all variants, all levels) | 2 | 48 | 4% |
| Form template images (`Resources/FormTemplates/`) | 0 | 30 | 0% |
| Reference documents (`Resources/existing_forms/`) | 11 | 30 | 37% |

> **Total count breakdown:** 30 ApplicationTypes × approx. 1.6 average (Application + ApplicationItem levels + variants) = ~48 report classes needed. Adjust the Total column as new variants are confirmed.

---

### Progress by Group

| Group | AppTypes | Reports Done | Reports Total | Images Done | Images Total |
|---|---|---|---|---|---|
| Invitation | 6 | 0 | 8 | 0 | 6 |
| Invitation + Work Permit | 2 | 0 | 3 | 0 | 2 |
| Visa | 5 | 0 | 7 | 0 | 5 |
| Visa FM | 1 | 0 | 3 | 0 | 3 |
| Visa + Work Permit | 2 | 0 | 3 | 0 | 2 |
| Work Permit | 3 | 0 | 4 | 0 | 3 |
| Registration | 8 | 1 | 8 | 0 | 8 |
| Border Zone | 2 | 0 | 3 | 0 | 2 |
| Cancellation | 1 | 1 | 1 | 0 | 1 |
| **Total** | **30** | **2** | **40+** | **0** | **32+** |

> Registration row counts `RegistrationListReport` as 1 done (generic list, not per-type).
> Cancellation row counts `ApplicationLetterReport` as a proxy until a dedicated cancel report exists.

---

### What Is Blocking Progress

| Blocker | Affects |
|---|---|
| Form template images not yet exported to `Resources/FormTemplates/` | All reports that overlay a scanned form |
| Reference documents missing for ~19 ApplicationTypes | Cannot confirm field layout before designing |
| `BusinessTrip.cs` missing flattened `[NotMapped]` properties | `BusinessTripReport` cannot be started |

---

## 1. How to Use This Document

When asked to **create** a report:
1. Find the ApplicationType in the [ApplicationType Master List](#4-applicationtype-master-list) — note its Name, category, data type, and reference document.
2. Find or create the report entry in the [Report Catalog](#3-report-catalog).
3. Check the [Asset Registry](#2-asset-registry) for the form image path.
4. Follow the [New Report Checklist](#6-how-to-add-a-new-report).
5. Update the catalog row status from `Planned` → `Implemented`.

When asked to **update** a report:
1. Find its catalog entry for context on what it does and what data it binds.
2. Check the form template image to understand the expected layout.

When asked to **add a new form**:
1. Add the reference document to `Resources/existing_forms/category/{both|employee|family_member}/`.
2. If a background image is needed for XtraReports, export it as `.jpg` to `Resources/FormTemplates/`.
3. Add entries in the [Asset Registry](#2-asset-registry) and [ApplicationType Master List](#5-applicationtype-master-list).

## Key Architectural Rule

**One report class per ApplicationType** (for `Application`-based reports). Each report is registered in `ReportsUpdater.cs` with a visibility criteria that restricts it to exactly the matching `ApplicationType.Name`. This ensures that when a user opens an Application of type `App_Inv`, only the invitation report appears — not reports for other application types.

```
ApplicationType.Name = 'App_Inv'  →  AppInvReport  →  visible only when [ApplicationType.Name] = 'App_Inv'
ApplicationType.Name = 'App_Inv_FM'  →  AppInvFMReport  →  visible only when [ApplicationType.Name] = 'App_Inv_FM'
```

### Report Class Naming Convention

Convert `ApplicationType.Name` to PascalCase and append `Report`:

| ApplicationType.Name | Report Class Name |
|---|---|
| `App_Inv` | `AppInvReport` |
| `App_Inv_FM` | `AppInvFMReport` |
| `App_Inv_And_WP` | `AppInvAndWPReport` |
| `App_Visa_Ext` | `AppVisaExtReport` |
| `App_Reg_Check_In` | `AppRegCheckInReport` |

### ReportsUpdater Template

For every new Application-based report, add **both** lines to `ReportsUpdater.cs`:

```csharp
// In constructor:
AddPredefinedReport<AppInvReport>("App Inv Report", typeof(Application), isInplaceReport: true);

// In UpdateDatabaseAfterUpdateSchema():
CreateReportVisibility(
    reportName: "App Inv Report",
    displayName: "Çakylyk Almak",           // use the Turkmen display name from ApplicationType
    targetType: typeof(Application),
    criteria: "[ApplicationType.Name] = 'App_Inv'"
);
```

For `Registration`-based reports:
```csharp
AddPredefinedReport<AppRegCheckInReport>("App Reg Check In Report", typeof(Registration), isInplaceReport: true);
CreateReportVisibility(
    reportName: "App Reg Check In Report",
    displayName: "Hasaba Almak",
    targetType: typeof(Registration),
    criteria: "[Application.ApplicationType.Name] = 'App_Reg_Check_In'"
);
```

For `ApplicationItem`-based reports:
```csharp
AddPredefinedReport<AppInvItemReport>("App Inv Item Report", typeof(ApplicationItem), isInplaceReport: true);
CreateReportVisibility(
    reportName: "App Inv Item Report",
    displayName: "Çakylyk — Şahsy",
    targetType: typeof(ApplicationItem),
    criteria: "[Application.ApplicationType.Name] = 'App_Inv'"
);
```

> Some ApplicationTypes may need **two** reports: one at `Application` level (the cover letter/summary) and one at `ApplicationItem` level (the per-person form). In that case, create two separate classes — e.g., `AppInvReport` and `AppInvItemReport`.

### Report Variants

Some ApplicationTypes have **up to 3 variants** of the same report — different form layouts for the same data (e.g. three versions of a visa extension form the user can choose between).

**Naming:** append `V0`, `V1`, `V2` to the class name:

| Variant | Class Name | Registered Name |
|---|---|---|
| Variant 0 (default) | `AppVisaExtFMV0Report` | `"App Visa Ext FM V0 Report"` |
| Variant 1 | `AppVisaExtFMV1Report` | `"App Visa Ext FM V1 Report"` |
| Variant 2 | `AppVisaExtFMV2Report` | `"App Visa Ext FM V2 Report"` |

**ReportsUpdater — all variants share the same criteria:**

```csharp
// Constructor — register all variants
AddPredefinedReport<AppVisaExtFMV0Report>("App Visa Ext FM V0 Report", typeof(Application), isInplaceReport: true);
AddPredefinedReport<AppVisaExtFMV1Report>("App Visa Ext FM V1 Report", typeof(Application), isInplaceReport: true);
AddPredefinedReport<AppVisaExtFMV2Report>("App Visa Ext FM V2 Report", typeof(Application), isInplaceReport: true);

// UpdateDatabaseAfterUpdateSchema — same criteria, different display names
CreateReportVisibility("App Visa Ext FM V0 Report", "Wiza Uzaltmak FM — Form 0", typeof(Application), "[ApplicationType.Name] = 'App_Visa_Ext_FM'");
CreateReportVisibility("App Visa Ext FM V1 Report", "Wiza Uzaltmak FM — Form 1", typeof(Application), "[ApplicationType.Name] = 'App_Visa_Ext_FM'");
CreateReportVisibility("App Visa Ext FM V2 Report", "Wiza Uzaltmak FM — Form 2", typeof(Application), "[ApplicationType.Name] = 'App_Visa_Ext_FM'");
```

The user sees all registered variants in the report list and picks which one to print.

> **Implementation order:** always implement `V0` (main variant) first. Add `V1`/`V2` only when explicitly requested. When the master list shows "2–3 (start with V0)", create and register only `V0Report` until further variants are needed.

**Asset files:** store variant images as `App_Visa_Ext_FM_v0.jpg`, `App_Visa_Ext_FM_v1.jpg`, `App_Visa_Ext_FM_v2.jpg` in `Resources/FormTemplates/`.

**In the ApplicationType Master List** (Section 4), the Variants column shows how many variants exist for each type.

---

## 2. Asset Registry

### 2a. Company Letterhead

| File | Path | Used By |
|---|---|---|
| `clkbackground.jpg` | `Resources/clkbackground.jpg` | `ApplicationLetterReport` |

Loaded at runtime in the report constructor. Fallback path: `[BaseDirectory]/Reports/clkbackground.jpg` → `[BaseDirectory]/clkbackground.jpg`.

---

### 2b. Form Templates (XtraReports Backgrounds)

Images placed here are used **as background overlays** in XtraReports — exported from the reference documents below.

| File | Path | Represents | Used By Report |
|---|---|---|---|
| *(none yet)* | `Resources/FormTemplates/` | — | — |

> When adding: export as `.jpg` at 150–200 DPI, name using the same base name as the source file (e.g. `App_Inv.jpg`). One file per page; use suffix `_p1`, `_p2` for multi-page forms.

---

### 2c. Reference Documents (`existing_forms/`)

These are the **original scanned/authored government forms**. They define the layout and required fields for each report. Not embedded in reports — used for design reference only.

#### Category: Both (Employee + Family Member)

| File | Description | Planned Report |
|---|---|---|
| `category/both/App_Cancel_App.docx` | Application cancellation request letter | `AppCancelReport` |
| `category/both/App_Cancel_Visa.pdf` | Visa cancellation request | `VisaCancelReport` |
| `category/both/App_Change_Inv.rtf` | Change of inviting party request | `ChangeInvitationReport` |
| `category/both/App_Change_Passport.pdf` | Passport change notification | `ChangePassportReport` |

#### Category: Employee

| File | Description | Planned Report |
|---|---|---|
| `category/employee/App_Inv.rtf` | Invitation letter for employee visa | `InvitationEmployeeReport` |
| `category/employee/App_Inv_And_WP.docx` | Invitation + work permit application | `InvAndWorkPermitReport` |
| `category/employee/App_Inv_And_WP.xls` | Invitation + work permit (spreadsheet annex) | *(annex to above)* |

#### Category: Family Member

| File | Description | Planned Report |
|---|---|---|
| `category/family_member/App_Inv_FM.pdf` | Invitation letter for family member | `InvitationFamilyMemberReport` |
| `category/family_member/App_Visa_Ext_FM_variant_00.pdf` | Visa extension FM — variant 0 | `VisaExtFamilyMemberReport` |
| `category/family_member/App_Visa_Ext_FM_variant_01.pdf` | Visa extension FM — variant 1 | `VisaExtFamilyMemberReport` (variant) |
| `category/family_member/App_Visa_Ext_FM_variant_02.pdf` | Visa extension FM — variant 2 | `VisaExtFamilyMemberReport` (variant) |
| `category/family_member/App_Visa_For_New_Born_FM.pdf` | Visa application for newborn | `VisaNewBornReport` |

#### Root Resources (not yet categorized)

| File | Description | Planned / Current Report |
|---|---|---|
| `Resources/App_Reg_Check_In.docx` | Registration check-in form | `RegistrationCheckInReport` (planned) |
| `Resources/Form_16.docx` | Form 16 — purpose TBD | TBD |
| `Resources/Greeting.docx` | Greeting / cover letter | TBD |
| `Resources/Registration.docx` | Registration document | Likely `RegistrationListReport` reference |
| `Resources/Reg_PersonInApplication.docx` | Person-in-application registration | TBD |
| `Resources/Rejection_Notice.docx` | Rejection notification letter | TBD |
| `Resources/Visa_Application_TM_QR_08.pdf` | Official Turkmenistan visa application form (XFA/QR) | PDF fill — **not XtraReports**, see `PdfMappingHelper` |
| `Resources/Visa_Grant_Letter.docx` | Visa grant / approval letter | `ApplicationLetterReport` (reference) |

> `Visa_Application_TM_QR_08.pdf` is an XFA interactive PDF — it is filled programmatically via `PdfMappingHelper`, not rendered as an XtraReport. Field reference: `Resources/Pdf field reference .md`.

---

## 3. Report Catalog

### Implemented

#### `RegistrationListReport`

| Property | Value |
|---|---|
| **Class** | `RegistrationListReport` |
| **Registered Name** | `Registration List Report` |
| **Data Type** | `Registration` |
| **Form Template** | None — tabular layout, no background image |
| **Reference Document** | `Resources/Registration.docx` |
| **Page** | A4 Landscape |
| **Purpose** | Lists all foreign nationals included in a registration application. One row per person. |
| **Status** | ✅ Implemented |

**Key fields used:**

| Column | Binding |
|---|---|
| № (row number) | `sumRecordNumber()` |
| Family Name | `[Person_FullName]` (last name part) |
| First Name | `[Person_FullName]` (first name part) — or split if available |
| Date of Birth | `[Person_DateOfBirthText]` |
| Gender | `[Person_GenderTm]` |
| Nationality | `[Person_NationalityTm]` |
| Passport No. | `[Passport_Number]` |
| Passport Expiry | `[Passport_ExpirationDateText]` |
| Purpose of Travel | `[Travel_PurposeOfTravelTm]` |
| Visa Info | `[Visa_Number]` + `[Visa_TypeTm]` + `[Visa_StartDateText]` + `[Visa_ExpirationDateText]` (multiline) |
| Address | `[Address_FullAddress]` |
| Signature (footer) | Position: `[CompanyHead_PositionTm]` / Name: `[CompanyHead_FullName]` |

---

#### `ApplicationLetterReport`

| Property | Value |
|---|---|
| **Class** | `ApplicationLetterReport` |
| **Registered Name** | *(check ReportsUpdater.cs)* |
| **Data Type** | `Application` |
| **Form Template** | `Resources/clkbackground.jpg` (company letterhead) |
| **Reference Document** | `Resources/Visa_Grant_Letter.docx` |
| **Page** | A4 Portrait (verify) |
| **Purpose** | Official application letter on company letterhead. Shows application metadata. |
| **Status** | ✅ Implemented |

**Key fields used:** `FullApplicationNumber`, `ApplicationDate`, `ApplicationType.Name`, `Company.Name`, `CompanyHead.FullName`.

**Background image loading:**
```csharp
// Loaded in constructor — tries Reports/ subfolder first, then BaseDirectory
string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "clkbackground.jpg");
if (!File.Exists(path)) path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clkbackground.jpg");
```

---

### Shell (Class Exists, Not Yet Designed)

#### `ApplicationItemReport`

| Property | Value |
|---|---|
| **Class** | `ApplicationItemReport` |
| **Data Type** | `ApplicationItem` |
| **Form Template** | TBD — see `existing_forms/category/employee/` or `family_member/` |
| **Purpose** | Per-person report within a visa/work permit application |
| **Status** | 🔧 Shell — first name / last name labels only |

---

#### `ApplicationReport`

| Property | Value |
|---|---|
| **Class** | `ApplicationReport` |
| **Data Type** | `Application` |
| **Form Template** | TBD |
| **Purpose** | Application-level summary or letter |
| **Status** | 🔧 Shell — no content |

---

#### `ApplicationVisaExtEmp`

| Property | Value |
|---|---|
| **Class** | `ApplicationVisaExtEmp` |
| **Data Type** | `Application` (visa extension / employment) |
| **Form Template** | TBD — likely `category/employee/App_Inv_And_WP.docx` or `category/family_member/App_Visa_Ext_FM_variant_*.pdf` |
| **Purpose** | Report for visa extension and employment-related applications |
| **Status** | 🔧 Shell — no content |

---

### Planned

| Report Class | Data Type | Reference Document | Category | Status |
|---|---|---|---|---|
| `InvitationEmployeeReport` | `Application` | `category/employee/App_Inv.rtf` | Employee | 📋 Planned |
| `InvAndWorkPermitReport` | `Application` + `ApplicationItem` | `category/employee/App_Inv_And_WP.docx` | Employee | 📋 Planned |
| `InvitationFamilyMemberReport` | `Application` | `category/family_member/App_Inv_FM.pdf` | Family Member | 📋 Planned |
| `VisaExtFamilyMemberReport` | `ApplicationItem` | `category/family_member/App_Visa_Ext_FM_variant_*.pdf` | Family Member | 📋 Planned |
| `VisaNewBornReport` | `ApplicationItem` | `category/family_member/App_Visa_For_New_Born_FM.pdf` | Family Member | 📋 Planned |
| `AppCancelReport` | `Application` | `category/both/App_Cancel_App.docx` | Both | 📋 Planned |
| `VisaCancelReport` | `Application` | `category/both/App_Cancel_Visa.pdf` | Both | 📋 Planned |
| `ChangeInvitationReport` | `Application` | `category/both/App_Change_Inv.rtf` | Both | 📋 Planned |
| `ChangePassportReport` | `ApplicationItem` | `category/both/App_Change_Passport.pdf` | Both | 📋 Planned |
| `RegistrationCheckInReport` | `Registration` | `Resources/App_Reg_Check_In.docx` | Registration | 📋 Planned |
| `BusinessTripReport` | `BusinessTrip` | TBD | BusinessTrip | 📋 Planned |

---

## 4. ApplicationType Master List

Complete list of all seeded `ApplicationType` records. Use this table to determine the report class name, data type, and reference document for each type. Source of truth: `LOOKUPS.md` + `lookup.xlsm`.

**Columns:**
- **Name** — `ApplicationType.Name` — used in visibility criteria
- **Display (Tm)** — Turkmen label shown to users
- **Filter Group** — `ApplicationTypeFilter` grouping
- **Category** — Employee / FamilyMember / Both
- **Report Data Type** — which BO the report binds to (see Section 5)
- **Report Class** — target class name (append `Item` suffix for per-person `ApplicationItem` variant; append `V0`/`V1`/`V2` for variants)
- **Variants** — number of report variants (1 = single report, 2–3 = multiple form layouts)
- **Reference Doc** — form file in `Resources/`
- **Status** — report implementation status

---

### Invitation Group (`ApplicationTypeFilter: Invitation`)

| Name | Display (Tm) | Category | Report Data Type | Report Class | Variants | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Inv` | Çakylyk Almak | Employee | Application + ApplicationItem | `AppInvReport` / `AppInvItemReport` | 1 | `category/employee/App_Inv.rtf` | 📋 Planned |
| `App_Inv_FM` | Çakylyk Almak FM | FamilyMember | Application + ApplicationItem | `AppInvFMReport` / `AppInvFMItemReport` | 1 | `category/family_member/App_Inv_FM.pdf` | 📋 Planned |
| `App_Sevice_Passport` | Gulluk Pasporty Üçin Çakylyk Almak | Employee | Application + ApplicationItem | `AppServicePassportReport` | 1 | TBD | 📋 Planned |
| `App_Inv_According_to_WP` | İş Rugsatnama görä Çakylyk Almak | Employee | Application + ApplicationItem | `AppInvAccordingToWPReport` | 1 | TBD | 📋 Planned |
| `App_Change_Inv` | Çakylygy üýtgetmek | Both | Application | `AppChangeInvReport` | 1 | `category/both/App_Change_Inv.rtf` | 📋 Planned |
| `App_Cancel_Inv` | Çakylygy Ýatyrmak | Both | Application | `AppCancelInvReport` | 1 | TBD | 📋 Planned |

---

### Invitation + Work Permit Group (`ApplicationTypeFilter: InvitationAndWorkPermit`)

| Name | Display (Tm) | Category | Report Data Type | Report Class | Variants | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Inv_And_WP` | Çakylyk we Iş Rugsatnamasyny Almak | Employee | Application + ApplicationItem | `AppInvAndWPReport` / `AppInvAndWPItemReport` | 1 | `category/employee/App_Inv_And_WP.docx` | 📋 Planned |
| `App_Cancel_Inv_WP` | Çakylyk we Iş Rugsatnamasyny Ýatyrmak | Employee | Application | `AppCancelInvWPReport` | 1 | TBD | 📋 Planned |

---

### Visa Group (`ApplicationTypeFilter: Visa`)

| Name | Display (Tm) | Category | Report Data Type | Report Class | Variants | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Visa_Ext` | Wiza Möhletini Uzaltmak | FamilyMember | Application + ApplicationItem | `AppVisaExtV0Report` (main), up to `AppVisaExtV2Report` | 2–3 (start with V0) | `category/family_member/App_Visa_Ext_FM_variant_*.pdf` | 📋 Planned |
| `App_Visa_Ext_According_to_WP` | Iş Rugsatnamasyna Görä Wizany Uzaltmak | Employee | Application + ApplicationItem | `AppVisaExtAccToWPReport` | 1 | TBD | 📋 Planned |
| `App_Change_Visa_Category` | Wiza Kategoriýasyny üýtgetmek | Both | Application + ApplicationItem | `AppChangeVisaCategoryReport` | 1 | TBD | 📋 Planned |
| `App_Change_Passport` | Wizany KP>Täze Pasporta Geçirmek | Both | Application + ApplicationItem | `AppChangePassportReport` | 1 | `category/both/App_Change_Passport.pdf` | 📋 Planned |
| `App_Cancel_Visa` | Wizany Ýatyrmak | Both | Application | `AppCancelVisaReport` | 1 | `category/both/App_Cancel_Visa.pdf` | 📋 Planned |

---

### Visa + Work Permit Group (`ApplicationTypeFilter: VisaAndWorkPermit`)

| Name | Display (Tm) | Category | Report Data Type | Report Class | Variants | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Visa_and_WP_Ext` | Wiza we Iş Rugsatnamasyny Uzaltmak | Employee | Application + ApplicationItem | `AppVisaAndWPExtReport` | 1 | TBD | 📋 Planned |
| `App_Cancel_Visa_and_WP` | Wiza we Iş Rugsatnamany Ýatyrmak | Employee | Application | `AppCancelVisaAndWPReport` | 1 | TBD | 📋 Planned |

---

### Work Permit Group (`ApplicationTypeFilter: WorkPermit`)

| Name | Display (Tm) | Category | Report Data Type | Report Class | Variants | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_WP_Ext` | Iş Rugsatnamasyny Uzaltmak | Employee | Application + ApplicationItem | `AppWPExtReport` / `AppWPExtItemReport` | 1 | TBD | 📋 Planned |
| `App_Cancell_WP` | Iş Rugsatnamany Ýatyrmak | Employee | Application | `AppCancelWPReport` | 1 | TBD | 📋 Planned |
| `App_Additional_WP_location` | Iş Rugsatnama goşmaça barjak ýeri | Employee | Application | `AppAdditionalWPLocationReport` | 1 | TBD | 📋 Planned |

---

### Visa (FM) Group (`ApplicationTypeFilter: Visa_FM`)

| Name | Display (Tm) | Category | Report Data Type | Report Class | Variants | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Visa_Ext_FM` | Wiza Möhletini Uzaltmak FM | FamilyMember | Application + ApplicationItem | `AppVisaExtFMV0Report`, `AppVisaExtFMV1Report`, `AppVisaExtFMV2Report` | **3** | `category/family_member/App_Visa_Ext_FM_variant_00/01/02.pdf` | 📋 Planned |

---

### Registration Group (`ApplicationTypeFilter: Registration`)

These ApplicationTypes use **`Registration`** as the report data type. The report is visible on the `Application` object but binds to its `Registrations` collection.

| Name | Display (Tm) | Category | Report Class | Variants | Reference Doc | Status |
|---|---|---|---|---|---|---|
| `App_Reg_Check_In` | Hasaba Almak (Daşary ýurtdan) | Both | `AppRegCheckInReport` | 1 | `Resources/App_Reg_Check_In.docx` | 📋 Planned |
| `App_Reg_Check_In_Internal` | Hasaba Almak (Welaýatdan) | Both | `AppRegCheckInInternalReport` | 1 | TBD | 📋 Planned |
| `App_Reg_Check_Out` | Hasapdan Çykarmak (Daşary ýurda) | Both | `AppRegCheckOutReport` | 1 | TBD | 📋 Planned |
| `App_Reg_Check_Out_Internal` | Hasapdan Çykarmak (Başga welaýata) | Both | `AppRegCheckOutInternalReport` | 1 | TBD | 📋 Planned |
| `App_Reg_ext` | Hasaba alyşy uzaltmak | Both | `AppRegExtReport` | 1 | TBD | 📋 Planned |
| `App_Reg_Info_Change_Passport` | Hasaba alyş — Pasport Çalyşmagy | Both | `AppRegInfoChangePassportReport` | 1 | TBD | 📋 Planned |
| `App_Reg_Info_Change_Visa` | Hasaba alyş — Visa Çalyşmagy | Both | `AppRegInfoChangeVisaReport` | 1 | TBD | 📋 Planned |
| `App_Reg_Info_Change_Address` | Hasaba alyş — Salgy Çalyşmagy | Both | `AppRegInfoChangeAddressReport` | 1 | TBD | 📋 Planned |

> `RegistrationListReport` (already implemented) is the generic personnel list. The above are planned per-type variants.

---

### Border Zone Group (`ApplicationTypeFilter: BorderZone`)

| Name | Display (Tm) | Category | Report Data Type | Report Class | Variants | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Border_Zone_Permission` | Serhet Ýaka Üçin Rugsatnama Almak | Employee | Application + ApplicationItem | `AppBorderZonePermissionReport` | 1 | TBD | 📋 Planned |
| `App_Cancel_BZ` | Serhet Ýaka Üçin Rugsatnamany Ýatyrmak | Employee | Application | `AppCancelBZReport` | 1 | TBD | 📋 Planned |

---

### Cancellation Group (`ApplicationTypeFilter: Cancellation`)

| Name | Display (Tm) | Category | Report Data Type | Report Class | Variants | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Cancel_App` | Ýüztutmany Ýatyrmak | Both | Application | `AppCancelAppReport` | 1 | `category/both/App_Cancel_App.docx` | 📋 Planned |

---

## 5. Data Types — Quick Reference

### When to use `Registration`

Use when the report is about a **movement/arrival/departure event** for one or more persons — check-in, check-out, border registration, visa entry. Data flows through `Registration.cs` flattened properties.

ApplicationType names that generate Registration rows: `App_Reg_Check_In`, `App_Reg_Check_Out`, `App_Reg_Check_In_Internal`, `App_Reg_Check_Out_Internal`, `Visa Entry`, `Border Registration`.

Available flattened regions: `Person_*`, `Passport_*`, `Visa_*`, `Travel_*`, `Address_*`, `Position_*`, `Application_*`, `CompanyHead_*`, `RowNumber`.

---

### When to use `ApplicationItem`

Use when the report is **per-person within an application** — visa application form, work permit, invitation per individual. Data flows through `ApplicationItem.cs` flattened properties.

Extra regions not in `Registration`: `PreviousPassport_*`, `Contract_*`, `WorkPermit_*`, `MedicalRecord_*`, `Education_*`, `Person_Photo`.

---

### When to use `Application`

Use when the report is **application-level** — cover letters, summaries, cancellation requests, anything that references the whole application rather than individual persons. Access fields directly on `Application` (no flattening needed, or add `[NotMapped]` props to `Application.cs` as needed).

Key fields: `FullApplicationNumber`, `ApplicationDate`, `ApplicationType.Name`, `Company.Name`, `CompanyHead.FullName`, `TotalPersonCount`, `TotalPersonCountText`.

Available lookup fields via navigation: `Representative.FullName`, `MigrationService_NameTm`, `Urgency`, `VisaCategory`, `VisaType`, `VisaPeriod`, `FromCityName`, `ToCityName`, `FromRegionName`, `ToRegionName`.

---

### When to use `BusinessTrip`

Use when the report is about a **business trip event** for an employee — travel authorization letters, trip plans, departure/return records.

`BusinessTrip` does **not yet have flattened `[NotMapped]` properties**. Before creating a BusinessTrip report, add them to `BusinessTrip.cs` following the same pattern used in `Registration.cs` and `ApplicationItem.cs`.

Fields available on the object:

| Property | Type | Notes |
|---|---|---|
| `Person.FullName` | string | Employee name |
| `Purpose` | string | Trip purpose (required) |
| `DestinationCountry.Name` / `.Code` / `.NameTm` | string | Destination country |
| `DestinationCity` | string | Destination city |
| `StartDate` | DateTime | Trip start (required) |
| `EndDate` | DateTime | Trip end (required) |
| `Status` | `BusinessTripStatus` | Planned / Ongoing / Completed / Cancelled |
| `Application.FullApplicationNumber` | string | Linked application number |
| `Address` (aggregated) | `BusinessTripAddress` | Address at destination |

Suggested flattened property names to add:

```csharp
[NotMapped] public string Person_FullName => Person?.FullName;
[NotMapped] public string DestinationCountry_NameTm => DestinationCountry?.NameTm;
[NotMapped] public string DestinationCountry_Code => DestinationCountry?.Code;
[NotMapped] public string StartDateText => $"{StartDate:dd.MM.yyyy}";
[NotMapped] public string EndDateText => $"{EndDate:dd.MM.yyyy}";
[NotMapped] public string Application_FullNumber => Application?.FullApplicationNumber;
[NotMapped] public string Application_DateText => $"{Application?.ApplicationDate:dd.MM.yyyy}";
[NotMapped] public string CompanyHead_FullName => Application?.CompanyHead?.FullName;
[NotMapped] public string CompanyHead_PositionTm => Application?.CompanyHead?.Position?.NameTm;
```

---

## 6. Background Image Pattern

Use a background image when the report must visually match a specific government form (e.g., overlay data fields on a scanned form).

1. Export the form page as `.jpg` (150–200 DPI) → save to `Resources/FormTemplates/{name}.jpg`.
2. Add to report `.resx` as an embedded resource named `BackgroundImage`.
3. In the report constructor:

```csharp
string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "FormTemplates", "your_form.jpg");
if (File.Exists(path))
{
    this.xrPictureBoxBackground.Image = Image.FromFile(path);
}
else
{
    System.Diagnostics.Debug.WriteLine($"[Report] Background image not found: {path}");
}
```

4. Set `XRPictureBox` to cover the full page (width = `PageWidthF - margins`, height = `PageHeightF - margins`), `SizeMode = Zoom` or `Stretch`, `BringToFront = false`.
5. Place data labels/tables **on top** of the picture box.

---

## 7. How to Add a New Report

- [ ] 1. Open the reference document from `Resources/existing_forms/` to understand the layout and required fields.
- [ ] 2. Identify the **Data Type**: `Registration`, `ApplicationItem`, `Application`, or `BusinessTrip`.
- [ ] 3. Confirm all required fields exist as flattened properties on the data type. If not, add them in the corresponding `.cs` file following the existing `[NotMapped]` pattern.
- [ ] 4. If the report needs a background image: export the form page to `Resources/FormTemplates/` and note the path.
- [ ] 5. Create the report class: `Reports/{ClassName}.cs` + `Reports/{ClassName}.Designer.cs` + `Reports/{ClassName}.resx`.
- [ ] 6. Register in `DatabaseUpdate/ReportsUpdater.cs` — `AddPredefinedReport<>` + `CreateReportVisibility`.
- [ ] 7. If adding `ExpressionBindings` in `Designer.cs`, **also update the `.resx` file** or the binding will not work at runtime (see REPORTS.md — .resx Sync Requirement).
- [ ] 8. Update the **Report Catalog** in this document: move the entry from Planned → Implemented and fill in all fields.
- [ ] 9. Update the **Existing Reports** table in `REPORTS.md`.

---

## 8. File Naming Conventions

| Type | Convention | Example |
|---|---|---|
| Report class | Remove underscores from `ApplicationType.Name`, PascalCase, + `Report` suffix | `AppInvReport`, `AppVisaExtFMReport` |
| Form template image | `snake_case` matching source file base name | `App_Inv.jpg` |
| Multi-page image | Base name + `_p{n}` | `App_Inv_p1.jpg`, `App_Inv_p2.jpg` |
| Reference document | Keep original government file name | `App_Inv.rtf` |

---

## 9. Visa Application PDF (Special Case)

`Visa_Application_TM_QR_08.pdf` is **not an XtraReport**. It is an interactive XFA PDF form filled programmatically using Spire.PDF via `PdfMappingHelper`.

- Field reference: `Resources/Pdf field reference .md`
- 75 fields across 2 pages; field keys follow XFA path notation (`topmostSubform[0].Page1[0]._01[0]`)
- Dates are `picture` type — pass as `dd.MM.yyyy` string
- Choice fields (gender, marital status, urgency) require **raw values**, not display labels
- Currently 20+ fields unmapped — see `⚠️` entries in the field reference

This PDF fill path is separate from the XtraReports pipeline. Do not create an XtraReport for this form.
