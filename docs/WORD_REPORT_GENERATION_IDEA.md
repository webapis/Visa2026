# Word Report Generation — Design Idea

**Date:** 2026-05-12  
**Status:** Phase 2 complete — multi-report "Resminamalar" button live; three reports working for `App_Business_Trip_Arrival` / `App_Business_Trip_Departure` (sanawy table + arrival letter + departure letter)

## Decision Log

| Decision | Choice | Reason |
|---|---|---|
| Template library | `DocumentFormat.OpenXml` 3.3.0 | Already fits in the existing .NET 8 stack; no extra runtime; MIT license |
| Template fill library | `DocxTemplater` 2.4.4 | `{{#ds.rows}}` loop support for repeating rows; simpler template authoring. Model bound under prefix `ds`. |
| Template authoring approach | **Code-generated** via `BusinessTripSanawyTemplateGenerator.cs` | Templates are version-controlled C# — reproducible, no manual Word authoring needed; generated once and stored as `EmbeddedResource` |
| Template storage | `Visa2026.Module/Resources/` as `EmbeddedResource` | Consistent with existing `App_Reg_Check_In.docx` pattern |
| Map source | `Visa2026.Module/Resources/FormTemplates/*_map.md` | Field names, **dynamic vs static** regions, layout, column widths — drives generators and **audits** Word placeholders against BO data; use when **redesigning** a report that also exists (or existed) as XtraReport |
| Layout fidelity | `Visa2026.Module/Resources/FormTemplates/` (scans, e.g. `.png`, plus maps) | **Scans** = static ministry text + visual structure for **comparison** with output. **Maps** = dynamic data source contract. Shipped Word templates must match the scan when one exists. **If no scan is in repo yet, the agent must ask the user for the official scan before designing layout.** Agent checklist: `.cursor/skills/visa2026-word-reports/SKILL.md`. |

---

## Strategic Decision (2026-05-12, clarified)

**Word pipeline:** **Alternative** (parallel) implementation for ministry outputs—**not** a one-time wholesale replacement of the XAF XtraReports stack. Most forms were **first implemented as XtraReports** in `Visa2026.Module/Reports/`; the product direction is to **redesign the same report** using Word + DocxTemplater (**`Resources/*.docx`**, **`IWordReportDefinition`**, **Resminamalar**) where that delivery fits best.

| Category | Decision | Reason |
|---|---|---|
| **New tabular list reports** (`_Sanawy`, registration lists, WP lists, etc.) | ✅ Prefer Word pipeline | Proven by `BusinessTrip_Sanawy`; loop rows, header repeat, cantSplit |
| **New item-level form reports** (`App_*_item`) | ✅ Prefer Word pipeline | Plain `.docx` layout; ministry forms align with scans in **`FormTemplates/`** |
| **New letter-type reports** (Groups A–E, new ApplicationTypes) | ✅ Prefer Word pipeline | Native Word justification; simpler than XRRichText maintenance for many letters |
| **Existing XtraReports** | ✅ Keep until Word parity; **redesign in Word** per report | Legacy reports stay **reference** for bindings and QA while Word versions are built; no forced delete-by-date |
| **Redesign workflow** | Cross-check **XtraReport** + **`FormTemplates/*_map.md`** + scan | Maps and scans separate **dynamic** (BO) vs **static** (ministry) text; see **`.cursor/skills/visa2026-word-reports/SKILL.md`** |

The XtraReports pipeline (`REPORTS.md`, `REPORT_GENERATION_GUIDE.md`, `PROMPT_CREATE_REPORT.md`, **`.cursor/skills/report-predefined-xaf/SKILL.md`**) remains the guide for **maintaining and reading** predefined reports. For **Word delivery** of the same forms, follow **this document** and the **visa2026-word-reports** skill.

### Layout families (standardize typography and steps)

New Word reports are categorized by **layout similarity** (letters, sanawy tables, statutory item forms, etc.) so **font, margins, and generator patterns** stay aligned. Codes (**L1**, **L2**, **T1**, **F1**, …), margin/font matrix, and patternized design steps live in **`.cursor/skills/visa2026-word-reports/SKILL.md`** and **`reference.md`**. Phase 1 of the skill workflow asks which family applies before implementation.

### Data binding (business objects)

Template keys map to **`Application`**, **`ApplicationItem`**, **`Registration`**, and **`BusinessTrip`** (`Visa2026.Module/BusinessObjects/`). **`WordReportsController`** passes the current **`Application`** into each `IWordReportDefinition`. The skill’s **Phase 3** and **Missing information** checklist require explicit user confirmation of **row source**, **header vs row fields**, and **property existence** before implementation so placeholders are not invented.

### Reviewing initialized templates (`Resources/*.docx`)

Most Word templates are already embedded under **`Visa2026.Module/Resources/`**. Ongoing work should **batch** review/refactor/redesign using **`.cursor/skills/visa2026-word-reports/SKILL.md`** → *Review, refactor, and redesign* and **`reference.md`** → *Inventory*: trace each file to a `*ReportDef`, assign a **layout family**, verify **FormTemplates** scan parity, audit **bindings**, and extend **`PreviewWordReports`** presets—without mass-changing behavior unless the user approves the batch scope.

**Clerk-authored templates** uploaded as **`UserReportTemplate`** (not embedded `Resources/*.docx`) are documented for authors in **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** (DocxTemplater **`ds`** prefix, extract/validate, visibility criteria editor).

**Status tracking:** **`.cursor/skills/visa2026-word-reports/review-status.md`** lists each template with **Pending** / **In review** / **Completed** / **Blocked** — update it as batches progress.

---

## Phase 2 Architecture — Multi-Report Button ✅ Implemented

### Decisions

| Question | Decision | Reason |
|---|---|---|
| Multiple applicable reports per button click | **Single `.zip`** containing all generated `.docx` files | Cleaner UX — one download, one file; avoids multiple browser download prompts |
| Button visibility | **Disabled** when no Word report is applicable for the current `ApplicationType`; always visible | User knows the feature exists; disabled state is more informative than hidden |
| Applicability check | **`ApplicationType.Name` string match** (explicit, per-report) | Boolean flags (`ShowBusinessTrips` etc.) control UI field visibility, not report existence. Name strings are stable, self-contained, and make each report definition fully explicit without requiring new flags on `ApplicationType`. |

---

### Core Interface: `IWordReportDefinition`

Each Word report is a small class implementing this interface and registered in DI. The controller discovers all of them at runtime via `IEnumerable<IWordReportDefinition>`.

```csharp
// Visa2026.Module/Services/WordReports/IWordReportDefinition.cs
public interface IWordReportDefinition
{
    /// <summary>
    /// ApplicationType.Name values this report applies to.
    /// Null or empty = applies to ALL application types.
    /// </summary>
    string[] ApplicableApplicationTypeNames { get; }

    /// <summary>
    /// Output file name (without path). May include application number at runtime.
    /// Example: "Sanawy_{FullApplicationNumber}_{date}.docx"
    /// </summary>
    string GetFileName(Application application);

    /// <summary>
    /// Secondary applicability check — called only if ApplicationType.Name matches.
    /// Use to guard against empty collections (e.g. no BusinessTrips).
    /// </summary>
    bool IsApplicable(Application application);

    /// <summary>
    /// Generate the filled .docx and write it to outputStream.
    /// </summary>
    Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream);
}
```

---

### Controller: `WordReportsController`

Single controller, single **`"Resminamalar"`** button on the `Application` detail view:

```
1. Inject IEnumerable<IWordReportDefinition> from DI
2. On view activated: filter definitions by ApplicationType.Name + IsApplicable()
   → disable action if none applicable
3. On execute:
   a. For each applicable definition: call GenerateAsync() → MemoryStream
   b. If exactly 1 result: download as plain .docx
   c. If 2+ results: zip all streams → download as .zip
   d. File name: "{ApplicationType.Name}_{FullApplicationNumber}_{date}.zip"
```

---

### Implemented File Structure

```
Visa2026.Module/
  Services/
    WordReports/
      IWordReportDefinition.cs                    ← interface
      BusinessTripSanawyReportDef.cs              ← 11-col personnel list (arrival + departure)
      BusinessTripArrivalLetterReportDef.cs       ← arrival notification letter
      BusinessTripDepartureLetterReportDef.cs     ← departure notification letter
  Controllers/
    WordReportsController.cs                      ← single "Resminamalar" button; zips 2+ reports
  Resources/
    BusinessTrip_Sanawy.docx                      ← EmbeddedResource; landscape A4 table
    BusinessTrip_Arrival_Letter.docx              ← EmbeddedResource; portrait A4 letter
    BusinessTrip_Departure_Letter.docx            ← EmbeddedResource; portrait A4 letter
tools/GenerateTemplates/Program.cs               ← regenerates all three templates
```

`BusinessTripWordController.cs` was **removed** — replaced by `WordReportsController` + `BusinessTripSanawyReportDef`.

> **To regenerate templates** (after layout changes):
> ```
> dotnet run --project tools\GenerateTemplates\GenerateTemplates.csproj -- "Visa2026.Module\Resources\BusinessTrip_Sanawy.docx" "Visa2026.Module\Resources\BusinessTrip_Arrival_Letter.docx" "Visa2026.Module\Resources\BusinessTrip_Departure_Letter.docx"
> ```

> **Preview a template with dump data (no app rebuild / no Blazor):** `tools/PreviewWordReports` binds the same `ds` / `ds.rows` model as `WordFormFillerService` and writes `out/<preset>_preview.docx` next to the tool EXE.
> ```
> dotnet run --project tools\PreviewWordReports -- list
> dotnet run --project tools\PreviewWordReports -- borcnama
> dotnet run --project tools\PreviewWordReports -- business-trip-arrival-letter
> ```
> Add presets in `tools/PreviewWordReports/Program.cs` when you introduce new templates.
>
> **Yellow highlight (preview only):** After DocxTemplater merge, the tool runs an Open XML pass that applies **yellow highlight** (and light shading) to text matching the preset’s sample strings (the same values that stand in for **business object–backed fields**). That makes **dynamic** merge targets obvious when you compare the preview to the **`FormTemplates`** scan; **static** Turkmen body text stays unhighlighted. **Production** downloads from the app do **not** include this styling — it is for designers/QA only. Sample strings are defined in code, not extracted from the scan image (layout still must match the scan).

> **Experience log:** After finishing a report design or fix, append a dated entry to **`.cursor/skills/visa2026-word-reports/learnings.md`** (append-only). Read that file before starting similar work so pitfalls (DocxTemplater, OpenXml paths, one-page layout) are not repeated.

> **Process:** New or materially changed reports follow **Predetermined workflow (ask, response, then act)** in **`.cursor/skills/visa2026-word-reports/SKILL.md`** so scope, scan, data shape, and verification are explicit before implementation.

---

### Applicability Examples

| Report definition | `ApplicableApplicationTypeNames` | `IsApplicable()` guard |
|---|---|---|
| `BusinessTripSanawyReportDef` | `["App_Business_Trip_Arrival", "App_Business_Trip_Departure"]` | `application.BusinessTrips.Any(bt => !bt.IsDeleted)` |
| `WorkPermitListReportDef` *(future)* | `["App_Inv_And_WP", "App_Visa_and_WP_Ext", ...]` | `application.ApplicationItems.Any(...)` |
| `RegistrationListReportDef` *(future)* | `["App_Reg_Check_In", "App_Reg_Check_In_Internal", ...]` | `application.Registrations.Any(...)` |

---

### DI Registration Pattern

```csharp
// Startup.cs — register each definition as IWordReportDefinition
services.AddScoped<IWordReportDefinition, BusinessTripSanawyReportDef>();
services.AddScoped<IWordReportDefinition, BusinessTripArrivalLetterReportDef>();
services.AddScoped<IWordReportDefinition, BusinessTripDepartureLetterReportDef>();
services.AddScoped<IWordReportDefinition, WorkPermitListReportDef>(); // future
// Controller resolves IEnumerable<IWordReportDefinition> — gets all of them
```

> **Adding a new report:** create the definition class → register one line here. `WordReportsController` picks it up automatically.

---

## Goal

Replace (or supplement) XAF Reports V2 / PDF form filling with **Word `.docx` template filling** for report generation. Templates ship alongside the project as embedded resources and are filled from business object data on button click, with output saved to a destination folder.

---

## Context

The project already has two document generation pipelines:

1. **PDF form filling** — `PdfFormFillerService` + `PdfMappingHelper` + `PdfFormMapping` (Spire.PDF, XFA forms)
2. **XAF Mail Merge** — `RichTextMailMergeData` / `ShowMailMergeController` / `MailMergeUpdater` (DevExpress Office module, `.docx` stored in DB)

The requirement is a **third, independent pipeline** that does NOT touch or modify the existing Mail Merge setup.

---

## Requirements

- Free / open-source library — no additional license cost
- No Microsoft Word installation required on the server (runs in Docker)
- `.docx` templates ship with the project (embedded resources in `Visa2026.Module/Resources/`)
- On button click in a XAF Detail/List View: fill template from current business object → save output `.docx` to a configured destination folder
- Must support **tabular / repeating-row reports** (list of people, like the "Daşary ýurt raýatlarynyň sanawy" table)
- Must support **single-record forms** (one person / application per document)

---

## Chosen Library Candidates

### Option A — `DocumentFormat.OpenXml` (Microsoft, MIT)
- NuGet: `DocumentFormat.OpenXml`
- Template authoring: **named plain-text Content Controls** in Word (tag = field name, e.g. `Person_LastName`)
- Runtime: open template bytes → replace content control values → save output
- Pro: fully standards-based, Word-compatible, widely used
- Con: repeating table rows require more manual XML manipulation

### Option B — `DocxTemplater` (free tier, MIT-like)
- NuGet: `DocxTemplater`
- Template authoring: `{{Person_LastName}}` placeholder text typed directly in Word body
- Supports `{{#foreach}}` table row repetition — much easier for list reports
- Pro: simpler template authoring, built-in foreach for collections
- Con: slightly less standards-based than content controls

**Recommendation for list/tabular reports:** Option B (`DocxTemplater`) — foreach support makes repeating rows trivial.  
**Recommendation for single-record forms:** Either option works equally well.

---

## Proposed Architecture

Follow the exact same pattern as the existing PDF pipeline:

### 1. Interface
```csharp
// Visa2026.Module/Services/IWordFormFillerService.cs
public interface IWordFormFillerService
{
    // Single-record: all keys become {{ds.Key}}
    void FillForm(Stream templateStream, Stream outputStream,
                  IDictionary<string, object> data);

    // List/tabular: header keys → {{ds.Key}}, rows → {{#ds.rows}}…{{/ds.rows}}, fields → {{.FieldName}}
    void FillListForm(Stream templateStream, Stream outputStream,
                      IDictionary<string, object> header,
                      IEnumerable<IDictionary<string, object>> rows);
}
```

### 2. Implementation
```csharp
// Visa2026.Module/Services/WordFormFillerService.cs
public class WordFormFillerService : IWordFormFillerService { ... }
```

### 3. Template
- Place `.docx` in `Visa2026.Module/Resources/` — Build Action: **Embedded Resource**
- Register in `.csproj`:
  ```xml
  <None Remove="Resources\MyTemplate.docx" />
  <EmbeddedResource Include="Resources\MyTemplate.docx" />
  ```

### 4. Controller
```csharp
// Visa2026.Module/Controllers/BusinessTripWordController.cs
public class BusinessTripWordController : ViewController<DetailView>
{
    // SimpleAction "Generate Word Report"
    // On execute: build Dictionary<string, object> from current BO → call IWordFormFillerService → save to configured output folder
}
```

### 5. DI Registration
```csharp
// Visa2026.Blazor.Server/Startup.cs
services.AddScoped<IWordFormFillerService, WordFormFillerService>();
```

### 6. Configuration
```json
// appsettings.json
"WordReportSettings": {
  "OutputPath": "Reports/Output"
}
```

---

## Target Business Objects

Reports will be generated from the following four business objects. All `[NotMapped]` flat properties are already present on each class and are ready to use as template placeholders.

---

### `Application`
Header-level data for a single application. Use for cover letters, summary forms, multi-person list headers.

| Placeholder | Source property | Notes |
|---|---|---|
| `FullApplicationNumber` | `FullApplicationNumber` | e.g. CEC-0042/2026 |
| `ApplicationDate` | `ApplicationDate` | DateTime |
| `ApplicationDateText` | *(format inline)* | dd.MM.yyyy |
| `Company_Code` | `Company_Code` | |
| `MigrationService_NameTm` | `MigrationService_NameTm` | |
| `Urgency_NameTm` | `Urgency_NameTm` | |
| `VisaPeriod_NameTm` | `VisaPeriod_NameTm` | |
| `VisaCategory_NameTm` | `VisaCategory_NameTm` | |
| `ProjectContract_Description` | `ProjectContract_Description` | |
| `ProjectContract_Ministry_RecipientBlock` | `ProjectContract_Ministry_RecipientBlock` | |
| `ProjectContract_Ministry_RecipientBlock_Line1`, `_Line2`, `_HasLine2` | Derived in `AppInvAndWPLetterReportDef` from `RecipientBlock` | **App_Inv_And_WP_Letter** only: two-line stepped addressee; see `MinistryRecipientBlockFormatter` and `App_Inv_And_WP_app_map.md`. |
| `ProjectContract_Ministry_FormOfAddress` | `ProjectContract_Ministry_FormOfAddress` | |
| `FamilyMember_Relationship_NameTm` | `FamilyMember_Relationship_NameTm` | Joined Turkmen list |
| `SponsoringEmployee_FullName` | `SponsoringEmployee_FullName` | |
| `SponsoringEmployee_PositionTm` | `SponsoringEmployee_PositionTm` | |
| `BusinessTripStartDateText` | `BusinessTripStartDateText` | dd.MM.yyyy |
| `BusinessTripEndDateText` | `BusinessTripEndDateText` | dd.MM.yyyy |
| `BusinessTripDurationDays` | `BusinessTripDurationDays` | int |
| `BusinessTripPurpose_NameTm` | `BusinessTripPurpose_NameTm` | |
| `MovementPermitLocation_NameTm` | `MovementPermitLocation_NameTm` | |
| `BorderZoneLocation_NameTm` | `BorderZoneLocation_NameTm` | |
| `FromCityName` | `FromCityName` | |
| `FromRegionName` | `FromRegionName` | |
| `FromRegionName_Genitive` | `FromRegionName_Genitive` | Turkmen genitive case |
| `FromCityName_Ablative` | `FromCityName_Ablative` | Turkmen ablative case |
| `ToCityName` | `ToCityName` | |
| `ToRegionName` | `ToRegionName` | |
| `ToRegionName_Genitive` | `ToRegionName_Genitive` | Turkmen genitive case |
| `ToCityName_Dative` | `ToCityName_Dative` | Turkmen dative case |
| `TotalPersonCount` | `TotalPersonCount` | int |
| `TotalPersonCountText` | `TotalPersonCountText` | Turkmen words |
| `CancelPersonCount` / `CancelPersonCountText` | same | |
| `CancelWPCount` / `CancelWPCountText` | same | |
| `CancelInvCount` / `CancelInvCountText` | same | |

**Collections for repeating rows:**
- `ApplicationItems` → list of `ApplicationItem` (see below)
- `Registrations` → list of `Registration` (see below)
- `BusinessTrips` → list of `BusinessTrip` (see below)

---

### `ApplicationItem`
One person within an application. Use for per-person visa forms and as repeating rows in list reports.

**Person**

| Placeholder | Source property |
|---|---|
| `Person_FullName` | `Person_FullName` |
| `Person_LastName` | `Person_LastName` |
| `Person_FirstName` | `Person_FirstName` |
| `Person_GenderTm` | `Person_GenderTm` |
| `Person_BirthPlace` | `Person_BirthPlace` |
| `Person_ForeignAddress` | `Person_ForeignAddress` |
| `Person_DateOfBirthText` | `Person_DateOfBirthText` |
| `Person_NationalityCode` | `Person_NationalityCode` |
| `Person_NationalityTm` | `Person_NationalityTm` |
| `Person_CountryOfBirthCode` | `Person_CountryOfBirthCode` |
| `Person_CountryOfBirthTm` | `Person_CountryOfBirthTm` |

**Passport**

| Placeholder | Source property |
|---|---|
| `Passport_Number` | `Passport_Number` |
| `Passport_PersonalNumber` | `Passport_PersonalNumber` |
| `Passport_Authority` | `Passport_Authority` |
| `Passport_IssueDateText` | `Passport_IssueDateText` |
| `Passport_ExpirationDateText` | `Passport_ExpirationDateText` |
| `Passport_CountryCode` | `Passport_CountryCode` |
| `Passport_CountryTm` | `Passport_CountryTm` |
| `PreviousPassport_Number` | `PreviousPassport_Number` |
| `PreviousPassport_ExpirationDateText` | `PreviousPassport_ExpirationDateText` |

**Visa**

| Placeholder | Source property |
|---|---|
| `Visa_Number` | `Visa_Number` |
| `Visa_IssueDateText` | `Visa_IssueDateText` |
| `Visa_StartDateText` | `Visa_StartDateText` |
| `Visa_ExpirationDateText` | `Visa_ExpirationDateText` |
| `Visa_IssuedPlaceTm` | `Visa_IssuedPlaceTm` |
| `Visa_CategoryTm` | `Visa_CategoryTm` |
| `Visa_TypeTm` | `Visa_TypeTm` |

**Address**

| Placeholder | Source property |
|---|---|
| `Address_FullAddress` | `Address_FullAddress` |
| `Address_RegionTm` | `Address_RegionTm` |
| `Address_CityTm` | `Address_CityTm` |
| `Address_StartDateText` | `Address_StartDateText` |
| `Address_ExpirationDateText` | `Address_ExpirationDateText` |

**Position / Contract / Education**

| Placeholder | Source property |
|---|---|
| `Position_PositionTm` | `Position_PositionTm` |
| `Position_DepartmentTm` | `Position_DepartmentTm` |
| `Contract_Salary` | `Contract_Salary` |
| `Contract_SalaryText` | `Contract_SalaryText` |
| `Contract_StartDateText` | `Contract_StartDateText` |
| `Contract_ExpirationDateText` | `Contract_ExpirationDateText` |
| `Education_LevelTm` | `Education_LevelTm` |
| `Education_InstitutionName` | `Education_InstitutionName` |
| `Education_SpecialtyTm` | `Education_SpecialtyTm` |
| `Education_GraduationYear` | `Education_GraduationYear` |

**Work Permit / Invitation**

| Placeholder | Source property |
|---|---|
| `WorkPermit_Number` | `WorkPermit_Number` |
| `WorkPermit_StartDateText` | `WorkPermit_StartDateText` |
| `WorkPermit_ExpirationDateText` | `WorkPermit_ExpirationDateText` |
| `WorkPermit_ASNumber` | `WorkPermit_ASNumber` |
| `PreviousWorkPermit_Number` | `PreviousWorkPermit_Number` |
| `Invitation_Number` | `Invitation_Number` |
| `Invitation_StartDateText` | `Invitation_StartDateText` |
| `Invitation_ExpirationDateText` | `Invitation_ExpirationDateText` |
| `PreviousInvitation_Number` | `PreviousInvitation_Number` |

**Signatory / Company**

| Placeholder | Source property |
|---|---|
| `CompanyHead_FullName` | `CompanyHead_FullName` |
| `CompanyHead_PositionTm` | `CompanyHead_PositionTm` |
| `CompanyHead_PassportNumber` | `CompanyHead_PassportNumber` |
| `CompanyHead_PassportLine` | `CompanyHead_PassportLine` |
| `Representative_FullName` | `Representative_FullName` |
| `Representative_PassportLine` | `Representative_PassportLine` |
| `Application_CompanyRegistryAddressLine` | `Application_CompanyRegistryAddressLine` |
| `Application_SponsorName` | `Application_SponsorName` |

**FM (Family Member) report helpers**

| Placeholder | Source property | Notes |
|---|---|---|
| `FM_EducationLevelTm` | `FM_EducationLevelTm` | "Çaga" if under 18 |
| `FM_SpecialtyTm` | `FM_SpecialtyTm` | "Çaga" if under 18 |
| `FM_WezipesiTm` | `FM_WezipesiTm` | Position + sponsor name + relationship |

---

### `Registration`
One person's registration/movement record within an application. Use for check-in/check-out forms and registration list reports.

**Person**

| Placeholder | Source property |
|---|---|
| `Person_FullName` | `Person_FullName` |
| `Person_LastName` | `Person_LastName` |
| `Person_FirstName` | `Person_FirstName` |
| `Person_MiddleName` | `Person_MiddleName` |
| `Person_BirthPlace` | `Person_BirthPlace` |
| `Person_DateOfBirthText` | `Person_DateOfBirthText` |
| `Person_GenderTm` | `Person_GenderTm` |
| `Person_MaritalStatusTm` | `Person_MaritalStatusTm` |
| `Person_NationalityCode` | `Person_NationalityCode` |
| `Person_NationalityTm` | `Person_NationalityTm` |
| `Person_CountryOfBirthCode` | `Person_CountryOfBirthCode` |
| `Person_CountryOfBirthTm` | `Person_CountryOfBirthTm` |
| `Person_CompanyName` | `Person_CompanyName` |
| `Person_CompanyAddress` | `Person_CompanyAddress` |
| `Person_ForeignAddress` | `Person_ForeignAddress` |
| `Person_ForeignAddressCountryCode` | `Person_ForeignAddressCountryCode` |
| `Person_ForeignAddressCountryTm` | `Person_ForeignAddressCountryTm` |
| `Person_RelationshipTm` | `Person_RelationshipTm` |
| `Person_SponsoringEmployeeFullName` | `Person_SponsoringEmployeeFullName` |
| `Person_SponsoringEmployeePositionTm` | `Person_SponsoringEmployeePositionTm` |

**Passport / Visa / Address / Position** — same field names as `ApplicationItem` above.

**Travel**

| Placeholder | Source property |
|---|---|
| `Travel_DateText` | `Travel_DateText` |
| `Travel_PurposeOfTravelTm` | `Travel_PurposeOfTravelTm` |
| `Travel_CheckPointTm` | `Travel_CheckPointTm` |

**Application / Signatory**

| Placeholder | Source property |
|---|---|
| `Application_FullNumber` | `Application_FullNumber` |
| `Application_DateText` | `Application_DateText` |
| `Application_MigrationServiceCode` | `Application_MigrationServiceCode` |
| `Application_RegistrationDateText` | `Application_RegistrationDateText` |
| `CompanyHead_FullName` | `CompanyHead_FullName` |
| `CompanyHead_PositionTm` | `CompanyHead_PositionTm` |
| `RowNumber` | `RowNumber` | Set before execute by ShowMailMergeController pattern |

---

### `BusinessTrip`
One person's business trip record. Use for departure/arrival forms and the "Daşary ýurt raýatlarynyň sanawy" tabular list report.

| Placeholder | Source property |
|---|---|
| `Person_LastName` | `Person_LastName` |
| `Person_FirstName` | `Person_FirstName` |
| `Person_DateOfBirthText` | `Person_DateOfBirthText` |
| `Person_BirthPlace` | `Person_BirthPlace` |
| `Person_GenderTm` | `Person_GenderTm` |
| `Person_NationalityCode` | `Person_NationalityCode` |
| `Passport_Number` | `Passport_Number` |
| `Passport_ExpirationDateText` | `Passport_ExpirationDateText` |
| `Position_NameTm` | `Position_NameTm` |
| `Visa_NumberAndType` | `Visa_NumberAndType` |
| `Visa_StartDateText` | `Visa_StartDateText` |
| `Visa_ExpirationDateText` | `Visa_ExpirationDateText` |
| `Address_FullAddress` | `Address_FullAddress` |
| `BusinessTripAddress_FullAddress` | `BusinessTripAddress_FullAddress` |
| `Application_CompanyHead_FullName` | `Application_CompanyHead_FullName` |
| `Application_CompanyHead_PositionTm` | `Application_CompanyHead_PositionTm` |

---

## Step-by-Step Implementation Plan

### Step 1 — Add NuGet package
Add `DocxTemplater` to `Visa2026.Module.csproj`:
```xml
<PackageReference Include="DocxTemplater" Version="2.*" />
```
No Word installation needed on the server. Runs in Docker.

---

### Step 2 — Create the service interface
`Visa2026.Module/Services/IWordFormFillerService.cs`

Two methods — mirrors `IPdfFormFillerService` pattern:
```csharp
public interface IWordFormFillerService
{
    // Single-record form (one Application, one ApplicationItem, etc.)
    void FillForm(Stream templateStream, Stream outputStream,
                  Dictionary<string, object> data);

    // List/tabular report — header fields + repeating rows
    void FillListForm(Stream templateStream, Stream outputStream,
                      Dictionary<string, object> header,
                      IEnumerable<Dictionary<string, object>> rows);
}
```

---

### Step 3 — Implement the service
`Visa2026.Module/Services/WordFormFillerService.cs`

Uses `DocxTemplater` to open the template stream, substitute placeholders, save output:
- `FillForm` → passes `data` dictionary directly; `{{Key}}` in template replaced by `data["Key"]`
- `FillListForm` → merges `header` + `{ "rows": rows }` into one model bound as `"ds"`; `{{#ds.rows}}` table row in template repeats per item; row fields use `{{.FieldName}}` dot notation

---

### Step 4 — Register in DI
`Visa2026.Blazor.Server/Startup.cs` — one line added in the same block as `IPdfFormFillerService`:
```csharp
services.AddScoped<IWordFormFillerService, WordFormFillerService>();
```

---

### Step 5 — Author `.docx` templates (code-generated)
Templates are **code-generated** via `tools/GenerateTemplates/Program.cs` using `DocumentFormat.OpenXml`. Run it to regenerate:
```
dotnet run --project tools\GenerateTemplates\GenerateTemplates.csproj -- "Visa2026.Module\Resources\<Template>.docx"
```
Place each template in `Visa2026.Module/Resources/`. Set **Build Action = Embedded Resource**.

**Placeholder syntax (DocxTemplater, model prefix `ds`):**

- Single-record fields: `{{ds.FieldName}}`
- List loop open (must be alone in its own `<w:p>` in the first cell): `{{#ds.rows}}`
- Row fields (dot notation): `{{.FieldName}}`
- List loop close (must be alone in its own `<w:p>` in the last cell): `{{/ds.rows}}`
- Header/signatory outside table: `{{ds.Application_CompanyHead_FullName}}`

**Critical template rules learned from `BusinessTrip_Sanawy`:**
- `{{#ds.rows}}` and `{{/ds.rows}}` must each be in their **own `<w:p>` paragraph** (not mixed with other text in the same paragraph) — otherwise DocxTemplater renders them as literal text
- Add `<w:tblHeader/>` to the header row's `<w:trPr>` for header repeat on every page
- Add `<w:cantSplit/>` to the data row's `<w:trPr>` to prevent rows splitting across pages
- Add `RowNumber` (1-based int) to each row dictionary for the № column

---

### Step 6 — Register templates as embedded resources
`Visa2026.Module.csproj` — one pair per template, same pattern as the existing PDF template:
```xml
<None Remove="Resources\BusinessTrip_Sanawy.docx" />
<EmbeddedResource Include="Resources\BusinessTrip_Sanawy.docx" />
```

---

### Step 7 — XAF actions (historical sketch vs current code)

This section was an **early sketch** (“one controller per report type”). **What shipped instead** is under **Phase 2 Architecture** above: a single `WordReportsController` on `Application` plus **`IWordReportDefinition`** classes registered in DI, each using `IWordFormFillerService` with embedded `Resources/*.docx` templates.

**Clerk-authored Word templates** (uploaded `.docx`, in-app extract/validate) use **`UserReportTemplate`** and the same **Resminamalar** pipeline; see **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** (DocxTemplater **`ds`** prefix and visibility rules).

| Original sketch | Current approach |
|---|---|
| `ApplicationWordController`, … | `IWordReportDefinition` + `WordReportsController` |
| `BusinessTripWordController` | `BusinessTripSanawyReportDef` (+ arrival/departure letter defs) |

---

### Step 8 — Add output path configuration
`Visa2026.Blazor.Server/appsettings.json`:
```json
"WordReportSettings": {
  "OutputPath": ""
}
```
- **Empty string** → browser download via `IFileDownloader`
- **Absolute or relative path** → save `.docx` file to that folder (Docker: must be a mounted volume)

---

### Execution order (when ready to implement)

```
1.  dotnet add package DocxTemplater              ← Step 1
2.  Create IWordFormFillerService                 ← Step 2
3.  Create WordFormFillerService                  ← Step 3
4.  Register in Startup.cs                        ← Step 4
5.  Add WordReportSettings to appsettings.json    ← Step 8
6.  Author first .docx template in Word           ← Step 5
7.  Add EmbeddedResource to .csproj               ← Step 6
8.  Add first `IWordReportDefinition` + DI registration  ← Step 7 (current pattern)
9.  Build & smoke-test
10. Repeat steps 6–8 for each remaining template
```

---

## Files — Implementation Status

| File | Status | Notes |
|---|---|---|
| `Visa2026.Module/Services/IWordFormFillerService.cs` | ✅ Done | `FillForm` + `FillListForm`, stream-based |
| `Visa2026.Module/Services/WordFormFillerService.cs` | ✅ Done | `new DocxTemplate(stream)` + `BindModel("ds", model)` |
| `Visa2026.Module/Controllers/BusinessTripWordController.cs` | 🗑 Removed | Replaced by `WordReportsController` + `BusinessTripSanawyReportDef` |
| `Visa2026.Module/Resources/BusinessTrip_Sanawy.docx` | ✅ Done | Landscape A4, 11-col, header repeat, `cantSplit` |
| `Visa2026.Module/Resources/BusinessTrip_Arrival_Letter.docx` | ✅ Done | Portrait A4 letter; arrival notification |
| `Visa2026.Module/Resources/BusinessTrip_Departure_Letter.docx` | ✅ Done | Portrait A4 letter; departure notification |
| `tools/GenerateTemplates/Program.cs` | ✅ Done | Regenerates all three templates; run with explicit output paths |
| `Visa2026.Blazor.Server/Startup.cs` | ✅ Done | `IWordFormFillerService` + all three `IWordReportDefinition` registrations |
| `Visa2026.Module.csproj` | ✅ Done | `DocxTemplater` 2.4.4 + `DocumentFormat.OpenXml` 3.3.0; all three `.docx` as EmbeddedResource |
| `Visa2026.Module/Services/WordReports/IWordReportDefinition.cs` | ✅ Done | Core interface — applicability + generate |
| `Visa2026.Module/Services/WordReports/BusinessTripSanawyReportDef.cs` | ✅ Done | 11-col personnel list; both arrival + departure |
| `Visa2026.Module/Services/WordReports/BusinessTripArrivalLetterReportDef.cs` | ✅ Done | Arrival notification letter |
| `Visa2026.Module/Services/WordReports/BusinessTripDepartureLetterReportDef.cs` | ✅ Done | Departure notification letter |
| `Visa2026.Module/Controllers/WordReportsController.cs` | ✅ Done | Single "Resminamalar" button; zips 2+ reports |
| `Visa2026.Module/BusinessObjects/UserReportTemplate.cs` (+ placeholders) | ✅ In use | User-uploaded templates; visibility uses **popup criteria editor** (same pattern as `ReportVisibility`); author guide: **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** |

---

## Notes

- Do NOT modify `ShowMailMergeController`, `MailMergeUpdater`, `RichTextMailMergeData`, or any existing Mail Merge infrastructure
- Follow the `IPdfFormFillerService` / `PdfFormFillerService` pattern exactly for consistency
- Output folder should be configurable via `appsettings.json` (same pattern as `PdfSettings:TemplatePath`)
- In Docker, output path must be a mounted volume path
- For list/tabular reports (e.g. sanawy), the controller iterates the parent `Application`'s collection and passes a list of row dictionaries to the service
- `RowNumber` on `Registration` is already present for numbered list rows
