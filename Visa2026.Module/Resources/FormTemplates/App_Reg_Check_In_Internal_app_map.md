# Report Map: App_Reg_Check_In_Internal_app

**Status:** ✅ Implemented — `AppRegCheckInInternalReport`
**Inherits from:** `AppGroupEBaseReport` → `AppBaseReport`

> ⚠️ Catalog note: originally listed as Reg-level only (`—:—:1`). Scanned image confirmed App-level. Catalog updated to `1:—:1`.

---

## Identity

| | |
|---|---|
| Class | `AppRegCheckInInternalReport` |
| Registered name | `"App Reg Check In Internal Report"` |
| Display name (Tm) | `"Hasaba Almak (Içerki) — Ýüztutma"` |
| ApplicationType | `App_Reg_Check_In_Internal` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Reg_Check_In_Internal'` |
| Data type | `Application` |
| Reference image | `App_Reg_Check_In_Internal_app.jpg` |

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

### xrRichBody1 — internal address change registration request paragraph

Differs from `AppRegCheckInReport`: instead of "Türkmenistana gelendigi sebäpli", uses the address movement phrase with From/To fields.

```
Hatymyzyň goşundysynda görkezilen sanawdaky **[TotalPersonCount] ([TotalPersonCountText])** sany
daşary ýurt raýatynyň **ýaşaýan salgysyny [FromRegionName] welaýatynyň [FromCityName] etrabyndan
[ToRegionName] welaýatynyň [ToCityName] etrabyna üýtgeýändigi** sebäpli hasaba almagy'ňyzy
Sizden haýyş edýäris.
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
| `FromCityName` | `Application → FromCity?.Name` | ✅ |
| `FromRegionName` | `Application → FromCity?.Region?.Name` | ✅ |
| `ToCityName` | `Application → ToCity?.Name` | ✅ |
| `ToRegionName` | `Application → ToCity?.Region?.Name` | ✅ |

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppRegCheckInInternalReport>("App Reg Check In Internal Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Reg Check In Internal Report", "[ApplicationType.Name] = 'App_Reg_Check_In_Internal'");
```
