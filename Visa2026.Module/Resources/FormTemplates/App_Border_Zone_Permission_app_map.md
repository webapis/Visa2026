# Report Map: App_Border_Zone_Permission_app

**Status:** ✅ Implemented — `AppBorderZonePermissionReport`
**Inherits from:** `AppGroupCBaseReport` → `AppBaseReport`

---

## Identity

| | |
|---|---|
| Class | `AppBorderZonePermissionReport` |
| Registered name | `"App Border Zone Permission Report"` |
| Display name (Tm) | `"Serhet Ýaka Rugsatnama — Ýüztutma"` |
| ApplicationType | `App_Border_Zone_Permission` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Border_Zone_Permission'` |
| Data type | `Application` |
| Reference image | `App_Border_Zone_Permission_app.jpg` |

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

### xrRichBody2 — border zone permission request paragraph

Differs from `AppAdditionalWPLocationReport`: uses `[BorderZoneLocation_NameTm]` and requests **"serhet ýaka wizasynyň resmileşdirilmegine"** instead of work permit issuance.

```
Şertname esasynda, öňde goýlan wezipeleri ýetinlikli durmuşa geçirmek üçin hatymyzyň
goşundysynda görkezilen "[Company.Name]" kompaniýasynyň işçi bolup
**[TotalPersonCount] ([TotalPersonCountText]) sany** daşary ýurt raýatynyň
**[BorderZoneLocation_NameTm]** serhet ýaka wizasynyň resmileşdirilmegine ýardam bermegiňizi
Sizden haýyş edýäris.
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
| `BorderZoneLocation_NameTm` | `Application → BorderZoneLocation` | ✅ |

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppBorderZonePermissionReport>("App Border Zone Permission Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Border Zone Permission Report", "[ApplicationType.Name] = 'App_Border_Zone_Permission'");
```
