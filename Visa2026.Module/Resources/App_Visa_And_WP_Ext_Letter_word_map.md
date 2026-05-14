# Word Map: App_Visa_And_WP_Ext_Letter — Word Report Design Contract

## Identity

| | |
|---|---|
| **ApplicationType** | `App_Visa_And_WP_Ext` |
| **Level** | `Application` (single letter per application) |
| **Template** | `App_Visa_And_WP_Ext_Letter.docx` |
| **ReportDef** | `AppVisaAndWPExtLetterReportDef.cs` |
| **Display (Tm)** | Wiza we Iş Rugsatnamasynyň Möhletini Uzaltmak — Hat (Word) |
| **Reference image** | `App_Visa_and_WP_Ext_app.jpg` |
| **Status** | 🔄 Redesigning to match scanned document format |

## Page

- **Format**: A4 portrait (11906×16838 twips).
- **Margins**: 1800 twips L/R (~1.25"), 1440 twips T/B (~1").
- **Font**: Times New Roman throughout.
- **Content width**: ~8306 twips.

## Layout Family

- **C1**: Company letter — formal extension request from company to ministry.
- Company letterhead at top (logo placeholder)
- Reference/date top left, recipient top right
- Uses `MakeCompanyLetterTemplate` generator pattern (new).

## Data Anchors

- **Header**: Single-form data via `FillForm` (no row repeat).
- **Page break**: None (single page typical; multi-page allowed with footer page numbers).

## Band / Control Map (Word → OpenXML)

| Section | OpenXML Structure | Content |
|---|---|---|
| **Company letterhead** | Static paragraph (logo placeholder) | Company logo area (not embedded) |
| **Reference** | Left-aligned paragraph | `{{FullApplicationNumber}}` (top left, e.g., "№ 2/-213") |
| **Date** | Left-aligned paragraph below reference | `{{ApplicationDate}}` (e.g., "06.02.2026 ý.") |
| **Urgency** | Italic left paragraph | "Adaty tertipde !" (if applicable) |
| **Recipient** | Right-aligned block (2 lines) | `{{Recipient_Name}}` + `{{Recipient_Title}}` (e.g., "Türkmenenergo... D. Elyasowa") |
| **Form of address** | Center/left paragraph | "Hormatly `{{Recipient_FullName}}`!" (e.g., "Hormatly Durdý Baýjanowiç!") |
| **Project description** | Justified paragraph | Detailed project: GT-15 agreement, Balkan power plant |
| **Extension request** | Justified paragraph | Body with company, person count, visa period, category |
| **Responsibility** | Justified paragraph | Jogapkärçilik statement |
| **Attachments** | Left-aligned list | Company format: "1. Daşary ýurtly raýatlaryň pasport nusgalary – [count] sany" etc. |
| **Signature block** | Two-column layout | Left: `{{CompanyHead_PositionTm}}` (e.g., "Türkmenistandaky Şahamçasynyň müdiri"), Right: `{{CompanyHead_FullName}}` |
| **Company footer** | Right-aligned small text | Company address, website, email |

## Data Fields (`Application`)

| Field | Notes |
|---|---|
| `FullApplicationNumber` | Reference number for "Dogry gelýänçä:" |
| `ApplicationDate` | Letter date (dd.MM.yyyy format) |
| `ProjectContract_Ministry_RecipientBlock_Line1` | First line of recipient address |
| `ProjectContract_Ministry_RecipientBlock_Line2` | Second line (optional) |
| `ProjectContract_Ministry_FormOfAddress` | Salutation (e.g., "Hormatly") |
| `ProjectContract_Description` | Project scope description |
| `Urgency_NameTm` | Urgency level text (if applicable) |
| `Company_Name` | Sponsor company legal name |
| `TotalPersonCount` | Numeric count of persons |
| `TotalPersonCountText` | Written form (e.g., "bş adamlary") |
| `VisaPeriod_NameTm` | Visa extension period (e.g., "12 (on iki) aýlyk") |
| `VisaCategory_NameTm` | Visa type (e.g., "Işçi (WP)") |
| `Application_CompanyHead_PositionTm` | Signatory position in Turkmen (e.g., "Türkmenistandaky Şahamçasynyň müdiri") |
| `Application_CompanyHead_FullName` | Signatory full name |
| `ProjectContract_GTOfferDescription` | Detailed project description with GT offer details |
| `Recipient_Name` | Recipient organization name |
| `Recipient_Title` | Recipient title/position |
| `Recipient_FullName` | Recipient full name for form of address |
| `IsUrgent` | Boolean to show/hide "Adaty tertipde !" |
| `PersonCount` | Numeric person count (e.g., 1, 5) |
| `PassportCopiesCount` | Number of passport copy attachments |

## Template Patterns

- **Letterhead**: Company logo placeholder at top (not embedded).
- **Header layout**: Reference + date left-aligned, recipient right-aligned.
- **Urgency note**: "Adaty tertipde !" in italic when applicable.
- **Form of address**: "Hormatly [FullName]!" format.
- **Project description**: Detailed paragraph with GT offer numbers, dates, power plant details.
- **Extension body**: Company name, person count, visa period, category.
- **Attachments**: Company-specific wording ("pasport nusgalary" not ministry "sanawy").
- **Signature**: Two-column: position left, name right.
- **Footer**: Company contact info (address, website, email).

## Differences from XtraReport

| Aspect | XtraReport | Word |
|---|---|---|
| Letterhead | `xrPictureBoxHeader` embedded image | Not embedded (pre-printed) |
| Attachments | `xrLabelAttachments` (dynamic from XAF) | Static ministry wording |
| Footer | `xrPageInfo` (XAF controlled) | Word native page numbers |
| Layout | XtraReport bands | OpenXML paragraphs |

## Verification

- **Generator**: `MakeGroupALetterTemplate` in `GenerateTemplates/Program.cs`
- **Check**: Proper recipient line wrapping; subject line bold; signature right-aligned; extension wording matches ministry sample.

## Generator Pattern

Uses `MakeGroupALetterTemplate()` which provides:
- Standard ministry letter margins (1800/1440 twips)
- 15pt (30 half-pt) body text
- Italic urgency block when applicable
- Formal letter structure with proper spacing
