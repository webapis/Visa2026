# Template scan — placeholder guessing (Mermaid sources)

Flowcharts for **Create from yellow marks**: how yellow-highlighted Word/Excel sample literals are mapped to library merge placeholders during **Analyze**.

Canonical product/engineering specs:

- [`docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md`](../../TEMPLATE_AI_SCAN_PRODUCT_SPEC.md)
- [`docs/TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md`](../../TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md)
- Skill: [`.cursor/skills/visa2026-template-scan/SKILL.md`](../../../.cursor/skills/visa2026-template-scan/SKILL.md)

Implementation: `Visa2026.Module/Services/TemplateScan/` (`ScanFieldPlanService`, `ScanExcelYellowResolver`, `ScanYellowHighlightTokenResolver`, `ScanAmbiguousYellowGate`, `ScanAmbiguousYellowRefinementService`, `ScanFieldPlanMerger`).

| File | Purpose |
|------|---------|
| `overview.mmd` | End-to-end Analyze path: extract → local rules → optional Azure → merger → Review |
| `excel-local-rules.mmd` | Excel per-cell inference (headers, profiles, shape matcher, compound cells) |
| `word-local-rules.mmd` | Word yellow text: regex patterns and compound splits |
| `azure-ambiguous-gate.mmd` | When local guesses escalate to Azure, and **what JSON is sent** (manual role/description + nearby snippet; no file/case dump) |

**Design lock:** yellow cells contain **fictitious sample literals** (e.g. Erol, Hilmi). They are matched against column headers and the placeholder manual — **not** against the selected case roster.

Preview: open a `.mmd` in Cursor/VS Code with a Mermaid preview extension, or paste into [mermaid.live](https://mermaid.live).
