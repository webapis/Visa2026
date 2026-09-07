# Plan — field extraction from uploaded document copies (Passport first)

> **Status:** **Phase 0 plan · decisions X-D1–X-D13 locked 2026-09-07 · nothing implemented yet.**
> **Purpose:** Officer uploads a copy of a real document (passport page, foreign visa, invitation, work permit, diploma), the system proposes field values from it, the officer reviews and accepts, and the business object is saved **together with** the uploaded file. This doc defines the services that do not exist yet and the decisions a developer would otherwise have to invent.
> **Audience:** Developer / AI agent implementing the feature. Not officer-facing.
> **Scope BOs:** `Passport` (slice X3), `Visa` (X6), `Invitation` / `WorkPermit` (X7, verification only), `Education` (X8).
> **Rule:** all extraction logic in **`Visa2026.Module`**; Blazor host only for upload, the review panel, and the preview slot. See `.cursor/rules/visa2026-module.mdc`.
> **Skill:** none yet — docs-first per **AGENTS.md** (promote to `.cursor/skills/` after the second repeat or when a slice touches prod data).
> **UI prototypes:** 9 screens covering the whole flow — [`prototypes/document-scan-README.md`](prototypes/document-scan-README.md). Illustrative of the locked decisions, not final pixels.

---

## 0. Feasibility summary

The officer flow already exists without any AI. `IssueIssuedVisaComposeService` holds an uploaded copy as pending bytes (`PendingCopyBytes` / `PendingCopyFileName`) and calls `AttachPendingCopy` → `AttachFile` only when the record commits. Extraction inserts a stage between upload and review; it does not need a new persistence or commit story.

Three of the four remaining pieces are already shipped for other features (provider pattern, staged review wizard, document byte storage). The one genuine gap is **reading text off raster scans** — `ScanOcrExtractor` returns an empty result for `ScanSourceKind.Image` by design, because Template Scan delegated that to vision.

The decisive enabler for slice 1 is that **passport MRZ is deterministic and offline**, and `Country.Code` in `DatabaseUpdate/LookupCatalogs/country.json` is ISO 3166-1 **alpha-3** (`TKM`, `GBR`, `SAU`) — the exact code format the MRZ carries. Nationality and issuing state therefore resolve by exact match with no fuzzy matching, and MRZ check digits give real per-field confidence instead of a model's self-reported score.

---

## 0a. Evidence from the real corpus (legacy `VISA2015`, profiled 2026-09-07)

Read-only profiling of **`dbo.Visa.Göçürme Nusga`** (`varbinary(max)`, inline on the visa row) on `10.100.128.15`. Full detail and method: [`visa2014-to-visa2026-import/learnings.md`](../.cursor/skills/visa2014-to-visa2026-import/learnings.md) entry **2026-09-07**.

| Measurement | Value | Consequence for this plan |
|-------------|-------|---------------------------|
| Active visa rows / with scan | 6,392 / **6,327** (99.0%) | Officers already attach a copy essentially always |
| Format **in the legacy corpus** | **JPEG 89.4%, PNG 10.6%, zero PDF** | Raster OCR is unavoidable — X4 is not optional. **This is a fact about legacy data only, not about accepted input:** Visa2026 accepts PDFs too, so both paths must exist (see **§0b**) |
| Blob size | min 51 KB · avg 1.2 MB · **max 25.7 MB** · 7.4 GB total | `MaxUploadSizeMB: 20` was too low; raised to 32 |
| Short side (400-row sample) | <500 px **3.5%** · 500–799 px 6.3% · 800–1199 px 28.5% · 1200–1899 px 43.5% · ≥1900 px 18.3% | **~90% workable, ~10% marginal/unusable** — the suitability gate must reject, not guess |
| Orientation | 99.3% portrait **frames** | Frame aspect ≠ content rotation; photos still appear sideways |

**What the documents actually are:** every sample is a **Turkmenistan visa sticker** on a passport page — this system's **own issued output** — a fixed-layout bilingual form with machine-printed values (PLACE OF ISSUE, DATE OF ISSUE, DOCUMENT NUMBER, VALID FROM, VALID UNTIL, DURATION OF STAY, TYPE OF VISA, NUMBER OF ENTRIES, ÇAKYLYK/INVITATION, SURNAME, GIVEN NAMES, PASSPORT No, SEX, DATE OF BIRTH, CITIZENSHIP). Fixed labels plus machine print means **anchor/template extraction is viable**, not just free-form OCR.

**The sticker carries its own MRZ.** Line 2 is standard **MRV-B with check digits** (doc no(9)+cd · nationality(3) · DOB(6)+cd · sex · expiry(6)+cd). Line 1 is **Turkmenistan-specific with two generations**:

| Generation | Line 1 shape | Seen in |
|------------|--------------|---------|
| Old | `V{TYPE2}<SURNAME<<GIVEN` | 2014–2018 samples |
| New | `{TYPE2}TKM SURNAME<<GIVEN` (`WPTKM…`, `BSTKM…`) | 2024–2026 samples |

`TYPE2` matches the legacy `VisaType` codes (`WP`, `BS`). **A stock ICAO MRZ library will mis-parse line 1** — a custom parser plus a corpus study is required. Verified on 2 spot checks (`A0982228` 2015 flatbed, `A1607916` 2025 photo): MRZ document number, DOB, sex and expiry matched the printed zone **and** the database columns exactly.

**Two hazards to design for:**

1. The red arrival **rubber stamp overlaps the MRZ and data fields** on many stickers. Because the stamp is red and the text black, **red-channel suppression** is a cheap, high-value preprocessing step.
2. A minority are **phone photos** with content rotation, page curvature and hologram glare.

### Header documents: `Invitation`, `WorkPermit`, `Education`, `Passport` (`dbo.PassportCopy`)

Those four share one legacy scan table, `dbo.PassportCopy` (blob column `Göçürme`), with a separate owner FK each — `Implicit_IWorkPermitLetter_…` → `WorkPermitLetter`, `Implicit_IApplicationResult_…` → `ApplicationResult` (`Result=0` invitation, `Result=1` rejection), `Education`, `Passport`. Only `dbo.Visa` keeps its blob inline.

| Owner | rows / with blob | avg · max | Pages per owner |
|-------|------------------|-----------|-----------------|
| Education | 4,500 / 4,466 | 881 KB · **16.1 MB** | avg 1.72 — **48% have 2+** (1,129 have exactly 2), max 15 |
| Passport | 3,799 / 3,798 | 734 KB · **18.5 MB** | avg 1.01 — 99.3% single |
| Invitation (`Result=0`) | 3,092 / 3,087 | 715 KB · 3.0 MB | avg 1.05 — 95.7% single |
| WorkPermitLetter | 1,024 / 1,022 | 1.01 MB · 4.3 MB | **avg 2.53 — never 1 page**, max 10 |
| Rejection (`Result=1`) | 164 / 163 | 328 KB · 2.7 MB | — |

**Still zero PDFs** across all of it (~18.9k scans corpus-wide), but **TIFF ×3 and BMP ×4** do occur, so those branches in §0b are real rather than defensive.

**`Invitation` is the easiest target in the corpus.** The copy is an A4 typed State Migration Service letterhead (`ÇAKYLYK - INVITATION`) at 2480×3507 (300 DPI) or 1654×2340 (200 DPI): machine-printed, high contrast, fixed layout. **All four database header fields appear verbatim on the page** — `Number`, `IssuedDate`, `DateOfExpire`, `ASBelgisi` — plus lookup-resolvable type of visa (`BS1`), number of entries (`Iki gezeklik` → Double), visa period (`1 AY` → Month1) and border zone (`Ýok` → None).

**`WorkPermit` is the hardest target.** Page 1 is the `RUGSATNAMA` letter at 3310×4704 (~400 DPI) whose **number and date are handwritten** (`« 01 » 04 201 4 ý.`, `№ 769`). Printed-text OCR will not read those; handwriting recognition is a materially weaker problem, so **page-1 number and date stay officer-entered**. Pages 2..n are the annexed roster (`SANAWY`) — the letter even declares its own annex length, which is why the page count is never 1.

**A legacy data gap the copies could fill:** invitation `ASGH457223` (2017) has an **empty `ASBelgisi` in the database** while the scan plainly prints `AS-621785`.

### The legacy corpus is a labelled benchmark

`dbo.Visa` stores `VisaNumber`, `ASNumber`, `VisaIssuedDate`, `VisaStartDate`, `VisaEndDate` and the `VisaType` / `VisaCategory` / `VisaIssuedPlace` FKs **on the same row as the blob**. So 6,327 real scans come with ground truth attached, and extraction accuracy can be measured **at scale with no manual labelling**. This turns the X4 OCR decision gate from a judgement call into a measured number, and it is the single most useful thing found in this profiling pass.

Normalization note: the invitation number is printed with a hyphen (`AS-515666`) but stored without it (`ASNumber = AS515666`); the newer series is `CO…`. Strip separators before comparing.

### Sequencing reviewed against this evidence — **`Passport` first retained** (2026-09-07)

The corpus made `Visa` a credible alternative first slice (single fixed layout, MRZ with check digits, ~6.3k labelled samples, 99% attachment coverage). **Reviewed and rejected:** `Passport` stays slice 1 because its extracted data is genuinely new to the system, whereas a visa this system issued is already in the database and extraction there is verification only (**X-D5**) — so `Passport` delivers officer value first. The MRZ, review-panel and lookup-resolution work transfers to `Visa` in X6, and the X2b benchmark harness can still be built against the visa corpus regardless of slice order.

---

## 0b. Input formats — **PDF and images both** (X-D11)

The uploaded copy is whatever `DocumentBase.File` already accepts, so extraction must handle **PDF and raster images**, not images alone. The allow-list and magic-byte sniff are already implemented and **must be reused, not re-declared**:

```15:22:Visa2026.Module/Services/DocumentFileUploadConstraints.cs
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".gif", ".bmp"
    };

    public const string AllowedExtensionsDisplay = ".pdf, .png, .jpg, .jpeg, .tif, .tiff, .gif, .bmp";
```

`ContentMatchesDeclaredExtension` already rejects a file renamed to the wrong extension, so **the source kind is decided by sniffed content, never by the file name**.

### Per-format path

| Input | Source kind | Path | Notes |
|-------|-------------|------|-------|
| **PDF with a text layer** | `PdfTextLayer` | `ScanOcrExtractor.ExtractFromPdf` (Spire `PdfTextExtractor`) — **already shipped** | Cheapest and most exact route. Digitally produced PDFs (e-visas, printed-to-PDF invitations, work permits) land here. Try this **before** OCR |
| **PDF, image-only (scanned)** | `Pdf` → rasterize → `Ocr` | Render each page to a bitmap, then the X4 OCR path | The common case for scanned paperwork. Detect by "text layer yielded almost nothing" |
| **PDF, mixed** | per page | Decide **per page**, not per document | A scan appended to a generated PDF is normal |
| **JPEG / PNG** | `Image` | X4 OCR path directly | 100% of the legacy visa corpus (§0a) |
| **TIFF** | `Image`, **multi-frame** | Enumerate frames, treat each as a page | Fax/scanner output is often multi-page TIFF — a single-frame assumption silently drops pages. **Present in the legacy corpus (§0a)**, so this branch is real |
| **GIF / BMP** | `Image` | X4 OCR path directly | Rare (**4 BMPs in the legacy corpus**); accept but do not optimise |

### Rules this adds

| Rule | Why |
|------|-----|
| **Try the text layer first, always** | A digital PDF gives exact characters with no OCR error. Skipping it would throw away the best signal and spend OCR time for worse results |
| **Page/frame model is mandatory** | PDFs and TIFFs are multi-page. `DocumentExtractionFieldProposal.SourcePageIndex` already exists in §3 — it must be populated, and the review panel must show *which page* a value came from |
| **The relevant page must be located, not assumed** | A passport scan is often several pages with only one bio-data page; a visa PDF may hold many stickers. Score pages (MRZ present? expected anchors present?) and let the officer switch pages. Never silently read page 1 only |
| **Per-page suitability** | The `MinShortSidePixels` gate from §0a applies **per rasterized page**, not to the file |
| **The page set spans sibling files, not just pages** | A logical document is often several uploads (X-D13). Building the page set per *file* would read page 1 of 3 of a work-permit letter and call it complete |
| **`MaxPages` cap** | A 60-page passport PDF must not trigger 60 OCR passes; cap it, and prefer pages that scored highest |

**Rasterization:** Spire.PDF is already referenced and used for text extraction, and Template Scan already models page images (`ScanOcrRequest.Input.Pages`, `ScanPageImage.PageIndex`) — extend that shape rather than inventing a parallel one. Choosing the render DPI is part of the **X4** decision gate: too low and the MRZ is unreadable, too high and memory and latency suffer on a 25 MB input.

---

## 1. Reuse map (do not rebuild these)

| Need | Existing type | Location |
|------|---------------|----------|
| Pending upload held until commit | `IssueIssuedVisaComposeService.AttachPendingCopy` / `.AttachFile` / `.AddDocument` · `IssueIssuedHeaderComposeService.AddDocument` | `Services/PreviewSlot/` |
| Attachment storage (bytes in Postgres) | `DocumentBase.File` → `DevExpress.Persistent.BaseImpl.EF.FileData` (`Content`, `FileName`, `Size`), `[Aggregated]` | `BusinessObjects/DocumentBase.cs` |
| Concrete attachment rows | `PassportDocument`, `VisaDocument`, `InvitationDocument`, `WorkPermitDocument`, `EducationDocument` | `BusinessObjects/` |
| Upload validation (type sniff + size) | `DocumentFileUploadConstraints` · `IssueIssuedHeaderComposeService.ValidateDocumentBytes` | `Services/` |
| AI provider pattern to copy | `ITemplateScanAiProvider` + `NoneTemplateScanAiProvider` + `Adapters/AzureOpenAiTemplateScanAiProvider` + `TemplateAiScanOptions` + `AddTemplateScan()` | `Services/TemplateScan/` |
| Same pattern, second precedent | `ITemplateConvertAiProvider` + `None…` + `Adapters/AzureOpenAi…` + `TemplateAiConvertOptions` + `AddTemplateConvert()` | `Services/TemplateConvert/` |
| Identifier redaction before any cloud call | `TemplateMappingRequestBuilder.MaskPreview` | `Services/TemplateConvert/` |
| PDF text layer extraction | `ScanOcrExtractor.ExtractFromPdf` (Spire `PdfTextExtractor`) · confidence via `ComputeConfidence` | `Services/TemplateScan/ScanOcrExtractor.cs` |
| Page-image model for multi-page input | `ScanOcrRequest.Input.Pages`, `ScanPageImage.PageIndex` · `ScanPdfMetadataReader` | `Services/TemplateScan/` |
| Quality gate before spending on AI | `ScanSuitabilityEvaluator` + `ScanSuitabilityOptions` | `Services/TemplateScan/` |
| Staged review wizard with approval gate | `TemplateScanDialog.razor` (`Upload → FieldReview → Clarification → Generating → Preview → NeedsHelp → Done`) · `TemplateScanFieldReviewView.razor` | `Visa2026.Blazor.Server/Editors/` |
| Officer per-field override model | `ScanFieldPlanOfficerOverride` | `Services/TemplateScan/` |
| Permission gate precedent | `TemplateConvertAccess.CanConvertTemplates()` · `TemplateScanAccess.CanCreateFromScan()` | `Services/TemplateConvert|TemplateScan/` |
| Side-by-side scan + fields host | `IVisaPreviewSlotService`, `VisaPreviewSlotMode`, `VisaPreviewSlotService` | Module + Blazor — skill `visa2026-preview-slot` |
| Lookup normalization playbook | `docs/VISA2014_MIGRATION/LOOKUP_RESOLUTION_STRATEGY.md` | docs |

**Namespace for new code:** `Visa2026.Module.Services.DocumentExtraction`.

---

## 2. What the MRZ actually gives (and what it does not)

Passport TD3 MRZ is two lines of 44 characters. Every value below is check-digit verified except the name field.

| Target | MRZ source | Confidence |
|--------|-----------|------------|
| `Passport.PassportNumber` | document number + check digit | High (verified) |
| `Passport.ExpirationDate` | expiry `YYMMDD` + check digit | High (verified) |
| `Passport.IssuedCountry` | issuing state alpha-3 → `Country.Code` | High (exact match) |
| `Passport.PassportType` | document code first char (`P`) → `PassportType.PdfForm_Code`, cf. `Passport.DefaultPassportTypeCode` | High |
| `Passport.PersonalNumber` (legacy, `[Browsable(false)]`) | optional personal number + check digit | Medium — prefer `Person.PersonalNumber` |
| **`Passport.IssueDate`** | **not in MRZ** | Must come from OCR (X4) or officer |
| **`Passport.Authority`** | **not in MRZ** | Must come from OCR (X4) or officer |

Surname, given names, date of birth, sex and nationality are also in the MRZ. Because `Passport` is created from the **`Person` DetailView**, the person is already known, so those five are used as **verification signals against the existing `Person`** (X-D5), not as prefill.

**MRZ codes that are not ISO alpha-3** need an alias table, or `IssuedCountry` silently fails to resolve: `D` (Germany), `GBD` / `GBN` / `GBO` / `GBP` / `GBS` (British subsets), `RKS` (Kosovo), `XXA` / `XXB` / `XXC` / `XXX` (stateless / refugee), `UNO` / `UNA` / `UNK` (UN). `Country.Code` is `MaxLength(20)`, so alpha-3 fits without schema change.

**Validation interaction:** `Passport` carries `[RuleCriteria("Passport_DateRange", …, "ExpirationDate > IssueDate")]` and `[RuleRequiredField]` on `IssueDate`, `Authority`, `PassportType`, `IssuedCountry`, `Person`. Since MRZ cannot supply `IssueDate` or `Authority`, the draft must be allowed to be incomplete and the officer must finish it before save. Extraction never bypasses XAF validation (X-D4).

---

## 2c. Layouts **will** change — versioning and drift resilience (X-D12)

Assume every form in scope gets redesigned during the system's life. This is not speculation; the corpus already shows three independent kinds of drift inside the sampled window:

| Observed drift | Evidence |
|----------------|----------|
| **MRZ line 1 restructured** | `V{TYPE2}<NAME` (2014–2018) → `{TYPE2}TKM NAME` (2024–2026) — §0a |
| **Label wording edited** | Invitation `BS (Business)` → `BS1 (Business)`, `1 AY (MONTH(s))` → `1 AY (Month)`, department line reworded — 2017 vs 2026 |
| **Numbering series replaced** | Invitation `ASGH…` → `CO…`; registration `AS0102361` → `CO0202351`; visa `AS-…` → `CO…` |

### Design rules that follow

| Rule | Why it matters |
|------|----------------|
| **No hard-coded pixel geometry.** Locate values *relative to* matched text anchors, never at absolute coordinates | A form nudged 4 mm down breaks coordinates but not anchors |
| **Anchors are fuzzy and multi-lingual.** Match on the stable part of the Turkmen/English label pair with tolerant comparison, not string equality | `1 AY (MONTH(s))` vs `1 AY (Month)` must both match |
| **Prefer the standardized substrate over the cosmetic layer.** MRZ line 2 (ICAO, check-digit protected) outranks printed labels, which outrank position | Line 2 survived the generation change untouched while line 1 did not — a general lesson, not a visa quirk |
| **Layout profiles are data, not code.** Version them and select at runtime | A new form version should be a seed/config change, not a redeploy of parsing logic. Follow the `PdfFormMapping` BO + `DatabaseUpdate/PdfFormMappingUpdater.cs` precedent already in the Module |
| **Detect the layout; never assume it.** Score every candidate profile by how many anchors matched, pick the winner, and require a minimum score | Silently applying the 2017 profile to a 2027 form is how wrong values get written confidently |
| **"Unrecognised layout" is a first-class outcome.** Below the match threshold, degrade to the generic pass and tell the officer the form looks new | X-D6 (blank beats wrong) applies at layout level too |
| **Degrade, don't fail.** With no profile match, still run MRZ, the PDF text layer, and generic regex passes (dates, `A#######`, invitation series) | A redesigned form drops accuracy and costs the officer typing; it must never block the save path |
| **Number series are a growable pattern set, not an enum.** An unknown prefix lowers confidence; it does not hard-fail | `CO…` appeared after `ASGH…`; the next one will appear too |
| **Stamp the profile into provenance.** X-D10 records the profile id + version used per field | Lets you tell which records were read with which profile when a layout turns out to be wrong |
| **Never retroactively rewrite.** Adding a profile must not re-extract or alter already-saved records | Officer-accepted data is the record of truth |

### Detecting drift early

Two signals, both nearly free because the groundwork is already planned:

1. **Benchmark by issue year.** The X2b harness has ground truth for ~18.9k documents. Bucketing accuracy by document *issue date* turns it into a drift detector: a new form version shows up as accuracy collapsing for recent years while older years hold steady. Re-run it periodically, not just once.
2. **Officer override rate.** X5 already records which fields the officer changed. A rising override rate on one field of one document kind is the production signal that its layout moved — alert on it rather than waiting for complaints.

When drift is confirmed, the developer path reuses `ScanGapPacketExporter` from Template Scan: export an anonymised gap packet (anchors found, anchors missing, profile scores) so a new profile version can be authored from evidence.

---

## 3. Contracts (Phase 0 deliverable)

```csharp
namespace Visa2026.Module.Services.DocumentExtraction;

/// <summary>One extractable field on one target BO. Registry-driven, not reflection-guessed.</summary>
public sealed class DocumentExtractionField
{
    public required string Key { get; init; }              // "Passport.ExpirationDate"
    public required string PropertyName { get; init; }
    public required DocumentExtractionValueKind ValueKind { get; init; }
    public Type? LookupEntityType { get; init; }           // Country, PassportType, ...
    public bool IsRequiredForSave { get; init; }
    public bool VerifyOnly { get; init; }                  // X-D5: compare, never write
}

public enum DocumentExtractionValueKind { Text, Date, Number, Boolean, Lookup }

public interface IDocumentExtractionTargetRegistry
{
    DocumentExtractionTarget? Find(Type businessObjectType);
}

/// <summary>What was uploaded, decided by sniffed content - never by file name (§0b).</summary>
public enum DocumentExtractionSourceKind { Pdf, Image, Tiff, Unsupported }

/// <summary>
/// Normalizes any accepted upload (PDF or raster) into pages (§0b).
/// PDF text layer is read first; pages with no usable text are rasterized for OCR.
/// </summary>
public interface IDocumentExtractionInputNormalizer
{
    DocumentExtractionPageSet Normalize(byte[] content, string fileName);
}

public sealed class DocumentExtractionPageSet
{
    public required DocumentExtractionSourceKind SourceKind { get; init; }
    public required IReadOnlyList<DocumentExtractionPage> Pages { get; init; }
    public string? RejectionReason { get; init; }           // unsupported / empty / all pages below gate
}

public sealed class DocumentExtractionPage
{
    public required int PageIndex { get; init; }
    public string? TextLayer { get; init; }                 // non-null only for digital PDF pages
    public byte[]? RasterContent { get; init; }             // populated lazily, only when OCR is needed
    public int WidthPixels { get; init; }
    public int HeightPixels { get; init; }
    public bool MeetsResolutionGate { get; init; }          // MinShortSidePixels, per page (§0a)

    /// <summary>Higher = more likely to hold the target document (MRZ pattern, expected anchors).</summary>
    public double RelevanceScore { get; init; }
}

/// <summary>Ranks pages so a multi-page PDF/TIFF is not read as "page 1 only" (§0b).</summary>
public interface IDocumentExtractionPageSelector
{
    IReadOnlyList<DocumentExtractionPage> Rank(DocumentExtractionPageSet pages, DocumentExtractionTarget target);
}

/// <summary>
/// A versioned, data-driven description of one form version (X-D12). Seeded like `PdfFormMapping`,
/// never hard-coded, so a redesigned form is a seed change rather than a parser rewrite.
/// </summary>
public sealed class DocumentLayoutProfile
{
    public required string Key { get; init; }              // "Invitation.SMS.A4"
    public required int Version { get; init; }
    public required Type TargetType { get; init; }
    public DateTime? ValidFrom { get; init; }              // document issue date window, not row dates
    public DateTime? ValidTo { get; init; }

    /// <summary>Tolerant label anchors; matching is fuzzy, never string equality.</summary>
    public required IReadOnlyList<LayoutAnchor> Anchors { get; init; }

    /// <summary>Value locators expressed *relative to* anchors - never absolute pixels.</summary>
    public required IReadOnlyList<LayoutFieldLocator> Fields { get; init; }
}

/// <summary>Scores profiles against a page and picks a winner, or reports none (X-D12).</summary>
public interface IDocumentLayoutProfileMatcher
{
    LayoutMatchResult Match(DocumentExtractionPage page, DocumentExtractionTarget target);
}

public sealed class LayoutMatchResult
{
    public DocumentLayoutProfile? Profile { get; init; }   // null => unrecognised layout
    public required double Score { get; init; }            // anchors matched / anchors expected
    public required IReadOnlyList<string> MissingAnchors { get; init; }

    /// <summary>True when no profile cleared the threshold: degrade to the generic pass and warn.</summary>
    public bool IsUnrecognisedLayout => Profile is null;
}

/// <summary>Deterministic, offline pre-stage. Runs before any provider.</summary>
public interface IMrzExtractor
{
    MrzExtractionResult TryExtract(IReadOnlyList<string> ocrLines);
}

/// <summary>Pluggable AI stage for fields no deterministic reader can supply. Default: None.</summary>
public interface IDocumentFieldExtractionProvider
{
    string Key { get; }
    bool IsEnabled { get; }

    Task<DocumentFieldExtractionProposal> ProposeFieldValuesAsync(
        DocumentFieldExtractionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Extracted text -> lookup FK, with candidates when unsure. Never auto-creates lookup rows.</summary>
public interface IExtractedValueLookupResolver
{
    ExtractedLookupResolution Resolve(ExtractedLookupQuery query);
}

/// <summary>Blocks proposing a number that already exists among active records.</summary>
public interface IDocumentExtractionDuplicateGuard
{
    DocumentExtractionDuplicateVerdict Check(IObjectSpace objectSpace, DocumentExtractionDraft draft);
}

/// <summary>In-memory only. No draft BO (X-D3).</summary>
public sealed class DocumentExtractionDraft
{
    public required Type TargetType { get; init; }
    public required IReadOnlyList<DocumentExtractionFieldProposal> Fields { get; init; }
    public required string ProviderKey { get; init; }
    public byte[]? PendingFileBytes { get; set; }
    public string? PendingFileName { get; set; }
}

public sealed class DocumentExtractionFieldProposal
{
    public required string Key { get; init; }
    public string? RawText { get; init; }                  // exactly what was read
    public object? ProposedValue { get; init; }            // parsed / resolved, may be null
    public required double Confidence { get; init; }
    public required DocumentExtractionSource Source { get; init; }  // Mrz | PdfTextLayer | Ocr | Ai
    public int? SourcePageIndex { get; init; }
    public string? LayoutProfileKey { get; init; }         // X-D12 provenance: which form version was assumed
    public int? LayoutProfileVersion { get; init; }
    public bool Accepted { get; set; }                     // officer decision
    public IReadOnlyList<ExtractedLookupCandidate>? Candidates { get; init; }
}

public interface IDocumentExtractionOrchestrator
{
    Task<DocumentExtractionDraft> ExtractAsync(
        DocumentExtractionInput input, CancellationToken cancellationToken = default);

    DocumentExtractionCommitResult Commit(
        IObjectSpace objectSpace, DocumentExtractionDraft draft);
}
```

Options class `DocumentExtractionOptions` bound to config section **`DocumentExtraction`**, registered by `AddDocumentExtraction()` in `Visa2026.Blazor.Server/Startup.cs` next to the existing `AddTemplateConvert()` / `AddTemplateScan()` calls.

```json
"DocumentExtraction": {
  "Enabled": false,
  "Provider": "None",
  "AllowCloudProviders": false,
  "MaxUploadSizeMB": 32,
  "MinAcceptConfidence": 0.75,
  "MinShortSidePixels": 800,
  "MaxPages": 12,
  "PreferPdfTextLayer": true,
  "MinLayoutMatchScore": 0.6,
  "AllowGenericFallbackOnUnknownLayout": true,
  "Ocr": { "Engine": "None", "Languages": [ "eng", "tur", "rus" ], "SuppressRedChannel": true, "RasterizeDpi": 300 },
  "AzureOpenAI": { "Endpoint": "", "Deployment": "", "ApiVersion": "2024-10-21", "ApiKey": "" }
}
```

---

## 4. Locked decisions

| # | Decision |
|---|----------|
| **X-D1** | **Local-first.** Scan bytes never leave the host in the default configuration. `Provider` defaults to `None`, `AllowCloudProviders` defaults to `false`. A cloud adapter may be added (X9) but stays off unless explicitly enabled per environment. |
| **X-D2** | **Deterministic before probabilistic.** MRZ / PDF text layer run first; the AI provider is only asked for fields still unresolved. A field filled deterministically is never overwritten by AI. |
| **X-D3** | **No draft BO.** The draft is in-memory for the lifetime of the panel, mirroring `TemplateConvertDialog` and `PendingCopyBytes`. Nothing is persisted until the officer accepts. |
| **X-D4** | **No validation bypass.** Accepted values are written to the BO and go through normal XAF validation (`RuleRequiredField`, `RuleCriteria`, `IsPassportNumberUniqueAmongActive`). Extraction cannot force a save. |
| **X-D5** | **Verification mode for case-side records.** For `Invitation`, `WorkPermit`, issued `Visa` and for `Person` fields behind a `Passport`, the scan is compared against known values and mismatches are flagged. It does not overwrite officer-entered or case-derived data. |
| **X-D6** | **Blank beats wrong.** Below `MinAcceptConfidence` a field is left empty and shown as "needs entry". No guessed value is ever pre-accepted. |
| **X-D7** | **Officer accepts per field.** Accept-all is allowed only after the review panel has been shown; there is no silent auto-apply. Commit gate mirrors `CanApprove` in `TemplateConvertDialog`. |
| **X-D8** | **Duplicate guard before commit.** If a proposed `PassportNumber` / `VisaNumber` matches an active record, the panel offers to open the existing record instead of creating a duplicate that validation would reject anyway. |
| **X-D9** | **Lookups are resolved, never created.** An unresolved lookup surfaces as a gap with candidates; growing `Country` / `EducationInstitution` / `Specialty` stays a catalog task (`docs/LOOKUP_SEEDING.md`). |
| **X-D10** | **Provenance is retained.** Every accepted field records source (`Mrz` / `PdfTextLayer` / `Ocr` / `Ai`), confidence, page index, and whether the officer changed it — so a wrong value can be traced and accuracy tuned (X5). |
| **X-D11** | **PDF and images are both first-class inputs** (§0b). The accepted set is whatever `DocumentFileUploadConstraints.AllowedExtensions` already allows (`.pdf` + 6 raster formats); the extraction layer does **not** declare its own allow-list. Source kind comes from sniffed content, never the file name. A PDF's **text layer is tried before OCR**, and every input is normalized to a **page set** so multi-page PDFs and multi-frame TIFFs cannot be silently truncated to page 1. |
| **X-D12** | **Layout change is assumed, not exceptional** (§2c). Form versions live in **versioned, data-driven layout profiles** (the `PdfFormMapping` precedent), values are located **relative to fuzzy text anchors — never absolute pixels**, the profile is **detected by score and never assumed**, and an unrecognised layout **degrades to MRZ + text layer + generic regex and says so** instead of guessing. Number series are a growable pattern set, and the profile key + version is recorded in provenance. Adding a profile never re-extracts or rewrites saved records. |
| **X-D13** | **A logical document can be several files.** WorkPermit letters average **2.53 pages and are never a single page**, and 48% of `Education` records have 2+ (§0a) — in the legacy corpus that is *sibling scan rows*, not multi-page files. The page set (§0b) is therefore built across **all attachments of the record**, and the officer may upload several files as one document. Page indices stay stable per file, so provenance still points at a specific file and page. |

---

## 5. Slices

| Slice | Content | Exit criteria |
|-------|---------|---------------|
| **X0** | This doc; decisions locked | Developer sign-off |
| **X1** | `DocumentExtraction` namespace: contracts from §3, `DocumentExtractionOptions`, `AddDocumentExtraction()`, `None` provider, target registry entry for `Passport`. No UI | Unit tests over registry + options; solution builds |
| **X2** | `MrzExtractor`: TD3 parse, all check digits, `YYMMDD` century window, alias table for non-ISO codes; `ExtractedValueLookupResolver` for `Country` / `PassportType`; `DocumentExtractionDuplicateGuard` | Unit tests with synthetic MRZ incl. bad check digits, `D`/`GBD`/`XXA`, expiry century rollover |
| **X2b** | **Benchmark harness** over the legacy corpus (§0a): pull scan + ground-truth columns from `VISA2015` read-only, run the extractor, report per-field precision / recall / blank rate by resolution bucket **and by document issue year** (§2c drift detector). Read-only, dev-only, PII stays outside the repo | Repeatable accuracy number per field **and per issue year**; baseline committed to this doc (not the images); harness is re-runnable, not one-shot |
| **X3** | Review panel in `#visa-preview-slot` (scan page beside field list), entry from `Person` DetailView → `Passport`; accept → commit `Passport` + `PassportDocument` + `FileData` in one transaction | Officer creates a passport from a **raster** scan (no text layer exists in practice — §0a); deterministic fields prefilled, remainder typed, file attached |
| **X4** | **OCR decision gate** + raster path: rasterization, deskew/rotation detection, **red-channel suppression** for stamp overlap, suitability reject below `MinShortSidePixels`, local OCR engine for the printed visual zone | Engine chosen and licensed; **X2b numbers** recorded in this doc; ~10% low-resolution population rejected cleanly rather than guessed |
| **X5** | `DocumentExtractionAudit` BO (provider, per-field source/confidence/override, **layout profile key + version**) + first accuracy review + **override-rate view per field** as the production drift signal (§2c) | Audit rows visible to admin; baseline numbers recorded; a rising override rate on one field is visible without a query |
| **X5b** | **Layout profile engine** (§2c / X-D12): seeded `DocumentLayoutProfile` data + `IDocumentLayoutProfileMatcher` scoring, fuzzy multi-lingual anchor matching, anchor-relative locators, generic fallback pass, "unrecognised layout" state in the review panel, anonymised gap-packet export via the `ScanGapPacketExporter` precedent | The 2017 and 2026 invitation versions each match their own profile; an unknown form scores below `MinLayoutMatchScore` and reports "this looks like a new form version" instead of proposing values; adding a profile needs no parser code change |
| **X6** | `Visa` stickers: custom **MRV-B parser** covering **both line-1 generations** (§0a) as two profile versions, anchor-based read of the printed zone (`IssueDate`, `StartDate`, place of issue, entries, invitation no., passport no.), lookup resolution for `VisaType` / `VisaCategory` / `VisaIssuedPlace` | Benchmarked against the labelled corpus; both MRZ generations parse; hyphen normalization on invitation numbers; unknown series lowers confidence instead of failing |
| **X7** | Verification mode for `Invitation` / `WorkPermit` / issued `Visa` inside the existing compose slot panels, over a page set spanning **all attachments of the record** (X-D13). `Invitation` is the strong case — all four header fields plus visa type / entries / period / border zone are printed on the page. For `WorkPermit`, **number and date are handwritten (§0a) and stay officer-entered**; extraction covers only the printed remainder | Mismatch between scan and case values is flagged; nothing is overwritten; multi-file work-permit sets are read as one document; no handwriting is guessed |
| **X8** | `Education` diploma: `EducationLevel`, `EducationInstitution`, `EducationCountry`, `Specialty`, `GraduationYear` + lookup-gap workflow | Gaps reported with candidates rather than wrong FKs |
| **X9** | Optional cloud provider adapter (Azure OpenAI vision or Document Intelligence), off by default, redaction via `MaskPreview` where applicable | Toggling `Provider` changes behaviour with no code change; disabled in any environment without sign-off |

---

## 6. Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| OCR quality on the low-resolution tail and on phone photos (rotation, curvature, glare) | **High** | ~10% of the corpus is below 800 px short side (§0a) — reject via `MinShortSidePixels` instead of guessing. MRZ check digits sidestep the rest. X4 is a measured gate (X2b), not an assumption |
| Red arrival stamp overlapping the MRZ / data fields | **High** | Common in the corpus. Red ink over black text ⇒ red-channel suppression in X4 preprocessing |
| **A form is redesigned after go-live** (visa sticker, invitation, work permit) | **High** | Already observed three times in the sampled window (§2c). X-D12: versioned data-driven profiles, anchor-relative locators, score-based detection, generic fallback, and no silent guessing. Detection via X2b accuracy-by-issue-year and X5 override rate |
| Turkmen visa MRZ line 1 is non-ISO with two generations | Medium | Custom parser in X6; stock ICAO libraries mis-parse it. Line 2 is standard, so the check-digit fields stay reliable — prefer the standardized substrate (§2c) |
| **Handwritten fields on the work-permit letter** (`№ 769`, `« 01 » 04 201 4 ý.`) | **High** for those two fields | Printed-text OCR will not read them and handwriting recognition is a much weaker problem. X7 leaves work-permit number and date officer-entered rather than proposing a guess |
| Number series replaced (`ASGH…` → `CO…`) breaking recognizers | Medium | X-D12: series are a growable pattern set; an unknown prefix lowers confidence and never hard-fails |
| A logical document spans several files, so reading "the" file misses data | Medium | X-D13: the page set spans all attachments of the record. Work-permit letters are **never** single-page and 48% of `Education` records have 2+ (§0a) |
| Cyrillic / mixed-script text | Medium | Visa stickers are Turkmen **Latin** + English, so this mostly affects older diplomas and Russian-era documents in X8, not X3/X6 |
| On-prem host may have no outbound internet | High | X-D1 makes local the default; cloud is additive only. Confirm before planning any cloud slice |
| Local OCR engine deployment weight (native binaries + language data on Windows Server **and** Linux Docker) | Medium | Evaluate in X4 before committing; Spire.OCR is a separate paid SKU from the already-licensed Spire.PDF — check licensing first |
| Officers over-trusting proposed values | Medium | X-D6 (blank beats wrong), X-D7 (per-field accept), X-D10 (provenance) |
| `Education` institution / specialty lookup gaps | Medium | X-D9 — report gaps, grow catalogs deliberately |
| Duplicate passports created from re-scanned documents | Low | X-D8 duplicate guard offers the existing record |
| Cost / latency per page if cloud vision is enabled | Low while X-D1 holds | Suitability gate pattern from `ScanSuitabilityEvaluator` before any paid call |

---

## 7. Deliberately out of scope

- Creating or matching `Person` records from a scan — `Passport` and `Education` are reached from an existing `Person` DetailView, so the person is known (X-D5).
- Auto-creating lookup rows (X-D9).
- **Reading the annexed roster tables** on work-permit (`SANAWY`) and invitation letters into `WorkPermitItem` / `InvitationItem` child rows. Table structure recognition is a separate problem from field extraction, and on the case side the roster comes from the application profile instead — revisit only after X7 measures well.
- **Re-extracting or migrating already-saved records** when a new layout profile is added (X-D12).
- Replacing `PdfFormMapping`. That pipeline fills PDFs *from* known values and is the opposite direction; see `.cursor/skills/visa2026-pdf-form-mapping/`.
- Template authoring. `TemplateScan` ("Create from yellow marks") and `TemplateConvert` produce merge **templates**; this feature produces **business object field values**.
