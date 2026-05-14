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
| **Status** | ✅ Implemented |

## Page

- **Format**: A4 portrait (11906×16838 twips).
- **Margins**: 1800 twips L/R (~1.25"), 1440 twips T/B (~1").
- **Font**: Times New Roman throughout.
- **Content width**: ~8306 twips.

## Layout Family

- **L2**: Ministry letter (Group A) — formal extension request letter.
- Uses `MakeGroupALetterTemplate` generator pattern.

## Data Anchors

- **Header**: Single-form data via `FillForm` (no row repeat).
- **Page break**: None (single page typical; multi-page allowed with footer page numbers).

## Band / Control Map (Word → OpenXML)

| Section | OpenXML Structure | Content |
|---|---|---|
| **Ministry header** | Static paragraphs (not included in Word template) | Placeholder area for pre-printed letterhead |
| **Date** | Right-aligned paragraph | `{{ApplicationDate}}` (dd.MM.yyyy) |
| **Reference** | Left paragraph | "Dogry gelýänçä: `{{FullApplicationNumber}}`" |
| **Recipient** | Left-aligned block (1-2 lines) | `{{ProjectContract_Ministry_RecipientBlock_Line1}}` + optional Line2 |
| **Form of address** | Left paragraph with prefix | `{{ProjectContract_Ministry_FormOfAddress}}` (e.g., "Hormatly") |
| **Subject** | Bold justified paragraph | Extension request subject with project description |
| **Urgency** | Paragraph (if urgent) | `{{Urgency_NameTm}}` — displayed when urgent |
| **Body intro** | Justified paragraph | Introduction explaining extension reason |
| **Project description** | Justified paragraph | `{{ProjectContract_Description}}` |
| **Extension context** | Justified paragraph | Company context + extension period details |
| **Request** | Justified paragraph | Formal request for visa/work permit extension |
| **Attachments header** | Bold left paragraph | "Goşundy:" |
| **Attachments list** | Bulleted/numbered list | Passport copies + extension justification docs |
| **Closing** | Left paragraph | "Gerekli işleriň ýerine ýetirilmeginizi soraýarys." |
| **Signature block** | Right-aligned block | `{{Application_CompanyHead_PositionTm}}` / `{{Application_CompanyHead_FullName}}` |
| **Page footer** | Page number (Word native) | Auto-generated page numbers |

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
| `Application_CompanyHead_PositionTm` | Signatory position in Turkmen |
| `Application_CompanyHead_FullName` | Signatory full name |

## Template Patterns

- **Letterhead**: Not embedded in template (pre-printed stationery).
- **Recipient block**: May have 1 or 2 lines; use conditional logic for Line2.
- **Subject line**: Bold, justified, contains dynamic extension description.
- **Body text**: Justified, formal/legal style, compact line spacing.
- **Attachments**: Passport copies + extension justification documents.
- **Signature**: Right-aligned position above name.

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
