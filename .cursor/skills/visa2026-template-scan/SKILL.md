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
| **2 Review** | Mapped tokens vs yellow only; no bogus gaps. Left pane is the **uploaded Word/Excel as pdf.js pages** (no browser PDF chrome), numbered `#` on the letter matching Detected fields. **Click a Detected fields row** to highlight it and **add one or more library placeholders** on that same yellow mark (compound spans). Optional **Ask AI** docks chat with that mark’s context. Outline fallback if convert fails. Not `#visa-preview-slot` |
| **3 Generate** | Token writer on **copy**; **strip all yellow** markup; diff gate |
| **4 Preview** | Generated Office copy as PDF in the modal (not `#visa-preview-slot`). Outline fallback |
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
| Preview shows `{{IMAGE:Person_Photo}}` in the photo box after Add existing template | Word wrapped the long token in the photo cell. Restart, hard-refresh Preview. New Generate uses `{{IMAGE:PPH}}` | **This skill** + user-report-templates |
| Inserted sample photo not mapped | Body portrait (not a tiny icon) → `{{IMAGE:PPH}}` on Generate (`Person_Photo` still injects). Yellow still required for text values. Restart, Analyze | **This skill** |
| Create template should be one Project contract or all via-ministry cases | Upload **Save to** = This profile only. **All contracts** or pick one (same as profile Templates wizard). Shared catalog has no contract filter. Restart, hard-refresh | **This skill** |
| Comma yellow guessed from the wrong catalog group (e.g. TUR → PNAT on an Education line) | Printed **label** picks the group first (`Bilimi` → Education), then each comma part is guessed inside that group (`EGLV`, `EGCC`, `EGIN`). Restart, Analyze | **This skill** |
| Review Add placeholder missing Signatory / CompanySignatory (CHPN, CHPL, CHPD, CHPE) | Filter box: type `CompanySignatory` or `CHPE`. Group is **Authorized signatory**. Compound parts no longer hide sibling Signatory codes. Restart, hard-refresh | **This skill** |
| Review date 5.1 (`19.02.2034ý.`) has no Signatory passport expiration | `AuthorizedSignatory.PassportExpirationDate` + catalog **CHPE**. Fill expiration in Configuration. Restart, Analyze | **This skill** + user-report-templates |
| Review has extra 10.1 / 10.2 rows on one yellow | Row **×** hides that part; remaining token stays on the span. Last × drops the mark so Generate leaves printed text. Hard-refresh | **This skill** |
| PNG/JPG/PDF rejected | Expected — use yellow-marked Word/Excel | **This skill** |
| Yellow not detected | Word Text Highlight Color / Excel solid yellow fill | **This skill** |
| Wrong tokens / compound split | `ScanYellowHighlightTokenResolver` + catalog ShortCodes | **This skill** + user-report-templates |
| Wekil slot mapped to `{{.PFN}}` / catalog Preview fills a roster person | Printed caption `ygtyýarly wekili` → `{{ds.RPFN}}` (`AuthorizedRepresentative`). Isolated names stay `PFN`. Restart Analyze | **This skill** |
| Review maps company hasaba alyş date to `ADAT` / `ApplicationDateText` | `CompanyProfile.RegistrationDate` + `{{ds.ACRDT}}`. Nearby `hasaba alyş` / `şahamça` → not letter date. Restart, Analyze, set Company Registration Date in Configuration | **This skill** + user-report-templates |
| `42703: column c.RegistrationDate does not exist` | Host-start heal `CompanyProfileRegistrationDateSchemaSql`. Restart app (ModuleInfo already current skips XAF schema). Then Analyze | **This skill** |
| Review left pane is HTML text, not the Word page | Office→PDF via pdf.js pages (`TemplateScanOfficePdfPreview`). Hard-refresh, Analyze again. Not `#visa-preview-slot` | **This skill** |
| Review shows Chrome/Edge PDF toolbar or thumbnail sidebar | pdf.js canvases, not an iframe. Hard-refresh CSS/JS | **This skill** |
| Review AI mapped the wrong placeholder | Click the Detected fields row; add/remove Short codes on that mark (one yellow span can be a combination). Optional Ask AI with that mark in context | **This skill** |
| Yellow text contains a comma but Review is one row | Comma = combination candidate. Left label + parenthetical under the line (`hasaba alnan belgisi, senesi…`) guide each part. Review shows **6.1 / 6.2 / 6.3** with separate preview borders. Generate still writes one span. Restart, Analyze, hard-refresh | **This skill** |
| Review lost numbered marks / row click does not highlight the letter | Numbered overlays + sticky row select (`ActiveFieldId`). Click a Detected fields row | **This skill** |
| Review preview stays portrait for a landscape Word/Excel | Outline reads `sectPr`/`PageSetup`. Hard-refresh CSS, Analyze again. Not `#visa-preview-slot` | **This skill** |
| Review has no left document / no `#` on fields | Office outline + `ScanReviewFieldOrder` (top→bottom). Not `#visa-preview-slot`. Restart Analyze | **This skill** |
| Need to remap a saved Resminamalar template | Catalog row **Review placeholders** (nested this-profile Word/Excel). Opens scan Review on existing `{{…}}` tokens. Not desktop **Edit template**. Not `#visa-preview-slot` | **This skill** + resminamalar |
| Preview: 0 placeholders / “No yellow-marked spans could be written” | Review had tokens but Generate lost Word spans — restart, Analyze, Continue | **This skill** |
| Preview skips `CHFN`/`RPFN`: overlapping spans | Duplicate yellow of the same name in one paragraph — restart, Analyze, Generate | **This skill** |
| Word letter catalog Preview fails after Approve | Row tokens `{{.PFN}}` without `{{#ds.rows}}` — restart, re-Approve | **This skill** + resminamalar |
| Excel roster gaps (names, TUR, …) | Column header + manual inference (`ScanExcelYellowResolver`); not case value match | **This skill** |
| Preview blocking `{{ds.PVFM}}` / `Person_* not found on ApplicationProfileInstance` | Şahsy yellow classified Header wrote `{{ds.CODE}}` for Row-only Person tokens. Continue / Regenerated writes `{{.PVFM}}` (`PDBT`/`PCBT`/`PBPL`/`PFWC` same). Restart, hard-refresh | **This skill** |
| Review shows `{{ds.PLN}}` / Approve blocks `not found on ApplicationProfileInstance` | Row-only codes now stay `{{.PLN}}` even on Header yellow. Analyze again after restart | **This skill** |
| Azure ambiguous guess needs more context | Payload sends role/description + nearby snippet — not the Office file | **This skill** |
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
| Wizard Review/Preview pdf.js pages inside the modal | `#visa-preview-slot` inside wizard |
| | Resminamalar ZIP |

## Locked rules

1. Separate from Convert (yellow marks ≠ instance value match).
2. Yellow only → placeholders; library tokens only.
3. Preserve source Office layout (token writer on copy).
4. **Yellow is scan markup only** — after Generate, strip **all** highlighter/yellow fill from the saved copy (not only substituted runs). Unmapped leftovers (e.g. `6 (alty)` when only VCAT mapped) must not survive catalog Preview.
5. Officer Approve required.
6. Wizard Review/Preview shows the Office file as **pdf.js pages inside the modal** (not `#visa-preview-slot`, not the browser PDF viewer chrome). Numbered marks + row highlight stay. HTML outline is fallback only.
7. Config lock allows **new** templates.
8. **Comma in a yellow highlight** means a combination candidate. Use the **left-side label** and the **parenthetical caption under the line** to guess each part. Review shows **6.1 / 6.2 / 6.3** with separate preview borders; Generate still writes one compound token on the original span.
9. Resminamalar **Review placeholders** reopens the same Review dialog on a saved nested template. After Approve, yellow is gone — Review is driven by library `{{…}}` clusters (comma compounds stay one Generate span). Config lock still blocks overwrite of an existing name; officer must rename to save a copy.

## Pipeline

```text
Upload .docx/.xlsx (or Resminamalar Review placeholders)
  → Ingest → ScanOfficeYellowExtractor + body pictures (`{{IMAGE:PPH}}`) (else library {{…}} clusters)
  → Merge/split → Yellow gate (token-backed plans skip the no-yellow fail)
  → Review / optional Clarification
  → ITemplateTokenWriter → StripAllYellow* → diff gate → Extract/Validate → Outline → Approve
```

## Triage

| Layer | Look at |
|-------|---------|
| UI | `TemplateScanDialog.razor`, Resminamalar **Create from yellow marks** / **Review placeholders** |
| Yellow | `ScanOfficeYellowExtractor`, `ScanYellowHighlight*` |
| Generate | `TemplateScanOrchestrator` Office path, `ITemplateTokenWriter`, `StripAllYellowMarkup` / `StripAllYellowFills` |
| Tests | `Visa2026.Module.Tests/TemplateScan/` |

```powershell
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug --filter "FullyQualifiedName~TemplateScan"
```
