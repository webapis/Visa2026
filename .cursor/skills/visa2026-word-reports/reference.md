# Word report layout families — reference matrix

Companion to **`SKILL.md`**. Use when classifying a new report or aligning typography with an existing generator.

**OOXML note:** `w:sz` / `FontSize` values are **half-points** (e.g. `30` = 15 pt).

## Data anchors (business objects)

| Type | Path | Role |
|------|------|------|
| `Application` | `Visa2026.Module/BusinessObjects/Application.cs` | Root for **Resminamalar**; header `ds.*`; owns collections below. |
| `ApplicationItem` | `…/ApplicationItem.cs` | Per-person merge fields; typical **row** in item/sanawy lists. |
| `Registration` | `…/Registration.cs` | Registration list / movement reports; **rows** from `Application.Registrations`. |
| `BusinessTrip` | `…/BusinessTrip.cs` | Trip sanawy / letters; **rows** from `Application.BusinessTrips`. |

Placeholder names and dictionary keys must match **`docs/WORD_REPORT_GENERATION_IDEA.md`** and actual properties—if unclear, **ask the user** (see **`SKILL.md`** → *Missing information*).

**`FormTemplates/`:** ministry **scans** (static text + layout) and **`*_map.md`** (dynamic fields, widths, BO-oriented labels) — use both when designing or reviewing Word output; when paralleling an XtraReport, reconcile map + Word + `Visa2026.Module/Reports/*`.

## Map document checklist (`FormTemplates/<basename>_map.md`)

Create or extend a map **before** coding `GenerateTemplates` / new `Resources/*.docx` for a report. Keep the scan image **in the same folder** with a name referenced from the map.

**Required sections (in order):**

1. **Title + status** — Report name, Word template filename (`Resources/*.docx`), XtraReport class if any, `ApplicationType` / visibility.
2. **Identity** — Table: class, registered name, display name (Tm), application type criteria, **`Data type`** row: primary object(s) passed to generation (e.g. `Application` for `FillForm`; row type `ApplicationItem` | `Registration` | `BusinessTrip` for `FillListForm` / loops). State explicitly if the controller contract differs (today **`WordReportsController`** uses **`Application`** as root).
3. **Reference image(s)** — Filename(s) under **`FormTemplates/`** (e.g. `App_Inv_And_WP_app.jpg`); note if multiple crops exist.
4. **Layout / bands** — For reports with regions (XAF bands, table columns): positions, static vs dynamic, typography notes.
5. **Field contract** — Tables: placeholder or label → BO property path (`Application.*`, `ApplicationItem.*`, …) → `ds.*` / `{{.Field}}` key; mark static ministry text.
6. **Word-specific** — Embedded resource name (`Resources/*.docx`), `PreviewWordReports` preset name, scan vs XAF typography differences.
7. **Preview dump data (from scan)** — Table or bullet list: each **dynamic** `ds.*` / row key → **literal value transcribed from the reference scan** (same image as §3). This is the contract for the preset in **`tools/PreviewWordReports/Program.cs`**; keep in sync when the scan or bindings change.

**Approval:** An **explicit user OK** on this file is the gate to **Phase 5** implementation (`SKILL.md` → **Prerequisites** item 4, **Phase 4**).

## Inventory — embedded Word templates (`Visa2026.Module/Resources`)

Authoritative list: **`Visa2026.Module/Visa2026.Module.csproj`** (`EmbeddedResource` for `Resources\*.docx`). Group for **review batches**:

| Group | Templates (file names) |
|-------|-------------------------|
| **Business trip** | `BusinessTrip_Sanawy.docx`, `BusinessTrip_Arrival_Letter.docx`, `BusinessTrip_Departure_Letter.docx` |
| **Sanawy (app)** | `App_Sanawy_Letter.docx` |
| **Letters — registration / info** | `App_Reg_Check_In_Letter.docx`, `App_Reg_Check_In_Internal_Letter.docx`, `App_Reg_Check_Out_Letter.docx`, `App_Reg_Check_Out_Internal_Letter.docx`, `App_Reg_Ext_Letter.docx`, `App_Reg_Info_Change_Address_Letter.docx`, `App_Reg_Info_Change_Passport_Letter.docx` |
| **Letters — invitation / visa / cancel / change** | `App_Inv_Letter.docx`, `App_Inv_And_WP_Letter.docx`, `App_Inv_FM_Letter.docx`, `App_Cancel_Visa_Letter.docx`, `App_Cancel_Visa_And_WP_Letter.docx`, `App_Cancel_Inv_WP_Letter.docx`, `App_Change_Passport_Letter.docx`, `App_Exit_Visa_Letter.docx`, `App_Additional_WP_Location_Letter.docx`, `App_Border_Zone_Permission_Letter.docx`, `App_Change_Inv_Letter.docx`, `App_Visa_And_WP_Ext_Letter.docx`, `App_Visa_Ext_FM_Letter.docx` |
| **Item / table forms** | `App_Cancel_Inv_WP_Item.docx`, `App_Cancel_Visa_And_WP_Item.docx`, `App_Change_Inv_Item.docx`, `App_Border_Zone_Permission_Item.docx`, `App_Inv_And_WP_Borcnama_Item.docx`, `App_Labor_Contract_Item.docx` |

Other **`Resources\*.docx`** (e.g. legacy mail-merge **`App_Reg_Check_In.docx`**) may exist in the project file—confirm on disk when tracing consumers.

**Review progress:** **`review-status.md`** (Pending / In review / Completed / Blocked) — one row per Resminamalar template.

## Family codes (use in Phase 1 / `learnings.md`)

| Code | Name | Page | Typical margins (twips L/R, T/B) | Primary body (half-pt) | Generator / pattern in `tools/GenerateTemplates/Program.cs` | Example `*ReportDef` / template |
|------|------|------|----------------------------------|-------------------------|---------------------------------------------------------------|----------------------------------|
| **L1** | Simple portrait letter (migration service) | A4 portrait | 1800 / 1440 | 30 (15 pt) headers & justified body | `MakeSimpleLetterTemplate` | `App_Reg_*_Letter`, many `App_*_Letter` using that helper |
| **L2** | Ministry letter (Group A) | A4 portrait | 1800 / 1440 | 30 body; urgency line 30 italic | `MakeGroupALetterTemplate` | `App_Inv_Letter`, `App_Inv_And_WP_Letter`, … |
| **L3** | Letter variants (custom header/body blocks) | A4 portrait | Usually 1800 / 1440 | 30 or 24 per block | Other `Make*Letter` / inline builders in same file | FM, exit visa, specialized letters |
| **T1** | Landscape personnel sanawy | A4 landscape | Per map / column math | Table cells often **24** (12 pt) | `BusinessTrip` grid + `MakeSanawyTemplate` (App 14-col) | `BusinessTripSanawyReportDef`, `AppSanawyLetterReportDefBase` |
| **F1** | Statutory / ministry **item** form | A4 portrait | Tighter (e.g. 1020 L/R) | 20–28 mix; captions **14** | `MakeBorcnamaTemplate` | `App_Inv_And_WP_Borcnama_Item` |
| **F2** | Long structured **item** (sections) | A4 portrait | Per template | Section titles vs body | `MakeLaborContractTemplate` | `App_Labor_Contract_Item` |
| **T2** | Compact **item** table (row loop) | portrait or landscape | Per map | Cell lines **24** typical | Inline column arrays + `MakeCellParagraphs` | Cancel/change/border item `.docx` |

## Defaults (unless the **FormTemplates** scan forces otherwise)

- **Font:** Times New Roman (`RunFonts` Ascii + HighAnsi).
- **Page:** A4 — `PageSize` width **11906**, height **16838** twips (portrait unless **T1**).
- **New report:** Pick the **closest family** and **reuse** the same helper or copy its margins/font sizes before introducing one-off numbers.

## Letter category (company → organization, company head)

**Meaning:** Formal letters from the applicant company to a ministry or migration authority, closed with the company head signatory (`AppendSignatoryLetter` / L1–L3 in `GenerateTemplates`).

**Body paragraph indent (standard):**

- **First line:** **`w:firstLine` = 720 twips** (~0.5 in, same as legacy XAF `\fi720` on `AppBaseReport.RtfResponsibility`).
- **Justification:** `w:jc` both (fully justified) for main body blocks built with `MakeJustifiedParagraph` / `InvAndWPLetterBodyParagraphProperties`.
- **No leading spaces** in static Turkmen strings: do **not** prefix paragraphs with ASCII spaces; indent comes **only** from OOXML `Indentation.FirstLine`. Otherwise the first line appears doubly indented.
- **Responsibility clause:** use the **same plain text** as `AppBaseReport.RtfResponsibility` (no leading spaces). In code: `FormalCompanyLetterLayout.ResponsibilityPlain` in `tools/GenerateTemplates/Program.cs`.

**Scan exception:** If a **FormTemplates** image clearly shows different body indent for one report, document the delta in that report’s `*_map.md` and in `learnings.md`; do not silently diverge from this baseline for new letters.

### Ministry addressee block (recipient / inside address)

**Terms:** **addressee block**, **recipient block** — organization + title + name, usually **right-aligned** under the date in L2-style letters.

**`App_Inv_And_WP_Letter` standard:** Two **right-aligned** paragraphs when `MinistryRecipientBlockFormatter.SplitIntoAddressLines` finds a newline in `RecipientBlock` or the ` … korporasiýasynyň` break (typical corporate-ministry shape). Second paragraph: **`w:ind w:left="720"`** (same step twips as body first line) so line 2 starts **inboard** of line 1. Merge keys: `ProjectContract_Ministry_RecipientBlock_Line1`, `Line2`, `HasLine2` (+ legacy full `ProjectContract_Ministry_RecipientBlock`). Second paragraph body uses DocxTemplater `{?{ds.ProjectContract_Ministry_RecipientBlock_HasLine2}}…{{/}}` so single-line ministries do not print an empty row.

**Data source:** All visible addressee text is **`Ministry.RecipientBlock`** (exposed on `Application` as `ProjectContract_Ministry_RecipientBlock`). The Word file does **not** embed that Turkmen text as static copy — only `{{ds.*}}` placeholders. `PreviewWordReports` uses sample strings in the preset to mimic production data for QA.

Other L1–L3 templates that still use one placeholder keep a **single** `ProjectContract_Ministry_RecipientBlock` paragraph until/unless you adopt the same formatter pattern.

### Signatory block (company head)

**Names (for maps and reviews):** **signatory block** — left cell = **capacity line** / **official title** (wezipede görkezilişi); right cell = **signatory name**.

**Standard layout** (ministry sample style: wide gap between title and name for mühür / gol):

| Aspect | Standard |
|--------|----------|
| **Mechanism** | Single-row, **borderless** two-column **table** (`TableLayout` fixed), full width = printable width (page **11906** − left **1800** − right **1800** = **8306** twips by default). **`App_Inv_And_WP_Letter`** uses **symmetric** **1200** / **1200** twips → printable **9506**; pass that width into `AppendSignatoryLetter` so the table matches the section. |
| **Left column** | Width **5200** twips (`FormalCompanyLetterLayout.SignatoryLeftColumnTwips`). One paragraph: **left**-justified, **bold**, font **30** half-points (15 pt), `SpacingBefore` **480** twips. Token: `{{ds.Application_CompanyHead_PositionTm}}`. Title may wrap; cell **top**-aligned. |
| **Right column** | Remaining printable width minus **5200** (e.g. **3106** twips at default margins; **4306** for `App_Inv_And_WP_Letter` at symmetric **1200** / **1200**). One paragraph: **right**-justified, **bold**, 15 pt, same `SpacingBefore` **480** twips. Token: `{{ds.Application_CompanyHead_FullName}}`. **Top**-aligned so the name lines up with the **first line** of the title. |
| **Borders** | All table and cell borders **nil** (no visible grid). |

**Code:** `AppendSignatoryLetter` in `tools/GenerateTemplates/Program.cs`. **Do not** duplicate this table with different column widths or alignment for another L1–L3 letter unless the reference scan requires it — then record the deviation in **`FormTemplates/<basename>_map.md`**.

**Hand-authored `.docx`:** If a template is not generator-built, replicate the same geometry (two columns, title left / name right, bold 15 pt, top-aligned, no borders).

## When a report does not fit any row

Document in **`learnings.md`** and add a **new code** (e.g. **F3**) here in the same table style after the template ships.
