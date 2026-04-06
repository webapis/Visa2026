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
| **Background** | Dynamic — `background_{Company.Code}.jpg`, loaded automatically by `AppBaseReport` when the data source is demanded (Blazor preview-safe). No per-company subclass needed. |

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
| *(Watermark)* | *(report-level)* | *(full page)* | Background | `background_{Company.Code}.jpg` | Letterhead watermark — loaded at runtime by `AppBaseReport` |
| `xrLabelAppNumber` | (0, 78) | 250 × 20 | Bound | `[FullApplicationNumber]` | Bold, left-aligned. Visible in image top-left as "№3/-370" |
| `xrLabelAppDate` | (0, 100) | 250 × 20 | Bound | `[ApplicationDate]` | Left-aligned, format `dd.MM.yyyy ý.` Visible as "04.03.2026 ý." |
| `xrLabelCompanyName` | (486, 110) | 300 × 35 | Bound | `[Company.Name]` | **Hidden** (`Visible=false`) — company branding is already in the background image |

> The company logo and "CALIK ENERJİ / TURKMENISTAN BRANCH" text visible in the top-right of the image are part of `background_CLK.jpg` — **not separate controls**.

---

### Detail — Defined in `AppRegCheckInReport.Designer.cs`

| Control | LocationFloat | SizeF | Source | Value / Expression | Notes |
|---|---|---|---|---|---|
| `xrLabelRecipient` | (393, 30) | 393 × 70 | Bound | `[MigrationService_NameTm]` | Bold, multiline, `TopRight`. Right half of page. Visible in image as "Türkmenistanyň Döwlet migrasýa gullugynyň..." |
| `xrLabelBody1Line1` | (0, 130) | 786 × 20 | Static | `"Hatymyzyň goşundysynda görkezilen sanawdaky"` | Centered line to match scan |
| `xrLabelBody1Count` | (0, 152) | 786 × 20 | Bound (bold) | `[TotalPersonCount] + ' (' + [TotalPersonCountText] + ')'` | Centered and bold to match scan emphasis (`1 (bir)`) |
| `xrLabelBody1Suffix` | (0, 174) | 786 × 40 | Static | `"sany daşary ýurt raýatynyň Türkmenistana gelendigi sebäpli hasaba almagyňyzy Sizden haýyş edýäris."` | Centered continuation line(s) |
| `xrLabelBody2Line1` | (0, 225) | 786 × 20 | Static | `"Daşary ýurt raýatynyň Türkmenistana gelmeginiň, onda bolmagynyň we ondan"` | Centered, line 1 |
| `xrLabelBody2Line2` | (0, 245) | 786 × 20 | Static | `"gitmeginiň düzgünlerini berjaý etmegine jogapkärçiligi kompaniýamyz öz üstüne alýar."` | Centered, line 2 |

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
