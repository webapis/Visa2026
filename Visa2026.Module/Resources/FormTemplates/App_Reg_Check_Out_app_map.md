# Report Map: App_Reg_Check_Out_app

**Status:** ✅ Implemented — `AppRegCheckOutReport`
**Inherits from:** `AppGroupEBaseReport` → `AppBaseReport`

> ⚠️ Catalog note: originally listed as Reg-level only (`—:—:1`). Scanned image confirmed App-level. Catalog updated to `1:—:1`.

---

## Identity

| | |
|---|---|
| Class | `AppRegCheckOutReport` |
| Registered name | `"App Reg Check Out Report"` |
| Display name (Tm) | `"Hasapdan Çykarmak — Ýüztutma"` |
| ApplicationType | `App_Reg_Check_Out` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Reg_Check_Out'` |
| Data type | `Application` |
| Reference image | `App_Reg_Check_Out_app.jpg` |

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

### xrRichBody1 — check-out request paragraph

Differs from `AppRegCheckInReport`: "Türkmenistandan gidendigi sebäpli" and "hasapdan doly çykarmagyňyzy" instead of check-in equivalents.

```
Hatymyzyň goşundysynda görkezilen sanawdaky **[TotalPersonCount] ([TotalPersonCountText])** sany
daşary ýurt raýatynyň **Türkmenistandan gidendigi sebäpli** hasapdan doly çykarmagynyzy
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

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppRegCheckOutReport>("App Reg Check Out Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Reg Check Out Report", "[ApplicationType.Name] = 'App_Reg_Check_Out'");
```
