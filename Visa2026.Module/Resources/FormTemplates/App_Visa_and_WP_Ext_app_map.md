# Report Map: App_Visa_and_WP_Ext_app

**Status:** ✅ Implemented — `AppVisaAndWPExtReport`
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
Hatymyzyň goşundysynda görkezilen sanawdaky **[TotalPersonCount] ([TotalPersonCountText])** sany
daşary ýurt raýatyna **[VisaPeriod_NameTm] [VisaCategory_NameTm]** möhleti bilen
uzaldylmagyna rugsat bermegini Sizden haýyş edýäris.
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
