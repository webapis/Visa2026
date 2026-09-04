# Template scan skill — continuous improvement

**Skill:** [SKILL.md](./SKILL.md) · **Log:** [learnings.md](./learnings.md) · **Reference:** [reference.md](./reference.md)

**Canonical docs:** [TEMPLATE_AI_SCAN_PRODUCT_SPEC.md](../../../docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md), [UI_FLOW](../../../docs/TEMPLATE_AI_SCAN_UI_FLOW.md), [ENGINEERING](../../../docs/TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md)

Shared promotion rules: [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md)

---

## Screenshot-driven loop (preferred)

```text
Officer runs Create from scan
  → Pastes screenshots: Upload, Review, Generate, Preview, Done
  → Optional: original scan image + catalog Preview / merge PDF
Agent:
  1. READ    learnings.md + Scenarios
  2. COMPARE expected vs each screenshot (yellow, tokens, layout)
  3. FIX     TemplateScan Module (+ thin Blazor) — minimal diff
  4. TEST    FullyQualifiedName~TemplateScan (+ targeted new tests)
  5. RECORD  append learnings.md (success and failure)
  6. PROMOTE 2+ same gap → Scenarios row; 3+ → tighten SKILL/reference
```

Partial packs are OK (e.g. only Review + Preview + original). Always note which steps were missing.

## Classic loop (no screenshots)

```text
1. READ   → learnings.md + Scenarios
2. CLASSIFY → scan vs Convert vs Resminamalar vs preview-slot vs catalog
3. TRY    → same PNG/PDF through Analyze → Generate → Approve
4. TEST   → TemplateScan unit filter; officer hard-refresh after Blazor changes
5. FIX    → minimal diff in TemplateScan Module (+ thin Blazor)
6. RECORD → append learnings.md (verified only)
7. PROMOTE → 2+ same root cause → Scenarios row in SKILL.md
```

## Learning entry shape

```markdown
### YYYY-MM-DD — short title

- Need / Symptom: (what the screenshots showed)
- Steps attached: Upload / Review / Generate / Preview / Done / catalog / original
- Cause:
- Fix: (or "No code change — confirmed good")
- Verify:
- Prevent:
- Cross-skill:
```

## Which skill owns the entry?

| Symptom | Log to |
|---------|--------|
| Scan wizard, yellow gate, Docx/Xlsx layout, Azure TemplateAiScan | **visa2026-template-scan** |
| Catalog Preview/ZIP after template saved | **visa2026-resminamalar** |
| `#visa-preview-slot` CSS/resize/occupant | **visa2026-preview-slot** |
| Placeholder ShortCode missing from library JSON | **visa2026-user-report-templates** |
| Profile lock / ApplicationProfileTemplate host | **visa2026-application-profile** |
| Convert existing Word/Excel | Convert docs / convert code (not this skill) |