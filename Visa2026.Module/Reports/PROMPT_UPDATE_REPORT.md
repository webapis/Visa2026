# Prompt: Update an Existing Report

> Paste this prompt at the start of a new session when modifying an existing report.
> Fill in the bracketed fields before sending.

---

## Session Context

I am working on the **Visa2026** project — an XAF (DevExpress Express Application Framework) Blazor application using **DevExpress XtraReports v25.2.5** and **Entity Framework Core**.

The project is located at: `c:\Users\IT\source\repos\Visa2026`

Key reference documents (read these before doing anything):
- `Visa2026.Module/Reports/REPORT_GENERATION_GUIDE.md` — workflow, catalog, naming conventions
- `Visa2026.Module/Reports/REPORT_STANDARDS.md` — all visual standards, control properties, RTF format
- `Visa2026.Module/Reports/REPORTS.md` — technical conventions, .resx sync requirement

---

## Task

Update the following existing report:

| Field | Value |
|---|---|
| Report class | `[e.g. AppRegCheckInReport]` |
| Designer file | `Reports/[ClassName].Designer.cs` |
| Map file | `Resources/FormTemplates/[image]_map.md` |
| Change requested | `[describe what needs to change]` |

---

## Required Sequence

**Step 1 — Read current state**
Before making any changes, read:
- The report's `_map.md` file — understand the agreed design
- The report's `Designer.cs` — understand the current implementation
- The report's `.cs` file — check for any code-behind logic

**Step 2 — Assess the change**
Determine what type of change this is:

| Change type | Action |
|---|---|
| Visual adjustment (position, size, font) | Update `Designer.cs` directly; update map file to match |
| New field added | Check BO property exists → add to `Designer.cs` → sync `.resx` |
| Text content change | Update RTF string in `Designer.cs` (or edit in designer) |
| New control added | Follow REPORT_STANDARDS.md for correct control type and properties |
| Layout restructure | Update map file first → get approval → then update code |

**Step 3 — Apply changes**
Follow REPORT_STANDARDS.md for all visual decisions. Key rules:
- Do not change fonts, margins, or spacing unless the user explicitly requests it
- Do not refactor or "improve" code outside the scope of the change
- If adding an `ExpressionBinding`, also update the `.resx` file

**Step 4 — Update the map file**
Keep the `_map.md` in sync with the code. If a control's position, size, or expression changed, update the corresponding row in the map's Control Map table.

**Step 5 — Update documentation if needed**
- If the change affects the Report Catalog entry in REPORT_GENERATION_GUIDE.md, update it
- If a new standard was established during the change, add it to REPORT_STANDARDS.md with a Change Log entry

---

## Standards Reminders

- All fonts: **Times New Roman 15pt**
- Margins: **100F left, 100F right** — printable width = **626.7717F**, split point = **313F**
- Body paragraphs: **`XRRichText`** — RTF template: `{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 [text]\par}`
- Dynamic fields in RTF: `\u8220?[FieldName]\u8221?`
- Paragraph gap: **8F** between body paragraphs
- `XRRichText` always needs: `BackColor=Transparent`, `Borders=None`, `CanGrow=true`, Rtf set after `EndInit`
- `XRLabel` always needs: `BackColor=Transparent`; add `CanGrow=true` + `WordWrap=true` for multi-line
- Content must be visible in the designer — **no text or RTF built in code-behind**
