# Report Map: App_Inv_FM_app

**Status:** ✅ Implemented — `AppInvFMReport`
**Inherits from:** `AppGroupBBaseReport` → `AppBaseReport`

---

## Identity

| | |
|---|---|
| Class | `AppInvFMReport` |
| Registered name | `"App Inv FM Report"` |
| Display name (Tm) | `"Çakylyk — FM Ýüztutma"` |
| ApplicationType | `App_Inv_FM` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Inv_FM'` |
| Data type | `Application` |
| Reference image | `App_Inv_FM_app.jpg` |

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

### xrRichBody3 — FM invitation request paragraph

```
Türkmenistandaky çäklerinde amala aşyrylýan taslamalar utgaşdyrmak boýunça [Company.Name]
kompaniýasyna degişli hünärmeniň maşgala agzalaryna ýagny, hatymyzyň goşundysynda görkezilen
sanawdaky **[TotalPersonCount] ([TotalPersonCountText])** sany daşary ýurt raýatyna
[FamilyMember_Relationship_NameTm] (**[SponsoringEmployee_FullName] - [SponsoringEmployee_PositionTm]**)
**[VisaPeriod_NameTm] möhlet** bilen **[VisaCategory_NameTm]** çakylyk resmileşdirilmegine
ýardam bermegiňizi Sizden haýyş edýäris.
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
| `VisaPeriod_NameTm` | `Application → VisaPeriod` | ✅ |
| `VisaCategory_NameTm` | `Application → VisaCategory` | ✅ |
| `FamilyMember_Relationship_NameTm` | `Application → ApplicationItems[0] → Person → Relationship` | ✅ |
| `SponsoringEmployee_FullName` | `Application → ApplicationItems[0] → Person → SponsoringEmployee` | ✅ |
| `SponsoringEmployee_PositionTm` | `Application → ApplicationItems[0] → Person → SponsoringEmployee → Position` | ✅ |

> `FamilyMember_Relationship_NameTm` must store the genitive form (e.g. "aýalynyň", "çagasynyň") so the sentence reads naturally.

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppInvFMReport>("App Inv FM Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Inv FM Report", "[ApplicationType.Name] = 'App_Inv_FM'");
```
