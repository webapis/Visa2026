# Create template from scan — reference

## File map

### Module (`Visa2026.Module/Services/TemplateScan/`)

| Area | Types |
|------|--------|
| DI | `TemplateScanServiceCollectionExtensions`, `TemplateAiScanOptions` |
| Ingest | `ScanIngestService`, `ScanInputNormalizer`, `ScanOcrExtractor`, `ScanSuitabilityEvaluator` |
| Field plan | `ScanFieldPlanService`, `ScanFieldPlanMerger`, `DeterministicScanFieldPlanner`, `ScanFieldPlanRequestBuilder` |
| Yellow | `ScanYellowHighlightGate`, `ScanYellowHighlightTokenResolver` |
| Layout | `ScanDocxLayoutService`, `DeterministicScanDocxLayoutPlanner`, `ScanLetterLayoutNormalizer`, `ScanDraftDocxBuilder` |
| AI | `ITemplateScanAiProvider`, `Adapters/AzureOpenAiTemplateScanAiProvider`, `NoneTemplateScanAiProvider` |
| Orchestrate | `TemplateScanOrchestrator`, `TemplateScanClarificationService`, `ScanAuthoringPlaybookService` |
| Models | `TemplateScanModels`, `ScanFieldPlanModels`, `TemplateScanOrchestratorModels` |

### Blazor (`Visa2026.Blazor.Server/Editors/`)

| File | Role |
|------|------|
| `TemplateScanDialog.razor` | Wizard host |
| `TemplateScanFieldReviewView.razor` | Review overlays + detected fields |
| `TemplateScanPreviewView.razor` | Outline-only draft preview (**not** PDF slot) |
| `TemplateScanClarificationView.razor` | Mapping Q&A |
| `TemplateScanGapHelpView.razor` | Needs help |
| `wwwroot/css/template-scan.css` | Wizard styles |

### Config

- `TemplateAiScan` in `appsettings.json` / `appsettings.Development.json`
- Env: `TEMPLATE_AI_SCAN_AZURE_OPENAI_API_KEY` (falls back to Convert key patterns when documented in learnings)

## Yellow compound splits (local)

| Scan text pattern | Tokens |
|-------------------|--------|
| `№ …` + date | `AFNUM`, `ADAT` |
| `N (words)` count | `TPCNT`, `TPCTX` |
| `N (words) aý` | `VPER` |
| `köp gezeklik` / gezeklik | `VCAT` |
| `Adaty tertipde!` | `Urgency_NameTm` |

## Letter layout blocks

- `kind=paragraph` — `align`: left \| right \| center \| justify; `style`: normal \| italic \| bold \| boldItalic
- `kind=twoColumn` — `text` (left), `rightText`, `rightAlign` (default right)
- Header row: left = `№ {{ds.AFNUM}}` + `{{ds.ADAT}}` (stacked); right = addressee boilerplate only
- Signature row: left = title; right = name; both bold

## Wizard Preview policy

- **In wizard:** `TemplateConvertOutlineView` (text + placeholder placement).
- **After save:** Resminamalar / profile catalog **Preview** → `#visa-preview-slot` PDF (preview-slot skill).
- Do **not** inject `ApplicationWordReportOfficePreviewPdfConverter` into `TemplateScanPreviewView`.

## Tests

`Visa2026.Module.Tests/TemplateScan/` — field plan, yellow resolver/gate, draft builder, orchestrator, Azure parse stubs, letter normalizer.
## Word vs Excel from scan

| | Word | Excel |
|--|------|-------|
| Shipped | `ScanDraftDocxBuilder` + `ScanLetterLayoutNormalizer` | Not yet (skill target) |
| Tokens | `{{ds.…}}` / `{{.…}}` Word syntax | Excel merge tokens per user-report-templates |
| Structure | Letter paragraphs + twoColumn | Sheets, header row, item loops |
| Spec note | Product S3 was Word-only v1 | Extend engineering spec when implementing |

When adding Excel-from-scan: new builder + layout proposal kinds; reuse `IEphemeralTemplateValidationService` / Extract for xlsx; do not invent tokens outside the profile Excel placeholder set.