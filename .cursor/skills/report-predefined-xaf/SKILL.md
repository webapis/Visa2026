---
name: report-predefined-xaf
description: >-
  Creates and updates predefined DevExpress XAF XtraReports in Visa2026 (code-based reports in Visa2026.Module/Reports). Use when the user asks to create a new report, update an existing report, work with map files (_map.md), ReportsUpdater registration, ExpressionBindings/.resx sync, or Turkmen static-text QA for reports.
disable-model-invocation: false
---

# Predefined reports (Visa2026): create/update workflow

## Canonical docs (read first)

- `Visa2026.Module/Reports/README.md` (hub/index)
- `Visa2026.Module/Reports/REPORT_GENERATION_GUIDE.md` (workflow + catalog + naming + map contract)
- `Visa2026.Module/Reports/REPORT_STANDARDS.md` (visual + Turkmen QA + base-class/group guidance)
- `Visa2026.Module/Reports/REPORTS.md` (technical conventions, `.resx` sync requirement)
- Capture new pitfalls/patterns in `.cursor/skills/report-predefined-xaf/learnings.md` (append-only)

## Goal

Produce a predefined report that:

- Matches the reference form layout (via an agreed `_map.md`)
- Uses the correct data type + base class
- Has correct `ReportsUpdater` registration and visibility criteria
- Passes Turkmen static-text QA
- Does not silently break ExpressionBindings due to missing `.resx` updates

## Non-negotiable gates (do not skip)

### Gate A — Map-first contract

- For a **new** report: create/update `Resources/FormTemplates/<image>_map.md` to `📋 Draft`, then **stop for approval**.
- Do **not** write report code until the user confirms the map and it is `✅ Agreed`.

### Gate B — Turkmen QA

- Before coding: run **pre-code** static-text QA per `REPORT_STANDARDS.md §14c`.
- After coding: run **post-code** QA per `REPORT_STANDARDS.md §14d`.
- When user shares a render screenshot: run the image review checklist per `REPORT_STANDARDS.md §14e`.

### Gate C — ExpressionBindings require `.resx` sync

- If any `ExpressionBinding` is added/changed/renamed in `*.Designer.cs`, update the matching `*.resx` or the binding may be ignored at runtime.
- If a control name/type changes, ensure `.resx` entries match (`REPORT_STANDARDS.md §18` + `REPORTS.md`).

## Pagination / PDF page count (XtraReports)

When a report **should be one page** but exports with a **second, mostly blank** page:

1. **`XRRichText.CanGrow`**: Default is `true`. Growing body text increases **Detail** height and can force a page break even when the preview still shows empty space. For fixed one-page legal forms, set **`CanGrow = false`** on header/body rich text (or increase designed height and move signatures instead of enabling unbounded growth).
2. **Footer bands**: If **`ReportFooter`** is hidden/unused, set **`HeightF = 0`**, **`Visible = false`**, and **`PrintAtBottom = false`** (see `REPORT_STANDARDS.md` + `AppItemBaseReport` designer). A non-zero designer height or bottom printing can contribute to odd pagination.
3. **Horizontal overflow**: Controls whose **right edge** meets or exceeds the printable width (full `PageWidthF - Margins.Left - Margins.Right`) can trigger an **extra page** that looks empty. Prefer width **`… - 2f`** (or similar epsilon) and/or clamp **`Detail`** controls in the report constructor so `LocationFloat.X + WidthF` stays inside the printable width. Example: `AppInvAndWPBorcnamaItemReport`.

Also scan RTF in `.resx` for accidental page breaks (`\page`, etc.) if behavior changed after text edits.

## Workflow: create a new predefined report

1. **Identify the report identity**
   - ApplicationType name (criteria anchor)
   - Level: `Application` / `ApplicationItem` / `Registration` (or `BusinessTrip`)
   - Variants (V0–V2) and whether it is shared across multiple ApplicationTypes
   - Class name + registered name + display name (Tm) (use `REPORT_GENERATION_GUIDE.md` naming rules)

2. **Confirm assets**
   - Reference image exists under `Visa2026.Module/Resources/FormTemplates/` with correct naming
   - Create/update the corresponding `_map.md` in the same folder

3. **Draft `_map.md` and stop**
   - Fill all required sections (Identity, Data, Page setup, Band map, Control maps, Ignored elements, Required BO properties)
   - Mark `📋 Draft`
   - Present the map and pause until the user confirms; revise until `✅ Agreed`

4. **Verify required BO properties exist**
   - Prefer flattened `[NotMapped]` properties on the report’s data type (follow existing patterns).
   - Avoid deep navigation expressions in reports unless the project standard explicitly allows it.

5. **Implement the report**
   - Create: `Reports/<ClassName>.cs`, `Reports/<ClassName>.Designer.cs`, `Reports/<ClassName>.resx`
   - Apply `REPORT_STANDARDS.md` for layout/control rules (fonts, margins, XRRichText patterns, Char(10), etc.)

6. **Register + scope visibility**
   - Add `AddPredefinedReport<...>` and `CreateReportVisibility(...)` entries in `Visa2026.Module/DatabaseUpdate/ReportsUpdater.cs`
   - Criteria must scope by `ApplicationType.Name` (or via parent `Application` when target is `ApplicationItem`/`Registration`)

7. **Update docs**
   - Set map status to `✅ Implemented`
   - Update `REPORT_GENERATION_GUIDE.md` catalog / progress dashboard
   - Update `REPORTS.md` “Existing Reports” table if applicable
8. **Capture learnings**
   - If anything surprising happened (bug, silent failure, repeated correction): append 1–3 bullets to `.cursor/skills/report-predefined-xaf/learnings.md`
   - Promote into `SKILL.md` only when repeated and stable (avoid churn)

## Workflow: update an existing predefined report

1. Read current:
   - The report’s `_map.md` (design contract)
   - `Reports/<ClassName>.Designer.cs` (current implementation)
   - `Reports/<ClassName>.cs` (code-behind rules; keep minimal)
   - `Reports/<ClassName>.resx` (binding sync)

2. Classify the change:
   - Visual adjustment → update `Designer.cs` and sync map
   - New field → ensure BO property exists → update `Designer.cs` → update `.resx`
   - Layout restructure → update map first → approval → then code changes

3. Apply the same gates:
   - Turkmen QA (pre/post)
   - `.resx` sync for any binding changes
4. Capture learnings (same rule as creation)

## Output expectations in chat

- Always report which gate you are at (Draft map / Awaiting approval / Implementing / Registration / QA).
- Summarize any risky changes (visibility criteria, base class change, binding changes) before finishing.

## Related / out-of-scope

- **Reports V2 runtime designer** is a different workflow. Only follow it if the user explicitly asks for runtime-designed reports (`REPORTS_V2_IMPLEMENTATION.md`).

