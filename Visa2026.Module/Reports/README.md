# Reports (Visa2026.Module) — hub

This folder contains the canonical workflow and standards for **predefined (code-based) XtraReports** in Visa2026.

## Start here

- **Create a new predefined report**: `PROMPT_CREATE_REPORT.md`
- **Update an existing predefined report**: `PROMPT_UPDATE_REPORT.md`

## Canonical references

- **Workflow + catalog + naming + map contract**: `REPORT_GENERATION_GUIDE.md`
- **Visual standards + Turkmen QA**: `REPORT_STANDARDS.md`
- **Technical conventions + `.resx` sync requirement**: `REPORTS.md`

## Key rules (do not skip)

- **Map-first**: create `Resources/FormTemplates/<image>_map.md`, mark `📋 Draft`, get approval (`✅ Agreed`) before writing report code.
- **Turkmen QA**: run pre-code and post-code checks (`REPORT_STANDARDS.md §14`).
- **ExpressionBindings need `.resx` sync**: changes in `*.Designer.cs` must be mirrored in `*.resx` or bindings may be ignored at runtime.

## Reports V2 (runtime designer)

Reports V2 (runtime-designed reports stored in DB) is a separate workflow. See `REPORTS_V2_IMPLEMENTATION.md` at repo root.

