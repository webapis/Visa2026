# Report Conventions

This document defines the conventions for all XtraReports in this project. Follow these guidelines when creating or modifying reports to ensure consistency.

---

## Existing Reports

| Class | Registered Name | Target Type | File |
|---|---|---|---|
| `AppBaseReport` | *(base — not registered)* | `Application` | `AppBaseReport.Designer.cs` |
| `AppItemBaseReport` | *(base — not registered)* | `ApplicationItem` | `AppItemBaseReport.Designer.cs` |
| `AppRegBaseReport` | *(base — not registered)* | `Registration` | `AppRegBaseReport.Designer.cs` |
| `RegistrationListReport` | Registration List Report | `Registration` | `RegistrationListReport.Designer.cs` |
| `AppBorderZonePermissionItemReport` | App Border Zone Permission Item Report | `ApplicationItem` | `AppBorderZonePermissionItemReport.Designer.cs` |
| `AppCancelInvWPItemReport` | App Cancel Inv WP Item Report | `ApplicationItem` | `AppCancelInvWPItemReport.Designer.cs` |

---

## Data Sources

Reports are generated against one of three business object types. Each type exposes flattened `[NotMapped]` properties for use as report bindings. **Always use these flattened properties — do not use deep navigation paths directly in the report.**

---

### `Registration`

Used for: lists of foreign nationals per application (e.g., Registration List Report).

| Property | Value |
|---|---|
| `Person_FullName` | Person full name |
| `Person_BirthPlace` | Birth place |
| `Person_DateOfBirthText` | Date of birth (dd.MM.yyyy) |
| `Person_GenderTm` | Gender (Turkmen) |
| `Person_MaritalStatusTm` | Marital status (Turkmen) |
| `Person_NationalityCode` | Nationality code |
| `Person_NationalityTm` | Nationality (Turkmen) |
| `Person_CountryOfBirthCode` | Country of birth code |
| `Person_CountryOfBirthTm` | Country of birth (Turkmen) |
| `Person_CompanyName` | Company name |
| `Person_CompanyAddress` | Company address |
| `Person_ForeignAddress` | Foreign address |
| `Person_ForeignAddressCountryCode` | Foreign address country code |
| `Person_ForeignAddressCountryTm` | Foreign address country (Turkmen) |
| `Passport_Number` | Passport number |
| `Passport_ExpirationDateText` | Passport expiry (dd.MM.yyyy) |
| `Passport_CountryCode` | Passport issuing country code |
| `Passport_CountryTm` | Passport issuing country (Turkmen) |
| `Visa_Number` | Visa number |
| `Visa_IssueDateText` | Visa issue date (dd.MM.yyyy) |
| `Visa_StartDateText` | Visa start date (dd.MM.yyyy) |
| `Visa_ExpirationDateText` | Visa expiry (dd.MM.yyyy) |
| `Visa_IssuedPlaceTm` | Visa issued place (Turkmen) |
| `Visa_CategoryTm` | Visa category (Turkmen) |
| `Visa_TypeTm` | Visa type (Turkmen) |
| `Travel_Date` | Travel date |
| `Travel_DateText` | Travel date (dd.MM.yyyy) |
| `Travel_PurposeOfTravelTm` | Purpose of travel (Turkmen) |
| `Travel_CheckPointTm` | Checkpoint (Turkmen) |
| `Address_FullAddress` | Full address of residence |
| `Address_RegionTm` | Region (Turkmen) |
| `Address_CityTm` | City (Turkmen) |
| `Position_PositionTm` | Job position (Turkmen) |
| `Position_DepartmentTm` | Department (Turkmen) |
| `Application_FullNumber` | Application number |
| `Application_DateText` | Application date (dd.MM.yyyy) |
| `Application_RegistrationDateText` | Registration date (dd.MM.yyyy) |
| `CompanyHead_PositionTm` | Signatory position (Turkmen) |
| `CompanyHead_FullName` | Signatory full name |

**Signatory path:** `Person?.Company?.CurrentAuthorizedSignatory`

---

### `ApplicationItem`

Used for: per-person reports within a visa/work permit application.

| Property | Value |
|---|---|
| `Person_FullName` | Person full name |
| `Person_BirthPlace` | Birth place |
| `Person_DateOfBirthText` | Date of birth (dd.MM.yyyy) |
| `Person_NationalityCode` | Nationality code |
| `Person_NationalityTm` | Nationality (Turkmen) |
| `Person_CountryOfBirthCode` | Country of birth code |
| `Person_CountryOfBirthTm` | Country of birth (Turkmen) |
| `Person_ForeignAddress` | Foreign address |
| `Person_Photo` | Photo (byte[]) |
| `Passport_Number` | Passport number |
| `Passport_PersonalNumber` | Personal number |
| `Passport_Authority` | Issuing authority |
| `Passport_IssueDateText` | Issue date (dd.MM.yyyy) |
| `Passport_ExpirationDateText` | Expiry (dd.MM.yyyy) |
| `Passport_CountryCode` | Issuing country code |
| `Passport_CountryTm` | Issuing country (Turkmen) |
| `PreviousPassport_Number` | Previous passport number |
| `PreviousPassport_PersonalNumber` | Previous personal number |
| `PreviousPassport_IssueDateText` | Previous issue date (dd.MM.yyyy) |
| `PreviousPassport_ExpirationDateText` | Previous expiry (dd.MM.yyyy) |
| `PreviousPassport_CountryCode` | Previous issuing country code |
| `PreviousPassport_CountryTm` | Previous issuing country (Turkmen) |
| `Visa_Number` | Visa number |
| `Visa_IssueDateText` | Issue date (dd.MM.yyyy) |
| `Visa_StartDateText` | Start date (dd.MM.yyyy) |
| `Visa_ExpirationDateText` | Expiry (dd.MM.yyyy) |
| `Visa_IssuedPlaceTm` | Issued place (Turkmen) |
| `Visa_CategoryTm` | Visa category (Turkmen) |
| `Visa_TypeTm` | Visa type (Turkmen) |
| `Address_FullAddress` | Full address of residence |
| `Address_RegionTm` | Region (Turkmen) |
| `Address_CityTm` | City (Turkmen) |
| `Address_StartDateText` | Address start date (dd.MM.yyyy) |
| `Address_ExpirationDateText` | Address expiry (dd.MM.yyyy) |
| `Position_PositionTm` | Job position (Turkmen) |
| `Position_DepartmentTm` | Department (Turkmen) |
| `Contract_SalaryText` | Salary (formatted) |
| `WorkPermit_Number` | Work permit number |
| `WorkPermit_ASNumber` | Work permit AS number (authorization reference) |
| `WorkPermit_StartDateText` | Work permit start date (dd.MM.yyyy) |
| `WorkPermit_ExpirationDateText` | Work permit expiry (dd.MM.yyyy) |
| `WorkPermit_WorkPermittedLocations` | Comma-joined permitted work cities |
| `Invitation_Number` | Invitation number |
| `Invitation_StartDateText` | Invitation start date (dd.MM.yyyy) |
| `Invitation_ExpirationDateText` | Invitation expiry (dd.MM.yyyy) |
| `MedicalRecord_Number` | Medical record number |
| `MedicalRecord_ExpirationDateText` | Medical record expiry (dd.MM.yyyy) |
| `Education_GraduationYear` | Graduation year |
| `Application_FullNumber` | Application number |
| `Application_DateText` | Application date (dd.MM.yyyy) |
| `Application_SponsorName` | Company name |
| `CompanyHead_FullName` | Signatory full name |
| `CompanyHead_PositionTm` | Signatory position (Turkmen) |

**Signatory path:** `Application?.CompanyHead` (stored directly on Application)

---

### `Application`

Used for: application-level reports (letters, summaries).

Key fields available directly on `Application`:

| Property | Value |
|---|---|
| `FullApplicationNumber` | Full application number |
| `ApplicationDate` | Application date |
| `ApplicationType.Name` | Application type name |
| `Company.Name` | Company name |
| `CompanyHead.FullName` | Signatory full name |
| `CompanyHead.Position.NameTm` | Signatory position (Turkmen) |
| `Representative.FullName` | Representative full name |
| `MigrationService_NameTm` | Migration service name (Turkmen) |
| `TotalPersonCount` | Total number of persons |
| `TotalPersonCountText` | Total persons in Turkmen words |
| `FromCityName` / `ToCityName` | Internal movement cities |
| `ExpirationDate` | Application expiration date |

> For `Application`-based reports, add flattened `[NotMapped]` properties directly to `Application.cs` following the same pattern used in `ApplicationItem.cs`.

**Signatory path:** `Company?.CurrentAuthorizedSignatory` (auto-populated into `CompanyHead` on company selection)

---

## Page Setup

All reports use **A4** paper with `DXMargins(20F, 20F, 50F, 60F)` (Left, Right, Top, Bottom).

**A4 Portrait** — used by `AppBaseReport` and `AppItemBaseReport` (Application and ApplicationItem reports):

```csharp
this.Landscape = false;
this.PageWidthF  = 826.7717F;
this.PageHeightF = 1169.291F;
this.PaperKind = DXPaperKind.A4;
this.Margins = new DevExpress.Drawing.DXMargins(20F, 20F, 50F, 60F);
```

**Available content width (Portrait):** `826.7717 - 20 - 20 = 786.7717F`

**A4 Landscape** — used by `AppRegBaseReport` (Registration reports) and `RegistrationListReport`:

```csharp
this.Landscape = true;
this.PageWidthF  = 1169.291F;
this.PageHeightF = 826.7717F;
this.PaperKind = DXPaperKind.A4;
this.Margins = new DevExpress.Drawing.DXMargins(20F, 20F, 50F, 60F);
```

**Available content width (Landscape):** `1169.291 - 20 - 20 = 1129.291F`

---

## Band Structure

```
TopMargin       — report title label (XRLabel, centered, Times New Roman 18F Bold)
PageHeader      — column header table (XRTable, repeats on every page)
Detail          — data row table (XRTable, CanGrow = true)
ReportFooter    — signature line (position left, full name right)
BottomMargin    — empty spacer (20F)
```

---

## Table Structure (XRTable)

Use `XRTable → XRTableRow → XRTableCell` for **both** header and detail bands. Do **not** use individual `XRLabel` controls for tabular data — cells in a row must grow together when content overflows.

- Set `CanGrow = true` on the `DetailBand` and the `XRTable`
- Use `Weight` (not fixed pixel widths) on cells — weights must sum to the table width (1129F)
- Header and detail tables must use **identical Weight values** so columns align

### RegistrationListReport Column Weights

| Column | Header Text | Weight |
|---|---|---|
| № | № | 35 |
| Familýasy | Familiyasy | 85 |
| Ady | Ady | 85 |
| Doglan senesi | Doğlan senesi | 90 |
| Jynsy | Jynsy | 65 |
| Raýatlygy | Raýatlygy | 79 |
| Pasportynyn belgisi | Pasportynyn belgisi | 105 |
| Pasportynyn möhleti | Pasportynyn möhleti | 110 |
| Gelmeginiin maksady | Gelmeginiin maksady | 150 |
| Wiza maglumatary | Wiza maglumatary | 125 |
| Türkmenistandaky salgysy | Türkmenistandaky salgysy | 200 |
| **Total** | | **1129** |

---

## Fonts

| Context | Font |
|---|---|
| Report title | Times New Roman, 18F, Bold |
| Header cells | Times New Roman, 11F, Bold |
| Detail cells | Times New Roman, 9F |
| Signature line | Times New Roman, 10F, Bold |

---

## Borders

**Header cells:** `BorderSide.All` (top border needed for the first row)

**Detail cells:** `BorderSide.Left | BorderSide.Right | BorderSide.Bottom` — omit `Top` to avoid doubled borders where adjacent rows meet.

**Border color:** `Color.Black`
**Border width:** `0.5F`

---

## Cell Padding

**Header cells:** no explicit padding (default).

**Detail cells:** `PaddingInfo(3, 3, 2, 2)` — 3px left/right, 2px top/bottom. Prevents text from touching cell borders, especially in narrow columns or when `WordWrap = true`.

```csharp
dc.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 2, 2);
```

---

## Colors

| Context | BackColor | ForeColor |
|---|---|---|
| Header cells | White | Black |
| Detail cells | (default) | (default) |
| Report background | White | — |

---

## Expression Bindings

For **computed or multiline content**, use `ExpressionBinding` instead of `.Text`:

```csharp
this.xrTableCell_VisaInfo.ExpressionBindings.AddRange(new ExpressionBinding[] {
    new ExpressionBinding("BeforePrint", "Text",
        "[Visa_Number] + Char(10) + [Visa_TypeTm] + Char(10) + [Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText]")
});
this.xrTableCell_VisaInfo.Multiline = true;
```

> Use `Char(10)` for newlines — `'\n'` is not supported in XtraReports expression syntax.

---

## Row Numbers

```csharp
this.xrTableCell_RowNumber.ExpressionBindings.AddRange(new ExpressionBinding[] {
    new ExpressionBinding("BeforePrint", "Text", "sumRecordNumber()")
});
XRSummary xrSummary1 = new XRSummary();
xrSummary1.Running = SummaryRunning.Report;
this.xrTableCell_RowNumber.Summary = xrSummary1;
```

---

## Signature Footer

Place the signature block in a `ReportFooterBand` — **not** `BottomMargin`. `BottomMargin` does not have reliable data context for bound fields.

| Label | Alignment | Binding |
|---|---|---|
| Position | Left | `[CompanyHead_PositionTm]` |
| Full Name | Right | `[CompanyHead_FullName]` |

---

## Registering a New Report

**1.** Add the report class in `DatabaseUpdate/ReportsUpdater.cs`:

```csharp
AddPredefinedReport<MyNewReport>("My Report Display Name", typeof(TargetType), isInplaceReport: true);
```

**2.** Add a visibility rule in `UpdateDatabaseAfterUpdateSchema`:

```csharp
CreateReportVisibility(
    reportName: "My Report Display Name",
    displayName: "My Report Display Name",
    targetType: typeof(TargetType),
    criteria: "" // empty = always visible; use criteria to restrict
);
```

**3.** Update this document — add a row to the **Existing Reports** table at the top.

---

## .resx Sync Requirement

When adding or modifying `ExpressionBindings` in a `*.Designer.cs` file, the corresponding `*.resx` file **must also be updated** — otherwise the binding is not picked up at runtime.

DevExpress XtraReports serializes expression binding data into both files. Changes to `.Designer.cs` alone are insufficient.
