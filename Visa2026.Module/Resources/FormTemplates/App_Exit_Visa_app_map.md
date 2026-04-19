# Report Map — App_Exit_Visa_app.jpg

**Status:** ✅ Implemented — `AppExitVisaReport`

---

## Report Identity

| Field | Value |
|---|---|
| Class Name | `AppExitVisaReport` |
| Base Class | `AppGroupABaseReport` → `AppBaseReport` → `XtraReport` |
| Registered Name | `"App Exit Visa Report"` |
| Display Name (Tm) | `"Çykyş Wiza — Ýüztutma"` |
| Reference Image | `App_Exit_Visa_app.jpg` |
| ApplicationType Name | `App_Exit_Visa` |

---

## Data

| Field | Value |
|---|---|
| Data Type | `Application` |
| Registration Target | `typeof(Application)` |
| Visibility Criteria | `[ApplicationType.Name] = 'App_Exit_Visa'` |
| Background Rule | Letterhead — handled automatically by `AppBaseReport` |

---

## Page Setup

Inherited from `AppBaseReport` (Portrait A4):

| Property | Value |
|---|---|
| Orientation | Portrait |
| Paper | A4 |
| Margins | L=100F, R=100F, T=50F, B=60F |
| PageWidthF | 826.7717F |
| PageHeightF | 1169.291F |
| Printable width | **626.7717F** |

---

## What AppGroupABaseReport Provides (do not redeclare)

| Control | Expression / Value | Notes |
|---|---|---|
| `xrLabelRecipient` | `[ProjectContract_Ministry_RecipientBlock]` | X=220, Y=20, Bold, TopLeft |
| `xrLabelUrgency` | `[Urgency_NameTm]`, Visible=`[ApplicationType.ShowUrgency]` | Bold; derived overrides to Italic |
| `xrLabelGreeting` | `[ProjectContract_Ministry_FormOfAddress]` | Bold, MiddleCenter |
| `xrRichBody1` | `[ProjectContract_Description]` | Justified, `\fi720` |
| `xrRichBody2` | *(empty placeholder — derived sets Rtf)* | |
| `xrRichBody3` | `AppBaseReport.RtfResponsibility` (static responsibility paragraph) | |
| `xrLabelAttachments` | *(empty placeholder — derived adds ExpressionBinding)* | |

---

## Derived Overrides (constructor only)

### xrLabelUrgency — font override

Same as `AppInvReport`:
```csharp
this.xrLabelUrgency.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Italic);
```

### xrRichBody2 — exit visa request paragraph

Human-readable text (encode per REPORT_STANDARDS.md §6):

> Hatymyzyň goşundysynda görkezilen Türkiýe Respublikasynyň "[Company.Name]" kompaniýasyna degişli bolan sanawdaky **[TotalPersonCount] ([TotalPersonCountText])** daşary ýurt raýaty wizalarynyň tamamlanýan senesine çenli öz jogapkärçiligine degişli bolan işleri doly tamamlap ýetişmeýändikleri sebäpli olara Türkmenistanyň Döwlet migrasiýa gullugy tarapyndan **[VisaPeriod_NameTm] möhleti bilen çykyş wizasyny resmileşdirmek** meselesinde ýardam bermeginizi Sizden haýyş edýäris.

RTF format: `\qj\fi720`, bold on `[TotalPersonCount] ([TotalPersonCountText])` and `[VisaPeriod_NameTm] möhleti bilen çykyş wizasyny resmileşdirmek`.

### xrLabelAttachments — expression

```
'Goşundy: 1. ' + [TotalPersonCount] + '-pasport kopiýalary,' + Char(10) +
'           2. Goşundy (' + [TotalPersonCount] + '-daşary ýurt raýatynyň maglumaty)'
```

### Detail.HeightF

Inherited from `AppGroupABaseReport` — **535F**. No override needed unless content overflows.

---

## Required BO Properties

All on `Application`.

| Property | Exists? | Source |
|---|---|---|
| `TotalPersonCount` | ✅ | `Application.TotalPersonCount` (NotMapped) |
| `TotalPersonCountText` | ✅ | `Application.TotalPersonCountText` (NotMapped) |
| `VisaPeriod_NameTm` | ✅ | `Application.VisaPeriod?.NameTm` (NotMapped) |
| `Company.Name` | ✅ | `Application.Company.Name` (navigation, used inline in RTF) |

Properties consumed by `AppGroupABaseReport` (Recipient, Urgency, Greeting, Description, CompanyHead) are not repeated here — see `AppGroupABaseReport.Designer.cs`.

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppExitVisaReport>("App Exit Visa Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Exit Visa Report", "[ApplicationType.Name] = 'App_Exit_Visa'");
```

---

## Ignored Elements

| Element | Reason |
|---|---|
| Round stamp / seal | Physical stamp — do not replicate |
| Handwritten signature | Physical — do not replicate |
| Company letterhead (logo, address footer) | Handled by `AppBaseReport` background image |
