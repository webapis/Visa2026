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
| Bottom margin | 100F | Increased from 60F to prevent signatory overlapping background footer text |
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
| Detail | varies | Derived report | Set to `last_control_end + 11F`; `ReportFooter` renders immediately after. If a group base sets the value, derived reports must not override it unless the derived report adds controls below the base's last control. |
| ReportFooter | 80F | AppBaseReport | Signatory block; `PrintAtBottom = false` so it follows content |
| BottomMargin | 100F | AppBaseReport | Must match `Margins.Bottom`; sized to clear background footer text |

---

## 8. Header Layout (PageHeader — inherited from AppBaseReport)

| Control | X | Y | Width | Height | Alignment | Value |
|---|---|---|---|---|---|---|
| `xrLabelAppNumber` | 0F | 72F | 300F | 28F | MiddleLeft Bold | `[FullApplicationNumber]` |
| `xrLabelAppDate` | 0F | 102F | 300F | 28F | MiddleLeft Bold | `[ApplicationDate]` → `{0:dd.MM.yyyy} ý.` |

---

## 9. Footer Layout (ReportFooter — inherited from AppBaseReport)

| Control | X | Y | Width | Height | Alignment | Extra | Value |
|---|---|---|---|---|---|---|---|
| `xrLabelSignatoryPosition` | 0F | 10F | 313F | 50F | **TopLeft** Bold | `CanGrow=true` `CanShrink=true` `Multiline=true` `WordWrap=true` | `[CompanyHead.Position.NameTm]` |
| `xrLabelSignatoryFullName` | 313F | 10F | 313.77F | 28F | MiddleRight Bold | — | `[CompanyHead.FullName]` |

> `xrLabelSignatoryPosition` uses **TopLeft** (not MiddleLeft) and H=50F to accommodate 2-line position titles (e.g. "Türkmenistandaky Şahamçasynyň müdiri"). `CanShrink=true` collapses unused height for single-line titles.

---

## 10. Variable Content Length Constraint

Groups A and C use `[ProjectContract_Description]` as `xrRichBody1`. Controls below it (`xrRichBody2`, `xrRichBody3`, `xrLabelAttachments`) have **fixed Y positions** — they do not move if body1 grows. `CanGrow=true` and `CanShrink=true` are set on `xrRichBody1` and `xrRichBody2` to handle minor variation, but overlapping will occur if content significantly exceeds the allocated height.

**Maximum allowed length for `ProjectContract_Description`:**

| Group | Body1 allocated height | Max safe character count (approx.) |
|---|---|---|
| A (`AppGroupABaseReport`) | 140F | ~600 characters (~7 lines at 15pt) |
| C (`AppGroupCBaseReport`) | 140F | ~600 characters (~7 lines at 15pt) |

> Admins entering `ProjectContract.Description` must keep the text within this limit. If the description is longer, it will overlap the request paragraph below it. This is a known constraint of the absolute-position layout — it is acceptable because `ProjectContract_Description` is standardized contract text that does not vary significantly in practice.

---

## 11. Background Image

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

## 14. Turkmen Language QA

**This check is mandatory.** Run it on every string before committing code, and again when a reference image is submitted for visual review.

---

### 14a. Special Character Substitution Errors

The most common class of mistake: ASCII or Turkish characters used where Turkmen-specific characters are required. Check every word in every static string.

| Wrong char | Looks like | Correct Turkmen char | Unicode | C# escape | RTF escape | Example mistake → fix |
|---|---|---|---|---|---|---|
| `n` at word end | suffix ending | `ň` | U+0148 | `\u0148` | `\u328?` | `-nyn` → `-nyň` |
| `y` in Turkmen words | Latin y | `ý` | U+00FD | `\u00FD` | `\u253?` | `Familiyasy` → `Famili`**ý**`asy` |
| `s` before vowel | Turkish/Latin s | `ş` | U+015F | `\u015F` | `\u351?` | `Dasary` → `Da`**ş**`ary` |
| `ğ` (Turkish g) | g with breve | `g` (plain) | — | `g` | `g` | `Doğlan` → `Dog`**lan** |
| `o` in Turkmen words | plain o | `ö` | U+00F6 | `\u00F6` | `\u246?` | `mohleti` → `m`**ö**`hleti` |
| `u` in Turkmen words | plain u | `ü` | U+00FC | `\u00FC` | `\u252?` | `hunari` → `h`**ü**`n`**ä**`ri` |
| `a` in Turkmen words | plain a | `ä` | U+00E4 | `\u00E4` | `\u228?` | `gore` → `g`**ö**`r`**ä** |
| `c` in Turkmen words | plain c | `ç` | U+00E7 | `\u00E7` | `\u231?` | `ucin` → `**ü**`**ç**`in` |

> **Trigger words to always double-check:** any word ending in `-nyň`, `-niň`, `-ynyň`, `-iniň` (genitive); any word containing `ş`, `ý`, `ö`, `ä`, `ü`, `ň`, `ç`.

---

### 14b. Grammar and Suffix Rules

**Genitive suffix** — the single most common error. The suffix must end in `ň`, never `n`.

| Context | Correct form | Wrong form |
|---|---|---|
| Singular noun genitive | `-nyň` / `-niň` | `-nyn` / `-nin` |
| Plural noun genitive (-lar/-ler) | `-larynyň` / `-leriniň` | `-larynyn` / `-lerinyn` |
| Noun ending in vowel, genitive | `-nyň` | `-nyn` |

**Common word errors found in this project:**

| Wrong | Correct | Rule |
|---|---|---|
| `raýatlarynyn` | `raýatlarynyň` | Genitive ň |
| `Pasportynyn` | `Pasportynyň` | Genitive ň |
| `Gelmeginiin` | `Gelmeginiň` | Triple-i → remove extra i, add ň |
| `maglumatary` | `maglumatlary` | Missing `l` |
| `Dasary` | `Daşary` | Missing ş |
| `Familiyasy` | `Familiýasy` | Missing ý |
| `Doğlan` | `Doglan` | Turkish ğ not in Turkmen |

---

### 14c. Pre-Code Review (Static Text)

Before writing `Designer.cs` code, list every static string — table headers, section labels, report titles — and run each through this checklist:

- [ ] Does every genitive suffix end in `ň` (not `n`)?
- [ ] Does every word with `ş` use `ş` (U+015F), not `s`?
- [ ] Does every word with `ý` use `ý` (U+00FD), not `y`?
- [ ] Are there any Turkish `ğ` characters? (There are none in Turkmen — always wrong)
- [ ] Does every word with `ö`, `ä`, `ü`, `ç`, `ň` use the correct Turkmen character?
- [ ] Are compound words spelled correctly? (e.g. `maglumatlary` not `maglumatary`)
- [ ] Do all column header texts match the approved map file exactly?

---

### 14d. Post-Code Review (String Literals in Designer.cs)

After writing `Designer.cs`, scan all `.Text`, `.Rtf`, and expression string arguments:

1. Search for any occurrence of `nyn` at word boundaries → should be `ny\u0148`
2. Search for `\u011F` (Turkish ğ) → must not appear in Turkmen text
3. Verify every title/label against the map file line by line
4. For multiline expressions (`Char(10)` joins): verify each segment individually

---

### 14e. Reference Image Review (when image is submitted after render)

When the user submits a screenshot of the rendered report:

1. **Title** — compare character by character with the coded string; verify ş, ý, ň
2. **Table headers** — verify each column header matches the map file; check all special chars
3. **Section labels** (GroupHeader text, sub-headers) — same check
4. **Static body text** (XRRichText paragraphs) — spot-check key words known to contain special chars
5. **Signatory block** — verify position and full name labels are aligned correctly

> Dynamic data (bound field values like names, dates, passport numbers) comes from the database and is not grammar-checked here. Only static strings are in scope.

---

### 14f. Character Reference Quick-Lookup

| Turkmen char | C# `\uXXXX` | RTF `\uN?` | Appears in |
|---|---|---|---|
| `ň` | `\u0148` | `\u328?` | Genitive, locative suffixes; "Türkmenistandaky" |
| `Ň` | `\u0147` | `\u327?` | Sentence-start form |
| `ş` | `\u015F` | `\u351?` | "Daşary", "Şahamça", "Işçi" |
| `Ş` | `\u015E` | `\u350?` | Sentence-start, proper nouns |
| `ý` | `\u00FD` | `\u253?` | "Ýurt", "ýatyrmak", "Familiýasy" |
| `Ý` | `\u00DD` | `\u221?` | Sentence-start |
| `ö` | `\u00F6` | `\u246?` | "möhleti", "görä", "köne" |
| `Ö` | `\u00D6` | `\u214?` | Sentence-start |
| `ä` | `\u00E4` | `\u228?` | "görä", "hünäri" |
| `Ä` | `\u00C4` | `\u196?` | Sentence-start |
| `ü` | `\u00FC` | `\u252?` | "üçin", "Üçin", "hünäri" |
| `Ü` | `\u00DC` | `\u220?` | Sentence-start |
| `ç` | `\u00E7` | `\u231?` | "çykarmak", "üçin", "Çakylyk" |
| `Ç` | `\u00C7` | `\u199?` | "Çakylyk", "Çalyşmagy" |

---

## 14. Recipient Label Standard (Letter-Type Reports)

For letter-type reports addressed to a named recipient, place a bold left-aligned label starting at X=220F so it occupies the right two-thirds of the page:

| Property | Value |
|---|---|
| Control | `XRLabel` |
| X | `220F` |
| Y | `20F` (from top of Detail) |
| Width | `406.7717F` |
| Height | `80F` with `CanGrow = true`, `CanShrink = true` |
| Font | Times New Roman 15pt Bold |
| `TextAlignment` | `TopLeft` |
| `WordWrap` | `true` |
| `Multiline` | `true` |
| `BackColor` | `Color.Transparent` |
| Binding | `ExpressionBinding("BeforePrint", "Text", "[RecipientField]")` |

> H=80F covers 3 lines at 15pt comfortably. `CanGrow` handles longer texts; `CanShrink` collapses unused space for shorter ones. The recipient control ends at **Y=100F** (20+80), which is the anchor for the greeting Y calculation (see §20).

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
| Y | `115F` (no urgency) or `150F` (with urgency) — see §20 |
| Width | `626.7717F` (full printable width) |
| Height | `35F` with `CanGrow = true` |
| Font | Times New Roman 15pt **Bold** |
| `TextAlignment` | `MiddleCenter` |
| `WordWrap` | `true` |
| `BackColor` | `Color.Transparent` |
| Binding | `ExpressionBinding("BeforePrint", "Text", "[Ministry_FormOfAddressField]")` |

> Both the recipient block and the greeting label must be **bold** — they are the most visually prominent elements in the letter header and must stand out against the background watermark.

---

## 20. Vertical Spacing Standards (Ministry Letter Reports)

All Y positions are relative to the top of the `Detail` band. Two layout variants exist depending on whether the report has an urgency line.

### Variant A — No urgency line (e.g. AppVisaAndWPExtReport, AppAdditionalWPLocationReport)

| Control | Y | Height | Ends at | Gap rule |
|---|---|---|---|---|
| `xrLabelRecipient` | 20F | 80F | 100F | Fixed top anchor |
| `xrLabelGreeting` | 115F | 35F | 150F | 15F after recipient end |
| `xrRichBody1` | 165F | varies | — | 15F after greeting end |
| `xrRichBody2..N` | prev_end + 8F | varies | — | 8F between body paragraphs |
| `xrLabelAttachments` | prev_end + 8F | 60F | — | 8F after last body |
| `Detail.HeightF` | — | attachments_end + 11F | — | 11F before ReportFooter Y offset |

### Variant B — With urgency line (e.g. AppInvReport, AppInvFMReport, AppInvAndWPReport, AppVisaExtFMReport)

| Control | Y | Height | Ends at | Gap rule |
|---|---|---|---|---|
| `xrLabelRecipient` | 20F | 80F | 100F | Fixed top anchor |
| `xrLabelUrgency` | 110F | 25F | 135F | 10F after recipient end |
| `xrLabelGreeting` | 150F | 35F | 185F | 15F after urgency end |
| `xrRichBody1` | 200F | varies | — | 15F after greeting end |
| `xrRichBody2..N` | prev_end + 8F | varies | — | 8F between body paragraphs |
| `xrLabelAttachments` | prev_end + 8F | 60F | — | 8F after last body |
| `Detail.HeightF` | — | attachments_end + 11F | — | 11F before ReportFooter Y offset |

### Quick reference — gap sizes

| Gap | Size | ≈ mm |
|---|---|---|
| Recipient → Urgency | 10F | ~2.5mm |
| Recipient → Greeting (no urgency) | 15F | ~4mm |
| Urgency → Greeting | 15F | ~4mm |
| Greeting → Body1 | 15F | ~4mm |
| Body → Body | 8F | ~2mm |
| Body / Attachments → Signatory | 11F + 10F ReportFooter offset | ~5mm |

> `ReportFooter.PrintAtBottom = false` ensures the signatory follows content directly. `Detail.HeightF` is the only lever controlling the gap to the signatory — set it to `last_control_end + 11F`.

---

## 21. Letter Content Groups and Group Base Classes

All Application-level letter reports fall into one of five **content groups**. Each group has a canonical content slot sequence and maps to a **group base class** that owns all shared controls. Derived reports only supply the one or two paragraphs that differ per ApplicationType.

### Why group base classes?

- **One change propagates to all.** Updating the Responsibility paragraph wording, recipient control size, font, or Y position in the group base class Designer.cs automatically updates every report in that group — no per-report edits.
- **Minimal derived class.** After migration, a derived report's entire `.cs` file is typically 5–10 lines (constructor + one Rtf assignment + `Detail.HeightF`). No Designer.cs file is needed for derived reports.
- **Proven by AppBaseReport.** The existing `AppBaseReport` proves the pattern works: it sets ExpressionBindings in `InitializeComponent()` code with an empty `.resx`, and all derived reports inherit those bindings correctly at runtime.

### Inheritance hierarchy

```
XtraReport
  └── AppBaseReport                (bands, header, footer, watermark — existing)
        ├── AppGroupABaseReport    Ministry letter — Employee — Invitation (urgency + greeting)
        ├── AppGroupBBaseReport    Ministry letter — Family Member (urgency + greeting + Berkarar intro)
        ├── AppGroupCBaseReport    Ministry letter — Extension / WP / BZ (no urgency)
        ├── AppGroupDBaseReport    Migration Service letter — Cancellation (fixed recipient)
        └── AppGroupEBaseReport    Migration Service letter — Registration (dynamic recipient)
```

---

### Group A — Ministry Letter · Employee · Invitation

**Base class:** `AppGroupABaseReport`  
**Reports:** `AppInvReport`, `AppInvAndWPReport`  
**Planned additions:** `AppInvAccordingToWPReport`, `AppServicePassportReport`  
**Spacing variant:** §20B (with urgency)

| Slot | Control | Content | Owner |
|---|---|---|---|
| Recipient | `xrLabelRecipient` | `[ProjectContract_Ministry_RecipientBlock]` | **Base** |
| Urgency | `xrLabelUrgency` | `[Urgency_NameTm]` + `Visible=[ApplicationType.ShowUrgency]` | **Base** |
| Greeting | `xrLabelGreeting` | `[ProjectContract_Ministry_FormOfAddress]` | **Base** |
| Body1 | `xrRichBody1` | `[ProjectContract_Description]` | **Base** |
| **Body2** | `xrRichBody2` | Request sentence (person count + type-specific action) | **Derived** |
| Body3 | `xrRichBody3` | Responsibility paragraph (shared constant) | **Base** |
| Attachments | `xrLabelAttachments` | Person list expression — **varies per report** | Derived sets expression |

**Y positions (§20B):** Recipient=20, Urgency=110, Greeting=150, Body1=200, Body2=348 (body1 end 340 + 8), Body3=body2_end+8, Attachments=body3_end+8.

**What differs between Group A reports:** only the Body2 RTF string (invitation only vs. invitation+WP) and the Attachments expression.

---

### Group B — Ministry Letter · Family Member

**Base class:** `AppGroupBBaseReport`  
**Reports:** `AppInvFMReport`, `AppVisaExtFMReport`  
**Spacing variant:** §20B (with urgency)

| Slot | Control | Content | Owner |
|---|---|---|---|
| Recipient | `xrLabelRecipient` | `[ProjectContract_Ministry_RecipientBlock]` | **Base** |
| Urgency | `xrLabelUrgency` | `[Urgency_NameTm]` + Visible | **Base** |
| Greeting | `xrLabelGreeting` | `[ProjectContract_Ministry_FormOfAddress]` | **Base** |
| Body1 | `xrRichBody1` | Static Berkarar intro paragraph | **Base** |
| Body2 | `xrRichBody2` | Static company partnership paragraph | **Base** |
| **Body3** | `xrRichBody3` | Request sentence (FM relationship + sponsor + VisaCategory + action verb) | **Derived** |
| Body4 | `xrRichBody4` | Responsibility paragraph | **Base** |
| Attachments | `xrLabelAttachments` | Person list expression | Derived sets expression |

**What differs between Group B reports:** only the action verb in Body3 (çakylyk vs. wiza uzaltmak) and the Attachments expression.

> **Berkarar paragraph and Company paragraph are identical in all Group B reports.** If this static text changes, edit `AppGroupBBaseReport.Designer.cs` once.

---

### Group C — Ministry Letter · Employee · Extension / WP / Border Zone

**Base class:** `AppGroupCBaseReport`  
**Reports:** `AppVisaAndWPExtReport`, `AppAdditionalWPLocationReport`, `AppBorderZonePermissionReport`  
**Planned additions:** `AppWPExtReport`, `AppVisaExtAccToWPReport`, `AppChangeVisaCategoryReport`  
**Spacing variant:** §20A (no urgency)

| Slot | Control | Content | Owner |
|---|---|---|---|
| Recipient | `xrLabelRecipient` | `[ProjectContract_Ministry_RecipientBlock]` | **Base** |
| Greeting | `xrLabelGreeting` | `[ProjectContract_Ministry_FormOfAddress]` | **Base** |
| Body1 | `xrRichBody1` | `[ProjectContract_Description]` | **Base** |
| **Body2** | `xrRichBody2` | Request sentence — N persons + **one type-specific field** | **Derived** |
| Body3 | `xrRichBody3` | Responsibility paragraph | **Base** |
| Attachments | `xrLabelAttachments` | Person list expression | Derived sets expression |

**Y positions (§20A):** Recipient=20, Greeting=115, Body1=165, Body2=313 (body1 end 305 + 8), Body3=body2_end+8, Attachments=body3_end+8.

**Type-specific field in Body2:** `[VisaCategory_NameTm]` (VisaAndWPExt), `[MovementPermitLocation_NameTm]` (AdditionalWPLocation), `[BorderZoneLocation_NameTm]` (BorderZone), etc.

---

### Group D — Migration Service Letter · Cancellation

**Base class:** `AppGroupDBaseReport`  
**Reports:** `AppCancelVisaReport`, `AppCancelVisaAndWPReport`, `AppCancelInvWPReport`, `AppChangePassportReport`  
**Planned additions:** `AppCancelWPReport`, `AppCancelBZReport`, `AppCancelAppReport`

| Slot | Control | Content | Owner |
|---|---|---|---|
| Recipient | `xrLabelRecipient` | **Static text:** "Türkmenistanyň Döwlet migrasia gullugynyn başlygyna" | **Base** |
| **Body1** | `xrRichBody1` | Cancellation/change request — N persons + what is being cancelled | **Derived** |
| Body2 | `xrRichBody2` | Responsibility paragraph | **Base** |

**Y positions:** Recipient at Y=217F (form background driven), Body1 at Y=297F (15F after recipient end 282F), Body2 at Y=body1_end+8F.  
**No Attachments, no Greeting, no Urgency.**

**What differs between Group D reports:** only the Body1 RTF — which count fields are used (CancelPersonCount, CancelWPCount, CancelInvCount) and what is being cancelled.

---

### Group E — Migration Service Letter · Registration

**Base class:** `AppGroupEBaseReport`  
**Reports:** `AppRegCheckInReport`, `AppRegCheckInInternalReport`, `AppRegCheckOutReport`, `AppRegCheckOutInternalReport`, `AppRegInfoChangeAddressReport`, `AppRegInfoChangePassportReport`, `AppRegExtReport`

| Slot | Control | Content | Owner |
|---|---|---|---|
| Recipient | `xrLabelRecipient` | `[MigrationService_NameTm]` (dynamic — regional office) | **Base** |
| **Body1** | `xrRichBody1` | Registration-specific request | **Derived** |
| Body2 | `xrRichBody2` | Responsibility paragraph | **Base** |

**Y positions:** Recipient at Y=218F, Body1 at Y=313F (15F after recipient end 298F), Body2 at Y=body1_end+8F.  
**No Attachments, no Greeting, no Urgency.**

---

### Shared Responsibility Paragraph — Named Constant

This RTF paragraph is identical in **every** implemented report across all five groups. It is defined once as a `static readonly string` constant in `AppBaseReport.cs` and referenced by all group base classes:

```csharp
// In AppBaseReport.cs
public static readonly string RtfResponsibility =
    @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}" +
    @"\f0\fs30\pard\qj\fi720 " +
    @"Da\u351?ary \u253?urt ra\u253?atyny\u328? T\u252?rkmenistana gelmegini\u328?, " +
    @"onda bolmagyny\u328? we ondan gitmegini\u328? d\u252?zg\u252?nlerini berja\u253? " +
    @"etmegine jogapk\u228?r\u231?iligi kompani\u253?amyz \u246?z \u252?st\u252?ne al\u253?ar.\par}";
```

Each group base class sets: `this.xrRichBodyN.Rtf = AppBaseReport.RtfResponsibility;`

If the text ever changes → one edit → all groups and all reports inherit the update.

---

### Derived Report Pattern After Migration

A derived report is reduced to its `.cs` file only — no Designer.cs, no complex `.resx`:

```csharp
// AppCancelVisaReport.cs — complete file
public partial class AppCancelVisaReport : AppGroupDBaseReport
{
    public AppCancelVisaReport()
    {
        this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}" +
            @"\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky " +
            @"\b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt " +
            @"ra\u253?atyny\u328? \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany " +
            @"\b wizasyny \u253?atyrmagy\u328?yzy\b0  Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";
        this.Detail.HeightF = 492F;
    }
}
```

```csharp
// AppVisaAndWPExtReport.cs — complete file (Group C, no urgency)
public partial class AppVisaAndWPExtReport : AppGroupCBaseReport
{
    public AppVisaAndWPExtReport()
    {
        this.xrRichBody2.Rtf = @"{\rtf1...[TotalPersonCount]...[VisaCategory_NameTm]...uzaldylmagyna...}";
        this.xrLabelAttachments.ExpressionBindings.AddRange(new ExpressionBinding[] {
            new ExpressionBinding("BeforePrint", "Text", "... attachments expression ...")
        });
        this.Detail.HeightF = 570F;
    }
}
```

### `.resx` Rule for Group Base Classes

Group base class ExpressionBindings are set in `InitializeComponent()` code — no `.resx` entries needed in the base class (same proven pattern as `AppBaseReport`). Derived class `.resx` only needs an entry if the derived class itself adds a new `ExpressionBinding` (e.g., the Attachments expression for Groups A/B/C). For Groups D/E derived reports, the `.resx` stays empty — there are no derived-class bindings.

### Migration Strategy

1. **New reports:** always inherit from the appropriate group base class — no Designer.cs, minimal `.cs`.
2. **Existing reports:** migrate on next edit. When touching a report for any reason, move its shared controls to the group base class and strip the derived Designer.cs.
3. **Group base classes:** create as needed — start with whichever group has the next report planned.

> Do not create a group base class speculatively for a group with only one existing report and no imminent additions. Create it when the second report in the group is being built.

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
| 2026-04-09 | §1 Bottom margin 60F→100F | Prevents signatory overlapping background letterhead footer text |
| 2026-04-09 | §7 ReportFooter 55F→80F, PrintAtBottom=false | Signatory follows content; taller band fits 2-line position titles |
| 2026-04-09 | §9 Signatory position: Y=10F, H=50F, TopLeft, CanGrow/CanShrink | Accommodates multi-line position titles without clipping |
| 2026-04-09 | §14 Recipient H=80F, CanShrink=true | Reduces visual gap caused by unused space in oversized control |
| 2026-04-09 | §19 Greeting Y updated to formula-based | 115F (no urgency) or 150F (with urgency) from §20 |
| 2026-04-09 | Added §20 Vertical Spacing Standards | Canonical gap table for all Ministry letter reports; applied to Group A reports |
| 2026-04-10 | Added §21 Letter Content Groups and Group Base Classes | Five content groups identified; group base class inheritance pattern defined to make group-wide updates propagate automatically |
