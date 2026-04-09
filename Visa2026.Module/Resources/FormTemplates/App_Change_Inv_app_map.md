# App_Change_Inv_app_map.md

**Status**: ✅ Implemented

---

## Report Identity

| Field | Value |
|---|---|
| Class | `AppChangeInvReport` |
| ApplicationType.Name | `App_Change_Inv` |
| Template image | `App_Change_Inv_app.jpg` |
| Registered name | `"App Change Inv Report"` |
| Display name (Turkmen) | `"Çakylygy üýtgetmek — Ýüztutma"` |
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

**Not applied.** Content height is dynamic (invitation table grows with row count). Content starts from top — recipient Y=20F.

---

## Band Map

| Band | HeightF | Notes |
|---|---|---|
| TopMargin | 50F | AppBaseReport |
| PageHeader | 150F | AppBaseReport |
| Detail | **366F** | Application-level content + table header row |
| DetailReportBand → Detail | **25F** per row | One row per Invitation in `Invitations` |
| ReportFooter | 80F | AppBaseReport — follows last invitation row |
| BottomMargin | 100F | AppBaseReport |

---

## Control Map — Detail Band (Application level)

| Control | Type | X | Y | W | H | Notes |
|---|---|---|---|---|---|---|
| xrLabelRecipient | XRLabel | 220F | 20F | 406.7717F | 80F | Static text, Bold, TopLeft, CanGrow, CanShrink, Multiline, WordWrap. Ends at 100F. |
| xrRichBody1 | XRRichText | 0F | 115F | 626.7717F | 100F | Request paragraph. Y=115F (15F gap). CanGrow, Borders=None. Ends at 215F. |
| xrRichBody2 | XRRichText | 0F | 223F | 626.7717F | 80F | Static responsibility. Y=223F (8F gap). CanGrow, Borders=None. Ends at 303F. |
| xrLabelTableTitle | XRLabel | 0F | 311F | 626.7717F | 25F | "Üýtgedilmeli çakylyklar:" Y=311F (8F gap). Bold, MiddleLeft. Ends at 336F. |
| xrTableHeader | XRTable | 0F | 336F | 626.7717F | 30F | Column header row. Bold. All borders. Ends at 366F. |

**Detail.HeightF = 366F** (no bottom padding — DetailReportBand rows follow directly)

### xrLabelRecipient — Static text

```
Türkmenistanyň Döwlet migrasiýa gullugynyň başlygyna
```

### xrRichBody1 — Request paragraph (generic — no specific invitation reference)

Times New Roman 15pt, justified (`\qj`), first-line indent 0.5" (`\fi720`).

> Hatymyzyň goşundysynda görkezilen sanawdaky **[TotalPersonCount] ([TotalPersonCountText]) sany** daşary ýurt raýatynyň **pasportyny çalyşandygy** sebäpli **aşakda görkezilen** çakylyklary täze pasportyna görä resmileşdirip bermegiňizi Sizden haýyş edýäris.

Bold spans: `[TotalPersonCount] ([TotalPersonCountText]) sany` · `pasportyny çalyşandygy` · `aşakda görkezilen`

### xrRichBody2 — Static responsibility paragraph

> Daşary ýurt raýatynyň Türkmenistana gelmeginiň, onda bolmagynyň we ondan gitmeginiň düzgünlerini berjaý etmegine jogapkärçiligi kompaniýamyz öz üstüne alýar.

### xrTableHeader — Column headers (XRTable, 4 cells)

| Cell | Caption | X | W | Alignment |
|---|---|---|---|---|
| colNo | № | 0F | 40F | MiddleCenter |
| colNumber | Çakylygyň belgisi | 40F | 200F | MiddleCenter |
| colStart | Resmileşdirilen senesi | 240F | 183F | MiddleCenter |
| colExpiry | Möhleti | 423F | 203.7717F | MiddleCenter |

Font: Times New Roman 15pt Bold. All borders. Background: transparent.

---

## Control Map — DetailReportBand (Invitations collection)

**DataMember**: `Invitations`

### Inner Detail band — HeightF = 25F (one row per invitation)

| Control | Type | X | W | H | Expression | Alignment |
|---|---|---|---|---|---|---|
| xrCellNo | XRTableCell | 0F | 40F | 25F | `[DataSource.CurrentRowIndex] + 1` | MiddleCenter |
| xrCellNumber | XRTableCell | 40F | 200F | 25F | `[InvitationNumber]` | MiddleCenter |
| xrCellStart | XRTableCell | 240F | 183F | 25F | `[StartDate]` (FormatString: `{0:dd.MM.yyyy}`) | MiddleCenter |
| xrCellExpiry | XRTableCell | 423F | 203.7717F | 25F | `[ExpirationDate]` (FormatString: `{0:dd.MM.yyyy}`) | MiddleCenter |

Font: Times New Roman 15pt. All borders.

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
| `InvitationNumber` | Invitation | ✅ | Used directly in DetailReportBand |
| `StartDate` | Invitation | ✅ | Used directly in DetailReportBand |
| `ExpirationDate` | Invitation | ✅ | Used directly in DetailReportBand |

**No new NotMapped properties needed.** The DetailReportBand binds directly to `Invitations` and accesses child fields natively.
