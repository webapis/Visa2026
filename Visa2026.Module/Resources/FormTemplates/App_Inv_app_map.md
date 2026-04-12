# Report Map: App_Inv_app

**Status:** ✅ Implemented — `AppInvReport`
**Inherits from:** `AppGroupABaseReport` → `AppBaseReport`

---

## Identity

| | |
|---|---|
| Class | `AppInvReport` |
| Registered name | `"App Inv Report"` |
| Display name (Tm) | `"Çakylyk — Ýüztutma"` |
| ApplicationType | `App_Inv` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Inv'` |
| Data type | `Application` |
| Reference image | `App_Inv_app.jpg` |

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

### xrRichBody2 — invitation request paragraph

```
Hatymyzyň goşundysynda görkezilen Türkiýe Respublikasynyň "[Company.Name]" kompaniýasyna
degişli bolan sanawdaky **[TotalPersonCount] ([TotalPersonCountText])** sany daşary ýurt
raýatyna **[VisaPeriod_NameTm] möhlet** bilen **[VisaCategory_NameTm]** çakylyk
resmileşdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.
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

Properties consumed by `AppGroupABaseReport` (Recipient, Urgency, Greeting, Description, CompanyHead) are not repeated here — see the group base.

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppInvReport>("App Inv Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Inv Report", "[ApplicationType.Name] = 'App_Inv'");
```
