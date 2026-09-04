# Engineering spec (Phase 0) — AI create profile template from scan/image

> **Status:** **§8 decisions locked 2026-08-28** · S0 product + prototypes **done** · **S1–S7 done 2026-08-28**  
> **Purpose:** Make [`TEMPLATE_AI_SCAN_PRODUCT_SPEC.md`](TEMPLATE_AI_SCAN_PRODUCT_SPEC.md) buildable. Product spec locks officer UX (S1–S13); this doc defines services, contracts, and slices a developer would otherwise invent.  
> **Audience:** Developer / AI agent. Not officer-facing.  
> **Skill:** [`visa2026-application-profile`](../.cursor/skills/visa2026-application-profile/SKILL.md) · [`visa2026-user-report-templates`](../.cursor/skills/visa2026-user-report-templates/SKILL.md) · sibling [`TEMPLATE_AI_CONVERT_ENGINEERING_SPEC.md`](TEMPLATE_AI_CONVERT_ENGINEERING_SPEC.md)  
> **Rule:** logic in **`Visa2026.Module/Services/TemplateScan/`**; Blazor host only for modal, scan viewport, and chat panel. **Do not** extend `TemplateConvertDialog` — separate `TemplateScanDialog.razor`.

---

## 0. Why this doc exists

Convert solves **reverse mapping** on structured Word/Excel. Scan solves **vision → field plan → draft generation**. Three new capabilities are required:

| Product rule | Depends on | Today |
|--------------|-----------|-------|
| **S11** — scan suitability | Raster/PDF ingest + OCR confidence | No scan ingest in Module |
| **S6 / S7** — placeholders from profile set | Field plan tied to E1 allowed set | E1 exists; no vision field plan |
| **S3 / S7** — draft `.docx` from layout | OOXML **builder** (not token writer) | E3 writer substitutes in existing docx only |
| **S8** — clarification chat | Chat that revises **field plan** before generate | Convert chat revises **mapping plan** on existing draft |
| **S9** — playbook in every AI call | Embedded `.md` + fingerprint | Playbook stub at `Resources/TemplateAuthoring/SCAN_AUTHORING_PLAYBOOK.md` |

**Phase 0 deliverable:** contracts §2–§7, decisions §8, slice plan §9. **S1–S3 need no cloud vendor** (deterministic stubs + local OCR spike optional).

---

## 1. Reuse map (do not rebuild)

| Need | Existing | Location / note |
|------|----------|-----------------|
| Profile-scoped placeholder set | `IApplicationProfilePlaceholderSetService` | `Services/TemplateConvert/` (E1) |
| Optional filled-scan value hints | `IApplicationProfileInstanceValueMapService` | E2 — when `ScanKind = FilledSample` + instance selected |
| Ephemeral Extract/Validate | `IEphemeralTemplateValidationService` | E6 — on generated draft bytes |
| Save nested template + bridge | `TemplateConvertOrchestrator.Save` logic | **Extract** to shared `ApplicationProfileTemplateSaveHelper` in S6 or call through thin wrapper — do not fork bridge rules |
| Plan sanitizer | `ITemplateMappingPlanSanitizer` | Reuse for any token list the AI returns pre-generate |
| Office → PDF preview | `ApplicationWordReportOfficePreviewPdfConverter` | Preview tab on V5 |
| Config lock (new templates) | `ApplicationProfileLockHelper.AllowsNestedEditWhenConfigLocked` | New-object carve-out already shipped for Convert |
| Permissions | `TemplateConvertAccess.CanConvertTemplates()` | Rename or add `TemplateScanAccess` alias — same write gates |
| Upload size | `TemplateEditStagingOptions.MaxFileSizeBytes` (50 MB) | Scan warn at 20 MB (product S2) |
| OpenXML | `DocumentFormat.OpenXml` 3.3.0 | Draft builder |

**Namespace for new code:** `Visa2026.Module.Services.TemplateScan`.

**Reuse for Office yellow path:** `ITemplateTokenWriter` + `ITemplateConversionDiffGate` (substitute yellow spans in existing OOXML). **Do not reuse:** `ITemplateConvertOrchestrator`, `ITemplateCandidateAnalyzer` (value-match highlights ≠ yellow highlighter). Image/PDF path still uses `ScanDraftDocxBuilder` (new docx).

---

## 2. S1 — Playbook loader + scan ingest + suitability

### 2.1 `IScanAuthoringPlaybookService` (singleton)

```csharp
public interface IScanAuthoringPlaybookService
{
    ScanAuthoringPlaybook GetPlaybook();
}

public sealed class ScanAuthoringPlaybook
{
    public required string Markdown { get; init; }
    /// <summary>SHA-256 hex of Markdown — audit metadata on every provider call.</summary>
    public required string Fingerprint { get; init; }
    public required string VersionLabel { get; init; }
}
```

Loads embedded resource `Resources/TemplateAuthoring/SCAN_AUTHORING_PLAYBOOK.md`. Fail closed if missing.

### 2.2 `IScanInputNormalizer` (scoped)

```csharp
public interface IScanInputNormalizer
{
    ScanNormalizedInput Normalize(ScanNormalizeRequest request);
}

public sealed class ScanNormalizeRequest
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    /// <summary>1-based page numbers to analyze; null = all (max <see cref="TemplateAiScanOptions.MaxPdfPages"/>).</summary>
    public IReadOnlyList<int>? SelectedPages { get; init; }
}

public sealed class ScanNormalizedInput
{
    public required ScanSourceKind SourceKind { get; init; }  // Image, Pdf
    public required IReadOnlyList<ScanPageImage> Pages { get; init; }
    public required long OriginalByteLength { get; init; }
}

public sealed class ScanPageImage
{
    public required int PageIndex { get; init; }
    public required byte[] PngBytes { get; init; }
    public required int WidthPx { get; init; }
    public required int HeightPx { get; init; }
}

public enum ScanSourceKind { Image, Pdf, Word, Excel }
```

**v1 implementation notes:**

| Format | Approach |
|--------|----------|
| PNG/JPG | Pass through or re-encode to PNG for vision API |
| PDF | Render pages to PNG — **Spire.PDF** (already in Module) or **DevExpress PdfDocumentProcessor** spike in S2; cap pages per **SD-D3** |
| Word `.docx` / Excel `.xlsx` | **Office yellow path:** `ScanOfficeYellowExtractor` → field plan (no vision); Generate uses `ITemplateTokenWriter` on a **copy** of the upload (layout preserved). Do **not** merge with Convert value-matching. |

### 2.3 `IScanSuitabilityEvaluator` (scoped)

```csharp
public interface IScanSuitabilityEvaluator
{
    ScanSuitabilityReport Evaluate(ScanSuitabilityRequest request);
}

public sealed class ScanSuitabilityRequest
{
    public required ScanNormalizedInput Input { get; init; }
    public required IReadOnlyList<ScanOcrLine> OcrLines { get; init; }  // from local OCR or provider
}

public sealed class ScanSuitabilityReport
{
    public required ScanSuitabilityVerdict Verdict { get; init; }  // Pass, Warn, Fail
    public required double TextConfidence { get; init; }           // 0..1 aggregate
    public required IReadOnlyList<ScanSuitabilityIssue> Issues { get; init; }
    public bool CanContinue => Verdict != ScanSuitabilityVerdict.Fail;
}

public enum ScanSuitabilityVerdict { Pass, Warn, Fail }
public enum ScanSuitabilityIssueCode
{
    FileTooLarge,
    TooManyPages,
    ResolutionTooLow,
    SkewExcessive,
    TextConfidenceLow,
    NoTextDetected,
    UnsupportedFormat,
}
```

**Thresholds (SD-D2):** Fail &lt; 40% text confidence; Warn 40–70%; Pass &gt; 70%. Bound in `TemplateAiScan:Suitability:*`, never `const`.

**S1 tests:** playbook fingerprint stable; PDF 6 pages → `TooManyPages`; 50×50 px image → `ResolutionTooLow`.

---

## 3. S2 — Vision adapter + field plan

### 3.1 `ITemplateScanAiProvider`

Separate from `ITemplateConvertAiProvider` — different payloads (images vs document extract).

```csharp
public interface ITemplateScanAiProvider
{
    string ProviderKey { get; }
    bool IsEnabled { get; }

    /// <summary>Detect fields + static regions on scan pages. Never receives raw DB rows — see SD-D1.</summary>
    Task<ScanFieldPlanProposal> ProposeFieldPlanAsync(
        ScanFieldPlanRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Clarification turn: revise field plan only (S8).</summary>
    Task<ScanClarificationResult> ClarifyAsync(
        ScanClarificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Layout + placeholder placement spec for local docx builder.</summary>
    Task<ScanDocxLayoutProposal> ProposeDocxLayoutAsync(
        ScanDocxLayoutRequest request,
        CancellationToken cancellationToken = default);
}
```

`NoneTemplateScanAiProvider`: `IsEnabled = false`; Analyze returns empty plan + message "AI provider required".

### 3.1.1 Request payload (SD-D1)

| Allowed in provider prompt | Forbidden |
|----------------------------|-----------|
| Page PNG(s) (or downscaled) | Other instances / cases |
| OCR text lines (optional adjunct) | Full placeholder catalog |
| **Allowed token list** (short code + label + scope) from E1 | Raw passport numbers unless officer opted in with redaction off |
| Playbook markdown + fingerprint | SQL, admin secrets |
| Field plan JSON schema | Entire `UserReportPlaceholderCatalog.json` |

When **filled scan + instance** (S5): include **masked value hints** `{ label, token, maskedValue }` built from E2 — same redaction flag as Convert (`RedactIdentifiersInExtract`).

### 3.2 Field plan model

```csharp
public sealed class ScanFieldPlan
{
    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }
    public required ScanKind ScanKind { get; init; }  // BlankForm, FilledSample
    public required IReadOnlyList<ScanDetectedField> Fields { get; init; }
    public required IReadOnlyList<ScanStaticRegion> StaticRegions { get; init; }
    public required IReadOnlyList<ScanGap> Gaps { get; init; }
    public required IReadOnlyList<ScanClarificationPrompt> PendingQuestions { get; init; }
}

public sealed class ScanDetectedField
{
    public required string FieldId { get; init; }           // stable guid string in session
    public required ScanBoundingBox Box { get; init; }      // normalized 0..1 on page
    public required int PageIndex { get; init; }
    public required string LabelText { get; init; }         // OCR label
    public string? ProposedToken { get; init; }               // {{ds.…}} or null if gap
    public required ScanFieldConfidence Confidence { get; init; }
    public required ScanFieldScope Scope { get; init; }      // Header, Row, Loop
}

public enum ScanFieldConfidence { High, Medium, Low }
public enum ScanFieldScope { Header, Row, LoopBoundary, Static }
public enum ScanKind { BlankForm, FilledSample }

public sealed record ScanGap(string FieldId, string LabelText, string? SuggestedPropertyName);
public sealed record ScanClarificationPrompt(string Question, IReadOnlyList<string> SuggestedAnswers);
```

### 3.3 `IScanFieldPlanMerger` (scoped)

Deterministic post-processor after provider returns:

| Step | Rule |
|------|------|
| 1 | Drop `ProposedToken` not in E1 set |
| 2 | Promote E2 value match when filled scan + instance (boost confidence) |
| 3 | Split gaps vs mapped fields |
| 4 | Flag `Low` confidence → drives V8 acknowledge checkbox |

**S2 tests:** sanitizer drops invented token; E2 boost raises confidence on golden filled scan.

---

## 4. S3 — Clarification chat (S4 product slice)

### 4.1 `ITemplateScanClarificationService`

```csharp
public interface ITemplateScanClarificationService
{
    Task<ScanClarificationTurnResult> ApplyAsync(
        ScanClarificationTurnRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ScanClarificationTurnRequest
{
    public required string OfficerMessage { get; init; }
    public required ScanFieldPlan CurrentPlan { get; init; }
    public required ScanAuthoringPlaybook Playbook { get; init; }
    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }
}

public sealed class ScanClarificationTurnResult
{
    public required bool Accepted { get; init; }
    public required string ReplyText { get; init; }
    public ScanClarificationRejectReason? RejectReason { get; init; }
    public required ScanFieldPlan Plan { get; init; }
}
```

**Intent classifier** (local, no AI): mirror `TemplateConvertChatIntentClassifier` with scan-specific out-of-scope rules (S8 — no restyle/translate).

Provider called only for **mapping / disambiguation** intents. Revised plan re-run through `IScanFieldPlanMerger`.

---

## 5. S5 — Draft docx builder + orchestrator

### 5.1 `IScanDraftDocxBuilder` (scoped)

```csharp
public interface IScanDraftDocxBuilder
{
    ScanDraftDocxResult Build(ScanDraftDocxRequest request);
}

public sealed class ScanDraftDocxRequest
{
    public required ScanDocxLayoutProposal Layout { get; init; }
    public required ScanFieldPlan FieldPlan { get; init; }
}

public sealed class ScanDraftDocxResult
{
    public required byte[] Content { get; init; }
    public required IReadOnlyList<string> EmittedTokens { get; init; }
}
```

**Layout proposal** describes blocks: paragraphs (static text), tables (columns → placeholders), optional `{{#ds.rows}}` wrapper when `ScanFieldScope.LoopBoundary` detected.

**Implementation:** `DocumentFormat.OpenXml` — create `WordprocessingDocument` from scratch. **Not** pixel-aligned to scan; officer preview sets expectations (product non-goal).

**Quality gate after build:**

1. `IEphemeralTemplateValidationService.ValidateAsync` on bytes + E1 set  
2. Merge validation errors/warnings into `TemplateScanOutcome` (same severity split as Convert E-D2)

### 5.2 `ITemplateScanOrchestrator` (scoped)

```csharp
public interface ITemplateScanOrchestrator
{
    TemplateScanAnalysis Analyze(TemplateScanAnalyzeRequest request);

    Task<TemplateScanOutcome> GenerateAsync(
        TemplateScanAnalysis analysis,
        CancellationToken cancellationToken = default);

    ApplicationProfileTemplate Save(TemplateScanSaveRequest request);
}

public sealed class TemplateScanAnalyzeRequest
{
    public required ApplicationProfile Profile { get; init; }
    public ApplicationProfileInstance? Instance { get; init; }  // optional — S5
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public required string TemplateName { get; init; }
    public ApplicationProfileTemplateDataScope DataScope { get; init; }
    public ScanKind ScanKind { get; init; }
    public IReadOnlyList<int>? SelectedPages { get; init; }
}

public sealed class TemplateScanAnalysis
{
    public required ScanNormalizedInput NormalizedInput { get; init; }
    public required ScanSuitabilityReport Suitability { get; init; }
    public required ScanFieldPlan FieldPlan { get; init; }
    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }
    public required ScanAuthoringPlaybook Playbook { get; init; }

    public bool CanGenerate =>
        Suitability.CanContinue
        && FieldPlan.Fields.Any(f => f.ProposedToken != null)
        && !SuitabilityIssuesBlockGenerate(Suitability);
}

public sealed class TemplateScanOutcome
{
    public required byte[] Content { get; init; }
    public required TemplateValidationReport Validation { get; init; }
    public required IReadOnlyList<string> Errors { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
    public required IReadOnlyList<ScanGap> Gaps { get; init; }

    public bool CanApprove => Errors.Count == 0;
}
```

**Sequence:**

```
Normalize → OCR (local or provider) → Suitability
  → E1 placeholder set → ProposeFieldPlan (AI) → Merger
  → [optional chat loops]
  → ProposeDocxLayout (AI) → Build docx → E6 Validate → Outcome
```

**Save:** same rules as Convert — caller owns `IObjectSpace`, orchestrator **never commits**. Reuse `TemplateConvertSaveRequest` shape or shared helper.

---

## 6. S7 — Gap packet export

```csharp
public interface IScanGapPacketExporter
{
    byte[] ExportJson(ScanGapPacketRequest request);
    byte[] ExportMarkdown(ScanGapPacketRequest request);
}

public sealed class ScanGapPacketRequest
{
    public required Guid ApplicationProfileId { get; init; }
    public Guid? ApplicationProfileInstanceId { get; init; }
    public required string ScanContentSha256 { get; init; }
    public required ScanFieldPlan FieldPlan { get; init; }
    public required TemplateValidationReport? Validation { get; init; }
    public required string PlaybookFingerprint { get; init; }
    public required string PlaceholderSetFingerprint { get; init; }
}
```

No new BO (SD-D5) — export file download only, same as Convert E-D8.

Optional v2: draft `_map.md` generator from `ScanFieldPlan` — not in S7 v1.

---

## 7. Configuration and DI

### 7.1 `TemplateAiScanOptions`

```csharp
public sealed class TemplateAiScanOptions
{
    public const string SectionName = "TemplateAiScan";

    public bool Enabled { get; set; }
    public bool ShowInstanceEntry { get; set; }  // mirror Convert L13 until per-user switch
    public long MaxUploadBytes { get; set; } = 20_971_520;
    public long HardMaxUploadBytes { get; set; } = 52_428_800;  // 50 MB
    public int MaxPdfPages { get; set; } = 5;
    public string Provider { get; set; } = NoneTemplateScanAiProvider.ProviderKey;
    public int RequestTimeoutSeconds { get; set; } = 90;
    public bool RedactIdentifiersInExtract { get; set; } = true;

    public TemplateAiScanAzureOpenAiOptions AzureOpenAI { get; set; } = new();
    public ScanSuitabilityOptions Suitability { get; set; } = new();
}

public sealed class TemplateAiScanAzureOpenAiOptions
{
    public string? Endpoint { get; set; }
    /// <summary>Vision-capable deployment (e.g. gpt-4o).</summary>
    public string? Deployment { get; set; }
    public string ApiVersion { get; set; } = "2024-10-21";
    /// <summary>Env: TEMPLATE_AI_SCAN_AZURE_OPENAI_API_KEY</summary>
    public string? ApiKey { get; set; }
}
```

### 7.2 Registration

```csharp
public static IServiceCollection AddTemplateScan(
    this IServiceCollection services,
    IConfiguration? configuration = null)
{
    services.Configure<TemplateAiScanOptions>(configuration?.GetSection(TemplateAiScanOptions.SectionName) ?? new ConfigurationBuilder().Build().GetSection("missing"));
    // … register S1–S7 services, provider keyed by TemplateAiScan:Provider
    return services;
}
```

Register in `Visa2026.Blazor.Server/Startup.cs` **alongside** `AddTemplateConvert`, not inside it.

### 7.3 Azure adapter (S2)

`Adapters/AzureOpenAiTemplateScanAiProvider` — separate env key from Convert:

| Convert | Scan |
|---------|------|
| `TEMPLATE_AI_CONVERT_AZURE_OPENAI_API_KEY` | `TEMPLATE_AI_SCAN_AZURE_OPENAI_API_KEY` |

Vision calls use **chat completions with image_url** (base64 PNG) or Azure Document Intelligence spike documented in S2 learnings — pick one in S2 spike, lock in SD-D4.

---

## 8. Locked engineering decisions

Approved 2026-08-28.

| # | Topic | Decision |
|---|--------|----------|
| **SD-D1** | Provider data access | Images + OCR lines + allowed token **names** + optional **masked** value hints only. Never raw instance dump |
| **SD-D2** | Suitability thresholds | Fail &lt; 0.40 text confidence; Warn 0.40–0.70; Pass &gt; 0.70. Config-bound |
| **SD-D3** | PDF page cap | **5 pages** v1 (`MaxPdfPages`). Officer selects subset (V10) |
| **SD-D4** | Vision backend v1 | **Azure OpenAI vision** deployment on same resource as Convert; Document Intelligence optional spike — not dual-required for v1 |
| **SD-D5** | Draft persistence | **Ephemeral** — bytes live in modal state until Approve. No `TemplateScanDraft` BO in v1 (product O3) |
| **SD-D6** | Warning gate | Same as Convert **E-D2**: checkbox on Preview for `Warning`; `Error` blocks Approve |
| **SD-D7** | Config lock | **New** template save allowed while locked (existing Convert carve-out). No extra engineering |
| **SD-D8** | Save helper | Extract `ApplicationProfileTemplateSaveHelper` from `TemplateConvertOrchestrator.Save` in **S6** — both features call it |
| **SD-D9** | Time budget | p95 **30 s** analyze, **120 s** generate (vision + build). Provider timeout 90 s default |
| **SD-D10** | Separate modal | `TemplateScanDialog.razor` + `wwwroot/css/template-scan.css`. Entry label **Create from scan** |

### Product open questions — engineering defaults (locked for build)

| Product O# | Decision |
|------------|----------|
| O1 | Case entry off by default — `ShowInstanceEntry = false` |
| O2 | 5 PDF pages — SD-D3 |
| O3 | Ephemeral — SD-D5 |
| O4 | 40/70% thresholds — SD-D2 |

---

## 9. Slice plan

| # | Slice | Depends on | Needs AI? | Deliverable |
|---|-------|------------|-----------|-------------|
| **S0** | Product + UI flow + prototypes | — | No | **Done** 2026-08-28 |
| **S1** | Playbook loader, normalizer, suitability, options, DI skeleton | S0, SD-D2/3 | No | **Done** 2026-08-28 |
| **S2** | `ITemplateScanAiProvider` + `None` + Azure vision adapter; field plan merger | S1 | **Yes** for real adapter | **Done** 2026-08-28 |
| **S3** | `TemplateScanDialog` upload + field review UI (V1–V2, V7–V8, V10–V11) | S1, S2 | No for UI | **Done** 2026-08-28 |
| **S4** | Clarification chat service + UI (V3) | S2, S3 | Optional | **Done** 2026-08-28 |
| **S5** | Docx builder + `GenerateAsync` + preview/validate (V4–V5) | S2, E6 | Layout call yes | Golden docx bytes + Validate pass |
| **S6** | Save helper extract + wizard/case entries (V0, V6) | S5, SD-D8 | No | Both entry points |
| **S7** | Gap packet export (V9) + `TemplateScan` audit category | S5 | No | JSON/MD download |

**Sequencing:** S1 → S2 (parallel UI S3 once field plan DTO stable) → S4 → S5 → S6 → S7.

**S1–S3 ship a shell** with `Provider=None` (entry disabled per prototype 12) without blocking manual Add / Convert.

**Tracking:** rows in [`IMPLEMENTATION_PLAN.md`](../.cursor/skills/visa2026-application-profile/IMPLEMENTATION_PLAN.md).

---

## 10. Test matrix

| Product criterion | Test |
|-------------------|------|
| S6 vocabulary | Field plan never emits token outside E1 set after merger |
| S10 validate gate | Approve disabled when `TemplateValidationReport.HasHardFailure` |
| S11 suitability | Fail low-confidence scan; warn mid band |
| S8 chat scope | Restyle request → rejected locally without provider call |
| SD-D1 | Provider request DTO has no `IObjectSpace`, no raw passport in snapshot test |
| Save | Wizard path: no commit inside orchestrator; case path: commit after Save |
| Config lock | New template save succeeds on locked profile; update existing blocked |

**Golden set (outstanding):** 3 ministry scan PNGs (header letter, simple table, low-quality phone photo) — gates pilot, not S1.

---

## 11. Blazor file layout (S3/S6)

| File | Role |
|------|------|
| `Editors/TemplateScanDialog.razor` | Modal orchestration V1–V6 |
| `Editors/TemplateScanFieldReviewView.razor` | Viewport + field list (V2) |
| `Editors/ApplicationProfileWizardStepTemplatesPerson.razor` | **Create from scan** button |
| `Editors/ApplicationReportPackageComponent.razor` | Case Resminamalar entry |
| `wwwroot/css/template-scan.css` | Ported from prototypes |

---

## 12. Relationship to Convert (do not merge)

| | Convert | Scan |
|---|---------|------|
| Orchestrator | `ITemplateConvertOrchestrator` | `ITemplateScanOrchestrator` |
| Provider | `ITemplateConvertAiProvider` | `ITemplateScanAiProvider` |
| Config section | `TemplateAiConvert` | `TemplateAiScan` |
| Core transform | Token writer on existing docx | Docx builder from field plan |
| Chat | Mapping on draft | Clarification on plan pre-generate |

Shared: E1, E2 (optional), E6, save helper, access gate pattern.

---

## Revision log

| Date | Change |
|------|--------|
| 2026-08-28 | S4 shipped: clarification service, S8 intent classifier, V3 chat UI, Azure clarify adapter |
| 2026-08-28 | S3 shipped: TemplateScanDialog, field review view, wizard entry, template-scan.css |
| 2026-08-28 | S2 shipped: field plan service, deterministic planner, merger, Azure vision adapter, 20 tests |
