# Prompt: Create a New Report

> Paste this prompt at the start of a new session when building a report from scratch.
> Fill in the bracketed fields before sending.

---

## Session Context

I am working on the **Visa2026** project — an XAF (DevExpress Express Application Framework) Blazor application using **DevExpress XtraReports v25.2.5** and **Entity Framework Core**.

The project is located at: `c:\Users\IT\source\repos\Visa2026`

Key reference documents (read these before doing anything):
- `Visa2026.Module/Reports/REPORT_GENERATION_GUIDE.md` — workflow, catalog, naming conventions, registration
- `Visa2026.Module/Reports/REPORT_STANDARDS.md` — all visual standards, control properties, RTF format, typography
- `Visa2026.Module/Reports/REPORTS.md` — technical conventions, .resx sync requirement

---

## Task

Create a new report for the following:

| Field | Value |
|---|---|
| Reference image | `[filename.jpg — place in Resources/FormTemplates/]` |
| ApplicationType Name | `[e.g. App_Reg_Check_In]` |
| Report class name | `[e.g. AppRegCheckInReport]` |
| Data type | `[Application / ApplicationItem / Registration / BusinessTrip]` |
| Inherits from | `[AppBaseReport / AppItemBaseReport / AppRegBaseReport / XtraReport]` |
| Registered name | `[e.g. "App Reg Check In Report"]` |
| Display name (Turkmen) | `[e.g. "Hasaba Almak — Ýüztutma"]` |

---

## Required Sequence — Do Not Skip Steps

**Step 1 — Read the reference image**
Study the scanned form image carefully. Identify every visible element: static text, bound fields, layout zones, ignored elements (stamps, signatures, logos in background).

**Step 2 — Create the map file**
Create `Resources/FormTemplates/[image_filename]_map.md` following the structure in REPORT_GENERATION_GUIDE.md Section 6c. Include:
- Report Identity, Data, Page Setup
- Band Map (with heights)
- Control Map per band (LocationFloat, SizeF, source, expression/value, notes)
- Ignored Elements
- Required BO Properties (with existence status)

Set map status to `📋 Draft`. **Stop here and wait for approval.**

**Step 3 — Get approval**
Present the map to the user. Do not write any `.cs` code until the user confirms the map is correct. Update the map based on feedback. Set status to `✅ Agreed` when approved.

**Step 4 — Verify BO properties**
Check that all required fields from the map exist as properties on the data type. If any are missing, add `[NotMapped]` flattened properties to the corresponding `.cs` file before writing report code.

**Step 5 — Generate report code**
Create three files following REPORT_STANDARDS.md for all visual decisions:
- `Reports/[ClassName].cs` — minimal constructor, summary comment referencing image and standards
- `Reports/[ClassName].Designer.cs` — all controls, positions, sizes, fonts per standards
- `Reports/[ClassName].resx` — sync all ExpressionBindings (see REPORTS.md)

**Step 6 — Register the report**
Add `AddPredefinedReport<>` and `CreateReportVisibility` entries in `DatabaseUpdate/ReportsUpdater.cs`.

**Step 7 — Update documentation**
- Set map file status to `✅ Implemented`
- Update Report Catalog in REPORT_GENERATION_GUIDE.md (move from Planned → Implemented)
- Update progress counters in the Completion Dashboard

---

## Standards Reminders

- All fonts: **Times New Roman 15pt**
- Margins: **100F left, 100F right** (1 inch) — printable width = **626.7717F**
- Body paragraphs: **`XRRichText`** with `\qj\fi720` (justified + 0.5" indent)
- Dynamic fields in RTF: `\u8220?[FieldName]\u8221?` (curly quotes)
- Bold inline in RTF: `\b text\b0`
- Non-ASCII Turkmen chars: see REPORT_STANDARDS.md Section 6
- Background: handled automatically by `AppBaseReport` — no code needed in derived class
- Content must be visible in the designer — **no text or RTF built in code-behind**
