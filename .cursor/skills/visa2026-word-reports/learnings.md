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
