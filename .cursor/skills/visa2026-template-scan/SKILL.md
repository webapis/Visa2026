---
name: visa2026-template-scan
description: >-
  Improve Create from yellow marks (Word/Excel): yellow-highlighted .docx/.xlsx → library
  placeholders → Approve. Officers submit wizard screenshots; agent compares, fixes TemplateScan,
  appends learnings. Not Convert (value-match), not PNG/JPG/PDF (retired), not Resminamalar ZIP,
  not #visa-preview-slot inside the wizard. Read learnings.md first; append after verified fixes.
  User prompts: prompts.md.
disable-model-invocation: false
---

# Visa2026 — Create from yellow marks

**Mission:** Generate **Word and Excel** merge templates from **yellow-marked `.docx` / `.xlsx`** (OpenXML highlight / yellow cell fill → library placeholders → officer Approve). **PNG/JPG/PDF uploads are retired.**

**Officer label:** **Create from yellow marks** (not “Create from scan”).  
**User prompts:** [prompts.md](./prompts.md) (`@visa2026-template-scan`).

## Screenshot feedback loop

Officers submit **wizard step screenshots** + optional **catalog Preview** + the **original yellow-marked Office file**. Treat as a regression pack; append learnings after every run.

| Step | Judge |
|------|--------|
| **1 Upload** | `.docx`/`.xlsx` only; yellow requirements; case hints |
| **2 Review** | Mapped tokens vs yellow only; no bogus gaps |
| **3 Generate** | Token writer on **copy**; **strip all yellow** markup; diff gate |
| **4 Preview** | Outline only (no PDF slot) |
| **5 Done** | Saved; correct TemplateKind |
| **Catalog Preview** | Real page/sheet; **no yellow highlighter** left on filled text |

## Agent workflow

1. Read [learnings.md](./learnings.md) (newest first) + Scenarios.
2. Classify vs Convert / Resminamalar / preview-slot / user-report-templates.
3. Re-read [`docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md`](../../../docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md).
4. Implement in `Visa2026.Module/Services/TemplateScan/` + thin Blazor `TemplateScan*`.
5. Verify: `dotnet test … --filter FullyQualifiedName~TemplateScan`.
6. Append learnings; promote repeated issues to Scenarios.

## Canonical docs

| Doc | Topic |
|-----|--------|
| [`docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md`](../../../docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md) | Product locks |
| [`docs/TEMPLATE_AI_SCAN_UI_FLOW.md`](../../../docs/TEMPLATE_AI_SCAN_UI_FLOW.md) | Wizard flow |
| [`docs/TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md`](../../../docs/TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md) | Contracts |

**Related:** [application-profile](../visa2026-application-profile/SKILL.md) · [resminamalar](../visa2026-resminamalar/SKILL.md) · [preview-slot](../visa2026-preview-slot/SKILL.md) · [user-report-templates](../visa2026-user-report-templates/SKILL.md) · Convert specs (value-match, not yellow).

## Output maturity

| Format | Status |
|--------|--------|
| **Word `.docx`** | Shipped — yellow → tokens on copy → strip all yellow markup |
| **Excel `.xlsx`** | Shipped foundation — yellow fill → tokens → strip yellowish fills |

## Scenarios

| Symptom | First step | Owner |
|---------|------------|--------|
| PNG/JPG/PDF rejected | Expected — use yellow-marked Word/Excel | **This skill** |
| Yellow not detected | Word Text Highlight Color / Excel solid yellow fill | **This skill** |
| Wrong tokens / compound split | `ScanYellowHighlightTokenResolver` + catalog ShortCodes | **This skill** + user-report-templates |
| Excel roster gaps (names, TUR, …) | Column header + manual inference (`ScanExcelYellowResolver`); not case value match | **This skill** |
| Review shows `{{ds.PLN}}` / Approve blocks `not found on ApplicationProfileInstance` | Yellow sample row above row 5 was treated as header — Analyze again after restart | **This skill** |
| Clarification chat disabled | Needs `TemplateAiScan` AI provider (optional); Analyze does not | **This skill** |
| Config lock | May add **new** templates | application-profile |
| Excel catalog Preview blank; pane titled `report_….docx` | Nested Resminamalar keys — Excel bytes converted as Word PDF | **resminamalar** |
| Diff gate fail on Generate | Span addresses; fingerprints **ignore** yellow strip | **This skill** + Convert writer |
| Yellow remains after Approve / catalog Preview | `StripAllYellowMarkup` / `StripAllYellowFills` after write; re-Approve old templates | **This skill** |

## Scope

| In | Out |
|----|-----|
| Yellow-marked `.docx` / `.xlsx` | PNG / JPG / PDF (retired) |
| OpenXML yellow → tokens → Approve | Convert value-match modal |
| Wizard outline Preview | `#visa-preview-slot` inside wizard |
| | Resminamalar ZIP |

## Locked rules

1. Separate from Convert (yellow marks ≠ instance value match).
2. Yellow only → placeholders; library tokens only.
3. Preserve source Office layout (token writer on copy).
4. **Yellow is scan markup only** — after Generate, strip **all** highlighter/yellow fill from the saved copy (not only substituted runs). Unmapped leftovers (e.g. `6 (alty)` when only VCAT mapped) must not survive catalog Preview.
5. Officer Approve required.
6. Wizard Preview = outline only.
7. Config lock allows **new** templates.

## Pipeline

```text
Upload .docx/.xlsx → Ingest → ScanOfficeYellowExtractor → Merge/split → Yellow gate
  → Review / optional Clarification
  → ITemplateTokenWriter → StripAllYellow* → diff gate → Extract/Validate → Outline → Approve
```

## Triage

| Layer | Look at |
|-------|---------|
| UI | `TemplateScanDialog.razor`, wizard / Resminamalar entry **Create from yellow marks** |
| Yellow | `ScanOfficeYellowExtractor`, `ScanYellowHighlight*` |
| Generate | `TemplateScanOrchestrator` Office path, `ITemplateTokenWriter`, `StripAllYellowMarkup` / `StripAllYellowFills` |
| Tests | `Visa2026.Module.Tests/TemplateScan/` |

```powershell
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug --filter "FullyQualifiedName~TemplateScan"
```
