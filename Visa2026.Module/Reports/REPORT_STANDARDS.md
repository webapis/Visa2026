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
\u8220?[FieldName]\u8221?
```

| Character | Unicode | Decimal | RTF escape |
|---|---|---|---|
| `"` (left double quote) | U+201C | 8220 | `\u8220?` |
| `"` (right double quote) | U+201D | 8221 | `\u8221?` |

**Example — bold dynamic value in the middle of a paragraph:**
```
\b \u8220?[TotalPersonCount]\u8221? (\u8220?[TotalPersonCountText]\u8221?)\b0
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
| `xrLabelAppDate` | 0F | 102F | 300F | 28F | MiddleLeft Bold | `[ApplicationDate]` → `{0:dd.MM.yyyy} ý.` |

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

## 11. XRLabel — Required Properties

Always set these properties when declaring an `XRLabel` in `Designer.cs`:

| Property | Value | When |
|---|---|---|
| `Font` | `DXFont("Times New Roman", 15F[, Bold])` | Always |
| `LocationFloat` | `PointFloat(x, y)` | Always |
| `SizeF` | `SizeF(width, height)` | Always |
| `TextAlignment` | e.g. `MiddleLeft`, `TopRight` | Always |
| `Name` | descriptive string | Always |
| `CanGrow` | `true` | When text may wrap to more than one line |
| `WordWrap` | `true` | When `CanGrow = true` or `Multiline = true` |
| `Multiline` | `true` | When the label may contain line breaks |
| `BackColor` | `Color.Transparent` | Always — allows background watermark to show through |

**Expression binding syntax** (for data-bound labels):
```csharp
this.xrLabel.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
    new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FieldName]")
});
```
- First argument: always `"BeforePrint"`
- Second argument: the property to bind — always `"Text"` for label content
- Third argument: the expression — `"[FieldName]"` for a direct field, or a compound expression like `"[Field1] + ' ' + [Field2]"`

> **Also update the `.resx` file** whenever you add or change an `ExpressionBinding` in `Designer.cs`. See `REPORTS.md — .resx Sync Requirement`. Without this, the binding is silently ignored at runtime.

---

## 12. XRRichText — Required Properties and Pattern

Always set these properties when declaring an `XRRichText` in `Designer.cs`:

| Property | Value | Why |
|---|---|---|
| `BackColor` | `Color.Transparent` | Allows background watermark to show through |
| `Borders` | `BorderSide.None` | Removes the default visible border |
| `CanGrow` | `true` | Allows the control to expand if text wraps to more lines than expected |
| `LocationFloat` | `PointFloat(x, y)` | Always |
| `SizeF` | `SizeF(width, height)` | Set to fit expected content (2–3 lines at 15pt ≈ 70–80F height) |
| `Name` | descriptive string | Always |

**`ISupportInitialize` pattern** — `XRRichText` requires `BeginInit` / `EndInit`. Set the `Rtf` property **after** `EndInit`:

```csharp
((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).BeginInit();
this.xrRichBody1.BackColor  = System.Drawing.Color.Transparent;
this.xrRichBody1.Borders    = DevExpress.XtraPrinting.BorderSide.None;
this.xrRichBody1.CanGrow    = true;
this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 155F);
this.xrRichBody1.Name = "xrRichBody1";
this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 80F);
((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
// Rtf must be set AFTER EndInit
this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 [text]\par}";
```

---

## 13. RTF Inline Formatting Reference

Use these codes inside RTF text to apply character-level formatting:

| Effect | RTF code | Notes |
|---|---|---|
| Bold on | `\b ` | Space after `\b` is required |
| Bold off | `\b0 ` | Space after `\b0` is required |
| Italic on | `\i ` | |
| Italic off | `\i0 ` | |
| Underline on | `\ul ` | |
| Underline off | `\ul0 ` | |
| Font size | `\fsN` | N = half-points; 15pt = `\fs30` |

**Example — bold segment in the middle of a sentence:**
```
normal text \b bold text\b0  normal text again
```
Note the space before bold text and after `\b0` to prevent characters from merging.

---

## 14. Recipient Label Standard (Letter-Type Reports)

For letter-type reports addressed to a named recipient, place a bold right-aligned label on the **right half** of the Detail band:

| Property | Value |
|---|---|
| Control | `XRLabel` |
| X | `313F` (starts at half-page split point) |
| Y | `30F` (from top of Detail) |
| Width | `313.7717F` (right half of printable area) |
| Height | `100F` with `CanGrow = true` |
| Font | Times New Roman 15pt Bold |
| `TextAlignment` | `TopRight` |
| `WordWrap` | `true` |
| `Multiline` | `true` |
| `BackColor` | `Color.Transparent` |
| Binding | `ExpressionBinding("BeforePrint", "Text", "[RecipientField]")` |

Body paragraphs (`XRRichText`) start at `Y = 155F` — leaving a `125F` gap below the recipient block for visual separation.

---

## Change Log

| Date | Change | Reason |
|---|---|---|
| 2026-04-06 | Document created | Establish standards for all reports |
| 2026-04-06 | Margins set to 100F left/right | Office Word default (1 inch) |
| 2026-04-06 | Font size set to 15pt | Matches government letter visual standard |
| 2026-04-06 | Body paragraphs: XRRichText with `\qj\fi720` | First-line indent + justified text; designer-editable |
| 2026-04-06 | Application date: bold | Matches application number style |
| 2026-04-06 | Field syntax: `\u8220?[F]\u8221?` (curly quotes) | Guillemets render literally in v25.2; curly quotes `" "` are the display standard |
| 2026-04-06 | Added Sections 11–14 | XRLabel/XRRichText required properties, RTF formatting codes, recipient label standard, expression binding syntax |
