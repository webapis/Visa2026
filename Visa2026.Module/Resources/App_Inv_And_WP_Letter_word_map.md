# Word Map: App_Inv_And_WP_Letter — Word Report Design Contract

## Identity

| | |
|---|---|
| **ApplicationType** | `App_Inv_And_WP` |
| **Level** | `Application` (single letter per application) |
| **Template** | `App_Inv_And_WP_Letter.docx` |
| **ReportDef** | `AppInvAndWPLetterReportDef.cs` |
| **Display (Tm)** | Çakylyk we Iş Rugsatnamasy — Hat (Word) |
| **Reference image** | `App_Inv_And_WP_app.jpg` |
| **Status** | ✅ Implemented |

## Page

- **Format**: A4 portrait (11906×16838 twips).
- **Margins**: 1020 twips L/R (~0.7"), 720 twips T/B (~0.5").
- **Font**: Times New Roman throughout.
- **Content width**: ~9866 twips.

## Layout Family

- **F3**: Formal ministry letter with letterhead area, recipient block, subject line, legal body, attachments list, signature.

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
| **Subject** | Bold justified paragraph | "Türkmenistanyň Daşary Işler Ministrligi bilen ylalaşylan... işe çagyrylmagy barada" (dynamic description) |
| **Urgency** | Paragraph (if urgent) | `{{Urgency_NameTm}}` — displayed when urgent |
| **Body intro** | Justified paragraph | "Türkmenistanyň ... kärhanasy tarapyndan ... işe çagyrylýan `{{TotalPersonCountText}}` `{{VisaCategory_NameTm}}` görnüşli ... berilmegi hakynda ýüz tutýarys." |
| **Project description** | Justified paragraph | `{{ProjectContract_Description}}` |
| **Company context** | Justified paragraph | "`{{Company_Name}}` ... ýerine ýetirýär." + visa period |
| **Request** | Justified paragraph | Formal request for visa/work permit processing |
| **Attachments header** | Bold left paragraph | "Goşundy:" |
| **Attachments list** | Bulleted/numbered list | Ministry sample: passport copies + foreign-citizen info (not XAF xrLabelAttachments wording) |
| **Closing** | Left paragraph | "Gerekli işleriň ýerine ýetirilmeginizi soraýarys." |
| **Signature block** | Right-aligned block | `{{Application_CompanyHead_PositionTm}}` / `{{Application_CompanyHead_FullName}}` |
| **Page footer** | Page number (Word native) | Auto-generated page numbers |

## Data Fields (`Application`)

| Field | Notes |
|---|---|
| `FullApplicationNumber` | Reference number for "Dogry gelýänçä:" |
| `ApplicationDate` | Letter date (dd.MM.yyyy format) |
| `ProjectContract_Ministry_RecipientBlock` | Raw multi-line recipient block |
| `ProjectContract_Ministry_RecipientBlock_Line1` | First line of recipient address |
| `ProjectContract_Ministry_RecipientBlock_Line2` | Second line (optional) |
| `ProjectContract_Ministry_RecipientBlock_HasLine2` | Boolean for conditional display |
| `ProjectContract_Ministry_FormOfAddress` | Salutation (e.g., "Hormatly") |
| `ProjectContract_Description` | Project scope description |
| `Urgency_NameTm` | Urgency level text (if applicable) |
| `Company_Name` | Sponsor company legal name |
| `TotalPersonCount` | Numeric count |
| `TotalPersonCountText` | Written form (e.g., "bş adamlary") |
| `VisaPeriod_NameTm` | Visa duration (e.g., "3 (üç) aýlyk", "12 (on iki) aýlyk") |
| `VisaCategory_NameTm` | Visa type (e.g., "Işçi (WP)") |
| `Application_CompanyHead_PositionTm` | Signatory position in Turkmen |
| `Application_CompanyHead_FullName` | Signatory full name |

## Template Patterns

- **Letterhead**: Not embedded in template (pre-printed stationery); content starts below letterhead area.
- **Recipient block**: May have 1 or 2 lines; use conditional logic for Line2.
- **Subject line**: Bold, justified, contains dynamic project description.
- **Body text**: Justified, formal/legal style, compact line spacing.
- **Attachments**: Follow ministry sample (passport copies + foreign-citizen info), not XAF xrLabelAttachments wording.
- **Signature**: Right-aligned position above name.

## Differences from XtraReport

| Aspect | XtraReport | Word |
|---|---|---|
| Letterhead | `xrPictureBoxHeader` embedded image | Not embedded (pre-printed) |
| Attachments | `xrLabelAttachments` (dynamic from XAF) | Static ministry wording |
| Footer | `xrPageInfo` (XAF controlled) | Word native page numbers |
| Layout | XtraReport bands | OpenXML paragraphs |

## Verification

- **Preview preset**: `inv-and-wp-letter` in `PreviewWordReports/Program.cs`
- **Check**: Proper recipient line wrapping; subject line bold; signature right-aligned; attachments wording matches ministry sample.
