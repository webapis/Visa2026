# App_Cancel_Inv_WP_app_map.md

**Status**: ✅ Implemented

---

## Report Identity

| Field | Value |
|---|---|
| Class | `AppCancelInvWPReport` |
| ApplicationType.Name | `App_Cancel_Inv_WP` |
| Template image | `App_Cancel_Inv_WP_app.jpg` |
| Registered name | `"App Cancel Inv WP Report"` |
| Display name (Turkmen) | `"Çakylyk we Iş Rugsatnamasyny Ýatyrmak — Ýüztutma"` |
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

**Applied.** Content is two short paragraphs — fits well within the page.

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

> Hatymyzyň goşundysynda görkezilen sanawdaky **[CancelPersonCount] ([CancelPersonCountText]) sany** daşary ýurt raýatynyň **[CancelWPCount] ([CancelWPCountText]) sany** işlemek üçin rugsatnamasyny we **[CancelInvCount] ([CancelInvCountText]) sany** çakylygyny ýatyrmagyňyzy Sizden haýyş edýäris.

Bold spans:
- `[CancelPersonCount] ([CancelPersonCountText]) sany`
- `[CancelWPCount] ([CancelWPCountText]) sany`
- `[CancelInvCount] ([CancelInvCountText]) sany`

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
| `CancelPersonCount` | Application | ❌ | `ApplicationItems.Count` — shared with `App_Cancel_Visa_and_WP` |
| `CancelPersonCountText` | Application | ❌ | `NumberToTurkmenWords(CancelPersonCount)` — shared |
| `CancelWPCount` | Application | ❌ | `ApplicationItems.Count + ApplicationItems.Count(ai => ai.SecondWorkPermitItem != null)` — shared |
| `CancelWPCountText` | Application | ❌ | `NumberToTurkmenWords(CancelWPCount)` — shared |
| `CancelInvCount` | Application | ❌ | `ApplicationItems.Count(ai => ai.CurrentInvitationItem != null)` — falls back to `ApplicationItems.Count` |
| `CancelInvCountText` | Application | ❌ | `NumberToTurkmenWords(CancelInvCount)` |

All six need to be added as `[NotMapped]` computed properties on `Application.cs`.

> **Note:** `CancelPersonCount`, `CancelPersonCountText`, `CancelWPCount`, `CancelWPCountText` are shared with `App_Cancel_Visa_and_WP` — add once, used by both reports.
