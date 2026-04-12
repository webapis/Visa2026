# Report Map: App_Visa_Ext_FM_app

**Status:** ✅ Implemented — `AppVisaExtFMReport`
**Inherits from:** `AppGroupBBaseReport` → `AppBaseReport`

---

## Identity

| | |
|---|---|
| Class | `AppVisaExtFMReport` |
| Registered name | `"App Visa Ext FM Report"` |
| Display name (Tm) | `"Wiza Möhletini Uzaltmak FM — Ýüztutma"` |
| ApplicationType | `App_Visa_Ext_FM` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Visa_Ext_FM'` |
| Data type | `Application` |
| Reference image | `App_Visa_Ext_FM_app.jpg` |

---

## What the base provides

`AppGroupBBaseReport` supplies (do not redeclare in derived):
- `xrLabelRecipient` — `[ProjectContract_Ministry_RecipientBlock]`, X=220, Y=20, Bold, TopLeft
- `xrLabelUrgency` — `[Urgency_NameTm]`, Visible=`[ApplicationType.ShowUrgency]`, Bold (derived overrides font to Italic)
- `xrLabelGreeting` — `[ProjectContract_Ministry_FormOfAddress]`, Bold, MiddleCenter
- `xrRichBody1` — static "Berkarar döwletimiziň..." intro paragraph
- `xrRichBody2` — static company partnership paragraph with `[Company.Name]`
- `xrRichBody3` — empty placeholder; **derived sets `Rtf`**
- `xrRichBody4` — static responsibility paragraph (`AppBaseReport.RtfResponsibility`)
- `xrLabelAttachments` — empty placeholder; **derived adds ExpressionBinding**

See `Reports/AppGroupBBaseReport.Designer.cs` for positions and sizes.

---

## Derived overrides (constructor)

### xrLabelUrgency — font override

```csharp
this.xrLabelUrgency.Font = new DXFont("Times New Roman", 15F, DXFontStyle.Italic);
```

### xrRichBody3 — FM visa extension request paragraph

Differs from `AppInvFMReport` only in the closing phrase: **"wizalaryny möhletiniň uzaldylmagyna"** instead of **"çakylyk resmileşdirilmegine"**.

```
Türkmenistandaky çäklerinde amala aşyrylýan taslamalar utgaşdyrmak boýunça [Company.Name]
kompaniýasyna degişli hünärmeniň (**[SponsoringEmployee_FullName] - [SponsoringEmployee_PositionTm]**)
maşgala agzalaryna ýagny, hatymyzyň goşundysynda görkezilen sanawdaky
**[TotalPersonCount] ([TotalPersonCountText])** sany daşary ýurt raýatyna
[FamilyMember_Relationship_NameTm] wiza möhletine görä
**[VisaCategory_NameTm] wizalaryny möhletiniň uzaldylmagyna** ýardam bermegiňizi Sizden haýyş edýäris.
```

### xrLabelAttachments — expression

```
'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) +
'                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'
```

### Detail.HeightF

```csharp
this.Detail.HeightF = 633F;
```

---

## Required BO properties

| Property | Source | Exists? |
|---|---|---|
| `TotalPersonCount` | `Application` | ✅ |
| `TotalPersonCountText` | `Application` | ✅ |
| `VisaCategory_NameTm` | `Application → VisaCategory` | ✅ |
| `FamilyMember_Relationship_NameTm` | `Application → ApplicationItems[0] → Person → Relationship` | ✅ |
| `SponsoringEmployee_FullName` | `Application → ApplicationItems[0] → Person → SponsoringEmployee` | ✅ |
| `SponsoringEmployee_PositionTm` | `Application → ApplicationItems[0] → Person → SponsoringEmployee → Position` | ✅ |

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppVisaExtFMReport>("App Visa Ext FM Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Visa Ext FM Report", "[ApplicationType.Name] = 'App_Visa_Ext_FM'");
```
