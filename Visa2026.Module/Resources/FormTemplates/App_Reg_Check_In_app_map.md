# Report Map: App_Reg_Check_In_app

**Status:** ✅ Implemented — `AppRegCheckInReport`
**Inherits from:** `AppGroupEBaseReport` → `AppBaseReport`

---

## Identity

| | |
|---|---|
| Class | `AppRegCheckInReport` |
| Registered name | `"App Reg Check In Report"` |
| Display name (Tm) | `"Hasaba Almak — Ýüztutma"` |
| ApplicationType | `App_Reg_Check_In` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Reg_Check_In'` |
| Data type | `Application` |
| Reference image | `App_Reg_Check_In_app.jpg` |

---

## What the base provides

`AppGroupEBaseReport` supplies (do not redeclare in derived):
- `xrLabelRecipient` — `[MigrationService_NameTm]`, X=220, Y=218, Bold, TopLeft, vertically centered
- `xrRichBody1` — empty placeholder; **derived sets `Rtf`**
- `xrRichBody2` — static responsibility paragraph (`AppBaseReport.RtfResponsibility`)
- `Detail.HeightF = 492F` (vertically centered layout)

See `Reports/AppGroupEBaseReport.Designer.cs` for positions and sizes.

---

## Derived overrides (constructor)

### xrRichBody1 — check-in request paragraph

```
Hatymyzyň goşundysynda görkezilen sanawdaky **[TotalPersonCount] ([TotalPersonCountText])** sany
daşary ýurt raýatynyň **Türkmenistana gelendigi sebäpli** hasaba almagy'ňyzy Sizden haýyş edýäris.
```

### Detail.HeightF

```csharp
this.Detail.HeightF = 492F;  // matches Group E base default
```

---

## Required BO properties

| Property | Source | Exists? |
|---|---|---|
| `TotalPersonCount` | `Application` | ✅ |
| `TotalPersonCountText` | `Application` | ✅ |
| `MigrationService_NameTm` | `Application` | ✅ (used by Group E base) |

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppRegCheckInReport>("App Reg Check In Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Reg Check In Report", "[ApplicationType.Name] = 'App_Reg_Check_In'");
```
