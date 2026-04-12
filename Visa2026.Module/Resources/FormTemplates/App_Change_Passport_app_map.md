# Report Map: App_Change_Passport_app

**Status:** ✅ Implemented — `AppChangePassportReport`
**Inherits from:** `AppGroupDBaseReport` → `AppBaseReport`

---

## Identity

| | |
|---|---|
| Class | `AppChangePassportReport` |
| Registered name | `"App Change Passport Report"` |
| Display name (Tm) | `"Wizany Geçirmek — Ýüztutma"` |
| ApplicationType | `App_Change_Passport` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Change_Passport'` |
| Data type | `Application` |
| Reference image | `App_Change_Passport_app.jpg` |

---

## What the base provides

`AppGroupDBaseReport` supplies (do not redeclare in derived):
- `xrLabelRecipient` — static text "Türkmenistanyň Döwlet migrasiýa gullugynyň başlygyna", X=220, Y=217, Bold, vertically centered
- `xrRichBody1` — empty placeholder; **derived sets `Rtf`**
- `xrRichBody2` — static responsibility paragraph (`AppBaseReport.RtfResponsibility`)
- `Detail.HeightF = 492F` (vertically centered layout)

See `Reports/AppGroupDBaseReport.Designer.cs` for positions and sizes.

---

## Derived overrides (constructor)

### xrRichBody1 — visa transfer request paragraph

```
Hatymyzyň goşundysynda görkezilen sanawdaky **[TotalPersonCount]([TotalPersonCountText])** sany
daşary ýurt raýatynyň **wizasyny köne pasportdan täze pasporta geçirip bermegiňizi**
Sizden haýyş edýäris.
```

### Detail.HeightF

```csharp
this.Detail.HeightF = 492F;  // matches Group D base default
```

---

## Required BO properties

| Property | Source | Exists? |
|---|---|---|
| `TotalPersonCount` | `Application` | ✅ |
| `TotalPersonCountText` | `Application` | ✅ |

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppChangePassportReport>("App Change Passport Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Change Passport Report", "[ApplicationType.Name] = 'App_Change_Passport'");
```
