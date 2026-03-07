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
| `topmostSubform[0].Page1[0].L02[0]` | 3. TIZLIGI (Urgency) | choiceList | `application.Urgency.Name` | ⚠️ Raw values: `'1'`/`'2'`/`'3'` |
| `topmostSubform[0].Page1[0].IP[1].#field[0]` | 4. Legal Entity checkbox | checkButton | `true` (when Company != null) | Unnamed field inside IP[1] subform |
| `topmostSubform[0].Page1[0].L10[0]` | 5. Company name | textEdit | `Company.Name` | |
| `topmostSubform[0].Page1[0].L11[0]` | 6. Company address | textEdit | `Company.Address` | |
| `topmostSubform[0].Page1[0].L13[0]` | 8. Company phone | textEdit | `Company.PhoneNumber` | |
| `topmostSubform[0].Page1[0]._01[0]` | 9. Last name | textEdit | `person.LastName` | |
| `topmostSubform[0].Page1[0]._03[0]` | 11. First name | textEdit | `person.FirstName` | |
| `topmostSubform[0].Page1[0]._04[0]` | 12. Date of birth | picture | `person.DateOfBirth` | ⚠️ Pass as `DateTime` |
| `topmostSubform[0].Page1[0]._05[0]` | 13. Gender | choiceList | `person.Gender.Name` | Raw = display: `'M'`/`'F'`/`'X'` |
| `topmostSubform[0].Page1[0]._08[0]` | 16. Birth place | textEdit | `person.BirthPlace` | |
| `topmostSubform[0].Page1[0]._09[0]` | 17. Personal/ID number | textEdit | `passport.PersonalNumber` | |
| `topmostSubform[0].Page1[0]._11[0]` | 19. Passport number | textEdit | `passport.PassportNumber` | |
| `topmostSubform[0].Page1[0]._12[0]` | 20. Passport issue date | picture | `passport.IssueDate` | ⚠️ Pass as `DateTime` |
| `topmostSubform[0].Page1[0]._13[0]` | 21. Passport expiry date | picture | `passport.ExpirationDate` | ⚠️ Pass as `DateTime` |
| `topmostSubform[0].Page1[0]._15[0]` | 23. Address of residence | textEdit | `CurrentAddressOfResidence.FullAddress` | |
| `topmostSubform[0].Page1[0]._18[0]` | 25. Marital status | choiceList | `person.MaritalStatus.Name` | ⚠️ Raw values: `'1'`/`'2'`/`'3'`/`'4'` |

### Page 1 Fields — NOT YET MAPPED (candidates for future implementation)

| XFA Key | Form Label | Type | Valid Raw Values |
|---------|-----------|------|-----------------|
| `Page1[0].L01[0]` | Visa operation type | choiceList | `'1'`–`'25'` (see field reference) |
| `Page1[0].IP[0]` | Natural person checkbox | checkButton | `'P'` |
| `Page1[0]._02[0]` | 10. Patronymic | textEdit | free text |
| `Page1[0]._06[0]` | 14. Country of birth | choiceList | ISO 3166-1 alpha-3 (e.g. `'TKM'`) |
| `Page1[0]._07[0]` | 15. Citizenship | choiceList | ISO 3166-1 alpha-3 |
| `Page1[0]._10[0]` | 18. Document type | choiceList | `'P'`, `'APD'`, `'PD'`, etc. |
| `Page1[0]._14[0]` | 22. Country of residence | choiceList | ISO 3166-1 alpha-3 |
| `Page1[0]._17[0]` | 24. Profession | textEdit | free text |
| `Page1[0]._18[0]` marital | 25. Marital status | choiceList | `'1'` Single, `'2'` Married, `'3'` Divorced, `'4'` Widowed |
| `Page1[0]._19[0]` | 26. Education level | choiceList | `'1'` Secondary, `'2'` Higher, `'3'` Student, `'4'` Pre-school, `'5'` Vocational |
| `Page1[0]._181[0]` | Spouse last name | textEdit | free text |
| `Page1[0]._182[0]` | Spouse first name | textEdit | free text |
| `Page1[0].L12[0]` | INN / tax number | textEdit | free text |

### Page 2 Fields — NOT YET MAPPED

| XFA Key | Form Label | Type | Valid Raw Values |
|---------|-----------|------|-----------------|
| `Page2[0]._25[0]` | 28. Visa category | choiceList | `'BS1'`,`'TU'`,`'ST'`,`'OF'`,`'DP'`,`'WP'`, etc. |
| `Page2[0]._26[0]` | 29. Number of entries | choiceList | `'1'` single, `'2'` double, `'4'` multiple |
| `Page2[0]._27[0]` | Duration of stay | textEdit | numeric string |
| `Page2[0]._271[0]` | Duration unit | choiceList | `'GUN'` days, `'AY'` months, `'YYL'` years |
| `Page2[0]._28[0]` | Visa valid to | picture | `"dd.MM.yyyy"` string |
| `Page2[0]._29[0]` | Visa valid from | picture | `"dd.MM.yyyy"` string |
| `Page2[0]._33[0]` | Region of stay | choiceList | `'AS'`,`'AH'`,`'MR'`,`'LB'`,`'DZ'`,`'BN'` |
| `Page2[0]._34[0]` | District of stay | choiceList | Region+code e.g. `'AS69'` |
| `Page2[0]._35[0]` | Stay address | textEdit | free text |
| `Page2[0]._40[0]` | Planned arrival date | textEdit | free text |
| `Page2[0]._41[0]` | Planned departure date | textEdit | free text |
| `Page2[0]._43[0]` | 31. Entry border point | choiceList | `'ASAP'`,`'SERA'`,`'FRPA'`, etc. |
| `Page2[0]._44[0]` | 32. Exit border point | choiceList | same codes as `_43` |
| `Page2[0]._42[0]` | Purpose of visit | textEdit | free text |
| `Page2[0]._36[0]`–`_39[0]` | Previous visit details | textEdit | free text |
| `Page2[0]._46[0]`–`_50[0]` | Accompanying persons | textEdit / choiceList | free text / country codes |
| `Page2[0].QRText[0]` | QR code text | textEdit | auto-generated by form script |

### choiceList Raw Value Reference

XFA `choiceList` fields have **two separate value arrays** in the XML: display labels and raw codes. Spire's `XfaChoiceListField.SelectedItem` requires the **raw code**. Passing the display label results in a silently blank field.

| Field | Display → Raw mapping |
|-------|-----------------------|
| Urgency `L02` | `'ADATY '` → `'1'`, `'TIZ'` → `'2'`, `'ORAN TIZ'` → `'3'` |
| Marital `_18` | `'Sallah...'` → `'1'`, `'Öýlenen...'` → `'2'`, `'Aýrylyşan'` → `'3'`, `'Dul'` → `'4'` |
| Gender `_05` | `'M'` → `'M'`, `'F'` → `'F'`, `'X'` → `'X'` (identical) |
| Education `_19` | `'ORTA'` → `'5'`, `'YOKARY'` → `'2'`, `'MEKDEP OKUWCYSY'` → `'3'`, `'MEKDEP YASYNA YETMEDIK'` → `'4'`, `'YORITE ORTA'` → `'1'` |
| Entries `_26` | `'1x'` → `'1'`, `'2x'` → `'2'`, `'Kx'` → `'4'` |
| Duration unit `_271` | `'Gün'` → `'GUN'`, `'Aý'` → `'AY'`, `'Ýyl'` → `'YYL'` |
| Region `_33` | `'Ashgabat'` → `'AS'`, `'Ahal'` → `'AH'`, `'Mary'` → `'MR'`, `'Lebap'` → `'LB'`, `'Dashoguz'` → `'DZ'`, `'Balkan'` → `'BN'` |

---

## 6. Component Deep-Dives

### 6.1 IPdfFormFillerService / PdfFormFillerService

**Where:** `Visa2026.Module/Services/`

**What it does:** The single responsibility of this service is to take a PDF template path, an output stream, and a pre-built data dictionary, then fill the XFA form and write the result to the stream. It knows nothing about business objects.

**Interface contract:**
```csharp
void FillForm(string templatePath, Stream outputStream, Dictionary<string, object> data);
void MergePdfs(Stream[] sources, Stream outputStream);
```

**How FillForm works internally:**

1. Validates inputs (null checks, file existence).
2. Opens the template with `PdfDocument.LoadFromFile()`.
3. Casts `pdfdoc.Form` to `PdfFormWidget` and checks for `XFAForm`.
4. Iterates `form.XFAForm.XfaFields` and for each field, looks up the field name in the data dictionary.
5. Dispatches to the correct Spire type handler:
   - `XfaTextField` → `.Value = value.ToString()`
   - `XfaDateTimeField` → `.Value = dt.ToString("dd.MM.yyyy")` (handles `DateTime` values)
   - `XfaCheckButtonField` → `.Checked = bool`
   - `XfaChoiceListField` → `.SelectedItem = value.ToString()` (**must be raw code**)
   - `XfaImageField` → see image handling below
6. Sets `form.IsFlatten = true` to flatten the XFA into static content.
7. Saves to `outputStream`.

**Critical: Image field handling**

The image field (`imageEdit` type) requires special handling because `Image.FromStream()` keeps a lazy reference to the stream — if the stream is disposed before `SaveToStream()` runs, the image silently fails or renders blank.

Solution: all image `MemoryStream` instances are added to a `streamsToDispose` list and disposed only in the `finally` block, **after** `SaveToStream()` completes.

```csharp
// WRONG — stream disposed before Save
using (var ms = new MemoryStream(imgBytes)) {
    imageField.Image = Image.FromStream(ms); // ms disposed immediately!
}

// CORRECT — stream kept alive until after Save
var imgStream = new MemoryStream(imageBytes);
streamsToDispose.Add(imgStream);            // disposed in finally block
imageField.Image = Image.FromStream(imgStream);
```

Additionally, `Image` objects are always converted to `byte[]` via a temporary `MemoryStream` before creating the long-lived stream. This prevents issues where the source `Image` itself is backed by an already-closed stream (common when loaded from a database blob).

**How MergePdfs works:**

Uses `PdfDocument.MergeFiles(Stream[])` which is Spire's built-in merge API. Each source stream must be at position 0 before calling (callers are responsible for this).

---

### 6.2 PdfMappingHelper

**Where:** `Visa2026.Module/Services/PdfMappingHelper.cs`  
**Access:** `internal static` — not part of the public API, used only by controllers.

**What it does:** Translates `Application` and `ApplicationItem` business objects into the `Dictionary<string, object>` format that `PdfFormFillerService.FillForm()` consumes. All XFA field key strings live here — nowhere else.

**Why static?** It has no state and no dependencies. Making it injectable would add unnecessary complexity for a pure mapping function.

**The `ILogger logger = null` parameter:** Optional logger passed from the calling controller. Allows the mapping step to emit `Debug` logs showing every field→value assignment without making the helper a registered service.

**choiceList raw value resolution:** Three lookup tables (`UrgencyRawValues`, `GenderRawValues`, `MaritalStatusRawValues`) map display-name strings to raw XFA codes. The `ResolveRawValue()` helper logs a warning if the incoming value isn't in the table, then passes it through as-is (best-effort) rather than throwing.

**Date formatting:** All date fields in this PDF are typed `picture` in the XFA XML (not `dateTimeEdit`). Spire may expose them as `XfaTextField` rather than `XfaDateTimeField`. To handle both cases, dates are passed as `DateTime` objects to the dictionary. The `PdfFormFillerService` handles the formatting to `"dd.MM.yyyy"` strings.

**Adding new field mappings:** Add a new `const string key = "topmostSubform[0]..."` and a `data[key] = ...` line in the appropriate section. Always consult the XFA Field Reference table in Section 5 for the correct key and type.

---

### 6.3 ApplicationItemPdfController

**Where:** `Visa2026.Module/Controllers/`  
**Target:** `ApplicationItem` objects, `ViewType.Any` (works on both detail and list views).  
**Action:** "Generate PDF" button in the "View" action container.

**Execution flow:**
1. Gets `templatePath` from config; falls back to embedded resource if file missing.
2. Calls `PdfMappingHelper.MapApplicationData()` to build the dictionary.
3. Calls `pdfFillerService.FillForm()` into a `MemoryStream`.
4. Resets stream to position 0, then calls `fileDownloader.DownloadAsync()`.
5. Shows a success/error message via `Application.ShowViewStrategy.ShowMessage()`.

**File naming convention:** `Visa_{FirstName}_{LastName}_{yyyyMMddHHmmss}.pdf`

**Error handling:** Wraps the generation block in `try/catch` and re-throws as `UserFriendlyException` so DevExpress XAF displays a readable error dialog to the user rather than a raw stack trace.

---

### 6.4 ApplicationPdfController

**Where:** `Visa2026.Module/Controllers/`  
**Target:** `Application` objects, `DetailView` only.  
**Action:** "Download All as PDF" button in the "View" action container.

**Execution flow:**
1. Validates that the application has at least one non-deleted `ApplicationItem`.
2. For each active `ApplicationItem`, fills a separate PDF into its own `MemoryStream`.
3. Resets all streams to position 0.
4. Calls `pdfFillerService.MergePdfs()` to combine all filled PDFs into one merged stream.
5. Downloads the merged stream as a single file.
6. Disposes all individual streams in a `finally` block.

**File naming convention:** `Application_{FullApplicationNumber}_{yyyyMMddHHmmss}.pdf`

**Why merge instead of zip?** A single PDF is more practical for printing and submission than a zip of individual files.

**Memory note:** All streams are held in memory simultaneously. For applications with very large numbers of items (50+), consider writing individual PDFs to temp files on disk instead.

---

### 6.5 IFileDownloader

**Where:** `Visa2026.Module/Services/IFileDownloader.cs`

**What it does:** Abstracts the mechanism of triggering a file download in the user's browser. The implementation is platform-specific (Blazor vs WebForms vs WinForms) and registered via DI, so the controllers stay portable.

```csharp
Task DownloadAsync(string fileName, Stream stream, string contentType = "application/pdf");
```

**Why an interface?** XAF applications can run as Blazor Server or as Windows Forms. The actual download mechanism differs between platforms — this interface hides that difference from the PDF controllers.

---

## 7. Critical Bugs Found & Fixed

This section documents every non-obvious bug discovered during implementation. An AI working on this codebase should check here before diagnosing similar symptoms.

---

### Bug 1: Image field renders blank — MemoryStream disposed too early

**Symptom:** Photo does not appear in the generated PDF even though the data was mapped correctly.

**Root cause:** `Image.FromStream()` keeps a lazy reference to the source stream for pixel data. Wrapping the stream in `using` disposes it before `SaveToStream()` can read the pixel data.

**Fix:** Store image streams in a `List<MemoryStream> streamsToDispose` and dispose them only in the `finally` block after `SaveToStream()`.

**File:** `PdfFormFillerService.cs`

---

### Bug 2: choiceList fields (Urgency, Marital Status) render blank

**Symptom:** Urgency and marital status fields are empty in the PDF despite being set in the dictionary.

**Root cause:** XFA `choiceList` fields have two arrays internally: display labels and raw codes. `XfaChoiceListField.SelectedItem` requires the **raw code** (`'1'`, `'2'`, etc.), not the display label (`'ADATY'`, `'Öýlenen...'`).

**Fix:** Added lookup tables `UrgencyRawValues` and `MaritalStatusRawValues` in `PdfMappingHelper` with a `ResolveRawValue()` helper.

**File:** `PdfMappingHelper.cs`

---

### Bug 3: Date fields render blank — `picture` type vs `dateTimeEdit`

**Symptom:** Date of birth, passport issue date, and passport expiry date are empty in the generated PDF.

**Root cause:** These fields are typed `picture` in the XFA XML template, not `dateTimeEdit`. Spire resolves them as `XfaTextField` rather than `XfaDateTimeField`. Passing a `DateTime` object falls through without matching any `if` branch in the dispatcher, so nothing gets written.

**Fix:** Updated `PdfFormFillerService` to check for `DateTime` values in `XfaTextField` and format them as `"dd.MM.yyyy"`.

**File:** `PdfFormFillerService.cs`

---

### Bug 4: CS1503 — Cannot convert `DateTime?` to `DateTime`

**Symptom:** Compiler error on `passport.IssueDate` — `DateTime?` cannot be passed to `FormatDate(DateTime dt)`.

**Root cause:** `IssueDate` is nullable (`DateTime?`) in the `Passport` business object, but the original code treated it as non-nullable.

**Fix:** Changed the guard from `if (passport.IssueDate != DateTime.MinValue)` to `if (passport.IssueDate.HasValue && passport.IssueDate.Value != DateTime.MinValue)` and passed `.Value` to `FormatDate()`.

**File:** `PdfMappingHelper.cs`

---

### Bug 5: PDF shows "Please wait..." in browsers

**Symptom:** The downloaded PDF shows the Adobe XFA loading screen instead of the form content when opened in Chrome, Edge, or Firefox.

**Root cause:** XFA PDFs require Adobe Reader to render dynamically. Other viewers cannot execute the XFA XML.

**Fix:** Set `form.IsFlatten = true` before `SaveToStream()`. This flattens the XFA layer into static PDF content that any viewer can render.

**File:** `PdfFormFillerService.cs`

---

## 8. Known Limitations & Future Work

### Unmapped fields

The following fields exist in the PDF template but are not yet mapped. They are candidates for future implementation when the corresponding business object properties become available:

- **Page 1:** Patronymic (`_02`), citizenship (`_07`), country of birth (`_06`), country of residence (`_14`), document type (`_10`), education level (`_19`), profession (`_17`), spouse details (`_181`, `_182`, `_183`), INN/tax number (`L12`), natural person checkbox (`IP[0]`), visa operation type (`L01`)
- **Page 2:** All Page 2 fields — visa category, number of entries, stay duration, destination region/district, border crossing points, planned dates, previous visits, accompanying persons.

### QR Code field

`Page2[0].QRText[0]` appears to be auto-populated by an embedded JavaScript in the original XFA form. When the form is filled and flattened, this script does not execute. Generating the QR code value server-side and writing it to this field manually would require knowing the exact QR data format used by the Turkmenistan visa system.

### Memory usage

`ApplicationPdfController` holds all individual PDF streams in memory simultaneously before merging. For applications with many items, this could consume significant RAM. A future improvement would be to use temp files on disk.

### Spire.PDF free version limitations

The free version of Spire.PDF adds a watermark to generated documents. Ensure the licensed version is used in production.

---

## 9. Configuration

### appsettings.json

```json
{
  "PdfSettings": {
    "TemplatePath": "Resources/Visa_Application_TM_QR_08.pdf"
  },
  "Logging": {
    "LogLevel": {
      "Visa2026.Module.Services": "Debug"
    }
  }
}
```

`TemplatePath` is relative to `AppContext.BaseDirectory` (the output directory). If the file is not found there, the system falls back to the embedded resource automatically.

**To enable verbose PDF debug logging** (field-by-field mapping output), set `Visa2026.Module.Services` log level to `Debug`. In production, set to `Warning` or `Error` to avoid log noise.

---

## 10. Dependency Injection Registration

Register in `Startup.cs` / `Program.cs` or your XAF module's DI configuration:

```csharp
services.AddScoped<IPdfFormFillerService, PdfFormFillerService>();
services.AddScoped<IFileDownloader, YourConcreteFileDownloader>();
```

`PdfMappingHelper` is `internal static` — **do not register it**. It is called directly as `PdfMappingHelper.MapApplicationData(...)`.

---

## 11. Error Handling Strategy

| Exception Type | Where caught | Action |
|---------------|--------------|--------|
| `FileNotFoundException` (template missing) | `PdfFormFillerService.FillForm()` | Logs error, re-throws |
| `InvalidOperationException` (no form in PDF) | `PdfFormFillerService.FillForm()` | Logs error, re-throws |
| Field-level exception (single field fails) | Inner `try/catch` in field loop | Logs error, **continues** filling remaining fields |
| `Exception` (image decode failure) | `PdfFormFillerService.FillForm()` | Logs error with hex header of bytes, re-throws |
| Any exception in controller | Controller `try/catch` | Wrapped as `UserFriendlyException` → shown in XAF dialog |

**Key design decision:** Field-level exceptions are caught and logged individually rather than aborting the entire fill operation. This means a single bad field (e.g., a corrupt photo) does not prevent the rest of the form from being filled. The caller still gets a (partially filled) PDF.

---

## 12. Logging Strategy

All logging uses structured logging (`{FieldName}`, `{Value}`, etc.) compatible with Serilog, NLog, or any `ILogger` sink.

### Log levels used

| Level | When |
|-------|------|
| `LogDebug` | Every field assignment (key, value, type), image dimensions, stream state, field list on entry, pre-save state |
| `LogInformation` | Successful completion of `FillForm` and `MergePdfs` |
| `LogWarning` | Skipped fields (null value, MinValue date, unrecognised choiceList value), empty image payload, non-XFA form |
| `LogError` | Exceptions (template missing, form missing, image decode failure, unexpected errors) |

### What to look for when debugging a blank field

1. Set log level to `Debug`.
2. Look for `"XFA field names in template: [...]"` — confirms the template loaded and lists all fields with their Spire types.
3. Look for `"Filling field '{FieldName}'"` — confirms the data key matched a template field.
4. If the field appears in step 2 but not step 3, the dictionary key doesn't match the template key exactly (check spelling, case, index numbers).
5. If the field appears in step 3 but is blank in output, check the Spire type. If it says `XfaTextField` but you're passing a `DateTime`, the value is silently ignored — pre-format to string.
6. For choiceList fields, look for `"resolved display '...' → raw '...'"` debug entries or `"has no known raw mapping"` warnings.

---

## 13. Data Flow: End-to-End Walkthrough

```
User clicks "Generate PDF" on ApplicationItem
    │
    ▼
ApplicationItemPdfController.GeneratePdfAction_Execute()
    │
    ├─ Reads templatePath from IConfiguration
    ├─ Resolves template file (disk → embedded resource fallback)
    ├─ Gets IPdfFormFillerService from DI
    │
    ├─ Calls PdfMappingHelper.MapApplicationData(data, application, item, _logger)
    │       │
    │       ├─ Maps application.Urgency.Name → resolved raw code → data["L02[0]"]
    │       ├─ Maps Company fields → data["L10[0]"], ["L11[0]"], ["L13[0]"]
    │       ├─ Maps person fields (name, DOB as string, gender raw, birthplace)
    │       ├─ Maps person.Photo (byte[]) → data["ImageField1[0]"]
    │       ├─ Maps marital status → resolved raw code → data["_18[0]"]
    │       ├─ Maps passport fields (numbers, dates as strings)
    │       └─ Maps address
    │
    ├─ Creates MemoryStream
    ├─ Calls pdfFillerService.FillForm(templatePath, memoryStream, data)
    │       │
    │       ├─ Loads PDF with Spire
    │       ├─ Iterates XfaFields (75 total)
    │       │   ├─ Matches each field.Name against data dictionary
    │       │   ├─ Dispatches to correct type handler
    │       │   └─ Image fields: uses long-lived MemoryStream (not using{})
    │       ├─ form.IsFlatten = true
    │       └─ SaveToStream → writes filled+flattened PDF bytes to memoryStream
    │
    ├─ memoryStream.Position = 0
    ├─ Calls fileDownloader.DownloadAsync(fileName, memoryStream)
    │       └─ Triggers browser file download
    │
    └─ ShowMessage("PDF Generated and downloaded: ...")
```

---

## 14. Testing Guidance

### Unit tests — PdfMappingHelper

Test that each field is mapped to the correct dictionary key, and that choiceList raw value resolution works:

```csharp
// Urgency raw value resolution
var data = new Dictionary<string, object>();
var app = new Application { Urgency = new Urgency { Name = "ADATY" } };
PdfMappingHelper.MapApplicationData(data, app, item);
Assert.Equal("1", data["topmostSubform[0].Page1[0].L02[0]"]);

// Date pre-formatting
Assert.Equal("15.06.1990", data["topmostSubform[0].Page1[0]._04[0]"]);

// Null safety
var appNoCompany = new Application { Company = null };
// Should not throw, company keys should not exist in dictionary
PdfMappingHelper.MapApplicationData(data, appNoCompany, item);
Assert.False(data.ContainsKey("topmostSubform[0].Page1[0].L10[0]"));
```

### Integration tests — PdfFormFillerService

Test with the actual template file:

```csharp
// Verify filled PDF is non-empty and not the XFA placeholder
var data = new Dictionary<string, object> {
    ["topmostSubform[0].Page1[0]._01[0]"] = "SMITH",
    ["topmostSubform[0].Page1[0]._03[0]"] = "JOHN"
};
using var output = new MemoryStream();
service.FillForm(templatePath, output, data);
Assert.True(output.Length > 1000); // non-trivial output
```

### Manual smoke test checklist

After any change to `PdfMappingHelper` or `PdfFormFillerService`:

- [ ] Generate a PDF for an item with all fields populated — verify all text fields show.
- [ ] Check date fields show as `dd.MM.yyyy` format.
- [ ] Check photo appears in the image box.
- [ ] Check urgency dropdown shows correct label (not blank, not a number).
- [ ] Check marital status dropdown shows correct label.
- [ ] Open PDF in Chrome/Edge (not Adobe Reader) — verify no "Please wait..." message.
- [ ] Generate "Download All" with 2+ items — verify merged PDF has correct number of pages.

---

## 15. Deployment Checklist

- [ ] `Visa_Application_TM_QR_08.pdf` is in `Visa2026.Module/Resources/`
- [ ] PDF file build action is **Content** + **Copy if newer** AND marked as **Embedded Resource**
- [ ] `appsettings.json` has `PdfSettings:TemplatePath` set
- [ ] `IPdfFormFillerService` and `IFileDownloader` are registered in DI
- [ ] Licensed version of Spire.PDF is used (free version adds watermark)
- [ ] `System.Drawing.Common` NuGet package is included (required for .NET 6+)
- [ ] Log level for `Visa2026.Module.Services` is set to `Warning` or higher in production

---

## 16. Decisions Log & Rationale

| Decision | Rationale |
|----------|-----------|
| Use Spire.PDF | Already used in predecessor project; supports XFA; commercial license available |
| Dictionary over typed DTO | Keeps `PdfFormFillerService` decoupled from business objects; easier to extend |
| `PdfMappingHelper` as `internal static` | Pure mapping function; no state; no reason to register in DI |
| Per-field try/catch | A bad photo or corrupt field value should not prevent the rest of the form from being filled |
| Pre-format dates as strings | XFA `picture` fields are resolved as `XfaTextField` by Spire; passing `DateTime` is silently ignored |
| Raw code lookup tables | XFA `choiceList` requires internal codes not display labels; lookup tables make the mapping explicit and debuggable |
| `form.IsFlatten = true` | Produces a static PDF readable by all viewers; required for browser compatibility |
| Image stream not in `using` | `Image.FromStream` keeps lazy reference to stream; disposing before `SaveToStream` causes blank image |
| Embedded resource fallback | Ensures PDF template is always available even if the `Resources/` copy is missing from deployment |
| `UserFriendlyException` in controllers | DevExpress XAF displays these as readable error dialogs; raw exceptions show ugly stack traces |
| `SelectionDependencyType.RequireSingleObject` | "Generate PDF" action only makes sense for a single selected item |