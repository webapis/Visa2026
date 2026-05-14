# Report Map: App_Visa_And_WP_Ext_GT15_Calik_Migration

**Status:** Word — `AppVisaWPExtGt15CalikMigrationLetterReportDef` / `App_Visa_And_WP_Ext_GT15_Calik_Migration_Letter.docx`  
**Layout family:** L3 (plain top-left ref+date, migration addressee; **no** embedded letterhead / header image — white background only)

## Data type(s)

| Role | Type | Notes |
|------|------|--------|
| Root | `Application` | `FillForm` only |

## Reference scan

| File | Role |
|------|------|
| `FormTemplates/App_Visa_And_WP_Ext_GT15_Calik_Migration_scan.png` | Çalık Turkmenistan branch → Döwlet migrasiýa gullugy (GT-15) |

## Applicability

- `ApplicationType.Name` = `App_Visa_and_WP_Ext`
- `IsApplicable`: `ProjectContract.NameTm` contains `GT-15` (case-insensitive), same gate as the Energy→Construction ministry letter.

## Dynamic fields (`{{ds.*}}`)

| Placeholder | Source on `Application` | Notes |
|-------------|-------------------------|--------|
| `FullApplicationNumber` | `FullApplicationNumber` | Top-left (bold) |
| `ApplicationDate` | `ApplicationDate` (`dd.MM.yyyy`) + ` ý.` in template | Top-left, line under ref (bold) |
| `MigrationService_NameTm` | `MigrationService_NameTm` | Addressee line (bold, right) |
| `ApplicationType_ShowUrgency` | `ApplicationType.ShowUrgency` | DocxTemplater `{?{…}}{{/}}` around urgency |
| `Urgency_NameTm` | `Urgency_NameTm` | Italic+bold line when shown |
| `ProjectContract_Description` | `ProjectContract.Description` | First justified body paragraph (GT-15 narrative) |
| `CancelPersonCount` | `CancelPersonCount` | Item count in request paragraph |
| `CancelPersonCountText` | `CancelPersonCountText` | Turkmen cardinal in parentheses |
| `VisaPeriod_NameTm` | `VisaPeriod_NameTm` | e.g. `6 (alty) aý` |
| `VisaCategory_NameTm` | `VisaCategory_NameTm` | e.g. `köp gezeklik` (separate token after period phrase) |
| `Application_CompanyHead_PositionTm` | `CompanyHead.Position.NameTm` | Signatory left cell |
| `Application_CompanyHead_FullName` | `CompanyHead.FullName` | Signatory right cell |

## Static in template

- Third body paragraph (company responsibility for rules of stay).
- Signatory block layout (position + name) from shared `AppendSignatoryLetter` helper.

## Generator

`MakeAppVisaAndWPExtGt15CalikMigrationLetterTemplate` in `tools/GenerateTemplates/Program.cs` (CLI override: `args[31]`).

## Preview preset

`tools/PreviewWordReports` — preset `visa-wp-ext-gt15-calik-migration`.
