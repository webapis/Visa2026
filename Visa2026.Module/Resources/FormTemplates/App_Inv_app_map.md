# Report Map: App_Inv_app

> Created from scanned reference image `App_Inv_app.jpg`.
> **Status:** ✅ Implemented — `AppInvReport`

---

## Report Identity

| Property | Value |
|---|---|
| **Class Name** | `AppInvReport` |
| **Registered Name** | `"App Inv Report"` |
| **Display Name (Tm)** | `"Çakylyk — Ýüztutma"` |
| **Reference Image** | `App_Inv_app.jpg` |

---

## Data

| Property | Value |
|---|---|
| **Data Type** | `Application` |
| **Inherits From** | `AppBaseReport` |
| **Visibility Criteria** | `[ApplicationType.Name] = 'App_Inv'` |
| **Registered In** | `ReportsUpdater.cs` |
| **Shared / Per-Type** | Per-type (single ApplicationType) |
| **Background** | Dynamic — `background_{Company.Code}.jpg`, loaded automatically by `AppBaseReport.BeforePrint` |

---

## Page Setup

| Property | Value |
|---|---|
| **Orientation** | Portrait |
| **Paper** | A4 |
| **Source** | Inherited from `AppBaseReport` |

---

## Band Map

| Band | Height | Source | Notes |
|---|---|---|---|
| TopMargin | 50F | Inherited | |
| PageHeader | 150F | Inherited | Company letterhead background + number/date labels |
| Detail | 640F | Defined here | Recipient + urgency + greeting + 3 body paragraphs + attachments |
| ReportFooter | 55F | Inherited | Signatory position (left) + full name (right) |
| BottomMargin | 60F | Inherited | |

---

## Control Map

### PageHeader — Inherited from `AppBaseReport` (do not redeclare)

| Control | LocationFloat | SizeF | Source | Value / Expression | Notes |
|---|---|---|---|---|---|
| `xrLabelAppNumber` | (0, 72) | 300 × 28 | Bound | `[FullApplicationNumber]` | Bold. Visible as "№ 1/-2" |
| `xrLabelAppDate` | (0, 102) | 300 × 28 | Bound | `[ApplicationDate]` | Bold, format `dd.MM.yyyy ý.`. Visible as "02.01.2026ý." |

---

### Detail — Defined in `AppInvReport.Designer.cs`

| Control | LocationFloat | SizeF | Type | Source | Value / Expression | Notes |
|---|---|---|---|---|---|---|
| `xrRichRecipient` | (313, 20) | 313.77 × 120 | `XRRichText` | Bound | `[ProjectContract_Ministry_RecipientBlock]` | Right-aligned, bold. Visible as `"Türkmenenergo" döwlet elektroenergetika korporasiýasynyň başlygy D. Elyasowa` |
| `xrLabelUrgency` | (0, 150) | 300 × 25 | `XRLabel` | Bound | `[Urgency_NameTm]` | Italic. Visible only when `ApplicationType.ShowUrgency = true` — controlled via `Visible` expression binding. |
| `xrRichGreeting` | (0, 185) | 626.77 × 35 | `XRRichText` | Bound | `[ProjectContract_Ministry_FormOfAddress]` | Centered, bold. Visible as "Hormatly Durdy Baýjanowiç!" |
| `xrRichBody1` | (0, 230) | 626.77 × 140 | `XRRichText` | Bound | `[ProjectContract_Description]` | Justified, `\fi720` first-line indent. Full contract context paragraph — stored in `ProjectContract.Description`. |
| `xrRichBody2` | (0, 378) | 626.77 × 100 | `XRRichText` | Expression | See body2 expression below | Justified, `\fi720`. Person count + visa period + entry type inline. |
| `xrRichBody3` | (0, 486) | 626.77 × 70 | `XRRichText` | Static | Static responsibility paragraph | Same text as `AppRegCheckInReport` body2. |
| `xrRichAttachments` | (0, 564) | 626.77 × 70 | `XRRichText` | Static | Attachment list — see below | Left-aligned, no indent. |

**xrRichBody2 expression (request paragraph):**
```
Hatymyzyň goşundysynda görkezilen Türkiýe Respublikasynyň "[Company.Name]"
kompaniýasyna degişli bolan sanawdaky "[TotalPersonCount]" ("[TotalPersonCountText]")
sany daşary ýurt raýatyna "[VisaPeriod_NameTm]" bilen "[VisaCategory_NameTm]"
çakylyk resmileşdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.
```
> Entry type ("iki gezeklik") comes from `VisaCategory.NameTm` → needs `[NotMapped]` flat property `VisaCategory_NameTm` on `Application`.

**xrRichBody3 static text:**
```
Daşary ýurt raýatynyň Türkmenistana gelmeginiň, onda bolmagynyň we ondan
gitmeginiň düzgünlerini berjaý etmegine jogapkärçiligi kompaniýamyz öz üstüne alýar.
```

**xrRichAttachments — dynamic count, expression:**
```
'Goşundy:   1. ' + [TotalPersonCount] + '-pasport kopiýalary,\n' +
'           2. Goşundy (' + [TotalPersonCount] + '-daşary ýurt raýatynyň maglumaty)'
```
> The number before "pasport kopiýalary" and "daşary ýurt raýatynyň maglumaty" is `TotalPersonCount`. Use `XRRichText` with expression binding, no first-line indent, left-aligned.

---

### ReportFooter — Inherited from `AppBaseReport` (do not redeclare)

| Control | LocationFloat | SizeF | Source | Value / Expression | Notes |
|---|---|---|---|---|---|
| `xrLabelSignatoryPosition` | (0, 15) | 313 × 28 | Bound | `[CompanyHead.Position.NameTm]` | Bold left. Visible as "Türkmenistandaky şahamçasynyň müdiri" |
| `xrLabelSignatoryFullName` | (313, 15) | 313.77 × 28 | Bound | `[CompanyHead.FullName]` | Bold right. Visible as "Mehmet ÇIRAK" |

---

## Ignored Elements (from scanned image)

| Element visible in image | Reason ignored |
|---|---|
| Company logo top-right (ÇALIK ENERJİ) | Part of `background_CLK.jpg` |
| Bottom address bar | Part of `background_CLK.jpg` |
| ÇALIK circle logo bottom-left | Part of `background_CLK.jpg` |
| Physical signature | Physical document artefact — not reproduced |
| Stamp / seal | Physical document artefact — not reproduced |

---

## Required Business Object Properties

| Property | Source Path | `[NotMapped]` Flat Name | Status |
|---|---|---|---|
| `FullApplicationNumber` | `Application` | — | ✅ Exists |
| `ApplicationDate` | `Application` | — | ✅ Exists |
| `Company.Name` | `Application → Company` | — | ✅ Exists (dot notation) |
| `Company.Code` | `Application → Company` | `Company_Code` | ✅ Exists |
| `ProjectContract.Description` | `Application → ProjectContract` | `ProjectContract_Description` | ❌ Needs `[NotMapped]` |
| `ProjectContract.Ministry.RecipientBlock` | `Application → ProjectContract → Ministry` | `ProjectContract_Ministry_RecipientBlock` | ❌ Needs `[NotMapped]` |
| `ProjectContract.Ministry.FormOfAddress` | `Application → ProjectContract → Ministry` | `ProjectContract_Ministry_FormOfAddress` | ❌ Needs `[NotMapped]` |
| `Urgency.NameTm` | `Application → Urgency` | `Urgency_NameTm` | ❌ Needs `[NotMapped]` |
| `VisaPeriod.NameTm` | `Application → VisaPeriod` | `VisaPeriod_NameTm` | ❌ Needs `[NotMapped]` |
| `VisaCategory.NameTm` | `Application → VisaCategory` | `VisaCategory_NameTm` | ❌ Needs `[NotMapped]` |
| `TotalPersonCount` | `Application` | — | ✅ Exists |
| `TotalPersonCountText` | `Application` | — | ✅ Exists |
| `CompanyHead.Position.NameTm` | `Application → CompanyHead → Position` | — | ✅ Exists (dot notation) |
| `CompanyHead.FullName` | `Application → CompanyHead` | — | ✅ Exists (dot notation) |

---

## Resolved Questions

| Question | Answer |
|---|---|
| Entry type "iki gezeklik" source | `VisaCategory.NameTm` → flat property `VisaCategory_NameTm` |
| Urgency label visibility | Shown only when `ApplicationType.ShowUrgency = true`; content from `Urgency.NameTm` |
| Attachments vary? | Yes — count prefix varies with `TotalPersonCount`; label text is otherwise static |
