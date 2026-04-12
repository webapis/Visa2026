# Report Map: App_Additional_WP_location_app

**Status:** ✅ Implemented — `AppAdditionalWPLocationReport`
**Inherits from:** `AppGroupCBaseReport` → `AppBaseReport`

---

## Identity

| | |
|---|---|
| Class | `AppAdditionalWPLocationReport` |
| Registered name | `"App Additional WP Location Report"` |
| Display name (Tm) | `"Goşmaça hereket çägi — Ýüztutma"` |
| ApplicationType | `App_Additional_WP_location` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Additional_WP_location'` |
| Data type | `Application` |
| Reference image | `App_Additional_WP_location_app.jpg` |

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

### xrRichBody2 — additional WP location request paragraph

```
Şertname esasynda, öňde goýlan wezipeleri ýetinlikli durmuşa geçirmek üçin hatymyzyň
goşundysynda görkezilen "[Company.Name]" kompaniýasyna degişli bolan
**[TotalPersonCount] ([TotalPersonCountText])** sany daşary ýurt raýatyna
**[MovementPermitLocation_NameTm]** iş rugsatnamalarynyň berilmegine ýardam bermegiňizi
Sizden haýyş edýäris.
```

### xrLabelAttachments — expression

```
'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) +
'                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'
```

### Detail.HeightF

```csharp
this.Detail.HeightF = 580F;  // overrides Group C base default of 540F
```

---

## Required BO properties

| Property | Source | Exists? |
|---|---|---|
| `TotalPersonCount` | `Application` | ✅ |
| `TotalPersonCountText` | `Application` | ✅ |
| `MovementPermitLocation_NameTm` | `Application → MovementPermitLocation` | ✅ |

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppAdditionalWPLocationReport>("App Additional WP Location Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Additional WP Location Report", "[ApplicationType.Name] = 'App_Additional_WP_location'");
```
