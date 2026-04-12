# Report Map: App_Inv_And_WP_app

**Status:** ✅ Implemented — `AppInvAndWPReport`
**Inherits from:** `AppGroupABaseReport` → `AppBaseReport`

---

## Identity

| | |
|---|---|
| Class | `AppInvAndWPReport` |
| Registered name | `"App Inv And WP Report"` |
| Display name (Tm) | `"Çakylyk we Iş Rugsatnamasy — Ýüztutma"` |
| ApplicationType | `App_Inv_And_WP` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Inv_And_WP'` |
| Data type | `Application` |
| Reference image | `App_Inv_And_WP_app.jpg` |

---

## What the base provides

`AppGroupABaseReport` supplies (do not redeclare in derived):
- `xrLabelRecipient` — `[ProjectContract_Ministry_RecipientBlock]`, X=220, Y=20, Bold, TopLeft
- `xrLabelUrgency` — `[Urgency_NameTm]`, Visible=`[ApplicationType.ShowUrgency]`, Bold (derived overrides font to Italic)
- `xrLabelGreeting` — `[ProjectContract_Ministry_FormOfAddress]`, Bold, MiddleCenter
- `xrRichBody1` — `[ProjectContract_Description]`, justified, `\fi720`
- `xrRichBody2` — empty placeholder; **derived sets `Rtf`**
- `xrRichBody3` — static responsibility paragraph (`AppBaseReport.RtfResponsibility`)
- `xrLabelAttachments` — empty placeholder; **derived adds ExpressionBinding**

See `Reports/AppGroupABaseReport.Designer.cs` for positions and sizes.

---

## Derived overrides (constructor)

### xrLabelUrgency — font override

```csharp
this.xrLabelUrgency.Font = new DXFont("Times New Roman", 15F, DXFontStyle.Italic);
```

### xrRichBody2 — invitation + work permit request paragraph

Differs from `AppInvReport`: appends "**[VisaCategory_NameTm] çakylyk we iş rugsatnamasyny**" (static suffix joins the category name).

```
Hatymyzyň goşundysynda görkezilen Türkiýe Respublikasynyň "[Company.Name]" kompaniýasyna
degişli bolan sanawdaky **[TotalPersonCount] ([TotalPersonCountText])** sany daşary ýurt
raýatyna **[VisaPeriod_NameTm] möhlet** bilen **[VisaCategory_NameTm] çakylyk we iş
rugsatnamasyny** resmileşdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.
```

### xrLabelAttachments — expression

```
'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) +
'                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'
```

### Detail.HeightF

Inherited from `AppGroupABaseReport` — **535F** (= attachments end 524F + 11F). No override needed.

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
AddPredefinedReport<AppInvAndWPReport>("App Inv And WP Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Inv And WP Report", "[ApplicationType.Name] = 'App_Inv_And_WP'");
```
