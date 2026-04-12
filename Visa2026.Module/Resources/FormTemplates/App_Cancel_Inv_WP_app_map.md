# Report Map: App_Cancel_Inv_WP_app

**Status:** ✅ Implemented — `AppCancelInvWPReport`
**Inherits from:** `AppGroupDBaseReport` → `AppBaseReport`

---

## Identity

| | |
|---|---|
| Class | `AppCancelInvWPReport` |
| Registered name | `"App Cancel Inv WP Report"` |
| Display name (Tm) | `"Çakylyk we Iş Rugsatnamasyny Ýatyrmak — Ýüztutma"` |
| ApplicationType | `App_Cancel_Inv_WP` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Cancel_Inv_WP'` |
| Data type | `Application` |
| Reference image | `App_Cancel_Inv_WP_app.jpg` |

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

### xrRichBody1 — invitation and work permit cancellation request paragraph

Three separate bold counts: persons, work permits, invitations.

```
Hatymyzyň goşundysynda görkezilen sanawdaky **[CancelPersonCount] ([CancelPersonCountText]) sany**
daşary ýurt raýatynyň **[CancelWPCount] ([CancelWPCountText]) sany** işlemek üçin
rugsatnamasyny we **[CancelInvCount] ([CancelInvCountText]) sany** çakylygyny ýatyrmagy'ňyzy
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
| `CancelPersonCount` | `Application` — `ApplicationItems.Count` | ❌ needs `[NotMapped]` (shared with `AppCancelVisaAndWPReport`) |
| `CancelPersonCountText` | `Application` — `NumberToTurkmenWords(CancelPersonCount)` | ❌ needs `[NotMapped]` (shared) |
| `CancelWPCount` | `Application` — `ApplicationItems.Count + count(SecondWorkPermitItem != null)` | ❌ needs `[NotMapped]` (shared) |
| `CancelWPCountText` | `Application` — `NumberToTurkmenWords(CancelWPCount)` | ❌ needs `[NotMapped]` (shared) |
| `CancelInvCount` | `Application` — `ApplicationItems.Count(ai => ai.CurrentInvitationItem != null)` | ❌ needs `[NotMapped]` |
| `CancelInvCountText` | `Application` — `NumberToTurkmenWords(CancelInvCount)` | ❌ needs `[NotMapped]` |

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppCancelInvWPReport>("App Cancel Inv WP Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Cancel Inv WP Report", "[ApplicationType.Name] = 'App_Cancel_Inv_WP'");
```
