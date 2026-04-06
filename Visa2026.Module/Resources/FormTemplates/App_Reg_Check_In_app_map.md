# Report Map: App_Reg_Check_In_app

> Auto-generated from scanned reference image `App_Reg_Check_In_app.jpg`.
> This map was agreed upon before code generation. Do not modify without re-agreeing with the team.
> **Status:** ✅ Implemented — `AppRegCheckInReport`

---

## Report Identity

| Property | Value |
|---|---|
| **Class Name** | `AppRegCheckInReport` |
| **Registered Name** | `"App Reg Check In Report"` |
| **Display Name (Tm)** | `"Hasaba Almak — Ýüztutma"` |
| **Reference Image** | `App_Reg_Check_In_app.jpg` |

---

## Data

| Property | Value |
|---|---|
| **Data Type** | `Application` |
| **Inherits From** | `AppBaseReport` |
| **Visibility Criteria** | `[ApplicationType.Name] = 'App_Reg_Check_In'` |
| **Registered In** | `ReportsUpdater.cs` |
| **Shared / Per-Type** | Per-type (single ApplicationType) |
| **Background** | Dynamic — `background_{Company.Code}.jpg`, loaded automatically by `AppBaseReport.BeforePrint`. No per-company subclass needed. |

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
| PageHeader | 150F | Inherited | Company letterhead background + number/date/company labels |
| Detail | 290F | Defined here | Recipient block + two body paragraphs |
| ReportFooter | 50F | Inherited | Signatory position (left) + full name (right) |
| BottomMargin | 60F | Inherited | |

---

## Control Map

### PageHeader — Inherited from `AppBaseReport` (do not redeclare)

| Control | LocationFloat | SizeF | Source | Value / Expression | Notes |
|---|---|---|---|---|---|
| `xrPictureBoxBackground` | (0, 0) | 786 × 150 | Background | `background_{Company.Code}.jpg` | Letterhead — loaded at runtime |
| `xrLabelAppNumber` | (486, 80) | 300 × 20 | Bound | `[FullApplicationNumber]` | Bold, right-aligned. Visible in image top-left as "№3/-370" |
| `xrLabelAppDate` | (486, 102) | 300 × 20 | Bound | `[ApplicationDate]` | Right-aligned, format `dd.MM.yyyy ý.` Visible as "04.03.2026 ý." |
| `xrLabelCompanyName` | (486, 124) | 300 × 20 | Bound | `[Company.Name]` | Right-aligned |

> The company logo and "CALIK ENERJİ / TURKMENISTAN BRANCH" text visible in the top-right of the image are part of `background.jpg` / `background_CLK.jpg` — **not separate controls**.

---

### Detail — Defined in `AppRegCheckInReport.Designer.cs`

| Control | LocationFloat | SizeF | Source | Value / Expression | Notes |
|---|---|---|---|---|---|
| `xrLabelRecipient` | (393, 30) | 393 × 70 | Bound | `[MigrationService_NameTm]` | Bold, multiline, `TopCenter`. Right half of page. Visible in image as "Türkmenistanyň Döwlet migrasýa gullugynyň..." |
| `xrLabelBody1` | (0, 140) | 786 × 60 | Expression | `'Hatymyzyň goşundysynda görkezilen sanawdaky ' + [TotalPersonCount] + ' (' + [TotalPersonCountText] + ') sany daşary ýurt raýatynyň Türkmenistana gelendigi sebäpli hasaba almagyňyzy Sizden haýyş edýäris.'` | Full-width, `TopJustify`, `CanGrow`. The bold "1 (bir)" in the image is the runtime value of `TotalPersonCount`/`TotalPersonCountText` — no static bold sub-label needed. |
| `xrLabelBody2` | (0, 215) | 786 × 50 | Static | `"Daşary ýurt raýatynyň Türkmenistana gelmeginiň, onda bolmagynyň we ondan gitmeginiň düzgünlerini berjaý etmegine jogapkärçiligi kompaniýamyz öz üstüne alýar."` | Full-width, `TopJustify`, `CanGrow`. Static — same text on every print. |

---

### ReportFooter — Inherited from `AppBaseReport` (do not redeclare)

| Control | LocationFloat | SizeF | Source | Value / Expression | Notes |
|---|---|---|---|---|---|
| `xrLabelSignatoryPosition` | (0, 15) | 393 × 20 | Bound | `[CompanyHead.Position.NameTm]` | Bold, left-aligned. Visible as "Türkmenistandaky Şahamçasynyň müdiri" |
| `xrLabelSignatoryFullName` | (393, 15) | 393 × 20 | Bound | `[CompanyHead.FullName]` | Bold, right-aligned. Visible as "Mehmet Çırak" |

---

## Ignored Elements (from scanned image)

| Element visible in image | Reason ignored |
|---|---|
| Company logo (bottom-left CALIK circle) | Part of `background.jpg` |
| Bottom address bar ("Bitarap Turkmenistan shayoly 538...") | Part of `background.jpg` |
| Stamp / seal (circular, center-bottom) | Physical document artefact — not reproduced |
| Physical signature | Physical document artefact — not reproduced |

---

## Required Business Object Properties

| Property | BO | Status |
|---|---|---|
| `FullApplicationNumber` | `Application` | ✅ Exists |
| `ApplicationDate` | `Application` | ✅ Exists |
| `Company.Name` | `Application` → `Company` | ✅ Exists |
| `Company.Code` | `Application` → `Company` | ✅ Added this session |
| `MigrationService_NameTm` | `Application` | ✅ Exists |
| `TotalPersonCount` | `Application` | ✅ Exists |
| `TotalPersonCountText` | `Application` | ✅ Exists |
| `CompanyHead.Position.NameTm` | `Application` → `CompanyHead` → `Position` | ✅ Exists |
| `CompanyHead.FullName` | `Application` → `CompanyHead` | ✅ Exists |
