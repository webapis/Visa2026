# Learnings (append-only): Word reports in Visa2026

Purpose: capture pitfalls, DocxTemplater quirks, OpenXml generator patterns, and preview workflow lessons from **each** report you design or change. The skill **gains experience** through this file: agents should **read entries before** starting a similar task and **append after** finishing.

Keep **`SKILL.md`** stable; **promote** a bullet into `SKILL.md` only when the same lesson has recurred or is a hard standard for everyone.

## How to use

**Before** implementing a new or unfamiliar Word report: skim **## Entries** (newest first is fine) for related template names, `GenerateTemplates`, or DocxTemplater issues.

**After** the task is done (user accepted or PR-ready), **append** one entry:

- **Date** (YYYY-MM-DD)
- **Layout family** (e.g. L1, T1, F1 — see `SKILL.md` / `reference.md`)
- **Data anchors** (e.g. header `Application`; rows `ApplicationItem` / `Registration` / `BusinessTrip`)
- **Report / template** (e.g. `App_Inv_And_WP_Borcnama_Item.docx`, `BusinessTripSanawyReportDef`)
- **Symptom** (what went wrong or what was non-obvious)
- **Root cause** (if known)
- **Fix**
- **Prevent** (what to do next time)

Template:

```markdown
### YYYY-MM-DD — <TemplateFile or ReportDef class> (family: L?)

- **Symptom**:
- **Root cause**:
- **Fix**:
- **Prevent**:
```

---

## Entries

### 2026-05-12 — Skill: learnings log for Word pipeline

- **Symptom**: Same DocxTemplater / OpenXml / preview mistakes could repeat on every new report.
- **Root cause**: No project-local append-only log tied to `visa2026-word-reports`.
- **Fix**: Added this file; `SKILL.md` requires read-before / append-after for each report task.
- **Prevent**: Always append at end of a report design task; promote repeated lessons into `SKILL.md`.

### 2026-05-12 — `GenerateTemplates` output path vs embedded `Resources`

- **Symptom**: Regenerated `.docx` did not appear in the app; `Visa2026.Module/Resources` stayed stale.
- **Root cause**: Relative path from `tools/GenerateTemplates/bin/...` used one too many `..` segments, writing outside the repo tree.
- **Fix**: Paths must resolve to **`Visa2026.Module/Resources/`** inside **this** repository (e.g. five `..` from `net8.0` output, not six).
- **Prevent**: After `dotnet run` GenerateTemplates, confirm the printed path is under the repo; spot-check file timestamp in `Visa2026.Module/Resources/`.

### 2026-05-12 — `PreviewWordReports` / one-page legal forms (`App_Inv_And_WP_Borcnama_Item`) (family: **F1**)

- **Symptom**: Signature block spilled to page 2; rules looked edge-to-edge.
- **Root cause**: Default paragraph/line spacing and margins too generous for A4 with long static body text.
- **Fix**: In `MakeBorcnamaTemplate()`: reduced `BodyJustified` line twips and paragraph spacing, tighter page margins, smaller caption `spaceBefore`, less table cell padding; rules stay within `contentW` (page width minus left/right margins).
- **Prevent**: For ministry one-pagers, tune spacing in the **generator** and verify with **longest realistic** registry/passport strings via `PreviewWordReports`; close Word if preview write fails with file lock.

### 2026-05-12 — Milestone: first completed Word report (`App_Inv_And_WP_Borcnama_Item`) (family: **F1**)

- **Layout family**: F1 (statutory item form).
- **Data anchors**: `Application` + per-row `ApplicationItem` via `FillListForm` (`{{#ds.rows}}`, `{{:s:}}{{:PageBreak}}` between persons); `AppInvAndWPBorcnamaItemReportDef`.
- **Symptom**: N/A (success record).
- **Outcome**: End-to-end pattern proven—**`FormTemplates/`** scan + map as reference, **`MakeBorcnamaTemplate`** in `tools/GenerateTemplates/Program.cs`, embedded **`App_Inv_And_WP_Borcnama_Item.docx`**, **`PreviewWordReports`** preset **`borcnama`** with yellow merge highlights, layout fits **one A4** with realistic dump data, user sign-off on preview vs ministry form.
- **Prevent**: Use Borçnama as the **reference F1** when adding similar one-page commitment forms; update **`review-status.md`** when other templates reach **Completed**.

### 2026-05-13 — `PreviewWordReports` yellow highlight gaps

- **Symptom**: Not all merged “dynamic” fields were highlighted; short strings (`iki`), ints (`TotalPersonCount`), mixed paragraphs, and `date + ý.` splits were missed.
- **Root cause**: Highlighter required **full paragraph** or **full run** equality, min length 4, and only **string** dictionary values.
- **Fix**: Paragraph plain text + substring intervals (longest-first, non-overlapping), `w:br`/tab-aware text, collect ints ≥2 digits, strings ≥2 chars, **`AddSingleDataComposites`** (`n (text)`, `n-daşary`, `ApplicationDate ý.`).
- **Prevent**: When a preset adds new bind shapes, extend composites if fragments don’t appear as a single preset value.

### 2026-05-13 — `App_Inv_And_WP_Letter.docx` Goşundy block (family: **L2**)

- **Symptom**: Template used XAF-style “sanawy + maglumaty” attachment lines; ministry scan `App_Inv_And_WP_app.jpg` shows “2-pasport kopiýalary” and “Goşundy (N-daşary ýurt raýatynyň maglumaty)”.
- **Root cause**: Word generator copied `xrLabelAttachments` wording; scan is a different Çalık sample.
- **Fix**: `MakeAppInvAndWPLetterTemplate()` — two left-aligned paragraphs for Goşundy; second line uses `{{ds.TotalPersonCount}}-daşary…`. Map documents Word vs XAF divergence. Preview preset `inv-and-wp-letter` uses full GT-15 `ProjectContract_Description` from importer/LOOKUPS for scan-like Maksady QA.
- **Prevent**: For L2 letters, when a **FormTemplates** `.jpg` disagrees with XAF, treat the scan as authority for **Word** and record XAF vs Word in `*_map.md`; use separate `<w:p>` for attachment lines (avoid `\n` in one `w:t`).

### 2026-05-13 — `App_Inv_And_WP_Letter` ministry addressee two-line step (family **L2**)

- **Symptom**: Single right-aligned `RecipientBlock` paragraph did not match ministry sample (line 2 stepped right under line 1).
- **Root cause**: One merge field; no OOXML indent on continuation line.
  - **Fix**: `MinistryRecipientBlockFormatter.SplitIntoAddressLines` (newline or ` korporasiýasynyň` split); `AppInvAndWPLetterReportDef` adds `Line1` / `Line2` / `HasLine2`; Word layout uses a **borderless table** (spacer + address column) with **left-aligned** lines in the address cell — see `AppendMinistryRecipientBlockRightColumnTable`. Documented in `reference.md`, maps, `WORD_REPORT_GENERATION_IDEA.md`.
- **Prevent**: Reuse formatter when rolling the same layout to other L2 letters; extend split patterns only with ministry-approved samples.

### 2026-05-13 — Letter category: signatory block standard (families **L1–L3**)

- **Symptom**: Risk of one-off signatory layouts (tabs, different column widths) and repeated QA churn.
- **Root cause**: Layout lived only in `AppendSignatoryLetter` without a named standard in the Word-reports skill.
- **Fix**: **`reference.md`** — subsection *Signatory block (company head)* (terminology, table geometry, tokens, scan-exception rule). **`SKILL.md`** — letter-category bullets point at body + signatory standards; repaired layout-family table. **`FormalCompanyLetterLayout`**: `SignatoryLeftColumnTwips`, `SignatoryParagraphSpaceBefore`; `AppendSignatoryLetter` uses them.
- **Prevent**: New L1–L3 letters call `AppendSignatoryLetter` (or match its spec); document scan-only deltas in `*_map.md`.

### 2026-05-13 — Letter category: body first-line indent + responsibility (families **L1–L3**)

- **Symptom**: Justified letter bodies mixed **OOXML `w:firstLine` 720** with **four leading spaces** in literals; business-trip letter had a corrupted responsibility string (mixed scripts).
- **Root cause**: Legacy mimic of RTF `\fi720` was duplicated in the run text; responsibility text was copy-pasted per template.
- **Fix**: `FormalCompanyLetterLayout` in `tools/GenerateTemplates/Program.cs` — shared **`JustifiedBodyFirstLineIndentTwips`** + **`ResponsibilityPlain`** (matches `AppBaseReport.RtfResponsibility`); stripped leading spaces from letter body strings; `MakeJustifiedParagraph` / `MakeMaksadyParagraph` / `InvAndWPLetterBodyParagraphProperties` use the constant; regenerated all letter `.docx`. Documented in `reference.md` (Letter category) and `SKILL.md` (L1–L3 note).
- **Prevent**: For new company→organization letters, never prefix static Turkmen with spaces; use one responsibility source; document scan-only exceptions in `*_map.md`.

### 2026-05-13 — `App_Labor_Contract_Item` redesigned (family **F2**)

- **Layout family**: F2 (sectioned item contract with 7 numbered sections).
- **Data anchors**: `Application` root + per-row `ApplicationItem` via `FillListForm` (`{{#ds.rows}}`, `{{:s:}}{{:PageBreak}}` between persons).
- **Symptom**: Original Word template used tab-based signature layout and lacked bold formatting in intro paragraph; didn't match `AppLaborContractItemReportV2` (XtraReport) layout.
- **Root cause**: Initial generator used simple single-run paragraphs; tabs don't align properly for two-column signatures.
- **Fix**: 
  - **Cross-cutting applicability**: `ApplicableApplicationTypeNames => Array.Empty<string>()` + `IsApplicable` checks for items (like XtraReport).
  - **Bold intro**: Multi-run paragraph with bold `{{Person_FullName}}` and `{{Position_PositionTm}}` only (matching XtraReport RTF).
  - **Two-column signatures**: Borderless table with fixed column widths (5200 twips each), matching XtraReport `panelSignatures` layout.
  - **Map**: Created `App_Labor_Contract_item_word_map.md` as build contract.
  - **Preview**: Added `labor-contract` preset with scan-transcribed dump data.
- **Prevent**: For F2 family forms with mixed static/dynamic content, use multi-run paragraphs for selective bold; use borderless tables (not tabs) for aligned columns; always create dedicated `*_word_map.md` when paralleling an XtraReport.

### 2026-05-13 — Labor Contract single-page layout refinements

- **Symptom**: Numbered clauses (1.1, 1.2) misaligned; first-line indent caused offset; font sizes inconsistent.
- **Root cause**: Default `indent: 360` on section bodies pushed numbers inward; some headers used 20pt vs 22pt; empty paragraphs created uneven spacing.
- **Fix**:
  - **Aligned numbering**: Removed `firstLineIndent` from section bodies (set to 0) so all `x.y` items align at left margin.
  - **Uniform fonts**: All text 11pt (22 half-points) — title, headers, body, signatures same size (boldness preserved for hierarchy).
  - **Tight spacing**: Use `spaceAfter` parameter (20-40 twips) instead of empty paragraphs for consistent gaps.
  - **Single-page fit**: Margins reduced to 432 twips (~0.3"), compact line spacing.
- **Prevent**: For sectioned contracts with numbered clauses, avoid first-line indent on body paragraphs; use explicit `spaceAfter` values; verify all `sz` parameters match after edits.

### 2026-05-13 — `App_Visa_And_WP_Ext_Letter.docx` (family: L2)

- **Symptom**: `MakeCompanyLetterTemplate` used wrong DocxTemplater keys (no `ds.` prefix), invalid conditionals, and a broken call site (orphaned `body2Text:` arguments); margins referenced non-existent `FormalCompanyLetterLayout` members.
- **Root cause**: Early stub never aligned with `WordFormFillerService` binding or the ministry scan; partial refactor left syntax errors.
- **Fix**: Replaced with `MakeAppVisaAndWPExtLetterTemplate` — clone of Inv+WP stepped header via `AppendMinistrySteppedHeaderWithUrgency(..., conditionalUrgency: true)`, shared `InvAndWP*` spacing/margins, `MakeVisaAndWPExtExtensionRequestParagraph` matching XAF bold spans, scan-accurate Goşundy two-line block; `AppVisaAndWPExtLetterReportDef` adds recipient split + `ApplicationType_ShowUrgency` + `Urgency_NameTm`; preview preset uses scan-derived dump data.
- **Prevent**: After changing a `Make*(` signature at a top-level call site, never leave dangling named arguments; always use `{{ds.Key}}`; reuse proven ministry header helpers instead of one-off company-letter stubs.

### 2026-05-13 — `App_Visa_And_WP_Ext_Letter.docx` production sign-off

- **Outcome**: User accepted **runtime** Resminamalar PDF/Word output (not preview): single page, ministry recipient on the right, formal body/signatory; no further layout changes requested.
- **Record**: `review-status.md` Notes, `App_Visa_and_WP_Ext_app_map.md`, `App_Visa_And_WP_Ext_Letter_word_map.md` (Product sign-off).

### 2026-05-13 — Preview highlights for Goşundy `TotalPersonCount` (single digit)

- **Symptom**: In `visa-and-wp-ext-letter` preview, `1` in `– 1 sany` / `( 1 sany` stayed unhighlighted; other `Application` fields highlighted.
- **Root cause**: `CollectMatchStrings` skips integers whose decimal string is shorter than 2 characters; those fragments are not full dictionary string values.
- **Fix**: `AddSingleDataComposites` adds phrases `– {n} sany`, `( {n} sany`, and `pasport nusgalary – {n} sany` (en dash) whenever `TotalPersonCount` is present so the highlighter matches merged Goşundy lines.
- **Prevent**: For any template where a numeric `ds` field renders as a single digit next to static Turkmen, add a composite phrase in `AddSingleDataComposites` (or preset-only composites) rather than lowering `MinCandidateLength` globally.

### 2026-05-13 — Ministry addressee: table column instead of dual right-aligned paragraphs

- **Symptom**: Two right-aligned lines of different lengths had **staggered left edges**; a **too-narrow** address column (~5600 twips) then made **line 2 wrap mid-phrase**, looking like three separate addressee lines. Later, a **too-wide** address column (tiny spacer + “rest of printable width”) left short ministry text visually **centered** on the page instead of on the **right**.
- **Root cause**: `w:jc right` on both lines; shorter line 1 sits visually “inside” line 2’s horizontal span. Fixed-width address cell was smaller than typical `RecipientBlock` line length. An oversized address cell left-aligned short text at the **left edge of a very wide cell**.
- **Fix**: `AppendMinistryRecipientBlockRightColumnTable` — full-width **borderless table**: **wide spacer** + **capped address column** (`MinistryRecipientTableAddressColumnMaxTwips` etc., floor **4800**, min spacer **360**), **`w:jc left`** for both lines in the address cell. `AppInvAndWPPrintableWidthTwips` centralizes width math.
- **Prevent**: For multi-line right-side blocks where line lengths differ, prefer **table column** or **frame** with left-aligned text over paired right-aligned paragraphs. Tune **max** address width vs longest real `RecipientBlock` lines to balance **right placement** vs **wraps**; document the chosen cap in **`reference.md`** / map if you change it.

### 2026-05-14 — `App_Visa_WP_Ext_Energy_To_Construction_Ministry_Letter.docx` (family: L3)

- **Symptom**: Needed a second Word output for `App_Visa_and_WP_Ext` that mirrors an official **Energy ministry → Construction ministry** scan (GT-15 narrative), with only two yellow merge slots from `Application`.
- **Root cause**: Existing `App_Visa_And_WP_Ext_Letter` is company→Migration/ministry (`AppVisaAndWPExtLetterReportDef`); different static body and **static minister signatory** (not `CompanyHead`).
- **Fix**: New `AppVisaWPExtEnergyToConstructionMinistryLetterReportDef` + `MakeAppVisaWPExtEnergyToConstructionMinistryLetterTemplate`; merge keys `CancelPersonCount`, `CancelPersonCountText`, `VisaPeriod_NameTm`; `IsApplicable` when `ProjectContract.Code` is `GT-15`; scan under `FormTemplates/App_Visa_WP_Ext_Energy_To_Construction_Ministry_scan.png`; preview preset `energy-to-construction-ministry-letter` + `AddSingleDataComposites` branch for `CancelPersonCount`/`CancelPersonCountText` phrase highlight.
- **Prevent**: When static narrative is contract-specific, encode applicability in `IsApplicable` (or drive body from `ProjectContract_Description`) so unrelated applications do not download misleading ministry text.
