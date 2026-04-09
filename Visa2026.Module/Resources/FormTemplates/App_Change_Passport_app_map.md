# App_Change_Passport_app_map.md

**Status**: ✅ Implemented

---

## Report Identity

| Field | Value |
|---|---|
| Class | `AppChangePassportReport` |
| ApplicationType.Name | `App_Change_Passport` |
| Template image | `App_Change_Passport_app.jpg` |
| Registered name | `"App Change Passport Report"` |
| Display name (Turkmen) | `"Wizany Geçirmek — Ýüztutma"` |
| Inherits from | `AppBaseReport` |
| Data type | `Application` |

---

## Page Setup (A4 Portrait)

| Property | Value |
|---|---|
| Page height | 1169F |
| TopMargin | 50F |
| BottomMargin | 100F |
| Left margin | 100F |
| Right margin | 100F |
| Printable width | 626.7717F |
| PageHeader | 150F (AppBaseReport — ref number + date) |
| ReportFooter | 80F (AppBaseReport — signatory, PrintAtBottom=false) |

---

## Vertical Centering

**Applied.** Content is static (no growing collection). Formula: T = (789 − content_height − 11 − 80) / 2

| Variable | Value |
|---|---|
| Available area | 789F (1169 − 50 − 150 − 100 − 80) |
| content_height | 263F (80 + 15 + 80 + 8 + 80) |
| T | (789 − 263 − 11 − 80) / 2 = **218F** |

---

## Band Map

| Band | HeightF | Notes |
|---|---|---|
| TopMargin | 50F | AppBaseReport |
| PageHeader | 150F | AppBaseReport |
| Detail | **492F** | T=218F centering |
| ReportFooter | 80F | AppBaseReport — signatory |
| BottomMargin | 100F | AppBaseReport |

---

## Control Map — Detail Band

| Control | Type | X | Y | W | H | Notes |
|---|---|---|---|---|---|---|
| xrLabelRecipient | XRLabel | 220F | 218F | 406.7717F | 80F | Static text, Bold, TopLeft, CanGrow, CanShrink, Multiline, WordWrap. Ends at 298F. |
| xrRichBody1 | XRRichText | 0F | 313F | 626.7717F | 80F | Request paragraph. Y=313F (15F gap). CanGrow, Borders=None. Ends at 393F. |
| xrRichBody2 | XRRichText | 0F | 401F | 626.7717F | 80F | Static responsibility. Y=401F (8F gap). CanGrow, Borders=None. Ends at 481F. |

**Detail.HeightF = 492F** (481F + 11F bottom padding)

### xrLabelRecipient — Static text

```
Türkmenistanyň Döwlet migrasiýa gullugynyň başlygyna
```

### xrRichBody1 — Visa transfer request paragraph

Times New Roman 15pt, justified (`\qj`), first-line indent 0.5" (`\fi720`).

> Hatymyzyň goşundysynda görkezilen sanawdaky **[TotalPersonCount]([TotalPersonCountText])** sany daşary ýurt raýatynyň **wizasyny köne pasportdan täze pasporta geçirip bermegiňizi** Sizden haýyş edýäris.

Bold spans: `[TotalPersonCount]([TotalPersonCountText])` · `wizasyny köne pasportdan täze pasporta geçirip bermegiňizi`

### xrRichBody2 — Static responsibility paragraph

> Daşary ýurt raýatynyň Türkmenistana gelmeginiň, onda bolmagynyň we ondan gitmeginiň düzgünlerini berjaý etmegine jogapkärçiligi kompaniýamyz öz üstüne alýar.

---

## Ignored Elements

| Element | Reason |
|---|---|
| Logo (top right) | AppBaseReport PageHeader |
| Application number, date | AppBaseReport PageHeader |
| Signatory block | AppBaseReport ReportFooter |
| Footer text + logos (bottom) | AppBaseReport BottomMargin |
| Background watermark | AppBaseReport background image |

---

## Required BO Properties

| Property | Level | Exists? | Notes |
|---|---|---|---|
| `TotalPersonCount` | Application | ✅ | — |
| `TotalPersonCountText` | Application | ✅ | — |

**No new NotMapped properties needed.**
