---
name: visa2026-template-scan
description: >-
  Improve Create template from scan (Word and Excel) from scanned document images. Officers submit
  screenshots per wizard step (Upload, Review, Generate, Preview, Done) plus optional catalog merge
  Preview and the original scan; the agent compares expected vs actual, fixes TemplateScan, and
  appends learnings so the next run is better. Use when user pastes scan-wizard screenshots, Create
  from scan bugs, Analyze failed, yellow mapping, letter/Excel layout, TemplateAiScan config. Not
  Convert (TEMPLATE_AI_CONVERT), Resminamalar ZIP, or #visa-preview-slot inside the wizard. Always
  read learnings.md first; append after verified fixes. User prompts: prompts.md.
disable-model-invocation: false
---

# Visa2026 — Create template from scan

**Mission:** Improve generation of **Word and Excel** merge templates from **scanned images** of ministry documents (filled sample + yellow highlighter → library placeholders → officer Approve).

**User prompts:** [prompts.md](./prompts.md) (`@visa2026-template-scan`).


## Screenshot feedback loop (primary improvement path)

Officers improve this skill by submitting **screenshots of each Create-from-scan step** (and often the **original scan** + **catalog Preview** after save). The agent must treat that pack as a regression case.

| Step screenshot | What to judge |
|-----------------|---------------|
| **1 Upload** | Filled sample, yellow requirements, case hints, file accepted |
| **2 Review** | Scan acceptable / Fail; mapped tokens vs yellow only; no bogus gaps; boxes OK |
| **3 Generate** | Completes without silent flat-list fallback |
| **4 Preview** | Outline shows tokens in letter/table context (not PDF slot); placeholder list |
| **5 Done** | Saved; placeholder count; Next points to catalog / Edit template |
| **Catalog Preview** (after Approve) | Real page/sheet layout vs original scan |
| **Original scan** | Ground truth for yellow regions + alignment |

**Agent when screenshots arrive:**

1. Read [learnings.md](./learnings.md) + Scenarios first.
2. Compare screenshots to locked rules and prior learnings (expected vs actual per step).
3. Fix the smallest TemplateScan change that addresses the gap.
4. Add/adjust tests when logic changed.
5. **Append** [learnings.md](./learnings.md) (Need / Symptom / Cause / Fix / Verify / Prevent) — even if the run was mostly good (record what worked).
6. Promote repeated issues into **Scenarios**.

Do **not** skip learnings after a screenshot review. Experience compounds only when logged.

## Agent workflow (every task — mandatory)

1. **Read** [learnings.md](./learnings.md) (**## Entries**, newest first) and **Scenarios** below.
2. **Classify** — scan wizard / yellow gate / Word or Excel draft layout / Azure scan (**this skill**) vs Convert (**docs/TEMPLATE_AI_CONVERT_***) vs catalog ZIP (**[resminamalar](../visa2026-resminamalar/SKILL.md)**) vs preview-slot shell (**[preview-slot](../visa2026-preview-slot/SKILL.md)**) vs placeholder seeds (**[user-report-templates](../visa2026-user-report-templates/SKILL.md)**).
3. **Re-read locked product rules** in [`docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md`](../../../docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md) + yellow-highlight rules in learnings.
4. **Implement** in **Visa2026.Module/Services/TemplateScan/** (logic) and **Visa2026.Blazor.Server/Editors/TemplateScan*** (thin UI). Excel drafts reuse user-report Excel merge conventions.
5. **Verify** — `dotnet test Visa2026.Module.Tests --filter FullyQualifiedName~TemplateScan`; officer path: Analyze → Review → Generate → Approve → catalog Preview.
6. **Record** — append [learnings.md](./learnings.md) after **verified** work ([MATURITY.md](./MATURITY.md)).
7. **Promote** — same root cause twice → **Scenarios** row; thrice → tighten this file or [reference.md](./reference.md).

## Canonical docs

| Doc | Topic |
|-----|--------|
| [`docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md`](../../../docs/TEMPLATE_AI_SCAN_PRODUCT_SPEC.md) | Product locks (separate from Convert) |
| [`docs/TEMPLATE_AI_SCAN_UI_FLOW.md`](../../../docs/TEMPLATE_AI_SCAN_UI_FLOW.md) | Wizard views / transitions |
| [`docs/TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md`](../../../docs/TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md) | Contracts, SD-D*, slices S* |

**Related skills (do not duplicate):**

| Topic | Skill |
|-------|--------|
| Profile lock, case workspace, ApplicationProfileTemplate host | [visa2026-application-profile](../visa2026-application-profile/SKILL.md) |
| Catalog Preview / ZIP / Edit template after save | [visa2026-resminamalar](../visa2026-resminamalar/SKILL.md) |
| `#visa-preview-slot` shell (not inside scan wizard) | [visa2026-preview-slot](../visa2026-preview-slot/SKILL.md) |
| Placeholder catalog, Extract/Validate, Word/Excel seeds | [visa2026-user-report-templates](../visa2026-user-report-templates/SKILL.md) |
| Convert filled Word/Excel → template | docs `TEMPLATE_AI_CONVERT_*` (not this skill) |

**Long reference:** [reference.md](./reference.md). **Experience:** [learnings.md](./learnings.md). **Maturity:** [MATURITY.md](./MATURITY.md).

---

## Output maturity

| Format | Status | Focus |
|--------|--------|--------|
| **Word `.docx`** | Shipped path | Letter layout (twoColumn, justify, italic/bold), yellow→tokens, Approve |
| **Excel `.xlsx`** | Improvement target under this skill | Table/grid from scan, row loops, Excel merge tokens; align with user-report-templates Excel families |

Product spec historically locked **Word-only for v1** (`S3`); **Excel-from-scan is in skill scope** for design and implementation as the next generation path — update the product/engineering specs when Excel ships.

---

## Scenarios (check first)

| Symptom | First step | Owner |
|---------|------------|--------|
| Analyze failed / DeploymentNotFound | `TemplateAiScan:AzureOpenAI:Deployment` (prefer same as Convert, e.g. `gpt-4.1-mini`); surface `ex.Message` | **This skill** |
| Flat `Label: {{token}}` draft | Layout AI + `ScanLetterLayoutNormalizer`; never leftover-token footer dump | **This skill** |
| Yellow values unmapped / duplicate compound gap | `ScanYellowHighlightTokenResolver` + merger; library token exists? | **This skill** + user-report-templates |
| Placeholder Manual missing AFNUM/ADAT/… | `rootBoTypes` `"Application"` → `ApplicationProfileInstance` alias | user-report-templates |
| Header has date on right, addressee missing | Normalizer: left = AFNUM+ADAT; right = addressee; OCR inject | **This skill** |
| PDF viewer inside scan wizard Preview | Must stay **outline-only** (`TemplateConvertOutlineView`) | **This skill** |
| Excel draft from scan missing / wrong grid | Excel builder + scan table detection (this skill); merge modes → user-report-templates | **This skill** |
| Config lock blocks new template | Lock allows **new** templates only; banner expected | application-profile |
| Wrong placeholders invented | Yellow-only + allowedTokens; no OCR invent on vision fail | **This skill** |
| Teal Review boxes not on yellow text | ScanFieldBoxLocalizer / yellow region detect | **This skill** |

---

## Scope

| In scope | Out of scope |
|----------|----------------|
| Improving **Word and Excel** templates from scanned images | Convert existing document modal |
| `TemplateScanDialog` wizard (Upload → Done) | Pixel-perfect letterhead / stamps |
| Yellow-highlight field plan + gate | Embedding `#visa-preview-slot` PDF in wizard |
| Draft `.docx` layout (twoColumn, styles, normalizer) | Resminamalar ZIP / batch worker |
| Draft `.xlsx` layout (tables, row loops, Excel tokens) — build/improve here | Open-ended restyle / translate letter |
| Azure / None scan AI providers (`TemplateAiScan`) | Authoring seed `.docx`/`.xlsx` under Resources/Templates (user-report-templates) |
| Clarification chat (mapping only) | |
| Save → `ApplicationProfileTemplate` | |

---

## Locked product rules (do not regress)

1. **Separate from Convert** — own modal, orchestrator, options section.
2. **Yellow highlighter only** — map only yellow spans; no yellow → Fail; unmapped yellow → Fail/Warn; non-yellow stays literal.
3. **Filled sample + case hints** — value hints from case; tokens from profile-allowed library (`DataScope.Both` for Word; Excel scope per template kind).
4. **Library tokens only** — never invent ShortCodes; gaps → Needs help.
5. **Preserve scan structure** — Word: letter layout (header №+date left / addressee right; justify body; italic urgency; bold split signature). Excel: table/grid structure from the scan, not a flat token dump.
6. **Officer Approve required** — no silent catalog publish.
7. **Wizard Preview = outline** — page/sheet layout via catalog Preview / Edit template after save.
8. **Config lock** — may add **new** templates; existing row edits stay blocked.

---

## Pipeline (mental model)

```text
Upload PNG/PDF → Ingest/OCR → Suitability
  → Vision field plan (yellow) → Merge/split compounds → Yellow gate
  → Officer Review / Clarification
  → Vision layout → Normalizer/builder
       → Word: ScanDraftDocxBuilder (+ letter normalizer)
       → Excel: ScanDraftXlsxBuilder (target) + Excel merge conventions
  → Extract/Validate → Outline Preview → Approve → ApplicationProfileTemplate
```

---

## Triage

| Layer | Look at |
|-------|---------|
| UI wizard | `Visa2026.Blazor.Server/Editors/TemplateScan*.razor` |
| Orchestration | `TemplateScanOrchestrator`, `ScanFieldPlanService`, `ScanDocxLayoutService` |
| Yellow / tokens | `ScanYellowHighlight*`, `ScanFieldPlanMerger`, catalog ShortCodes |
| Word bytes | `ScanDraftDocxBuilder`, `ScanLetterLayoutNormalizer` |
| Excel bytes | Scan Xlsx builder (when present); user-report Excel merge modes |
| Azure | `Adapters/AzureOpenAiTemplateScanAiProvider`, `appsettings` `TemplateAiScan` |
| Tests | `Visa2026.Module.Tests/TemplateScan/` |

```powershell
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug --filter "FullyQualifiedName~TemplateScan"
```

---

## Chat openers

See [prompts.md](./prompts.md).