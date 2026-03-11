# Visa2026 — PDF Form Filling: Complete Implementation Reference

> **Purpose of this document:** A comprehensive reference for any developer or AI assistant working on this codebase. It answers *why* each decision was made, *how* every component works, *where* each piece lives, *when* things execute, and documents every known bug, fix, and gotcha discovered during implementation.

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Architecture & Component Map](#2-architecture--component-map)
3. [Technology Stack & Dependencies](#3-technology-stack--dependencies)
4. [The PDF Template: What It Is and Why It Matters](#4-the-pdf-template-what-it-is-and-why-it-matters)
5. [XFA Field Reference (Confirmed from Template)](#5-xfa-field-reference-confirmed-from-template)
6. [Component Deep-Dives](#6-component-deep-dives)
   - 6.1 [IPdfFormFillerService / PdfFormFillerService](#61-ipdfformfillerservice--pdfformfillerservice)
   - 6.2 [PdfMappingHelper](#62-pdfmappinghelper)
   - 6.3 [ApplicationItemPdfController](#63-applicationitempdfcontroller)
   - 6.4 [ApplicationPdfController](#64-applicationpdfcontroller)
   - 6.5 [IFileDownloader](#65-ifiledownloader)
7. [Critical Bugs Found & Fixed](#7-critical-bugs-found--fixed)
8. [Known Limitations & Future Work](#8-known-limitations--future-work)
9. [Configuration](#9-configuration)
10. [Dependency Injection Registration](#10-dependency-injection-registration)
11. [Error Handling Strategy](#11-error-handling-strategy)
12. [Logging Strategy](#12-logging-strategy)
13. [Data Flow: End-to-End Walkthrough](#13-data-flow-end-to-end-walkthrough)
14. [Testing Guidance](#14-testing-guidance)
15. [Deployment Checklist](#15-deployment-checklist)
16. [Decisions Log & Rationale](#16-decisions-log--rationale)

---

## 1. System Overview

### What this feature does

When a user clicks **"Generate PDF"** on an `ApplicationItem` detail view, or **"Download All as PDF"** on an `Application` detail view, the system:

1. Loads a Turkmenistan visa application PDF template (`Visa_Application_TM_QR_08.pdf`).
2. Maps data from the `Application` and `ApplicationItem` business objects into a flat key-value dictionary.
3. Uses **Spire.PDF** to fill the XFA form fields in the template with that data.
4. Optionally merges multiple filled PDFs into one (for the "Download All" action).
5. Streams the resulting PDF to the user's browser as a file download.

### Why this approach

The visa application form is a government-issued **XFA PDF** (Adobe XML Forms Architecture). XFA forms are not simple AcroForms — they are XML-driven, dynamic, and require a library that understands the XFA spec. Spire.PDF was already used in the predecessor project (VISA2014) and supports XFA field manipulation, making it the natural choice.

A dictionary-based data transfer pattern (`Dictionary<string, object>`) was chosen over a typed DTO so that:
- The service layer (`PdfFormFillerService`) has zero dependency on business objects.
- Mapping can be tested or modified independently.
- New fields can be added to the mapping without changing the service contract.

---

## 2. Architecture & Component Map

```
┌─────────────────────────────────┐
│  UI Layer (DevExpress XAF)      │
│                                 │
│  ApplicationItemPdfController   │  ← Single item PDF (DetailView or ListView)
│  ApplicationPdfController       │  ← All items merged PDF (Application DetailView)
└────────────┬────────────────────┘
             │ calls
             ▼
┌─────────────────────────────────┐
│  Service Layer                  │
│                                 │
│  PdfMappingHelper (static)      │  ← Translates BOs → Dictionary<string, object>
│  IPdfFormFillerService          │  ← Interface (for testability)
│  PdfFormFillerService           │  ← Spire.PDF XFA field filling implementation
│  IFileDownloader                │  ← Interface for browser download trigger
└────────────┬────────────────────┘
             │ reads
             ▼
┌─────────────────────────────────┐
│  Resources                      │
│                                 │
│  Visa_Application_TM_QR_08.pdf  │  ← XFA PDF template (embedded resource)
└─────────────────────────────────┘
```

### File locations

| File | Location in project |
|------|---------------------|
| `IPdfFormFillerService.cs` | `Visa2026.Module/Services/` |
| `PdfFormFillerService.cs` | `Visa2026.Module/Services/` |
| `PdfMappingHelper.cs` | `Visa2026.Module/Services/` |
| `IFileDownloader.cs` | `Visa2026.Module/Services/` |
| `ApplicationItemPdfController.cs` | `Visa2026.Module/Controllers/` |
| `ApplicationPdfController.cs` | `Visa2026.Module/Controllers/` |
| `Visa_Application_TM_QR_08.pdf` | `Visa2026.Module/Resources/` |
| Template path config | `appsettings.json` → `PdfSettings:TemplatePath` |

---

## 3. Technology Stack & Dependencies

| Component | Technology | Why |
|-----------|------------|-----|
| PDF manipulation | **Spire.PDF** (NuGet) | Supports XFA form filling; already used in predecessor project |
| UI framework | **DevExpress XAF** | Project-wide standard; controllers integrate via `ViewController` |
| DI container | **Microsoft.Extensions.DependencyInjection** | XAF's default DI mechanism |
| Logging | **Microsoft.Extensions.Logging** (`ILogger<T>`) | Standard .NET logging abstraction; works with Serilog/NLog |
| Configuration | **Microsoft.Extensions.Configuration** (`IConfiguration`) | Template path read from `appsettings.json` |
| Image handling | **System.Drawing** (`Image`, `ImageFormat`) | Required to handle `byte[]` or `Image` objects for the photo field |

### NuGet packages required

```xml
<PackageReference Include="Spire.PDF" Version="..." />
<!-- System.Drawing is included in .NET Framework; for .NET 6+ add: -->
<PackageReference Include="System.Drawing.Common" Version="..." />
```

---

## 4. The PDF Template: What It Is and Why It Matters

**File:** `Visa_Application_TM_QR_08.pdf`  
**Type:** XFA (XML Forms Architecture) PDF — NOT a standard AcroForm.

### What is XFA?

XFA is Adobe's XML-based format for dynamic PDF forms. The entire form definition lives inside the PDF as embedded XML streams. When you open the file in a non-Adobe viewer, you see the message `"Please wait... If this message is not eventually replaced..."` — this is the XFA loading screen that non-Adobe viewers cannot render.

**This is normal. The filled and flattened output PDF will render correctly everywhere.**

### Template location strategy

The template is resolved in this order:
1. Check `appsettings.json` → `PdfSettings:TemplatePath` (relative to `AppContext.BaseDirectory`).
2. If the file doesn't exist on disk, fall back to the **embedded resource** `Visa2026.Module.Resources.Visa_Application_TM_QR_08.pdf`, write it to a temp file, and use that.

**Why embedded resource?** Ensures the template is always bundled with the assembly and cannot be accidentally deleted from the deployment folder.

**Build action for the PDF file:** Set to `Content`, `Copy to Output Directory: Copy if newer`. Also mark as embedded resource in project properties so both strategies work.

### Flattening

After filling, `form.IsFlatten = true` is set before `SaveToStream`. This converts the live XFA form into static rendered content, making it:
- Viewable in all PDF readers (Chrome, Edge, Firefox, Preview, etc.)
- Non-editable (appropriate for a visa application printout)
- Free from the "Please wait..." XFA loading message

---

## 5. XFA Field Reference (Confirmed from Template)

> All field names and types were extracted directly from the XFA `template` XML stream inside `Visa_Application_TM_QR_08.pdf` using pypdf. Total: **75 fields** across 2 pages.

### Page 1 Fields — Currently Mapped

| XFA Key | Form Label | Type | C# Source | Notes |
|---------|-----------|------|-----------|-------|
| `topmostSubform[0].Page1[0].ImageField1[0]` | 1. PHOTO | imageEdit | `person.Photo` | byte[] or Image |
| `topmostSubform[0].Page1[0].L01[0]` | Visa operation type | choiceList | `application.ApplicationType.PdfForm_Code` | |
| `topmostSubform[0].Page1[0].L02[0]` | 3. TIZLIGI (Urgency) | choiceList | `application.Urgency.Name` | ⚠️ Raw values: `'1'`/`'2'`/`'3'` |
| `topmostSubform[0].Page1[0].IP[1].#field[0]` | 4. Legal Entity checkbox | checkButton | `true` (when Company != null) | Unnamed field inside IP[1] subform |
| `topmostSubform[0].Page1[0].L10[0]` | 5. Company name | textEdit | `Company.Name` | |
| `topmostSubform[0].Page1[0].L11[0]` | 6. Company address | textEdit | `Company.Address` | |
| `topmostSubform[0].Page1[0].L12[0]` | INN / tax number | textEdit | `Company.Email` | Mapped to Company Email per request. |
| `topmostSubform[0].Page1[0].L13[0]` | 8. Company phone | textEdit | `Company.PhoneNumber` | |
| `topmostSubform[0].Page1[0]._01[0]` | 9. Last name | textEdit | `person.LastName` | |
| `topmostSubform[0].Page1[0]._03[0]` | 11. First name | textEdit | `person.FirstName` | |
| `topmostSubform[0].Page1[0]._02[0]` | 10. Patronymic | textEdit | `application.Urgency.PdfForm_Code` | Field repurposed for Urgency Code. |
| `topmostSubform[0].Page1[0]._04[0]` | 12. Date of birth | picture | `person.DateOfBirth` | Passed as `DateTime` |
| `topmostSubform[0].Page1[0]._05[0]` | 13. Gender | choiceList | `person.Gender.Name` | Raw = display: `'M'`/`'F'`/`'X'` |
| `topmostSubform[0].Page1[0]._06[0]` | 14. Country of birth | choiceList | `person.CountryOfBirth.Code` | ISO 3166-1 alpha-3 |
| `topmostSubform[0].Page1[0]._07[0]` | 15. Citizenship | choiceList | `person.Nationality.Code` | ISO 3166-1 alpha-3 |
| `topmostSubform[0].Page1[0]._08[0]` | 16. Birth place | textEdit | `person.BirthPlace` | |
| `topmostSubform[0].Page1[0]._09[0]` | 17. Personal/ID number | textEdit | `passport.PersonalNumber` | |
| `topmostSubform[0].Page1[0]._10[0]` | 18. Document type | choiceList | `passport.PassportType.Name` | Resolves to `'P'`, `'PD'`, etc. |
| `topmostSubform[0].Page1[0]._11[0]` | 19. Passport number | textEdit | `passport.PassportNumber` | |
| `topmostSubform[0].Page1[0]._12[0]` | 20. Passport issue date | picture | `passport.IssueDate` | Passed as `DateTime` |
| `topmostSubform[0].Page1[0]._13[0]` | 21. Passport expiry date | picture | `passport.ExpirationDate` | Passed as `DateTime` |
| `topmostSubform[0].Page1[0]._15[0]` | 23. Address of residence | textEdit | `ForeignAddressCountry` + `ForeignAddress` | Combined string |
| `topmostSubform[0].Page1[0]._18[0]` | 25. Marital status | choiceList | `person.MaritalStatus.Name` | ⚠️ Raw values: `'1'`/`'2'`/`'3'`/`'4'` |
| `topmostSubform[0].Page1[0]._19[0]` | 26. Education level | choiceList | `CurrentEducation.EducationLevel.PdfForm_Code` | Raw values: `'1'`-`'5'` |
| `topmostSubform[0].Page1[0]._20[0]` | Specialty | textEdit | `CurrentEducation.Specialty.Name` | |
| `topmostSubform[0].Page1[0]._21[0]` | Education Place | textEdit | `EducationCountry.Name` + `EducationInstitution.Name` | |
| `topmostSubform[0].Page1[0]._22[0]` | Work phone | textEdit | `Company.Name` + `, ` + `Company.PhoneNumber` | Mapped to Work Place and Work Phone Number. |
| `topmostSubform[0].Page1[0]._23[0]` | Work position / job title | textEdit | `CurrentPositionHistory.Position.Code` | |
| `topmostSubform[0].Page1[0]._26[0]` | Visa Category | textEdit | `CurrentVisa.VisaCategory.PdfForm_Code` | |

### Page 2 Fields — Currently Mapped

| XFA Key | Form Label | Type | C# Source | Notes |
|---------|-----------|------|-----------|-------|
| `topmostSubform[0].Page2[0]._25[0]` | 28. Visa category | choiceList | `Application.VisaType` / `CurrentVisa.VisaType` | Application level serves as default. |
| `topmostSubform[0].Page2[0]._27[0]` | Duration of stay | textEdit | `Application.VisaPeriod.PdfForm_Count` | |
| `topmostSubform[0].Page2[0]._271[0]`| Duration unit | choiceList | `Application.VisaPeriod.PdfForm__Code` | Raw: 'GUN', 'AY', 'YYL' |
| `topmostSubform[0].Page2[0]._33[0]` | Region of stay | choiceList | `CurrentAddressOfResidence.Region.PdfForm_Code` | |
| `topmostSubform[0].Page2[0]._34[0]` | District of stay | choiceList | `CurrentAddressOfResidence.City.PdfForm_Code` | |
| `topmostSubform[0].Page2[0]._35[0]` | Stay address | textEdit | `CurrentAddressOfResidence.FullAddress` | |