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

> **Critical — never bind a plain-text DB field to `XRRichText.Rtf`:** Binding a plain string to the `Rtf` property causes the control to render in a tiny default font because the string is not valid RTF. Use one of these two patterns instead:
> - Plain text DB field → `XRLabel` with `Text` expression binding (formatting owned by the control)
> - Long plain text DB field inside a paragraph → embed `[FieldName]` inside a static RTF template string set on `Rtf` in Designer.cs (e.g. `\pard\qj\fi720 [ProjectContract_Description]\par`)

---

## 5. Dynamic Values in XRRichText (Field References)

XtraReports evaluates `[FieldName]` expressions found inside `XRRichText` RTF content at render time — no special delimiters are required.

**Syntax in RTF string (in Designer.cs):**
```
[FieldName]
```

**Example — bold dynamic value in the middle of a paragraph:**
```
\b [TotalPersonCount] ([TotalPersonCountText])\b0
```
This renders as: **2 (iki)** with surrounding text in normal weight. No quotes around dynamic values — bold is sufficient visual distinction.

> **Do not use guillemets** (`«[FieldName]»` / `\u171?...\u187?`) or curly quotes (`\u8220?...\u8221?`). Both render as literal characters alongside the evaluated value in XtraReports v25.2.

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

## 15. Conditional Visibility Pattern

To show or hide a control based on an `ApplicationType` flag, use a `Visible` expression binding — no code-behind needed:

```csharp
this.xrLabelUrgency.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
    new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",    "[Urgency_NameTm]"),
    new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Visible", "[ApplicationType.ShowUrgency]")
});
```

- `Visible` binding evaluates to `true`/`false` at render time
- Dot notation works in expression bindings (e.g. `[ApplicationType.ShowUrgency]`) — unlike `GetCurrentColumnValue` which requires flat names
- Also add both bindings to the `.resx` file: `BeforePrint,Visible,[ApplicationType.ShowUrgency]`
- When the control is hidden, it still occupies space — set `CanShrink = true` if the band should collapse when the control is hidden

---

## 16. Line Breaks in XRLabel Expressions

To produce a multi-line value in an `XRLabel` expression binding, use `Char(10)` (line feed). Do **not** use `\n` — that is RTF/C# syntax and will render literally in an expression.

```csharp
// Correct — actual Unicode characters, Char(10) for line break
"'Goşundy:   1. ' + [TotalPersonCount] + '-pasport kopiýalary,' + Char(10) + '           2. Goşundy (' + [TotalPersonCount] + '-daşary ýurt raýatynyň maglumaty)'"
```

Also set `Multiline = true` and `WordWrap = true` on the label so the line break renders correctly.

---

## 17. Deep Navigation [NotMapped] Properties

When a report needs data from a chain of navigation properties (e.g. `Application → ProjectContract → Ministry → RecipientBlock`), add a `[NotMapped]` flat property on the report's data type (`Application`) for **each field needed**:

```csharp
[XafDisplayName("Ministry Recipient Block"), VisibleInDetailView(false), VisibleInListView(false)]
[NotMapped]
public string ProjectContract_Ministry_RecipientBlock => ProjectContract?.Ministry?.RecipientBlock;
```

**Naming convention:** join each navigation step with `_` (e.g. `ProjectContract_Ministry_RecipientBlock`).
**Null safety:** always use `?.` at every navigation step to prevent `NullReferenceException` at report render time.
**Depth limit:** there is no hard limit, but chains deeper than 3 levels are a sign the data should be denormalized or a dedicated BO property added.

Also add the corresponding entry to the `.resx` file so the expression binding is recognized at runtime.

---

## 18. Resx Must Be Updated When Control Name or Type Changes

If you rename a control (e.g. `xrRichRecipient` → `xrLabelRecipient`) or change its type (`XRRichText` → `XRLabel`), you **must** update the `.resx` file to match:

- Update the `name` attribute of the `<data>` entry to use the new control name
- Update the property in the binding if it changed (e.g. `Rtf` → `Text`)
- Remove any stale entries for controls that no longer exist

Stale resx entries do not cause compile errors — they fail silently at runtime, causing expression bindings to be ignored.

---

## 19. Greeting Label Standard (Letter-Type Reports)

For letter-type reports that include a salutation line (e.g. "Hormatly Durdy Baýjanowiç!"), place a bold centered label below the urgency note and above body paragraph 1:

| Property | Value |
|---|---|
| Control | `XRLabel` |
| X | `0F` |
| Y | `185F` (below urgency note at ~150F) |
| Width | `626.7717F` (full printable width) |
| Height | `35F` with `CanGrow = true` |
| Font | Times New Roman 15pt **Bold** |
| `TextAlignment` | `MiddleCenter` |
| `WordWrap` | `true` |
| `BackColor` | `Color.Transparent` |
| Binding | `ExpressionBinding("BeforePrint", "Text", "[Ministry_FormOfAddressField]")` |

> Both the recipient block and the greeting label must be **bold** — they are the most visually prominent elements in the letter header and must stand out against the background watermark.

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
| 2026-04-07 | Added Section 15: Greeting label standard | Recipient block and greeting must both be bold — confirmed in AppInvReport |
| 2026-04-07 | Dynamic field syntax: no quotes, bold only | Curly quotes render literally; plain `[FieldName]` with `\b...\b0` is the standard |
| 2026-04-07 | Added Sections 15–19 | Patterns from AppInvReport: conditional visibility, Char(10) line breaks, deep NotMapped chains, resx-on-rename rule, greeting label standard |
