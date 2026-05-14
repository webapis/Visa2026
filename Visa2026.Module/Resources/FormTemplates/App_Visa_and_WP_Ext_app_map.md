# Report Map: App_Visa_and_WP_Ext_app

**Status:** ✅ XtraReport — `AppVisaAndWPExtReport` · ✅ Word — `AppVisaAndWPExtLetterReportDef` / `App_Visa_And_WP_Ext_Letter.docx` (**Word runtime sign-off:** 2026-05-13)
**Inherits from:** `AppGroupCBaseReport` → `AppBaseReport`

---

## Identity

| | |
|---|---|
| Class | `AppVisaAndWPExtReport` |
| Registered name | `"App Visa And WP Ext Report"` |
| Display name (Tm) | `"Wiza we Iş Rugsatnamasyny Uzaltmak — Ýüztutma"` |
| ApplicationType | `App_Visa_and_WP_Ext` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Visa_and_WP_Ext'` |
| Data type | `Application` |
| Reference image | `App_Visa_and_WP_Ext_app.jpg` |

---

## What the base provides

`AppGroupCBaseReport` supplies (do not redeclare in derived):
- `xrLabelRecipient` — `[ProjectContract_Ministry_RecipientBlock]`, X=220, Y=20, Bold, TopLeft
- `xrLabelGreeting` — `[ProjectContract_Ministry_FormOfAddress]`, Bold, MiddleCenter (no urgency line in Group C)
- `xrRichBody1` — `[ProjectContract_Description]`, justified, `\fi720`
- `xrRichBody2` — empty placeholder; **derived sets `Rtf`**
- `xrRichBody3` — static responsibility paragraph (`AppBaseReport.RtfResponsibility`)
- `xrLabelAttachments` — empty placeholder; **derived adds ExpressionBinding**

See `Reports/AppGroupCBaseReport.Designer.cs` for positions and sizes.

---

## Derived overrides (constructor)

### xrRichBody2 — visa + work permit extension request paragraph

```
Hatymyzyň goşundysynda görkezilen «[Company.Name]» kompaniýasyna degişli bolan sanawdaky
**[TotalPersonCount] ([TotalPersonCountText]) sany** daşary ýurt raýaty üçin
Türkmenistanyň Döwlet migrasiýa gullugy tarapyndan wizasyny we iş rugsatnamasyny
**[VisaPeriod_NameTm] [VisaCategory_NameTm]** möhlet bilen uzadylmagyna rugsat berilmegine
ýardam bermegini Sizden haýyş edýäris.
```

### xrLabelAttachments — expression

```
'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) +
'                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'
```

### Detail.HeightF

```csharp
this.Detail.HeightF = 540F;  // matches Group C base default
```

---

## Required BO properties

| Property | Source | Exists? |
|---|---|---|
| `TotalPersonCount` | `Application` | ✅ |
| `TotalPersonCountText` | `Application` | ✅ |
| `VisaPeriod_NameTm` | `Application → VisaPeriod` | ✅ |
| `VisaCategory_NameTm` | `Application → VisaCategory` | ✅ |
| `Company.Name` | `Application → ProjectContract → Company` | ✅ |

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppVisaAndWPExtReport>("App Visa And WP Ext Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Visa And WP Ext Report", "[ApplicationType.Name] = 'App_Visa_and_WP_Ext'");
```

---

## Resolved decisions

| # | Question | Decision |
|---|---|---|
| 1 | Visa count in body | Appears once — `[VisaPeriod_NameTm]` already includes the number (e.g. "1 ýyllyk"), second count was redundant |
| 2 | Recipient control type | `XRLabel` + `Text` binding (standard §14), not XRRichText |

---

## Word report (`App_Visa_And_WP_Ext_Letter.docx`)

**Status:** Implemented — `AppVisaAndWPExtLetterReportDef` + `MakeAppVisaAndWPExtLetterTemplate` in `tools/GenerateTemplates/Program.cs`. **User accepted (production Resminamalar):** 2026-05-13 — single page, ministry addressee right, final layout OK.

**Layout family:** L2 (ministry letter) — shares ministry **addressee table** (same as `App_Inv_And_WP_Letter`), symmetric **1200/1200** twips side margins, header spacing, justified **720** twips first-line indent body, borderless signatory table, and `FormalCompanyLetterLayout.ResponsibilityPlain` with **App_Inv_And_WP_Letter** (`FormalCompanyLetterLayout` `InvAndWP*` constants).

**Typography (Word product standard vs scan):** Salutation is **bold, centered, black (`w:color` 000000), no underline** (ministry scan may show underline/color). Urgency line is **italic, black, no underline**. Responsibility → Goşundy uses **`w:after` 0** twips for this letter only (`VisaExtResponsibilityParagraphAfterTwips`), with no blank paragraph between.

**Reference:** `App_Visa_and_WP_Ext_app.jpg`. **Word vs XAF:** Goşundy block follows the **scan** (passport copies + single “Goşundy (n sany …)” line), not `xrLabelAttachments` “sanawy” wording.

### Placeholders (`{{ds.*}}`)

| Key | Source |
|-----|--------|
| `FullApplicationNumber`, `ApplicationDate` | `Application` |
| `ProjectContract_Ministry_RecipientBlock` | Full ministry block (legacy / diagnostics) |
| `ProjectContract_Ministry_RecipientBlock_Line1`, `Line2`, `HasLine2` | Derived via `MinistryRecipientBlockFormatter.SplitIntoAddressLines` in `AppVisaAndWPExtLetterReportDef` |
| `ApplicationType_ShowUrgency` | `Application.ApplicationType.ShowUrgency` |
| `Urgency_NameTm` | `Application.Urgency_NameTm` (italic header line; paragraph hidden when `ShowUrgency` is false via DocxTemplater `{?{ds.ApplicationType_ShowUrgency}}…{{/}}`) |
| `ProjectContract_Ministry_FormOfAddress` | Salutation line as stored (e.g. “Hormatly …!”) |
| `ProjectContract_Description` | First justified body block |
| `Company_Name`, `TotalPersonCount`, `TotalPersonCountText`, `VisaPeriod_NameTm`, `VisaCategory_NameTm` | Extension request paragraph (bold on company name, count parenthetical, and period+category per XAF RTF) |
| `Application_CompanyHead_PositionTm`, `Application_CompanyHead_FullName` | Signatory table |

### Goşundy (static shell + merge)

1. `Goşundy: 1. Daşary ýurtly raýatlaryň pasport nusgalary – {{ds.TotalPersonCount}} sany`
2. `                2. Goşundy ( {{ds.TotalPersonCount}} sany daşary ýurt raýatynyň maglumaty)`

**Preview:** `tools/PreviewWordReports` preset `visa-and-wp-ext-letter` — dump strings transcribed from the reference scan where possible.
