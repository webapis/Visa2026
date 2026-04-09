# App_Cancel_Visa_and_WP_app_map.md

**Status**: ✅ Implemented

---

## Report Identity

| Field | Value |
|---|---|
| Class | `AppCancelVisaAndWPReport` |
| ApplicationType.Name | `App_Cancel_Visa_and_WP` |
| Template image | `App_Cancel_Visa_and_WP_app.jpg` |
| Registered name | `"App Cancel Visa And WP Report"` |
| Display name (Turkmen) | `"Wiza we Iş Rugsatnamany Ýatyrmak — Ýüztutma"` |
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

**Applied.** Content is two short paragraphs — fits well within the page. Recipient Y calculated to center total content block vertically within the available height.

---

## Band Map

| Band | HeightF | Notes |
|---|---|---|
| TopMargin | 50F | AppBaseReport |
| PageHeader | 150F | AppBaseReport |
| Detail | **300F** | Recipient block + 2 body paragraphs |
| ReportFooter | 80F | AppBaseReport |
| BottomMargin | 100F | AppBaseReport |

---

## Control Map — Detail Band

| Control | Type | X | Y | W | H | Notes |
|---|---|---|---|---|---|---|
| xrLabelRecipient | XRLabel | 220F | 20F | 406.7717F | 60F | Static text, Bold, TopLeft, CanGrow, WordWrap |
| xrRichBody1 | XRRichText | 0F | 100F | 626.7717F | 120F | Request paragraph. CanGrow, Borders=None |
| xrRichBody2 | XRRichText | 0F | 228F | 626.7717F | 72F | Static responsibility paragraph. CanGrow, Borders=None |

### xrLabelRecipient — Static text

```
Türkmenistanyň Döwlet migrasiýa
gullugynyň başlygyna
```

### xrRichBody1 — Request paragraph

Times New Roman 15pt, justified (`\qj`), first-line indent 0.5" (`\fi720`).

> Hatymyzyň goşundysynda görkezilen sanawdaky **[CancelPersonCount] ([CancelPersonCountText]) sany** daşary ýurt raýatynyň **Türkmenistanyň çäginden çykyp gidendigi** sebäpli **[CancelPersonCount] ([CancelPersonCountText]) sany** wizasyny we **[CancelWPCount] ([CancelWPCountText]) sany** işlemek üçin rugsatnamasyny ýatyrmagyňyzy Sizden haýyş edýäris.

Bold spans:
- `[CancelPersonCount] ([CancelPersonCountText]) sany` (first occurrence — person count)
- `Türkmenistanyň çäginden çykyp gidendigi` (static)
- `[CancelPersonCount] ([CancelPersonCountText]) sany` (second occurrence — visa count, same value)
- `[CancelWPCount] ([CancelWPCountText]) sany`

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
| `CancelPersonCount` | Application | ❌ | `ApplicationItems.Count` — used for both person and visa count |
| `CancelPersonCountText` | Application | ❌ | `NumberToTurkmenWords(CancelPersonCount)` |
| `CancelWPCount` | Application | ❌ | `ApplicationItems.Count + ApplicationItems.Count(ai => ai.SecondWorkPermitItem != null)` |
| `CancelWPCountText` | Application | ❌ | `NumberToTurkmenWords(CancelWPCount)` |

All four need to be added as `[NotMapped]` computed properties on `Application.cs`. Shared with `App_Cancel_Inv_WP`.
