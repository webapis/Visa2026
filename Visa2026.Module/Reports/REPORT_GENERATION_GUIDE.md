# Report Generation Guide

This document is the **primary reference for AI-assisted report creation, review, and refactoring** in this project. It maps every known government form to its report class, data type, and asset files. Update this document whenever a new report is created, a form is added, or a report status changes.

For XtraReports technical conventions (page size, fonts, borders, expression bindings, .resx sync), see [REPORTS.md](REPORTS.md).

---

## Completion Dashboard

> Update this section every time a report is implemented or a form image is added. Percentages are calculated from the totals defined in Section 4 (ApplicationType Master List).

### Overall Progress

| Area | Done | Total | % Complete |
|---|---|---|---|
| Report classes (all ApplicationTypes, all variants, all levels) | 3 | 48 | 6% |
| Form template images (`Resources/FormTemplates/`) | 1 | 30 | 3% |
| Reference documents (`Resources/existing_forms/`) | 11 | 30 | 37% |

> **Total count breakdown:** Each ApplicationType can produce App-level, Item-level, and/or Reg-level reports, each with up to 3 variants. Current estimate: ~48 report classes minimum, rising as variants are confirmed. Update the Total column whenever variants are locked in.

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

> `RegistrationListReport` counts as 1 done under Registration (generic list, not per-type variant).
> `ApplicationLetterReport` counts as 1 done under Cancellation as a temporary proxy.
> Variants column format in Section 4: `App:Item:Reg` — e.g. `3:3:—` means 3 App-level variants, 3 Item-level variants, no Reg-level.

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

Every report — regardless of data type — is **scoped to a specific `ApplicationType`**. Visibility in `ReportsUpdater.cs` always uses `ApplicationType.Name` as the criteria, navigating through the parent `Application` where needed.

- `Application` reports: `[ApplicationType.Name] = 'App_Inv'`
- `ApplicationItem` reports: `[Application.ApplicationType.Name] = 'App_Inv'`
- `Registration` reports: `[Application.ApplicationType.Name] = 'App_Reg_Check_In'`

This means when a user opens an Application of type `App_Inv`, they see **only** the invitation reports — at Application level, at ApplicationItem level, and any variants of each.

---

### Report Levels per ApplicationType

Each ApplicationType can produce up to **3 levels** of reports:

| Level | Data Type | Bound To | Purpose |
|---|---|---|---|
| **App** | `Application` | The whole application | Cover letter, summary, cancellation request |
| **Item** | `ApplicationItem` | Each person in the application | Per-person form (visa form, invitation per individual) |
| **Reg** | `Registration` | Each registration row | Check-in/out list, movement record |

Not all ApplicationTypes use all levels. The master list (Section 4) specifies which levels apply per type.

---

### Report Variants

Each level can have **up to 3 variants** (V0–V2) — different form layouts for the same data. Variants apply independently per level: an ApplicationType can have 1 App-level variant but 3 Item-level variants, or any combination.

**Naming pattern:** `{AppTypePascalCase}[Item|Reg][V0|V1|V2]Report`

| Example | Class Name |
|---|---|
| App_Inv — App level, single variant | `AppInvReport` |
| App_Inv — Item level, single variant | `AppInvItemReport` |
| App_Visa_Ext_FM — App level, variant 0 | `AppVisaExtFMV0Report` |
| App_Visa_Ext_FM — App level, variant 1 | `AppVisaExtFMV1Report` |
| App_Visa_Ext_FM — Item level, variant 0 | `AppVisaExtFMItemV0Report` |
| App_Reg_Check_In — Reg level, single variant | `AppRegCheckInRegReport` |

> When there is only one variant, omit `V0` — just use the base name. Only add the `V0`/`V1`/`V2` suffix when 2+ variants exist for that level.

> **Implementation order:** always implement the main variant (`V0` / no suffix) first. Add further variants only when explicitly requested.

---

### Shared vs Per-Type Reports

An `ApplicationItem` (or `Registration`) report can be **scoped to one ApplicationType** or **shared across multiple ApplicationTypes**. The same rule applies to any level.

#### Decision Rule

| Condition | Approach |
|---|---|
| Form layout and fields differ per ApplicationType | **Per-type** — separate class and image per type |
| Form layout and fields are identical across types | **Shared** — one class, one image, `In (...)` visibility criteria |

#### Per-Type (default)

One class per ApplicationType, distinct image, single-type visibility:

```csharp
// Separate classes — layouts differ
AppInvItemReport    → App_Inv_item.jpg    → "[Application.ApplicationType.Name] = 'App_Inv'"
AppInvFMItemReport  → App_Inv_FM_item.jpg → "[Application.ApplicationType.Name] = 'App_Inv_FM'"
```

#### Shared Across Multiple ApplicationTypes

One class, one image, `In (...)` criteria. Name the class after the **primary type** in the group, or a **shared concept name** if no single type dominates:

```csharp
// One class covers multiple types — layout is identical
AppInvItemReport → App_Inv_item.jpg
CreateReportVisibility("App Inv Item Report", "Çakylyk — Şahsy", typeof(ApplicationItem),
    "[Application.ApplicationType.Name] In ('App_Inv', 'App_Inv_According_to_WP', 'App_Sevice_Passport')");
```

If no type is the obvious primary, use a descriptive shared name:

```csharp
AppInvSharedItemReport → App_Inv_shared_item.jpg
CreateReportVisibility("App Inv Shared Item Report", "Çakylyk — Şahsy (Umumy)", typeof(ApplicationItem),
    "[Application.ApplicationType.Name] In ('App_Inv', 'App_Inv_According_to_WP', 'App_Sevice_Passport')");
```

> This rule applies to all levels — `Application`, `ApplicationItem`, and `Registration`.

---

### ReportsUpdater Template — Complete Example

Below is the full registration pattern for `App_Visa_Ext_FM` which has:
- App level: 3 variants
- Item level: 1 variant (main only, for now)

```csharp
// ── Constructor ────────────────────────────────────────────────
// App level — 3 variants
AddPredefinedReport<AppVisaExtFMV0Report>("App Visa Ext FM V0 Report", typeof(Application), isInplaceReport: true);
AddPredefinedReport<AppVisaExtFMV1Report>("App Visa Ext FM V1 Report", typeof(Application), isInplaceReport: true);
AddPredefinedReport<AppVisaExtFMV2Report>("App Visa Ext FM V2 Report", typeof(Application), isInplaceReport: true);
// Item level — main only
AddPredefinedReport<AppVisaExtFMItemReport>("App Visa Ext FM Item Report", typeof(ApplicationItem), isInplaceReport: true);

// ── UpdateDatabaseAfterUpdateSchema ────────────────────────────
// App level — same criteria, different display names
CreateReportVisibility("App Visa Ext FM V0 Report",   "Wiza Uzaltmak FM — Form 0", typeof(Application),     "[ApplicationType.Name] = 'App_Visa_Ext_FM'");
CreateReportVisibility("App Visa Ext FM V1 Report",   "Wiza Uzaltmak FM — Form 1", typeof(Application),     "[ApplicationType.Name] = 'App_Visa_Ext_FM'");
CreateReportVisibility("App Visa Ext FM V2 Report",   "Wiza Uzaltmak FM — Form 2", typeof(Application),     "[ApplicationType.Name] = 'App_Visa_Ext_FM'");
// Item level — scoped via parent Application
CreateReportVisibility("App Visa Ext FM Item Report", "Wiza Uzaltmak FM — Şahsy",  typeof(ApplicationItem), "[Application.ApplicationType.Name] = 'App_Visa_Ext_FM'");
```

**Simpler example — `App_Inv` (single variant at each level):**

```csharp
// Constructor
AddPredefinedReport<AppInvReport>    ("App Inv Report",      typeof(Application),     isInplaceReport: true);
AddPredefinedReport<AppInvItemReport>("App Inv Item Report", typeof(ApplicationItem), isInplaceReport: true);

// UpdateDatabaseAfterUpdateSchema
CreateReportVisibility("App Inv Report",      "Çakylyk Almak",         typeof(Application),     "[ApplicationType.Name] = 'App_Inv'");
CreateReportVisibility("App Inv Item Report", "Çakylyk Almak — Şahsy", typeof(ApplicationItem), "[Application.ApplicationType.Name] = 'App_Inv'");
```

**Registration-level example — `App_Reg_Check_In`:**

```csharp
// Constructor
AddPredefinedReport<AppRegCheckInRegReport>("App Reg Check In Reg Report", typeof(Registration), isInplaceReport: true);

// UpdateDatabaseAfterUpdateSchema
CreateReportVisibility("App Reg Check In Reg Report", "Hasaba Almak — Sanaw", typeof(Registration), "[Application.ApplicationType.Name] = 'App_Reg_Check_In'");
```

**Reference images:** store all variant reference images as `{AppTypeName}_{level}[_{vN}].jpg` in `Resources/FormTemplates/` for design guidance. These are never loaded into reports.

**In the ApplicationType Master List** (Section 4), the Variants column specifies variants per level as `App:n / Item:n / Reg:n`.

---

## 2. Asset Registry

### 2a. Company Letterhead

Background images are per-company, identified by `Company.Code` (a dedicated short ALL CAPS identifier on the `Company` business object, e.g. `CLK`, `GAP`).

| File | Company | Used By |
|---|---|---|
| `background.jpg` | Default fallback — used when no company-specific file exists | All generic Application-level reports |
| `background_CLK.jpg` | Çalik Enerji (`Code = "CLK"`) | All CLK-specific Application-level reports |
| `background_GAP.jpg` | Gap Inşaat (`Code = "GAP"`) | All GAP-specific Application-level reports |

All placed in `Resources/FormTemplates/`.

**Loading logic in `AppBaseReport`:**
- Default constructor loads `background.jpg` automatically
- Company-specific derived reports call `LoadBackground("CLK")` or `LoadBackground("GAP")` after `InitializeComponent()`
- If `background_{Code}.jpg` is not found, falls back to `background.jpg`

Search order: `Resources/FormTemplates/` → `Reports/FormTemplates/` → `FormTemplates/` → `BaseDirectory`.

---

### 2b. Form Reference Images

Images placed here are **design references only** — they show what the target report should look like (layout, field positions, static labels, table structure). They are **never embedded in reports as backgrounds**. The only report backgrounds are `background.jpg` and `background_{Company.Code}.jpg` (company letterheads), loaded by `AppBaseReport` and inherited by all Application-level reports. `ApplicationItem` and `Registration` reports have plain white backgrounds.

When creating a report, read the reference image to understand the layout, then replicate it in `Designer.cs` using `XRLabel`, `XRTable`, etc.

The filename alone must identify the ApplicationType, Company (if layout differs per company), ProjectContract (if layout differs per contract), data level, variant, and page — so that the correct reference image can be located without any external lookup.

#### File Naming Convention

```
{ApplicationType.Name}[_{ProjectContract.Code}]_{level}[_v{n}][_p{n}].jpg
```

| Segment | Values | Meaning |
|---|---|---|
| `{ApplicationType.Name}` | e.g. `App_Inv`, `App_Visa_Ext_FM` | Exact `ApplicationType.Name` from the lookup — preserves underscores |
| `[_{Company.Code}]` | e.g. `_CLK`, `_GAP` | ALL CAPS `Company.Code` — **omit entirely** when form layout is identical across companies |
| `[_{ProjectContract.Code}]` | e.g. `_TAPI` | ALL CAPS project contract code — **omit entirely** when form layout is identical across contracts |
| `_{level}` | `_app`, `_item`, `_reg` | Data type: Application / ApplicationItem / Registration |
| `[_v{n}]` | `_v0`, `_v1`, `_v2` | Variant number — **omit entirely** when only one variant exists for this level |
| `[_p{n}]` | `_p1`, `_p2` | Page number — **omit entirely** for single-page forms |

> **Distinguishing segments:** `ApplicationType.Name` uses mixed case (`App_Inv`), `ProjectContract.Code` is ALL CAPS (`CLK`, `TAPI`), `level` is always lowercase (`app`, `item`, `reg`). This makes each segment unambiguous from the filename alone.

#### Examples

| Filename | ApplicationType | ProjectContract | Level | Variant | Page |
|---|---|---|---|---|---|
| `App_Inv_app.jpg` | `App_Inv` | *(generic)* | Application | single | 1 |
| `App_Inv_CLK_app.jpg` | `App_Inv` | `CLK` | Application | single | 1 |
| `App_Inv_TAPI_app.jpg` | `App_Inv` | `TAPI` | Application | single | 1 |
| `App_Inv_CLK_item.jpg` | `App_Inv` | `CLK` | ApplicationItem | single | 1 |
| `App_Inv_item.jpg` | `App_Inv` | *(generic)* | ApplicationItem | single | 1 |
| `App_Inv_And_WP_app_p1.jpg` | `App_Inv_And_WP` | *(generic)* | Application | single | 1 |
| `App_Inv_And_WP_app_p2.jpg` | `App_Inv_And_WP` | *(generic)* | Application | single | 2 |
| `App_Visa_Ext_FM_app_v0.jpg` | `App_Visa_Ext_FM` | *(generic)* | Application | V0 | 1 |
| `App_Visa_Ext_FM_app_v1.jpg` | `App_Visa_Ext_FM` | *(generic)* | Application | V1 | 1 |
| `App_Visa_Ext_FM_CLK_app_v0.jpg` | `App_Visa_Ext_FM` | `CLK` | Application | V0 | 1 |
| `App_Visa_Ext_FM_CLK_app_v1.jpg` | `App_Visa_Ext_FM` | `CLK` | Application | V1 | 1 |
| `App_Visa_Ext_FM_CLK_item_v0.jpg` | `App_Visa_Ext_FM` | `CLK` | ApplicationItem | V0 | 1 |
| `App_Visa_Ext_FM_CLK_app_v0_p1.jpg` | `App_Visa_Ext_FM` | `CLK` | Application | V0 | 1 |
| `App_Visa_Ext_FM_CLK_app_v0_p2.jpg` | `App_Visa_Ext_FM` | `CLK` | Application | V0 | 2 |
| `App_Reg_Check_In_reg.jpg` | `App_Reg_Check_In` | *(generic)* | Registration | single | 1 |

#### Expected Files (to be provided)

| File | Status |
|---|---|
| `App_Inv_app.jpg` | ⏳ Awaiting scan |
| `App_Inv_item.jpg` | ⏳ Awaiting scan |
| `App_Inv_FM_app.jpg` | ⏳ Awaiting scan |
| `App_Inv_FM_item.jpg` | ⏳ Awaiting scan |
| `App_Inv_And_WP_app.jpg` | ⏳ Awaiting scan |
| `App_Inv_And_WP_item.jpg` | ⏳ Awaiting scan |
| `App_Visa_Ext_FM_app_v0.jpg` | ⏳ Awaiting scan |
| `App_Visa_Ext_FM_app_v1.jpg` | ⏳ Awaiting scan |
| `App_Visa_Ext_FM_app_v2.jpg` | ⏳ Awaiting scan |
| `App_Visa_Ext_FM_item_v0.jpg` | ⏳ Awaiting scan |
| `App_Visa_Ext_FM_item_v1.jpg` | ⏳ Awaiting scan |
| `App_Visa_Ext_FM_item_v2.jpg` | ⏳ Awaiting scan |
| `App_Visa_Ext_app_v0.jpg` | ⏳ Awaiting scan |
| `App_Visa_Ext_item_v0.jpg` | ⏳ Awaiting scan |
| `App_Change_Passport_app.jpg` | ⏳ Awaiting scan |
| `App_Change_Passport_item.jpg` | ⏳ Awaiting scan |
| `App_Cancel_Visa_app.jpg` | ⏳ Awaiting scan |
| `App_Change_Inv_app.jpg` | ⏳ Awaiting scan |
| `App_Cancel_App_app.jpg` | ⏳ Awaiting scan |
| `App_Reg_Check_In_app.jpg` | ✅ |
| `App_Reg_Check_In_reg.jpg` | ⏳ Awaiting scan |

> Update status to ✅ when the file is placed in `Resources/FormTemplates/`. Add new rows as more ApplicationTypes receive scanned images. When a file is provided, also update the **Completion Dashboard** Images Done count.

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

| Name | Display (Tm) | Category | Levels | Report Classes | Variants (App:Item:Reg) | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Inv` | Çakylyk Almak | Employee | App + Item | `AppInvReport` / `AppInvItemReport` | 1:1:— | `category/employee/App_Inv.rtf` | 📋 Planned |
| `App_Inv_FM` | Çakylyk Almak FM | FamilyMember | App + Item | `AppInvFMReport` / `AppInvFMItemReport` | 1:1:— | `category/family_member/App_Inv_FM.pdf` | 📋 Planned |
| `App_Sevice_Passport` | Gulluk Pasporty Üçin Çakylyk Almak | Employee | App + Item | `AppServicePassportReport` / `AppServicePassportItemReport` | 1:1:— | TBD | 📋 Planned |
| `App_Inv_According_to_WP` | İş Rugsatnama görä Çakylyk Almak | Employee | App + Item | `AppInvAccordingToWPReport` / `AppInvAccordingToWPItemReport` | 1:1:— | TBD | 📋 Planned |
| `App_Change_Inv` | Çakylygy üýtgetmek | Both | App | `AppChangeInvReport` | 1:—:— | `category/both/App_Change_Inv.rtf` | 📋 Planned |
| `App_Cancel_Inv` | Çakylygy Ýatyrmak | Both | App | `AppCancelInvReport` | 1:—:— | TBD | 📋 Planned |

---

### Invitation + Work Permit Group (`ApplicationTypeFilter: InvitationAndWorkPermit`)

| Name | Display (Tm) | Category | Levels | Report Classes | Variants (App:Item:Reg) | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Inv_And_WP` | Çakylyk we Iş Rugsatnamasyny Almak | Employee | App + Item | `AppInvAndWPReport` / `AppInvAndWPItemReport` | 1:1:— | `category/employee/App_Inv_And_WP.docx` | 📋 Planned |
| `App_Cancel_Inv_WP` | Çakylyk we Iş Rugsatnamasyny Ýatyrmak | Employee | App | `AppCancelInvWPReport` | 1:—:— | TBD | 📋 Planned |

---

### Visa Group (`ApplicationTypeFilter: Visa`)

| Name | Display (Tm) | Category | Levels | Report Classes | Variants (App:Item:Reg) | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Visa_Ext` | Wiza Möhletini Uzaltmak | FamilyMember | App + Item | `AppVisaExtV0Report`…`AppVisaExtV2Report` / `AppVisaExtItemV0Report`…`AppVisaExtItemV2Report` | 2–3:2–3:— (start V0) | `category/family_member/App_Visa_Ext_FM_variant_*.pdf` | 📋 Planned |
| `App_Visa_Ext_According_to_WP` | Iş Rugsatnamasyna Görä Wizany Uzaltmak | Employee | App + Item | `AppVisaExtAccToWPReport` / `AppVisaExtAccToWPItemReport` | 1:1:— | TBD | 📋 Planned |
| `App_Change_Visa_Category` | Wiza Kategoriýasyny üýtgetmek | Both | App + Item | `AppChangeVisaCategoryReport` / `AppChangeVisaCategoryItemReport` | 1:1:— | TBD | 📋 Planned |
| `App_Change_Passport` | Wizany KP>Täze Pasporta Geçirmek | Both | App + Item | `AppChangePassportReport` / `AppChangePassportItemReport` | 1:1:— | `category/both/App_Change_Passport.pdf` | 📋 Planned |
| `App_Cancel_Visa` | Wizany Ýatyrmak | Both | App | `AppCancelVisaReport` | 1:—:— | `category/both/App_Cancel_Visa.pdf` | 📋 Planned |

---

### Visa + Work Permit Group (`ApplicationTypeFilter: VisaAndWorkPermit`)

| Name | Display (Tm) | Category | Levels | Report Classes | Variants (App:Item:Reg) | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Visa_and_WP_Ext` | Wiza we Iş Rugsatnamasyny Uzaltmak | Employee | App + Item | `AppVisaAndWPExtReport` / `AppVisaAndWPExtItemReport` | 1:1:— | TBD | 📋 Planned |
| `App_Cancel_Visa_and_WP` | Wiza we Iş Rugsatnamany Ýatyrmak | Employee | App | `AppCancelVisaAndWPReport` | 1:—:— | TBD | 📋 Planned |

---

### Work Permit Group (`ApplicationTypeFilter: WorkPermit`)

| Name | Display (Tm) | Category | Levels | Report Classes | Variants (App:Item:Reg) | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_WP_Ext` | Iş Rugsatnamasyny Uzaltmak | Employee | App + Item | `AppWPExtReport` / `AppWPExtItemReport` | 1:1:— | TBD | 📋 Planned |
| `App_Cancell_WP` | Iş Rugsatnamany Ýatyrmak | Employee | App | `AppCancelWPReport` | 1:—:— | TBD | 📋 Planned |
| `App_Additional_WP_location` | Iş Rugsatnama goşmaça barjak ýeri | Employee | App | `AppAdditionalWPLocationReport` | 1:—:— | TBD | 📋 Planned |

---

### Visa (FM) Group (`ApplicationTypeFilter: Visa_FM`)

| Name | Display (Tm) | Category | Levels | Report Classes | Variants (App:Item:Reg) | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Visa_Ext_FM` | Wiza Möhletini Uzaltmak FM | FamilyMember | App + Item | `AppVisaExtFMV0Report`, `V1`, `V2` / `AppVisaExtFMItemV0Report`, `V1`, `V2` | **3:3:—** | `category/family_member/App_Visa_Ext_FM_variant_00/01/02.pdf` | 📋 Planned |

---

### Registration Group (`ApplicationTypeFilter: Registration`)

Registration-type ApplicationTypes bind to the `Registration` data type (the people list). Visibility criteria navigates: `[Application.ApplicationType.Name] = '...'`.

| Name | Display (Tm) | Category | Levels | Report Classes | Variants (App:Item:Reg) | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Reg_Check_In` | Hasaba Almak (Daşary ýurtdan) | Both | App + Reg | `AppRegCheckInReport` / `AppRegCheckInRegReport` | 1:—:1 | `Resources/App_Reg_Check_In.docx` | ✅ App Done / 📋 Reg Planned |
| `App_Reg_Check_In_Internal` | Hasaba Almak (Welaýatdan) | Both | Reg | `AppRegCheckInInternalRegReport` | —:—:1 | TBD | 📋 Planned |
| `App_Reg_Check_Out` | Hasapdan Çykarmak (Daşary ýurda) | Both | Reg | `AppRegCheckOutRegReport` | —:—:1 | TBD | 📋 Planned |
| `App_Reg_Check_Out_Internal` | Hasapdan Çykarmak (Başga welaýata) | Both | Reg | `AppRegCheckOutInternalRegReport` | —:—:1 | TBD | 📋 Planned |
| `App_Reg_ext` | Hasaba alyşy uzaltmak | Both | Reg | `AppRegExtRegReport` | —:—:1 | TBD | 📋 Planned |
| `App_Reg_Info_Change_Passport` | Hasaba alyş — Pasport Çalyşmagy | Both | Reg | `AppRegInfoChangePassportRegReport` | —:—:1 | TBD | 📋 Planned |
| `App_Reg_Info_Change_Visa` | Hasaba alyş — Visa Çalyşmagy | Both | Reg | `AppRegInfoChangeVisaRegReport` | —:—:1 | TBD | 📋 Planned |
| `App_Reg_Info_Change_Address` | Hasaba alyş — Salgy Çalyşmagy | Both | Reg | `AppRegInfoChangeAddressRegReport` | —:—:1 | TBD | 📋 Planned |

> `RegistrationListReport` (already implemented) is the generic personnel list. The above are planned per-type variants.

---

### Border Zone Group (`ApplicationTypeFilter: BorderZone`)

| Name | Display (Tm) | Category | Levels | Report Classes | Variants (App:Item:Reg) | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Border_Zone_Permission` | Serhet Ýaka Üçin Rugsatnama Almak | Employee | App + Item | `AppBorderZonePermissionReport` / `AppBorderZonePermissionItemReport` | 1:1:— | TBD | 📋 Planned |
| `App_Cancel_BZ` | Serhet Ýaka Üçin Rugsatnamany Ýatyrmak | Employee | App | `AppCancelBZReport` | 1:—:— | TBD | 📋 Planned |

---

### Cancellation Group (`ApplicationTypeFilter: Cancellation`)

| Name | Display (Tm) | Category | Levels | Report Classes | Variants (App:Item:Reg) | Reference Doc | Status |
|---|---|---|---|---|---|---|---|
| `App_Cancel_App` | Ýüztutmany Ýatyrmak | Both | App | `AppCancelAppReport` | 1:—:— | `category/both/App_Cancel_App.docx` | 📋 Planned |

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

## 6. Background Image Rule

Background images are per-company letterheads. `AppBaseReport` loads the correct one at runtime based on `Company.Code`. No other report level uses a background image.

| Report Level | Background |
|---|---|
| `Application` (inherits `AppBaseReport`) | `background_{Company.Code}.jpg` — falls back to `background.jpg` if not found |
| `ApplicationItem` (inherits `AppItemBaseReport`) | None — plain white |
| `Registration` (inherits `AppRegBaseReport`) | None — plain white |

**Common case — same layout, background differs per company:**
One generic report class. `AppBaseReport` automatically reads `Company.Code` from the bound data at runtime via `OnDataSourceFilled` and loads `background_{code}.jpg`. No per-company subclass needed.

```csharp
// No override needed — AppBaseReport handles it automatically
public class AppRegCheckInReport : AppBaseReport
{
    public AppRegCheckInReport() { InitializeComponent(); }
}
// At runtime: Company.Code = "CLK" → loads background_CLK.jpg
// At runtime: Company.Code = "GAP" → loads background_GAP.jpg
// Falls back to background.jpg if no match found
```

**Rare case — layout itself differs per company:**
Separate report class per company. Derived constructor calls `LoadBackground("CLK")` explicitly to override at construction time instead of waiting for data.

```csharp
public class AppInvClkReport : AppBaseReport
{
    public AppInvClkReport()
    {
        InitializeComponent();
        LoadBackground("CLK"); // layout differs — hardcoded
    }
}
```

**Decision rule:**
> Only the background differs → one generic report, dynamic loading (automatic).
> Layout itself differs → separate report class per company, `LoadBackground(code)` in constructor.

Do **not** load form reference images as report backgrounds. Reference images are for design guidance only (see Section 2b).

---

## 6b. Reading Form Reference Images

When designing a report from a reference image, apply these rules to every element visible in the image:

| What you see | What to do |
|---|---|
| A blank line, box, or underline where data is expected | Place an `XRLabel` with an `ExpressionBinding` bound to the matching flattened property |
| Printed label text (e.g. "Familiýasy:", "Senesi:") | Replicate as a static `XRLabel` with `.Text` set — OR treat as part of the visual layout and use a combined label |
| A table with rows/columns | Use `XRTable → XRTableRow → XRTableCell` |
| A logo, stamp area, decorative border | Skip — do not place any control |
| A signature line at the bottom | Already handled by `ReportFooter` in the base class — do not duplicate |
| Page number / "sahypa N" | Use `XRPageInfo` control |

### Annotation Helper (optional but recommended)

To make field identification unambiguous, provide an annotated version of the reference image alongside the clean scan. Use any image editor to mark it up:

| Annotation | Meaning |
|---|---|
| 🟡 Yellow highlight + property name written on it | Bound data field — use the written property name in `ExpressionBinding` |
| 🔴 Red cross | Ignore — stamp, logo, or decoration |
| No marking | Pre-printed static label already visible in the image — replicate as static `XRLabel.Text` |

Name the annotated file with a `_map` suffix: e.g. `App_Inv_item_map.jpg`. Store it alongside the reference image in `Resources/FormTemplates/`. It is never embedded in any report.

---

## 7. Base Report Inheritance Hierarchy

**Main reports** (no variant suffix, or `V0`) inherit from the appropriate base class.
**Variant reports** (`V1`, `V2`, ...) inherit directly from `XtraReport` — they define their own bands, data source, and layout from scratch.

| Report Type | Inherits From | Example |
|---|---|---|
| Main / V0 — Application level | `AppBaseReport` | `AppInvReport`, `AppVisaExtFMV0Report` |
| Main / V0 — ApplicationItem level | `AppItemBaseReport` | `AppInvItemReport`, `AppVisaExtFMItemV0Report` |
| Main / V0 — Registration level | `AppRegBaseReport` | `AppRegCheckInRegReport` |
| Variant V1, V2 — any level | `XtraReport` (directly) | `AppVisaExtFMV1Report`, `AppVisaExtFMItemV2Report` |

| Base Class | Inherits From | Data Type | Page | Background | Use For |
|---|---|---|---|---|---|
| `AppBaseReport` | `XtraReport` | `Application` | A4 Portrait | `clkbackground.jpg` (letterhead) | Main app-level reports |
| `AppItemBaseReport` | `XtraReport` | `ApplicationItem` | A4 Portrait | None (white) | Main per-person reports |
| `AppRegBaseReport` | `XtraReport` | `Registration` | A4 Landscape | None (white) | Main registration reports |

### What the Base Provides

Every base class provides:
- **TopMargin** (50F), **PageHeader**, **Detail** (10F), **ReportFooter** (50F), **BottomMargin** (60F) — all declared `protected` so derived Designer.cs can access them
- **PageHeader labels:** `xrLabelAppNumber` and `xrLabelAppDate`, right-aligned
- **ReportFooter labels:** `xrLabelSignatoryPosition` (left) and `xrLabelSignatoryFullName` (right), bold
- **CollectionDataSource** wired to the correct business object type
- A4 paper size and margins

`AppBaseReport` additionally provides:
- `xrPictureBoxBackground` (`protected`) in PageHeader with `clkbackground.jpg` pre-loaded
- `xrLabelCompanyName` bound to `[Company.Name]`
- `LoadBackground(string fileName)` method — call from derived constructors to swap the letterhead

### Header Field Bindings by Base

| Base Class | App Number Binding | Date Binding |
|---|---|---|
| `AppBaseReport` | `[FullApplicationNumber]` | `[ApplicationDate]` (TextFormatString `{0:dd.MM.yyyy}`) |
| `AppItemBaseReport` | `[Application_FullNumber]` | `[Application_DateText]` |
| `AppRegBaseReport` | `[Application_FullNumber]` | `[Application_DateText]` |

### Derived Report Pattern

**`.cs` file** — constructor calls `InitializeComponent()`, optionally overrides background:

```csharp
public class AppInvReport : AppBaseReport        // Application level — keeps letterhead
{
    public AppInvReport() { InitializeComponent(); }
}

public class AppInvItemReport : AppItemBaseReport // ApplicationItem level — plain white
{
    public AppInvItemReport() { InitializeComponent(); }
}

public class AppRegCheckInRegReport : AppRegBaseReport // Registration level — A4 Landscape
{
    public AppRegCheckInRegReport() { InitializeComponent(); }
}
```

**`Designer.cs` file** — use the inherited `Detail` band to add content:

```csharp
private void InitializeComponent()
{
    // declare only new controls — do NOT re-declare base bands
    this.xrTable1 = new DevExpress.XtraReports.UI.XRTable();
    // ...setup xrTable1...

    // resize Detail to fit content
    this.Detail.HeightF = 400F;
    // add controls into the inherited band
    this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
        this.xrTable1
    });
}
private DevExpress.XtraReports.UI.XRTable xrTable1;
```

> Never call `base.InitializeComponent()` — XtraReports inheritance does not chain `InitializeComponent()`. The base bands already exist because the base constructor ran first.

---

## 8. How to Add a New Report

- [ ] 1. Open the reference document from `Resources/existing_forms/` to understand the layout and required fields.
- [ ] 2. Identify the **Data Type**: `Registration`, `ApplicationItem`, `Application`, or `BusinessTrip`.
- [ ] 3. Confirm all required fields exist as flattened properties on the data type. If not, add them in the corresponding `.cs` file following the existing `[NotMapped]` pattern.
- [ ] 4. If the report needs a background image: place the scanned `.jpg` in `Resources/FormTemplates/` named using the convention `{ApplicationType.Name}_{level}[_v{n}][_p{n}].jpg` (see Section 9). Update the Expected Files table in Section 2b to ✅.
- [ ] 5. Create the report class: `Reports/{ClassName}.cs` + `Reports/{ClassName}.Designer.cs` + `Reports/{ClassName}.resx`.
- [ ] 6. Register in `DatabaseUpdate/ReportsUpdater.cs` — `AddPredefinedReport<>` + `CreateReportVisibility`.
- [ ] 7. If adding `ExpressionBindings` in `Designer.cs`, **also update the `.resx` file** or the binding will not work at runtime (see REPORTS.md — .resx Sync Requirement).
- [ ] 8. Update the **Report Catalog** in this document: move the entry from Planned → Implemented and fill in all fields.
- [ ] 9. Update the **Existing Reports** table in `REPORTS.md`.

---

## 9. Naming Conventions

Every report has **four distinct names**. All four must be derived consistently from the same source — the `ApplicationType.Name`, level, optional `ProjectContract.Code`, and variant number.

---

### Name 1 — Class Name (and File Names on Disk)

Used in: `.cs`, `.Designer.cs`, `.resx` filenames and `AddPredefinedReport<ClassName>`.

**Derivation rule:**
1. Take `ApplicationType.Name` — remove all underscores, convert each word to PascalCase
2. Append optional `Company.Code` in PascalCase (e.g. `CLK` → `Clk`) — **only when the layout itself differs per company**. If only the background differs, use a generic class (no code suffix) — `AppBaseReport` handles the background automatically at runtime. Append `ProjectContract.Code` in PascalCase after it if also scoped per contract.
3. Append level suffix: nothing for App level, `Item` for ApplicationItem level, `Reg` for Registration level
4. Append variant suffix `V0`, `V1`, `V2` only when 2+ variants exist at this level
5. Always end with `Report`

| Situation | Class Name |
|---|---|
| App level, single variant, generic | `AppInvReport` |
| App level, single variant, contract-specific | `AppInvClkReport` |
| App level, multiple variants, generic | `AppVisaExtFMV0Report`, `AppVisaExtFMV1Report` |
| App level, multiple variants, contract-specific | `AppVisaExtFMClkV0Report`, `AppVisaExtFMClkV1Report` |
| Item level, single variant, generic | `AppInvItemReport` |
| Item level, single variant, contract-specific | `AppInvClkItemReport` |
| Item level, multiple variants, generic | `AppVisaExtFMItemV0Report`, `AppVisaExtFMItemV1Report` |
| Item level, multiple variants, contract-specific | `AppVisaExtFMClkItemV0Report` |
| Reg level, single variant | `AppRegCheckInRegReport` |

> **ProjectContract is optional.** It only becomes part of the report identity when the form layout physically differs per contract. Use the table below to decide:

| Case | ProjectContract on ApplicationType | Form layout | Reports needed | Visibility criteria |
|---|---|---|---|---|
| No contract involvement | Not shown (ApplicationType hides it) | — | 1 generic | `[ApplicationType.Name] = 'App_Inv'` |
| Contract shown, shared layout | Shown but form is identical for all contracts | Same | 1 generic — bind contract value as a data field if needed | `[ApplicationType.Name] = 'App_Inv'` |
| Contract shown, different layout per contract | Shown and each contract has its own form | Different | 1 per contract | `[ApplicationType.Name] = 'App_Inv' AND [ProjectContract.Code] = 'CLK'` |

> **Decision test:** compare the scanned reference images for different contracts. If they are structurally the same (same field positions, same static text) → shared report. If they differ → separate report per contract.

---

### Name 2 — Registered Name

Used in: `AddPredefinedReport<>(registeredName)` and both parameters of `CreateReportVisibility`.

**Derivation rule:** insert a space before each PascalCase word boundary in the class name, remove `Report` suffix word boundary handling — effectively the class name with spaces.

| Class Name | Registered Name |
|---|---|
| `AppInvReport` | `"App Inv Report"` |
| `AppInvItemReport` | `"App Inv Item Report"` |
| `AppInvClkReport` | `"App Inv Clk Report"` |
| `AppInvClkItemReport` | `"App Inv Clk Item Report"` |
| `AppVisaExtFMV0Report` | `"App Visa Ext FM V0 Report"` |
| `AppVisaExtFMItemV0Report` | `"App Visa Ext FM Item V0 Report"` |
| `AppVisaExtFMClkItemV0Report` | `"App Visa Ext FM Clk Item V0 Report"` |
| `AppRegCheckInRegReport` | `"App Reg Check In Reg Report"` |

---

### Name 3 — Display Name (Turkmen)

Used in: `CreateReportVisibility(reportName, displayName, ...)` — shown to end users in the UI.

**Derivation rule:**
1. Use the Turkmen display name of the `ApplicationType` as the base (from the ApplicationType Master List in Section 4)
2. Append level qualifier if not App level
3. Append contract code in parentheses if contract-specific
4. Append variant number if multiple variants exist

**Level qualifiers:**

| Level | Qualifier |
|---|---|
| Application | *(none)* |
| ApplicationItem | `— Şahsy` |
| Registration | `— Sanaw` |

| Class Name | Display Name |
|---|---|
| `AppInvReport` | `"Çakylyk Almak"` |
| `AppInvItemReport` | `"Çakylyk Almak — Şahsy"` |
| `AppInvClkReport` | `"Çakylyk Almak (CLK)"` |
| `AppInvClkItemReport` | `"Çakylyk Almak — Şahsy (CLK)"` |
| `AppVisaExtFMV0Report` | `"Wiza Uzaltmak FM — 0"` |
| `AppVisaExtFMV1Report` | `"Wiza Uzaltmak FM — 1"` |
| `AppVisaExtFMItemV0Report` | `"Wiza Uzaltmak FM — Şahsy 0"` |
| `AppVisaExtFMClkItemV0Report` | `"Wiza Uzaltmak FM — Şahsy (CLK) 0"` |
| `AppRegCheckInRegReport` | `"Hasaba Almak — Sanaw"` |

---

### Name 4 — Reference Image File Name

Used in: `Resources/FormTemplates/` — design reference only, never loaded into reports.

**Pattern:**
```
{ApplicationType.Name}[_{ProjectContract.Code}]_{level}[_v{n}][_p{n}].jpg
```

| Segment | Rule |
|---|---|
| `{ApplicationType.Name}` | Exact DB value — preserves underscores and mixed case, e.g. `App_Inv`, `App_Visa_Ext_FM` |
| `[_{ProjectContract.Code}]` | ALL CAPS — e.g. `_CLK`, `_TAPI` — omit for generic |
| `_app` / `_item` / `_reg` | Always present |
| `_v0` / `_v1` / `_v2` | Only when 2+ variants exist; omit for single-variant |
| `_p1` / `_p2` | Only for multi-page forms; omit for single-page |

| Class Name | Reference Image |
|---|---|
| `AppInvReport` | `App_Inv_app.jpg` |
| `AppInvItemReport` | `App_Inv_item.jpg` |
| `AppInvClkItemReport` | `App_Inv_CLK_item.jpg` |
| `AppVisaExtFMV0Report` | `App_Visa_Ext_FM_app_v0.jpg` |
| `AppVisaExtFMClkItemV0Report` | `App_Visa_Ext_FM_CLK_item_v0.jpg` |
| `AppRegCheckInRegReport` | `App_Reg_Check_In_reg.jpg` |

Full naming rules and expected file list: see [Section 2b](#2b-form-reference-images).

---

### All Four Names — Side by Side

| Class Name | Registered Name | Display Name | Reference Image |
|---|---|---|---|
| `AppInvReport` | `"App Inv Report"` | `"Çakylyk Almak"` | `App_Inv_app.jpg` |
| `AppInvItemReport` | `"App Inv Item Report"` | `"Çakylyk Almak — Şahsy"` | `App_Inv_item.jpg` |
| `AppInvClkReport` | `"App Inv Clk Report"` | `"Çakylyk Almak (CLK)"` | `App_Inv_CLK_app.jpg` |
| `AppInvClkItemReport` | `"App Inv Clk Item Report"` | `"Çakylyk Almak — Şahsy (CLK)"` | `App_Inv_CLK_item.jpg` |
| `AppVisaExtFMV0Report` | `"App Visa Ext FM V0 Report"` | `"Wiza Uzaltmak FM — 0"` | `App_Visa_Ext_FM_app_v0.jpg` |
| `AppVisaExtFMItemV0Report` | `"App Visa Ext FM Item V0 Report"` | `"Wiza Uzaltmak FM — Şahsy 0"` | `App_Visa_Ext_FM_item_v0.jpg` |
| `AppRegCheckInRegReport` | `"App Reg Check In Reg Report"` | `"Hasaba Almak — Sanaw"` | `App_Reg_Check_In_reg.jpg` |

---

### ReportsUpdater — Visibility Criteria Patterns

Generic (all companies, all contracts):
```csharp
CreateReportVisibility("App Inv Report", "Çakylyk Almak", typeof(Application),
    "[ApplicationType.Name] = 'App_Inv'");
```

Company-specific (layout differs per company):
```csharp
CreateReportVisibility("App Inv Clk Report", "Çakylyk Almak (CLK)", typeof(Application),
    "[ApplicationType.Name] = 'App_Inv' AND [Company.Code] = 'CLK'");
```

ProjectContract-specific (layout differs per contract):
```csharp
CreateReportVisibility("App Inv Tapi Report", "Çakylyk Almak (TAPI)", typeof(Application),
    "[ApplicationType.Name] = 'App_Inv' AND [ProjectContract.Code] = 'TAPI'");
```

Company + ProjectContract scoped (both dimensions differ):
```csharp
CreateReportVisibility("App Inv Clk Tapi Report", "Çakylyk Almak (CLK/TAPI)", typeof(Application),
    "[ApplicationType.Name] = 'App_Inv' AND [Company.Code] = 'CLK' AND [ProjectContract.Code] = 'TAPI'");
```

Shared across multiple ApplicationTypes:
```csharp
CreateReportVisibility("App Inv Item Report", "Çakylyk Almak — Şahsy", typeof(ApplicationItem),
    "[Application.ApplicationType.Name] In ('App_Inv', 'App_Inv_According_to_WP')");
```

---

### Reference Documents

Keep the original government file name exactly as received. Do not rename.

---

## 10. Visa Application PDF (Special Case)

`Visa_Application_TM_QR_08.pdf` is **not an XtraReport**. It is an interactive XFA PDF form filled programmatically using Spire.PDF via `PdfMappingHelper`.

- Field reference: `Resources/Pdf field reference .md`
- 75 fields across 2 pages; field keys follow XFA path notation (`topmostSubform[0].Page1[0]._01[0]`)
- Dates are `picture` type — pass as `dd.MM.yyyy` string
- Choice fields (gender, marital status, urgency) require **raw values**, not display labels
- Currently 20+ fields unmapped — see `⚠️` entries in the field reference

This PDF fill path is separate from the XtraReports pipeline. Do not create an XtraReport for this form.
