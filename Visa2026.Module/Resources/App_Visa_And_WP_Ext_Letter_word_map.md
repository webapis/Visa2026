# Word Map: App_Visa_And_WP_Ext_Letter — Word Report Design Contract

## Identity

| | |
|---|---|
| **ApplicationType** | `App_Visa_and_WP_Ext` |
| **Level** | `Application` (single letter per application) |
| **Template** | `App_Visa_And_WP_Ext_Letter.docx` |
| **ReportDef** | `AppVisaAndWPExtLetterReportDef.cs` |
| **Reference scan** | `FormTemplates/App_Visa_and_WP_Ext_app.jpg` |
| **Generator** | `MakeAppVisaAndWPExtLetterTemplate` in `tools/GenerateTemplates/Program.cs` |

## Product sign-off

**2026-05-13** — Runtime **Resminamalar** output (production, not preview): stakeholder accepted layout and typography; document fits **one A4 page**; ministry addressee block on the **right** as intended.

## Layout family

**L2** — ministry-bound formal letter, aligned with **`App_Inv_And_WP_Letter`**: symmetric **1200/1200** twips left/right margins, ministry **addressee borderless table** (spacer + address column; `MinistryRecipientBlockFormatter` + `Line1` / `Line2` / `HasLine2`), optional urgency line (**italic, black**), centered **bold black** salutation with **no underline** (`w:color` 000000), justified body **720** twips first-line indent, `FormalCompanyLetterLayout.ResponsibilityPlain` with **`w:after` 0** before Goşundy (this letter only), two-line **Goşundy** per scan, borderless signatory table (`AppendSignatoryLetter` with printable width override).

## DocxTemplater model

All keys are under **`ds`** (see `WordFormFillerService.FillForm`).

## Placeholder table

| `{{ds.*}}` | Source |
|------------|--------|
| `FullApplicationNumber` | `Application.FullApplicationNumber` |
| `ApplicationDate` | `Application.ApplicationDate` (`dd.MM.yyyy`) |
| `ProjectContract_Ministry_RecipientBlock` | Full block (optional diagnostics) |
| `ProjectContract_Ministry_RecipientBlock_Line1` / `Line2` / `HasLine2` | Derived in `AppVisaAndWPExtLetterReportDef` via `MinistryRecipientBlockFormatter.SplitIntoAddressLines` |
| `ApplicationType_ShowUrgency` | `Application.ApplicationType.ShowUrgency` |
| `Urgency_NameTm` | `Application.Urgency_NameTm` — shown only when `ApplicationType_ShowUrgency` is true (`{?{ds.ApplicationType_ShowUrgency}}…{{/}}` in template) |
| `ProjectContract_Ministry_FormOfAddress` | Salutation as stored on ministry |
| `ProjectContract_Description` | First body paragraph |
| `Company_Name`, `TotalPersonCount`, `TotalPersonCountText`, `VisaPeriod_NameTm`, `VisaCategory_NameTm` | Extension request paragraph (bold spans match XAF `AppVisaAndWPExtReport` RTF) |
| `Application_CompanyHead_PositionTm`, `Application_CompanyHead_FullName` | Signatory |

## Preview

Preset: **`visa-and-wp-ext-letter`** in `tools/PreviewWordReports/Program.cs`. Close the output `.docx` in Word before re-running if the file is locked.

## XtraReport cross-check

Legacy: `AppVisaAndWPExtReport` (`AppGroupCBaseReport`) — same merge semantics for body/attachments intent; Word **Goşundy** text follows the **scan** in `FormTemplates/App_Visa_and_WP_Ext_app.jpg`.
