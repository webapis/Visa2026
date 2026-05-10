# Report Map: App_Cancel_Visa_and_WP_app

**Status:** ✅ Implemented — `AppCancelVisaAndWPReport`
**Inherits from:** `AppGroupDBaseReport` → `AppBaseReport`

---

## Identity

| | |
|---|---|
| Class | `AppCancelVisaAndWPReport` |
| Registered name | `"App Cancel Visa And WP Report"` |
| Display name (Tm) | `"Wiza we Iş Rugsatnamany Ýatyrmak — Ýüztutma"` |
| ApplicationType | `App_Cancel_Visa_and_WP` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Cancel_Visa_and_WP'` |
| Data type | `Application` |
| Reference image | `App_Cancel_Visa_and_WP_app.jpg` |

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

### xrRichBody1 — visa and work permit cancellation request paragraph

Person count appears **twice**: once for persons, once for visas (same value). Bold departure phrase is static.

```
Hatymyzyň goşundysynda görkezilen sanawdaky **[CancelPersonCount] ([CancelPersonCountText]) sany**
daşary ýurt raýatynyň **Türkmenistanyň çäginden çykyp gidendigi** sebäpli
**[CancelPersonCount] ([CancelPersonCountText]) sany** wizasyny we
**[CancelWPCount] ([CancelWPCountText]) sany** işlemek üçin rugsatnamasyny ýatyrmagy'ňyzy
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
| `CancelPersonCount` | `Application` — `ApplicationItems.Count` | ❌ needs `[NotMapped]` |
| `CancelPersonCountText` | `Application` — `NumberToTurkmenWords(CancelPersonCount)` | ❌ needs `[NotMapped]` |
| `CancelWPCount` | `Application` — `ApplicationItems.Count + ApplicationItems.Count(ai => ai.PreviousWorkPermitItem != null)` | ❌ needs `[NotMapped]` |
| `CancelWPCountText` | `Application` — `NumberToTurkmenWords(CancelWPCount)` | ❌ needs `[NotMapped]` |

> `CancelPersonCount/Text` and `CancelWPCount/Text` are shared with `AppCancelInvWPReport` — add once, used by both.

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppCancelVisaAndWPReport>("App Cancel Visa And WP Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Cancel Visa And WP Report", "[ApplicationType.Name] = 'App_Cancel_Visa_and_WP'");
```
