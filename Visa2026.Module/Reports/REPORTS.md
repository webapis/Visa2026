# Report Conventions

This document defines the conventions for all XtraReports in this project. Follow these guidelines when creating or modifying reports to ensure consistency.

> **Turkmen Language QA is mandatory for every report.** Before writing code, review all static strings against [REPORT_STANDARDS.md — Section 14](REPORT_STANDARDS.md#14-turkmen-language-qa). After coding, run the post-code review (§ 14d). When a render screenshot is submitted, run the image review (§ 14e).

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
| `AppCancelVisaAndWPItemReport` | App Cancel Visa And WP Item Report | `ApplicationItem` | `AppCancelVisaAndWPItemReport.Designer.cs` |
| `AppChangeInvItemReport` | App Change Inv Item Report | `ApplicationItem` | `AppChangeInvItemReport.Designer.cs` |
| `AppChangePassportItemReport` | App Change Passport Item Report | `ApplicationItem` | `AppChangePassportItemReport.Designer.cs` |
| `AppLaborContractItemReport` | App Labor Contract Item Report | `ApplicationItem` | `AppLaborContractItemReport.Designer.cs` |
| `RegistrationListReport` | Registration List Report | `Registration` | `RegistrationListReport.Designer.cs` — **shared**: empty criteria, visible for all Registration-type ApplicationTypes (`App_Reg_Check_In`, `App_Reg_Check_In_Internal`, `App_Reg_Check_Out`, `App_Reg_Check_Out_Internal`, `App_Reg_ext`, `App_Reg_Info_Change_Address`, `App_Reg_Info_Change_Passport`, `App_Reg_Info_Change_Visa`, and any future types). No per-type subclasses. |

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
| `Contract_StartDateText` | Contract start date (dd.MM.yyyy) |
| `Contract_ExpirationDateText` | Contract expiration date (dd.MM.yyyy) |
| `Salary_CurrencyCode` | Current salary currency code (`USD` / `TMT`) |
| `Application_CompanyAddress` | Sponsor company address |
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
| `FM_EducationLevelTm` | FM education display: "Çaga" if under 18, "Orta" if adult FM; employee's actual level otherwise |
| `FM_SpecialtyTm` | FM specialty display: "Çaga" if under 18, "Orta" if adult FM; employee's actual specialty otherwise |
| `FM_WezipesiTm` | FM position display: "[Employee Position] [Employee FullName]-ň [Relationship]"; employee's actual position otherwise |

**Signatory path:** `Application?.CompanyHead` (stored directly on Application)

---

## Family Member (FM) Report Conventions

FM reports target `ApplicationItem` where `Person.IsEmployee == false`. Three columns differ from the employee pattern:

### Education columns ("Bilimi we okan ýeri" and "Bilimine görä hünäri")

Family members do not have meaningful education records. Use `FM_EducationLevelTm` and `FM_SpecialtyTm`:

| Person age | Display value |
|---|---|
| Under 18 | `Çaga` |
| 18 and over | `Orta` |

In the report binding, use a single-line expression (no institution line for FM):

```csharp
this.xrCellBilimi.ExpressionBindings.Clear();
this.xrCellBilimi.ExpressionBindings.Add(
    new ExpressionBinding("BeforePrint", "Text", "[FM_EducationLevelTm]"));

this.xrCellHunari.ExpressionBindings.Clear();
this.xrCellHunari.ExpressionBindings.Add(
    new ExpressionBinding("BeforePrint", "Text", "[FM_SpecialtyTm]"));
```

### Position column ("Wezipesi")

For family members, the Wezipesi column shows the **sponsoring employee's position and relationship**, not the FM's own position (which does not exist).

**Pattern:** `[Employee Position] [Employee FullName]-ň [Relationship]`

**Example:** `Zähmeti goramak we tehniki howpsuzlyk boýunça başlyk Bóra Yolcu-ň gyzy`

- Employee Position → `Person.SponsoringEmployee.CurrentPositionHistory.Position.NameTm`
- Employee FullName → `Person.SponsoringEmployee.FullName`
- Genitive suffix → `-ň` (hyphen + ň, appended directly to the name)
- Relationship → `Person.Relationship.NameTm` (e.g., "gyzy", "ogly", "aýaly", "adamsy")

The `FM_WezipesiTm` property on `ApplicationItem` computes this automatically. Use it in FM report overrides:

```csharp
this.xrCellWezipesi.ExpressionBindings.Clear();
this.xrCellWezipesi.ExpressionBindings.Add(
    new ExpressionBinding("BeforePrint", "Text", "[FM_WezipesiTm]"));
```

> **Apply this pattern to every FM item report.** All three overrides (`xrCellBilimi`, `xrCellHunari`, `xrCellWezipesi`) are required whenever the base `AppItemInvSanawBaseReport` is used for an FM application type.

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

### AppItemInvSanawBaseReport Column Weights

> **These weights apply to all reports inheriting `AppItemInvSanawBaseReport`.**
> Header and data cells must always use identical Weight values so columns align.
> Total must always equal **1129.291**.

| Column | Header Text | Weight |
|---|---|---|
| № | № | 25 |
| Familiýasy | Familiýasy | 70 |
| Ady | Ady | 55 |
| Doglan senesi we ýeri | Doglan senesi we ýeri | 80 |
| Jynsy | Jynsy | 35 |
| Raýatlygy | Raýatlygy | 60 |
| Pasport belgisi we möhleti | Pasport belgisi we möhleti | 80 |
| Bilimi we okan ýeri | Bilimi we okan ýeri | 95 |
| Bilimine görä hünäri | Bilimine görä hünäri | 95 |
| Wezipesi | Wezipesi | 90 |
| Möhleti we gezekligi | Möhleti we gezekligi | 80 |
| Türkmenistandaky salgysy | Türkmenistandaky salgysy | 120 |
| Daşary ýurtdaky salgysy | Daşary ýurtdaky salgysy | 120 |
| Barjak serhet ýakasy | Barjak serhet ýakasy | 124.291 |
| **Total** | | **1129.291** |

### RegistrationListReport Column Weights

| Column | Header Text | Weight |
|---|---|---|
| № | № | 35 |
| Familiýasy | Familiýasy | 85 |
| Ady | Ady | 85 |
| Doglan senesi | Doglan senesi | 90 |
| Jynsy | Jynsy | 65 |
| Raýatlygy | Raýatlygy | 79 |
| Pasportynyň belgisi | Pasportynyň belgisi | 105 |
| Pasportynyň möhleti | Pasportynyň möhleti | 110 |
| Gelmeginiň maksady | Gelmeginiň maksady | 150 |
| Wiza maglumatlary | Wiza maglumatlary | 125 |
| Türkmenistandaky salgysy | Türkmenistandaky salgysy | 200 |
| **Total** | | **1129** |

### Column Sizing Guidelines

Follow these rules whenever adjusting or designing a new column layout:

**1. Total width constraint**
The XRTable `SizeF` is fixed at `1129.291F` for landscape reports. Cell weights are proportional — they must always sum to exactly this value (or the integer equivalent, e.g. `1129` for RegistrationListReport). Mismatched totals cause invisible overflow or misaligned headers.

**2. Header-data weight parity**
The `PageHeader` table and `Detail` table are separate `XRTable` controls. Both must have **exactly the same Weight values per column**, otherwise columns will not align visually.

**3. Minimum width for header text at 7pt Bold Times New Roman**
A rough rule of thumb for landscape item reports:

| Header character count | Minimum safe weight |
|---|---|
| 1–3 chars (e.g. "Ady", "№") | 25–35 |
| 4–6 chars (e.g. "Jynsy") | 35–45 |
| 7–10 chars (e.g. "Raýatlygy", "Wezipesi") | 55–70 |
| 11–15 chars (e.g. "Familiýasy") | 70–85 |
| 16–22 chars (multi-word, e.g. "Bilimi we okan ýeri") | 90–100 |
| 23+ chars (e.g. "Türkmenistandaky salgysy") | 110–130 |

> These are starting points. Always verify by submitting a render screenshot and running the image review (REPORT_STANDARDS.md § 14e).

**4. How to redistribute weights**
When a column needs more width, take from columns whose content is short and which have surplus space. Typical donors are:
- "Ady" (first name — usually short)
- "Jynsy" (gender code — 1–5 chars)
- "№" (row number — 1–2 digits)

Never redistribute from address or multi-word columns — they need the space for wrapped content.

**5. After any weight change**
Update **both** the header row and the data row weights in `Designer.cs`. Update the column weight table in this document. Verify the total still equals the required sum.

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
