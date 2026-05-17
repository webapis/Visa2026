# Report Map: App_Visa_and_WP_Ext_app

**Status:** ✅ XtraReport — `AppVisaAndWPExtReport` · ✅ Word (user template) — **433-Elyasow (seed)** / `Resources/Templates/433_Elyasow_uzt.docx` (`App_Visa_and_WP_Ext` only)
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

## Word report (user template)

**Template:** `Resources/Templates/433_Elyasow_uzt.docx` — seeded as **433-Elyasow (seed)** in `UserReportTemplateUpdater.cs` (`UserReportBoType.Application`, `App_Visa_and_WP_Ext` only). Layout is maintained manually in Word; do not regenerate from `tools/GenerateTemplates`.

**Placeholders (`{{ds.*}}`):** `FullApplicationNumber`, `ApplicationDateText`, `TotalPersonCount`, `TotalPersonCountText`, `Urgency_NameTm`, `VisaPeriod_NameTm`, `VisaCategory_NameTm` — see `docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md` on `Application`.

**Removed:** code-generated `App_Visa_And_WP_Ext_Letter.docx` / `AppVisaAndWPExtLetterReportDef` (superseded by Elyasow manual template).
