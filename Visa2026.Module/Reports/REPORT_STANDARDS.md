# Report Standards

This document defines the visual and structural standards applied to **all** Application-level reports in this project. Update this document whenever a standard is changed or a new decision is made. All future reports must follow these standards.

For report creation workflow, control catalog, and map file process, see [REPORT_GENERATION_GUIDE.md](REPORT_GENERATION_GUIDE.md).

---

## 1. Page Setup

| Property | Value | Notes |
|---|---|---|
| Paper | A4 Portrait | `PaperKind.A4`, `Landscape = false` |
| Page width | 826.7717F | DevExpress report units (1/100 inch) |
| Page height | 1169.291F | |
| Left margin | 100F | 1 inch — Office Word default |
| Right margin | 100F | 1 inch — Office Word default |
| Top margin | 50F | |
| Bottom margin | 60F | |
| **Printable width** | **626.7717F** | `826.7717 - 100 - 100` |
| **Half-page split point** | **313F** | Used for left/right column layout (recipient, signatory) |

---

## 2. Typography

| Element | Font | Size | Style | Notes |
|---|---|---|---|---|
| Body paragraphs | Times New Roman | 15pt | Normal | XRRichText — see Section 4 |
| Application number | Times New Roman | 15pt | **Bold** | XRLabel |
| Application date | Times New Roman | 15pt | **Bold** | XRLabel, format `{0:dd.MM.yyyy} ý.` |
| Recipient label | Times New Roman | 15pt | **Bold** | XRLabel, right-aligned |
| Signatory position | Times New Roman | 15pt | **Bold** | XRLabel, left-aligned |
| Signatory full name | Times New Roman | 15pt | **Bold** | XRLabel, right-aligned |

---

## 3. Paragraph Formatting (XRRichText)

All body paragraph text must use `XRRichText` with the following RTF paragraph settings:

| Property | RTF code | Value | Notes |
|---|---|---|---|
| Alignment | `\qj` | Justified | Standard for formal Turkmen letters |
| First-line indent | `\fi720` | 720 twips = 0.5 inch | Office Word default paragraph indent |
| Font size | `\fs30` | 30 half-points = 15pt | Matches typography standard |
| Font | `\f0\froman\fcharset0 Times New Roman` | | Defined in `\fonttbl` |
| Paragraph end | `\par` | | Closes the paragraph |

**RTF template header** (copy-paste for new paragraphs):
```
{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 [text here]\par}
```

**Paragraph gap** (vertical space between consecutive body paragraphs): `8F` report units between the bottom of one `XRRichText` and the top of the next.

---

## 4. Control Selection Guide

Use the correct control for each content type. The rule is: **content must be visible and editable in the DevExpress Report Designer** — no text hidden in code-behind.

| Content type | Control | Designer experience |
|---|---|---|
| Bound field — single value (number, date, name) | `XRLabel` + expression binding | Expression editor in designer |
| Static label / heading | `XRLabel` | Direct text edit in designer |
| Paragraph with occasional inline bold + dynamic field | `XRLabel` + `AllowMarkupText` + expression | Expression editor; `<b>` tags are visible but editable |
| **Paragraph needing first-line indent or complex formatting** | **`XRRichText`** with content in designer | Full Word-like rich text editor; indent, justify, bold set visually |
| Full letter body from external RTF template (contracts) | `XRRichText` + DB-stored RTF | See `ContractTemplate` / `RichTextMailMergeController` |

> **Rule:** If a body paragraph needs first-line indent, always use `XRRichText`. Set the RTF content directly in `Designer.cs` (or via the designer's rich text editor). Never build RTF in code-behind — this hides content from the designer.

---

## 5. Dynamic Values in XRRichText (Field References)

XtraReports evaluates `[FieldName]` expressions found inside `XRRichText` RTF content at render time — no special delimiters are required. Surround the field reference with regular `"` quotes for the standard Turkmen formal letter display style.

**Syntax in RTF string (in Designer.cs):**
```
"[FieldName]"
```

**Example — bold dynamic value in the middle of a paragraph:**
```
\b "[TotalPersonCount]" ("[TotalPersonCountText]")\b0
```
This renders as: **"2" ("iki")** with surrounding text in normal weight.

> **Do not use guillemets** (`«[FieldName]»` / `\u171?...\u187?`). In XtraReports v25.2, guillemets are rendered as literal characters alongside the substituted value, producing output like `«2»` instead of `"2"`.

---

## 6. Turkmen Character Reference (RTF Unicode Escapes)

All non-ASCII characters in RTF strings must be escaped as `\uN?` where N is the decimal Unicode code point.

| Char | Unicode | Decimal | RTF escape |
|---|---|---|---|
| ň | U+0148 | 328 | `\u328?` |
| Ň | U+0147 | 327 | `\u327?` |
| ş | U+015F | 351 | `\u351?` |
| Ş | U+015E | 350 | `\u350?` |
| ö | U+00F6 | 246 | `\u246?` |
| Ö | U+00D6 | 214 | `\u214?` |
| ý | U+00FD | 253 | `\u253?` |
| Ý | U+00DD | 221 | `\u221?` |
| ä | U+00E4 | 228 | `\u228?` |
| Ä | U+00C4 | 196 | `\u196?` |
| ü | U+00FC | 252 | `\u252?` |
| Ü | U+00DC | 220 | `\u220?` |
| ç | U+00E7 | 231 | `\u231?` |
| Ç | U+00C7 | 199 | `\u199?` |

> In C# `Designer.cs`, always use verbatim strings (`@"..."`) for RTF content so that `\u328?` is treated as a literal RTF escape, not a C# unicode escape.

---

## 7. Band Heights

| Band | Height | Source | Notes |
|---|---|---|---|
| TopMargin | 50F | AppBaseReport | |
| PageHeader | 150F | AppBaseReport | Company letterhead (watermark) + header labels |
| Detail | varies | Derived report | Sized to fit content; `CanGrow=true` on controls |
| ReportFooter | 55F | AppBaseReport | Signatory block |
| BottomMargin | 60F | AppBaseReport | |

---

## 8. Header Layout (PageHeader — inherited from AppBaseReport)

| Control | X | Y | Width | Height | Alignment | Value |
|---|---|---|---|---|---|---|
| `xrLabelAppNumber` | 0F | 72F | 300F | 28F | MiddleLeft Bold | `[FullApplicationNumber]` |
| `xrLabelAppDate` | 0F | 102F | 300F | 28F | MiddleLeft | `[ApplicationDate]` → `{0:dd.MM.yyyy} ý.` |

---

## 9. Footer Layout (ReportFooter — inherited from AppBaseReport)

| Control | X | Y | Width | Height | Alignment | Value |
|---|---|---|---|---|---|---|
| `xrLabelSignatoryPosition` | 0F | 15F | 313F | 28F | MiddleLeft Bold | `[CompanyHead.Position.NameTm]` |
| `xrLabelSignatoryFullName` | 313F | 15F | 313.77F | 28F | MiddleRight Bold | `[CompanyHead.FullName]` |

---

## 10. Background Image

Background is rendered as a full-page `Watermark` — loaded automatically by `AppBaseReport.BeforePrint` using `Company.Code`. No per-derived-class code needed. See [REPORT_GENERATION_GUIDE.md — Section 6](REPORT_GENERATION_GUIDE.md#6-background-image-rule).

---

## Change Log

| Date | Change | Reason |
|---|---|---|
| 2026-04-06 | Document created | Establish standards for all reports |
| 2026-04-06 | Margins set to 100F left/right | Office Word default (1 inch) |
| 2026-04-06 | Font size set to 15pt | Matches government letter visual standard |
| 2026-04-06 | Body paragraphs: XRRichText with `\qj\fi720` | First-line indent + justified text; designer-editable |
| 2026-04-06 | Application date: bold | Matches application number style |
| 2026-04-06 | Field syntax changed from `«[F]»` to `"[F]"` | Guillemets render literally in v25.2; plain `[F]` is evaluated directly |
