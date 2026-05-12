# Word Report Generation — Design Idea

**Date:** 2026-05-12  
**Status:** Idea / Not yet implemented

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
    void FillForm(string templatePath, Stream outputStream, Dictionary<string, object> data);
    // overload for collections (list reports):
    void FillForm(string templatePath, Stream outputStream, IEnumerable<Dictionary<string, object>> rows);
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

## Files to Create (when implementing)

| File | Role |
|---|---|
| `Visa2026.Module/Services/IWordFormFillerService.cs` | Interface |
| `Visa2026.Module/Services/WordFormFillerService.cs` | Implementation |
| `Visa2026.Module/Controllers/ApplicationWordController.cs` | XAF action for `Application` |
| `Visa2026.Module/Controllers/ApplicationItemWordController.cs` | XAF action for `ApplicationItem` |
| `Visa2026.Module/Controllers/RegistrationWordController.cs` | XAF action for `Registration` |
| `Visa2026.Module/Controllers/BusinessTripWordController.cs` | XAF action for `BusinessTrip` |
| `Visa2026.Module/Resources/*.docx` | Word templates (to be authored per report type) |
| Add NuGet `DocxTemplater` or `DocumentFormat.OpenXml` to `Visa2026.Module.csproj` | Dependency |

---

## Notes

- Do NOT modify `ShowMailMergeController`, `MailMergeUpdater`, `RichTextMailMergeData`, or any existing Mail Merge infrastructure
- Follow the `IPdfFormFillerService` / `PdfFormFillerService` pattern exactly for consistency
- Output folder should be configurable via `appsettings.json` (same pattern as `PdfSettings:TemplatePath`)
- In Docker, output path must be a mounted volume path
- For list/tabular reports (e.g. sanawy), the controller iterates the parent `Application`'s collection and passes a list of row dictionaries to the service
- `RowNumber` on `Registration` is already present for numbered list rows
