# Engineering spec (Phase 0) — AI convert existing Word/Excel → profile templates

> **Status:** **§8 decisions locked 2026-08-20** · **E1 + E2 + E3 implemented 2026-08-20**, **E5 + E6 implemented 2026-08-21**, **E7b implemented 2026-08-21, case and wizard entries** · **E8 implemented 2026-08-21** (`None` provider + sanitizer) · **E9 implemented 2026-08-21** (Preview chat + L8 intent gate) · **E10 implemented 2026-08-21** (Azure OpenAI HTTP adapter, 171 tests) · E4 still contracts · slices tracked in [`IMPLEMENTATION_PLAN.md`](../.cursor/skills/visa2026-application-profile/IMPLEMENTATION_PLAN.md)
> **Purpose:** Make [`TEMPLATE_AI_CONVERT_PRODUCT_SPEC.md`](TEMPLATE_AI_CONVERT_PRODUCT_SPEC.md) buildable. The product spec locks *what* the officer sees (L1–L12); this doc defines the *services that do not exist yet* and the decisions a developer would otherwise have to invent.
> **Audience:** Developer / AI agent implementing the feature. Not officer-facing.
> **Skill:** [`visa2026-application-profile`](../.cursor/skills/visa2026-application-profile/SKILL.md) (profile + template config) · [`visa2026-user-report-templates`](../.cursor/skills/visa2026-user-report-templates/SKILL.md) (placeholder maps) · [`visa2026-resminamalar`](../.cursor/skills/visa2026-resminamalar/SKILL.md) (merge/preview)
> **Rule:** all logic in **`Visa2026.Module`**; Blazor host only for the modal, highlights, and chat panel.

---

## 0. Why this doc exists

The product spec is decision-complete but rests on three services that do not exist:

| Product rule | Depends on | Today |
|--------------|-----------|-------|
| **L10** — AI may use only the target profile's placeholders | A per-profile, per-data-scope placeholder set | `IUserReportPlaceholderCatalogService` filters by `RootBoType` / `Scope` / text search only — **no profile filter** |
| **L7** — candidate check matches document literals to instance values | A standalone instance value map | `UserReportGenerator.BuildDataDictionary` is **private** and needs a persisted `UserReportTemplate` |
| **L8 / Q10** — substitute only approved spans, fail on any other delta | An in-place token writer + diff gate | Only `WordUserReportImageInjector` (post-merge **image** injection) exists |

Plus: no AI provider abstraction anywhere in the repo, no draft persistence, and Extract/Validate currently require a saved template id.

**Phase 0 deliverable:** the six contracts in §2–§7 and the closed decisions in §8. Nothing in Phase 0 requires an AI vendor.

---

## 1. Reuse map (do not rebuild these)

| Need | Existing type | Location |
|------|---------------|----------|
| Profile template row (scope, data scope, bytes, contract binding) | `ApplicationProfileTemplate`, `ApplicationProfileTemplateCatalogScope`, `ApplicationProfileTemplateDataScope`, `ApplicationProfileTemplateKind` | `Visa2026.Module/BusinessObjects/ApplicationProfile.cs` |
| Create/link a master template from uploaded bytes | `ApplicationProfileTemplateUserReportBridge.EnsureLinkedUserReportTemplate`, `.WriteMasterFile`, `.EnsureMasterHasFile` | `Visa2026.Module` |
| Token extraction from a stream | `UserReportPlaceholderExtractor.ExtractPlaceholdersAsync(Stream)` · `ExcelTemplatePlaceholderExtractor.ExtractPlaceholdersAsync(Stream)` | `Services/UserReports`, `Services/ExcelReports` |
| Token validation | `UserReportValidationService` · `ExcelReportValidationService` → `PlaceholderValidationResult` | same |
| Global placeholder catalog | `IUserReportPlaceholderCatalogService.GetEntries(UserReportPlaceholderManualQuery?)`, `UserReportPlaceholderCatalogEntry` | `Services/UserReports` |
| Merge value helpers | `UserReportMergeDataHelper.BuildApplicationHeaderDictionary`, `.GetPropertyValue`, `.GetActiveApplicationItems`, row builders · `ApplicationRosterHelper.GetMergeLineItems` | `Services/UserReports` |
| Office → PDF for preview | `ApplicationWordReportOfficePreviewPdfConverter.TryConvertToPdf(byte[], string)` · `OfficeFilePreviewResultFactory.FromOfficeOrPdf` | `Visa2026.Module` |
| Preview slot | `IVisaPreviewSlotService.OpenFileAsync` · `ApplicationProfileTemplateFilePreviewSource` (`"application-profile-template"`) | Module + Blazor |
| Upload size / staging pattern | `TemplateEditStagingOptions.MaxFileSizeBytes` (50 MB) · `UserReportTemplateStagingController` · wizard `InputFile` (20 MB) | Module + Blazor |
| Config lock | `ApplicationProfileLockHelper.IsProfileConfigLocked`, `.AllowsNestedEditWhenConfigLocked` (returns **false** for `ApplicationProfileTemplate`) | `ApplicationProfile.cs` |
| In-place OOXML edit precedent | `WordUserReportImageInjector.Inject(Stream, Stream, …)` | `Services/UserReports` |
| Libraries | `DocumentFormat.OpenXml` 3.3.0 · `ClosedXML` 0.104.2 · `DocxTemplater` 2.4.4 · `DevExpress.Document.Processor` 25.2.6 | `Visa2026.Module.csproj` |

**Namespace for new code:** `Visa2026.Module.Services.TemplateConvert`.

---

## 2. E1 — Profile-scoped placeholder set (unblocks L10, Q1, Q13)

**Shipped** in `Visa2026.Module/Services/TemplateConvert/`. Signatures below are the implemented ones.

```csharp
public interface IApplicationProfilePlaceholderSetService
{
    ApplicationProfilePlaceholderSet GetSet(ApplicationProfilePlaceholderSetQuery query);
}

public sealed class ApplicationProfilePlaceholderSetQuery
{
    public required ApplicationProfile Profile { get; init; }
    public required ApplicationProfileTemplateDataScope DataScope { get; init; }
    public ApplicationProfileTemplateKind TemplateKind { get; init; } = ApplicationProfileTemplateKind.Word;
}

public sealed class ApplicationProfilePlaceholderSet
{
    public required Guid ApplicationProfileId { get; init; }
    public required IReadOnlyList<UserReportPlaceholderCatalogEntry> Allowed { get; init; }
    public required IReadOnlyList<PlaceholderExclusion> Excluded { get; init; }

    /// <summary>Stable hash of the allowed short codes. Audit + provider cache key.</summary>
    public required string Fingerprint { get; init; }

    public bool Contains(string token);
}

public sealed record PlaceholderExclusion(string ShortCode, PlaceholderExclusionReason Reason);

public enum PlaceholderExclusionReason
{
    OutOfDataScope,
    PersonPackDisabled,
    StructuralUnsupportedForKind,
    UnknownPack,
}
```

**Deviations from the proposal, and why:**

| Proposed | Shipped | Reason |
|----------|---------|--------|
| `Guid ApplicationProfileId` | `ApplicationProfile Profile` | Every caller already holds the profile; taking the BO keeps the service free of an `IObjectSpace` dependency and unit-testable against the real catalog |
| `WrongRootBoType` reason | dropped | Not used — see the `rootBoTypes` defect below. `Scope` already separates header from row |
| — | `UnknownPack` added | An unrecognised `packKey` must exclude the token, never default it into every profile |

**Derivation (in order):**

| Step | Rule |
|------|------|
| 1 | Start from `IUserReportPlaceholderCatalogService.GetEntries()` (unfiltered) |
| 2 | Map data scope → `UserReportPlaceholderScope`: `ApplicationHeader` → `Header`, `PeopleM2M` → `Row`, `Both` → both. Entries scoped `Both` always pass |
| 3 | Drop structural markers unsupported for `TemplateKind`: `IsImage` in Excel, and **everything** for `PdfForm` (convert is Word/Excel only) → `StructuralUnsupportedForKind` |
| 4 | Drop `Unknown` packs → `UnknownPack` |
| 5 | Drop entries whose pack is disabled by the profile's `RequirePerson*` toggles → `PersonPackDisabled` |
| 6 | `Fingerprint` = SHA-256 hex of allowed short codes joined by `\n`, ordinal-sorted |

**The prefix warning was justified.** `PackKey` now exists on every one of the 66 catalog entries, assigned by tracing each `ApplicationRosterMergeLine` property to the navigation it actually reads. Two entries prove prefix matching would have been wrong:

| Token | Prefix suggests | Actually reads | Pack |
|-------|-----------------|----------------|------|
| `CSDT` / `CEDT` (`Contract_StartDateText`, `Contract_ExpirationDateText`) | contract / salary | `CurrentVisa.ExpirationDate` (+ `VisaPeriod.PdfForm_Count`) | `PersonVisa` |
| `PPIN` (`Passport_PersonalNumber`) | passport | `Person.PersonalNumber ?? CurrentPassport.PersonalNumber` | `Core` — resolves with no passport record |

Both are locked by regression tests, since either mistake is invisible at a glance.

**Exclusions are returned, not swallowed** — the officer-facing "gap" explanation and the developer gap packet both need the reason.

> **Pre-existing data defect found (not fixed here):** `rootBoTypes` in `UserReportPlaceholderCatalog.json` uses `"Application"`, which is **not** a member of `UserReportBoType` (`ApplicationProfileInstance`, `ApplicationItem`, `Person`). `Enum.TryParse` drops it, so `["Application"]` silently falls back to *both* types while `["Application","ApplicationItem"]` resolves to `ApplicationItem` **only**. Filtering E1 by `RootBoType` would therefore have dropped header tokens for no visible reason. E1 does not filter on it. Correcting the 66 entries would change what the existing manual placeholder browser lists, so it needs its own decision.

---

## 3. E2 — Instance value map (unblocks L7 matching, P4)

**Shipped** in `Visa2026.Module/Services/TemplateConvert/`. Signatures below are the implemented ones.

```csharp
public interface IApplicationProfileInstanceValueMapService
{
    ApplicationProfileInstanceValueMap Build(ApplicationProfileInstanceValueMapRequest request);
}

public sealed class ApplicationProfileInstanceValueMapRequest
{
    public required ApplicationProfileInstance Instance { get; init; }
    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }   // from E1
    public ApplicationProfileTemplateDataScope DataScope { get; init; } = Both;
    public IReadOnlyList<ApplicationRosterMergeLine>? Rows { get; init; }            // null → resolve from the instance
}

public sealed class ApplicationProfileInstanceValueMap
{
    public required Guid ApplicationProfileInstanceId { get; init; }
    public required IReadOnlyDictionary<string, string?> Header { get; init; }
    public required IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows { get; init; }
    public required IReadOnlyList<ValueCandidate> Candidates { get; init; }
    public required IReadOnlyList<RejectedValue> Rejected { get; init; }
}

public sealed record ValueCandidate(
    string ShortCode,
    string Token,
    string RawValue,
    string NormalizedValue,
    ValueKind Kind,
    int? RowIndex,
    IReadOnlyList<string> MatchKeys);

public sealed record RejectedValue(
    string ShortCode, string RawValue, ValueKind Kind, int? RowIndex, ValueRejectionReason Reason);

public enum ValueKind { Text, Date, Number, Identifier, PersonName }
public enum ValueRejectionReason { TooShort, SmallNumber, Ambiguous }
```

**Deviations from the proposal, and why:**

| Proposed | Shipped | Reason |
|----------|---------|--------|
| `Task BuildAsync(Guid …)` | sync `Build(request)` taking the BO | The helpers it wraps are synchronous; taking the instance and an injectable `Rows` list makes the whole map unit-testable without a database |
| — | `PlaceholderSet` required | Ties the map to E1, so a token the profile disallows can never reach the matcher |
| single `NormalizedValue` | `NormalizedValue` **plus** `MatchKeys` | One normalized form cannot express "match any of five date renderings or either name order" |
| — | `ShortCode` alongside `Token` | Ambiguity detection and gap reporting both group by short code |
| — | `Rejected` list | §3 asks for rejections to be recorded; it needs a home in the result |

**Implementation:** wrap the existing statics — `UserReportMergeDataHelper.BuildApplicationHeaderDictionary`, `.GetActiveApplicationItems` / `ApplicationRosterHelper.GetMergeLineItems`, and the row builders. **Must not** require a `UserReportTemplate`; the whole point is that no template exists yet.

**Normalization is the hard part.** Matching Turkmen and Turkish documents against database values fails without it:

| Kind | Normalization for comparison |
|------|------------------------------|
| All | Trim, collapse internal whitespace, NFC, casefold with **invariant** culture (never `tr-TR` — dotted/dotless `i` will corrupt comparisons) |
| Text / PersonName | Fold Turkmen and Turkish diacritics (`ç ş ň ý ü ö ä ğ ı İ`) to an ASCII form for a secondary match key; keep the raw value for display. Try both orders for names (`Amanov Dowletmyrat` and `Dowletmyrat Amanov`) |
| Date | Parse and re-render in every candidate format: `dd.MM.yyyy`, `d.M.yyyy`, `dd/MM/yyyy`, `yyyy-MM-dd`, `dd-MM-yyyy`, plus a Turkmen long form (`20 awgust 2026`). Match on the parsed date, not the string. **The month-name table is not sourced from a repo lookup — confirm it against real ministry documents before relying on long-form matching** |
| Number | Strip thousands separators; both `,` and `.` decimal marks |
| Identifier | Strip spaces and hyphens (passport `T 12345678` = `T12345678`) |

**Minimum literal length.** Reject candidates whose normalized value is shorter than 3 characters or is a bare small integer — otherwise `"1"` or `"Mary"` (a Turkmen city **and** a name) produces false highlights. Record rejected-as-ambiguous separately so the suitability score is honest.

**As implemented:** `TooShort` below 3 normalized characters; `SmallNumber` for a `Number` with ≤ 2 digits (checked *before* length, so `"12"` reports the informative reason); `Ambiguous` when one match key resolves to more than one short code, in which case **every** colliding candidate is dropped. Missing data is absent from the map rather than rejected, which includes unset dates — `DateTime.MinValue` renders as `01.01.0001` through computed text properties like `ApplicationDateText`, and without that guard it becomes a candidate that highlights a date no document contains.

Two behaviours found while testing, both now locked:

- **Composed tokens can collapse onto their source.** `Person_ForeignAddressWithCountry` prefixes a country code, so with no country set it returns exactly `Person_ForeignAddress`. Both become unattributable and both drop out — correct, and a reminder that the officer-facing gap list will legitimately contain tokens that exist in the catalog.
- **`1,500` is 1500 or 1.5 depending on convention.** The matcher emits both readings plus a separator-stripped form as keys rather than choosing one, so a document using either convention still matches.

Images never enter the value map: there is no literal text to reverse-match.

---

## 4. E3 — Local token writer + diff gate (unblocks L8, Q10, Q4)

### 4.1 Writer

**Shipped** in `Visa2026.Module/Services/TemplateConvert/`. Signatures below are the implemented ones; they differ from the original proposal where noted.

```csharp
public interface ITemplateTokenWriter
{
    TokenWriteResult Apply(TemplateTokenWriteRequest request);
}

public sealed class TemplateTokenWriteRequest
{
    public required byte[] SourceContent { get; init; }
    public required TemplateSourceFormat Format { get; init; }          // Docx | Xlsx
    public IReadOnlyList<TokenSubstitution> Substitutions { get; init; } = Array.Empty<TokenSubstitution>();
    public IReadOnlyList<LoopMarker> Loops { get; init; } = Array.Empty<LoopMarker>();
}

public sealed record TokenSubstitution(DocumentRegion Region, string Token);
public sealed record LoopMarker(DocumentRegion Start, DocumentRegion End, string CollectionToken);

public abstract record DocumentRegion
{
    public sealed record WordSpan(string ParagraphAddress, int Start, int Length) : DocumentRegion;
    public sealed record ExcelCell(string SheetName, string CellReference) : DocumentRegion;
}

public enum WordPart { Body, Header, Footer }

public sealed record TemplateWriteSkip(DocumentRegion Region, string Token, string Reason);

public sealed record TokenWriteResult(
    byte[] Content,
    IReadOnlyList<TokenSubstitution> AppliedSubstitutions,
    IReadOnlyList<LoopMarker> AppliedLoops,
    IReadOnlyList<TemplateWriteSkip> Skipped);
```

**Deviations from the proposal, and why:**

| Proposed | Shipped | Reason |
|----------|---------|--------|
| `ApplyAsync` | `Apply` (sync) | Pure in-memory OOXML work with no I/O; an async wrapper would only add a state machine |
| `WordSpan(..., WordPart Part)` | `WordSpan(string ParagraphAddress, ...)` | The address already encodes the part (`body/12`, `header0/3`), so carrying `Part` invited the two disagreeing |
| `WordPart` incl. `TableCell`, `TextBox` | `Body`, `Header`, `Footer` | Table-cell and text-box paragraphs live *inside* one of those three parts; they are addressed there. `WordParagraphAddress.IsInTable` exposes the table case |
| `paragraphId` | ordinal address via `WordTemplateAddressing` | `w14:paraId` is optional and absent from many real ministry documents |
| `TokenWriteResult(..., int Applied, IReadOnlyList<string> Skipped)` | applied substitutions and loops returned in full, `Skipped` carries region + reason | The diff gate must be given what was *applied*; feeding it the requested set flags every skipped edit as a violation |

**Addressing:** `WordTemplateAddressing.EnumerateParagraphs` is the single source of paragraph addresses, shared by the writer, the diff gate, the residual scanner, and (later) the E5 candidate analyser. Offsets in a `WordSpan` are over `GetParagraphText`, the concatenated `w:t` text of that paragraph.

**Word rules (OpenXml):**

| Rule | Detail |
|------|--------|
| Offsets are paragraph-relative over concatenated run text | A visible phrase is routinely split across several `<w:r>`; resolve the span to a run range first |
| Insert, never split or merge | The token text is written **into the first `w:t` the span touches** and the remainder of the span is deleted from the later text nodes. The token inherits that run's `w:rPr` and the run count is unchanged, so no run is created, split, or merged. This removed most of the anticipated run-splitting complexity |
| Preserve `xml:space="preserve"` | Required or leading/trailing spaces vanish |
| Never touch | `styles.xml`, `numbering.xml`, `theme/`, `sectPr`, `rsid*` attributes, image parts, content controls |
| Headers/footers | Reachable but **default to not substituting** — letterhead is static content (L7 "unmatched literals / static") |

**Excel rules (ClosedXML):** set the cell **value** only. Do not touch number formats, styles, column widths, merged ranges, conditional formatting, defined names, or formulas. A cell holding a formula is never a substitution target, and a **non-anchor member of a merged range** is skipped — the range keeps its content in the anchor cell, so writing elsewhere is silently lost.

**Loop syntax** is whatever the existing generators already consume: `{{#ds.rows}}` / `{{/ds.rows}}` (see `ExcelReportGenerator.FindRowContainingToken`). `TemplateTokenSyntax` owns the wrapping so the writer and the gate cannot disagree.

### 4.2 Diff gate

```csharp
public interface ITemplateConversionDiffGate
{
    DiffGateResult Verify(TemplateDiffGateRequest request);
}

public sealed class TemplateDiffGateRequest
{
    public required byte[] OriginalContent { get; init; }
    public required byte[] ConvertedContent { get; init; }
    public required TemplateSourceFormat Format { get; init; }
    public IReadOnlyList<TokenSubstitution> Substitutions { get; init; } = Array.Empty<TokenSubstitution>();
    public IReadOnlyList<LoopMarker> Loops { get; init; } = Array.Empty<LoopMarker>();
}

public sealed record DiffGateResult(bool Passed, IReadOnlyList<string> Violations);
```

Positional parameters became a request object because the gate also needs the applied **loops**: loop markers change paragraph text, and a gate that did not know about them would fail every roster conversion.

Runs **after** the writer, on every convert, including the no-AI path. Any violation fails the convert (Q10). Callers pass `TokenWriteResult.AppliedSubstitutions` and `.AppliedLoops`, never the requested set.

The gate compares **structural and formatting invariants, not raw bytes** — the OpenXml SDK and ClosedXML both legitimately renormalise the parts they rewrite, so a byte comparison would fail every conversion.

| Must be identical | Word | Excel |
|-------------------|------|-------|
| Structure | Part list, paragraph count, table/row/cell counts, section count | Sheet count + names, used range, merged ranges |
| Formatting | `styles.xml`, `numbering.xml`, theme, `sectPr`, per-run `rPr` outside substituted spans | Cell styles, number formats, column widths, conditional formatting |
| Media | Image part count + SHA-256 per part | Same |
| Formulas | — | Every formula string |
| Text | Equal after masking each expected region with its token | Equal per cell after masking |

### 4.3 Residual value scan (Q4)

Before commit, assert that no filled-sample value survives anywhere in the committed bytes. A leftover passport number in a saved master is a data-protection defect, not a cosmetic one.

```csharp
public interface ITemplateResidualValueScanner
{
    ResidualValueScanResult Scan(byte[] content, TemplateSourceFormat format, IReadOnlyList<ResidualValueProbe> probes);
}

public sealed record ResidualValueProbe(string Value, string Label, ResidualProbeKind Kind = ResidualProbeKind.Text);
public enum ResidualProbeKind { Text, Identifier }
public sealed record ResidualValueHit(string Label, string Value, string LocationHint);
public sealed record ResidualValueScanResult(bool IsClean, IReadOnlyList<ResidualValueHit> Hits);
```

The scanner takes **probes** rather than depending on E2's `ValueCandidate`, so the two slices stay independent: E2 will project its `Identifier` and `PersonName` candidates into probes. `LocationHint` is the paragraph address or `Sheet!A1`, so a failure names the cell to fix.

**`TemplateTextNormalizer` landed here, not in E2**, because the scanner cannot match anything without it. E2 must consume it rather than write a second normaliser. It provides `Normalize` (trim, collapse whitespace, invariant lowercase), `NormalizeFolded` (plus Turkmen and Turkish diacritic folding), `NormalizeIdentifier` (folded, separators stripped, so `T-1234567` matches `T 1234567`), and `MinimumMatchLength = 3` to keep short literals from producing noise. Casing always folds **invariant**: `tr-TR` rules map `I`/`ı` inconsistently and corrupt exactly the comparisons this feature depends on.

---

## 4.4 E5 — Candidate check (L7, Q8, Q9)

**Shipped** in `Visa2026.Module/Services/TemplateConvert/`. The product spec locked the behaviour (L7) and E-D6 locked the thresholds, but no contract was written before coding; this is the shipped one.

```csharp
public interface ITemplateCandidateAnalyzer
{
    TemplateCandidateReport Analyze(TemplateCandidateRequest request);   // Content, Format, ValueMap (E2)
}

public sealed class TemplateCandidateReport
{
    public required SuitabilityLevel Level { get; init; }                // Fail | Warn | Pass
    public required IReadOnlyList<SuitabilityReason> Reasons { get; init; }
    public required IReadOnlyList<HighlightRegion> Highlights { get; init; }
    public required int DistinctHeaderMatches { get; init; }
    public required int DistinctRowMatches { get; init; }
    public required int GapCount { get; init; }
    public required bool RosterLoopDetected { get; init; }

    public bool CanConvert => Level != SuitabilityLevel.Fail;                       // Q9
    public bool RequiresWarningAcknowledgement => Level == SuitabilityLevel.Warn;   // Q9
}

public sealed record HighlightRegion(
    DocumentRegion Region, HighlightKind Kind, string MatchedText,
    string? Token, string? ShortCode, int? RowIndex);                    // Kind = Match | Gap
```

`HighlightRegion.Region` is the **same `DocumentRegion`** the E3 writer consumes, so a Match converts straight into a `TokenSubstitution` with no second addressing pass. Highlights only ever carry a token from the E2 value map, which is built from the E1 allowed set — that is how Q8 holds by construction. Gaps carry no token and are never written.

**Offset mapping is the load-bearing piece.** Matching must run on normalized text (folded diacritics, collapsed whitespace, invariant lowercase) while the writer needs offsets into the *original* text, and normalizing changes lengths. `TemplateTextIndex` keeps the source range of every normalized character, so a hit maps back exactly. Without it, any paragraph with a double space or a `ý` would highlight the wrong span — and the diff gate would then reject the conversion for touching text nobody approved.

| Rule | Behaviour |
|------|-----------|
| Overlaps | Longest match wins, so `PFN` "Dowletmyrat Amanov" beats `PLN` "Amanov" nested inside it |
| Excel | A cell is replaced whole, so a cell carries at most one token; the region is the cell, not a character span |
| Search | Each match key is searched in both the folded and separator-stripped views rather than tracking which normalization produced it |
| Roster loop | Row matches spanning **2+ distinct `RowIndex`** values. One roster row is indistinguishable from a one-off mention, so a single-person instance never detects a loop |
| Gaps | Only unmatched **date-like** and **6+ digit** literals. Anything looser marks ordinary prose as missing data |
| Already tokenized | Demotes `Pass` to `Warn` (product spec L7 "optional warn"); never rescues a `Fail` |
| Unreadable upload | `Fail` with the parser message, never an exception — this is an officer-supplied file boundary |

**Thresholds** live in `TemplateSuitabilityOptions` bound to `TemplateAiConvert:Suitability` (E-D6: proceed at 3, pass at 6, pass with roster loop at 2). Defaults apply when the section is absent.

---

## 5. E4 — Conversion draft persistence

The product spec says "persist draft (not catalog-live)" and "keep draft in session/temp" without naming storage. Session state will not survive a Blazor circuit reconnect mid-convert, so persist it.

```csharp
public class TemplateConversionDraft : BaseObject
{
    // Context
    public virtual Guid ApplicationProfileId { get; set; }
    public virtual Guid ApplicationProfileInstanceId { get; set; }

    // Intent (from Upload step)
    public virtual string TemplateName { get; set; }
    public virtual ApplicationProfileTemplateKind TemplateKind { get; set; }
    public virtual ApplicationProfileTemplateDataScope DataScope { get; set; }
    public virtual ApplicationProfileTemplateCatalogScope IntendedCatalogScope { get; set; }

    // Bytes
    public virtual string SourceFileName { get; set; }
    public virtual string SourceSha256 { get; set; }
    public virtual byte[]? SourceContent { get; set; }
    public virtual byte[]? DraftContent { get; set; }

    // State (JSON columns — these are transient working data, not queried domain data)
    public virtual string? MappingPlanJson { get; set; }
    public virtual string? SuitabilityJson { get; set; }
    public virtual string? ValidationJson { get; set; }
    public virtual string? ChatTranscriptJson { get; set; }

    // Audit
    public virtual string PlaceholderSetFingerprint { get; set; }
    public virtual string? ProviderKey { get; set; }
    public virtual TemplateConversionDraftState State { get; set; }
    public virtual string CreatedByUserName { get; set; }
    public virtual DateTime CreatedOnUtc { get; set; }
    public virtual DateTime ExpiresOnUtc { get; set; }
}

public enum TemplateConversionDraftState
{
    Uploaded, Checked, Converted, Committed, Abandoned, Failed,
}
```

| Concern | Rule |
|---------|------|
| Retention | `ExpiresOnUtc = CreatedOnUtc + 24h`. Clear `SourceContent` / `DraftContent` on `Committed`, `Abandoned`, and on expiry sweep |
| Cleanup | Startup + periodic sweep; follow the `WordReportGenerationBatch` worker pattern |
| Commit | `EnsureLinkedUserReportTemplate` → `WriteMasterFile(DraftContent)` → nested `ApplicationProfileTemplate` row with `CatalogScope` / `DataScope` / `TemplateName`, then `State = Committed` |
| Permissions | Same gate as template add/edit on that profile; `Global` (Shared) commit needs the elevated Shared gate (L4, Q6) — register in `Updater.cs` |
| Config lock | Check `ApplicationProfileLockHelper.IsProfileConfigLocked` before enabling Approve. The ObjectSpace hook already throws, but the UI must disable rather than surface an exception |

**Gap packet (§6.3 of the product spec):** no new BO. Export from the draft (`MappingPlanJson` gaps + `ValidationJson` + instance id + fingerprint) as JSON or Markdown. Revisit only if developers ask for a queryable inbox.

---

## 6. E8 — AI provider abstraction (unblocks L11, Q7, Q14) — **Shipped 2026-08-21**

**Shipped:** `TemplateConvertAiModels.cs`, `ITemplateConvertAiProvider` / `NoneTemplateConvertAiProvider`, `ITemplateMappingPlanSanitizer` / `TemplateMappingPlanSanitizer`, `TemplateMappingRequestBuilder`, options + DI in `AddTemplateConvert`, tests in `TemplateConvertAiProviderTests` (Q7 / Q13 / Q14). Convert dialog still uses the deterministic orchestrator only — E9 wires chat through this seam; E10 adds a real adapter behind the same interface.

### 6.1 Locked engineering decision: matching is local

The product spec's §8 privacy note ("prefer redaction of ID numbers if cloud AI") conflicts with L7, whose strongest matching signal *is* the passport number. Resolution:

> **E-D1 — Deterministic matching runs locally. The provider never receives raw instance values.**
> The provider sees: the document extract (optionally redacted), a list of **candidate regions** with `ValueKind` and a *masked* preview, and the allowed **token names**. It returns region→token decisions. This makes L6 a property of the type system rather than of prompt discipline, and makes Q7 a compile-time-adjacent test.

### 6.2 Contract

```csharp
public interface ITemplateConvertAiProvider
{
    string Key { get; }                 // "None" | "AzureOpenAI" | "xAI" | …
    bool IsEnabled { get; }

    Task<TemplateMappingPlan> ProposeMappingAsync(
        TemplateMappingRequest request, CancellationToken cancellationToken = default);

    Task<TemplateChatTurnResult> ApplyChatAdjustmentAsync(
        TemplateChatTurnRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// L6/L10 by construction: no IObjectSpace, no BO, no raw identifier values.
/// </summary>
public sealed class TemplateMappingRequest
{
    public required TemplateSourceFormat Format { get; init; }
    public required IReadOnlyList<DocumentExtractRegion> Regions { get; init; }
    public required IReadOnlyList<AllowedToken> AllowedTokens { get; init; }
    public required string PlaceholderSetFingerprint { get; init; }
    public required IReadOnlyList<DeterministicMatch> PreMatched { get; init; }
}

public sealed record AllowedToken(string ShortCode, string DisplayName, UserReportPlaceholderScope Scope);

public sealed record TemplateMappingPlan(
    IReadOnlyList<TokenSubstitution> Substitutions,
    IReadOnlyList<LoopMarker> Loops,
    IReadOnlyList<MappingGap> Gaps,
    string? Rationale);

public sealed record MappingGap(string LiteralPreview, string? SuggestedPropertyName, DocumentRegion Region);

public sealed record TemplateChatTurnResult(
    bool Accepted,
    string ReplyText,
    TemplateMappingPlan? UpdatedPlan,
    ChatRejectReason? RejectReason);

public enum ChatRejectReason
{
    OutOfScopeContentEdit,   // L8 — rewrite / restyle / translate
    TokenNotInProfileSet,    // L10
    AmbiguousRegion,
    NotUnderstood,
}
```

### 6.3 Never trust provider output

```csharp
public interface ITemplateMappingPlanSanitizer
{
    TemplateMappingPlan Sanitize(
        TemplateMappingPlan proposed,
        ApplicationProfilePlaceholderSet allowedSet,
        IReadOnlyList<DocumentExtractRegion> knownRegions,
        out IReadOnlyList<string> dropped);
}
```

Drops any substitution whose token is outside the set, whose region is unknown, or that overlaps another. Runs **before** the writer, on both the mapping and chat paths. This — not the prompt — is what makes Q11/Q12/Q13 hold.

### 6.4 Adapters and config

| Adapter | Behavior |
|---------|----------|
| `NoneTemplateConvertAiProvider` | Default. `IsEnabled = false`; returns the deterministic plan unchanged; chat replies "AI assistance is turned off." |
| `AzureOpenAiTemplateConvertAiProvider` (**E10**) | HTTP Chat Completions against Azure OpenAI (no vendor SDK). `IsEnabled` when `Provider=AzureOpenAI` and endpoint/deployment/API key are set. `ConvertAsync` proposes a plan, sanitizes it, and falls back to deterministic matches on failure. API key from `TEMPLATE_AI_CONVERT_AZURE_OPENAI_API_KEY` (preferred) or `TemplateAiConvert:AzureOpenAI:ApiKey` |

**Demo pilot (ops):** keep `Provider` = `None` everywhere except Demo. On Demo set `TemplateAiConvert:Provider` = `AzureOpenAI`, fill `AzureOpenAI:Endpoint` / `Deployment` / `ApiVersion`, and set the API key in the slot environment — never commit the key.

```json
"TemplateAiConvert": {
  "Enabled": false,
  "Provider": "None",
  "RequestTimeoutSeconds": 60,
  "MaxDocumentCharacters": 50000,
  "RedactIdentifiersInExtract": true
}
```

Secrets per slot via environment variables, never `appsettings`. Feature flag is per slot (Demo first).

---

## 7. E6 — Extract/Validate on ephemeral bytes + warning tier — **Shipped**

`IUserReportTemplateMaintenanceService` requires a persisted template id, and `PlaceholderValidationResult` has no severity — but product spec §6.1 needs a soft-warning tier.

**Shipped:** `EphemeralTemplateValidationService.cs`, `EphemeralTemplateValidationModels.cs` (15 tests). Registered **scoped**, not singleton, because the extractors and validators it wraps are scoped.

```csharp
public interface IEphemeralTemplateValidationService
{
    Task<TemplateValidationReport> ExtractAndValidateAsync(
        byte[] content,
        TemplateSourceFormat format,
        ApplicationProfilePlaceholderSet allowedSet,
        CancellationToken cancellationToken = default);
}

public sealed record TemplateValidationReport(
    IReadOnlyList<string> Tokens,
    IReadOnlyList<PlaceholderValidationResult> Results,
    IReadOnlyList<TemplateValidationIssue> Issues,
    bool HasHardFailure)
{
    public bool HasWarnings { get; }   // drives the E-D2 acknowledge checkbox
}

public sealed record TemplateValidationIssue(
    string Message, TemplateValidationSeverity Severity, TemplateValidationIssueCode Code, string? Token);

public enum TemplateValidationSeverity { Error, Warning }
```

Wraps the existing stream extractors and validators, adds the L10 set check and the severity split. No DB row.

**Deviations from the proposal:**

| Change | Why |
|--------|-----|
| `TemplateValidationIssue` carries a `TemplateValidationIssueCode` | UI copy and tests should not match on message text. Codes: `UnreadableDocument`, `NoTokensFound`, `UnknownToken`, `PackDisabledToken`, `UnsupportedImageToken`, `OutOfDataScopeToken`, `BrokenLoop`, `UnresolvedOnBoType`. |
| `ApplicationProfilePlaceholderSet` now echoes `DataScope` and `TemplateKind` | The validators need a `UserReportBoType`, and the set was the only argument carrying profile context. E1 already had both in its query. |
| "Low-confidence leftover literal → Warning" is **not** produced here | Leftover literals are found by the E3 residual scanner, which needs the instance value map. E6 sees only bytes + allowed set. **E7 merges both issue lists** before deciding Approve. |
| Loop markers are balance-checked, not property-validated | Collection names are authoring-defined (`rows`, `ApplicationItems` — see `WORD_REPORT_PLACEHOLDER_REFERENCE.md`), so the name cannot be judged, and `ValidateRowsCollection` rejects `rows` outright for an `ApplicationItem` root. |
| Nesting order is not checked | Both extractors de-duplicate into a `HashSet`, so document order is unavailable. What is checkable is set equality of open vs close names, which is what actually breaks the generators. |

**Severity mapping** (product spec §6.1): unknown token, out-of-scope token, broken loop, image token in Excel, empty extract, unreadable package, and any token the existing validator marks invalid → `Error`; pack-disabled reference → `Warning` (it resolves and merges as empty text).

**Merge root:** `PeopleM2M` → `UserReportBoType.ApplicationItem`, otherwise `ApplicationProfileInstance`. Excel always validates as `ExcelMergeMode.ItemList`; `SingleItem` (one workbook per person) is a seed-time authoring choice with no equivalent in the convert flow.

---

## 7.1 E7b — Orchestrator + document outline — **Shipped (both entries, 2026-08-21)**

Two Module services were added when the dialog was wired, both in `Visa2026.Module/Services/TemplateConvert/`.

### `ITemplateConvertOrchestrator` (scoped)

```csharp
bool TryResolveFormat(string fileName, out TemplateSourceFormat format);
TemplateConvertAnalysis Analyze(TemplateConvertAnalyzeRequest request);          // E1 → E2 → outline → E5
Task<TemplateConvertOutcome> ConvertAsync(TemplateConvertAnalysis analysis, byte[] originalContent, CancellationToken ct = default);
ApplicationProfileTemplate Save(TemplateConvertSaveRequest request);             // nested row + bridge + master file
```

The sequence lives here rather than in Razor because three rules are easy to get wrong and would otherwise be re-implemented per host — the wizard entry reuses all of it and needed no Module change:

| Rule | Why |
|------|-----|
| The diff gate receives `TokenWriteResult.AppliedSubstitutions`, never the requested list | A legitimate skip would otherwise read as an unapproved delta and fail a good conversion |
| Residual probes are derived from the **replaced** header candidates, deduplicated by raw value | Probing every instance value re-reports literals the officer intentionally left as text |
| `Errors` / `Warnings` merge E6 issues **with** diff-gate violations, residual hits, and write skips | Spec §6.1 promises one verdict; E6 alone cannot see the residual scan (see §7) |

`TemplateConvertAnalysis.ConvertibleHighlights` is header matches only, and `RosterLoopBlocksConversion` disables Convert when E5 reports a roster loop: the writer accepts `{{#ds.rows}}` markers but nothing derives them from a candidate report yet, and header-only substitution on a roster silently produces a template that repeats row one.

### `ITemplateDocumentOutlineReader` (singleton)

```csharp
TemplateDocumentOutline Read(byte[] content, TemplateSourceFormat format);
```

A read-only text projection — Word paragraphs (`Address`, `Part`, `Text`, `IsInTable`) and Excel cells (`SheetName`, `CellReference`, row/column, formatted text) — so the UI can draw the document under the E5 highlights. It reuses `WordTemplateAddressing.EnumerateParagraphs`, so paragraph addresses are identical to the writer's and the diff gate's; a second addressing scheme would drift and highlight the wrong span. An unreadable package returns `IsReadable = false` instead of throwing, matching the E5 contract.

### Hosting rule for `Save`

`TemplateConvertSaveRequest.ObjectSpace` is supplied by the caller and the orchestrator **never commits**. The case-side dialog creates an object space and commits after `Save`; the wizard passes its own and lets **Save profile** commit, so an abandoned wizard rolls back the nested template, its `FileData`, and the bridged `UserReportTemplate` together. Any future host must decide the same way — a commit inside `Save` would half-save a profile under edit.

**Not yet built on this path:** merged fill preview (needs E4 or an in-memory generate + PDF step, so Preview shows the V12 fallback), and the gap packet (export still open). Mapping chat (E9) shipped.

---

## 8. Locked engineering decisions

Approved 2026-08-20. **E-D1** is in §6.1 (matching runs locally; the provider never receives raw instance values).

| # | Topic | Decision |
|---|-------|----------|
| **E-D2** | Soft warnings — checkbox or block (product spec §9.1) | **Checkbox** on Preview. Driven by `TemplateValidationSeverity.Warning` from E6; `Error` always blocks Approve |
| **E-D3** | Excel preview fidelity (product spec §9.5) | Reuse `ApplicationWordReportOfficePreviewPdfConverter` (DevExpress Spreadsheet → PDF, already in production). Fall back to download + note **only** when conversion fails |
| **E-D4** | Convert while config locked (product spec §9.6) | **Already enforced in code** — `AllowsNestedEditWhenConfigLocked` returns `false` for `ApplicationProfileTemplate`. Preview allowed, Approve disabled with a plain message (prototype 11). No carve-out |
| **E-D5** | Derive vs constrain the catalog (profile plan §2.6 item **C**) | **Constrain.** E1 is the single source of allowed tokens. Unknown names never become mergeable tokens — they go to the gap list only (Q2). This closes item C for this feature |
| **E-D6** | L7 suitability thresholds | **Fail** when fewer than 3 distinct header matches **and** no roster loop · **Warn** at 3–5 header matches · **Pass** at 6+, or a roster loop plus 2+ header matches. Bound to config (`TemplateAiConvert:Suitability:*`), never `const` |
| **E-D7** | Preview time budget (product spec P5 "N") | p95 **20 s** without AI, **90 s** with AI. Provider timeout 60 s (§6.4) |
| **E-D8** | Gap packet — BO or export | **Export** from the draft (`MappingPlanJson` gaps + `ValidationJson` + instance id + fingerprint) as JSON or Markdown. No new BO in v1. Revisit only if developers ask for a queryable inbox |

### Outstanding input (not a decision)

| Item | Needs | Gates |
|------|-------|-------|
| **Golden set** | 3 real Çalık Word letters + 3 real Excel rosters, each with a matching `ApplicationProfileInstance` | Q5 and pilot exit E1–E3 only. Does **not** gate slices E1–E4 or E6 |

---

## 9. Slice plan

**Sequencing gate:** **E4** starts after profile slice 10 (Person M2M / Wave 2b F5 heal) lands — it adds a table, and interleaving a new BO with the outstanding `ApplicationProfileInstancePerson` heal makes an F5 failure hard to attribute. E1, E2, and E3 touch no schema and are not gated; E3 shipped ahead of the heal for exactly that reason.

| # | Slice | Depends on | Needs AI? |
|---|-------|-----------|-----------|
| E0 | §8 decisions locked (**done**); golden set still outstanding | — | No |
| E1 | Profile-scoped placeholder set + `PackKey` in catalog JSON — **done** | E-D5 | No |
| E2 | Instance value map + ambiguity rejection — **done** (reuses `TemplateTextNormalizer` from E3) | — | No |
| E3 | Token writer (Word + Excel) + diff gate + residual scan — **done** | — | No |
| E4 | Draft BO + EF mapping + permissions + expiry sweep | slice 10 heal | No |
| E5 | Candidate check: suitability score + highlight regions — **done** (§4.4) | E1, E2 | No |
| E6 | Ephemeral extract/validate + severity tier | E1 | No |
| E7 | Modal shell (Upload → Candidate check → Converting → Preview → Done), deterministic path end to end, commit via bridge | E1–E6 | No |
| E8 | Provider abstraction + `None` adapter + sanitizer + Q7 / Q13 / Q14 tests | E7 | No | **Done** 2026-08-21 |
| E9 | Preview chat panel (accept / reject copy) against `None` adapter | E8 | No | **Done** 2026-08-21 |
| E10 | First real adapter + per-slot flag + Demo pilot | E8, E9 | **Yes** | **Done** 2026-08-21 |

E0–E9 ship a working feature with **zero** AI dependency: deterministic matching already covers the common case where the officer's document was produced from the same data. E10 is the accelerator.

**Tracking:** rows **E0–E10** are in [`IMPLEMENTATION_PLAN.md`](../.cursor/skills/visa2026-application-profile/IMPLEMENTATION_PLAN.md). Set a row to **In progress** when the slice starts and **Done** only after `dotnet build Visa2026.slnx -c Debug` plus the slice's tests pass, per the skill workflow (steps 5–7).

---

## 10. Test matrix

| Product criterion | Test |
|-------------------|------|
| Q1, Q13 | `ApplicationProfilePlaceholderSet` for profile A never contains a token excluded by data scope or a disabled `RequirePerson*` pack; profile B's extra tokens absent |
| Q2 | Sanitizer drops an invented token; committed bytes contain no unknown `{{…}}` |
| Q3 | Approve disabled until `TemplateValidationReport.HasHardFailure == false` |
| Q4 | Residual value scan (§4.3) on a Word and an Excel golden document |
| Q5 | Golden-set integration test, 3 Word + 3 Excel |
| Q6 | Commit to `Global` scope without the Shared permission is rejected |
| Q7 | `TemplateMappingRequest` carries no BO and no raw identifier; assert by reflection over its property graph |
| Q8 | Highlight regions map only to allowed tokens or explicit gaps |
| Q9 | Suitability Fail blocks Convert; Warn requires the continue flag |
| Q10 | Diff gate rejects a tampered document that changes a font, adds a paragraph, or edits a formula |
| Q11, Q12 | Chat turn asking for a rewrite returns `Accepted = false`, `OutOfScopeContentEdit`, and byte-identical draft |
| Q14 | Solution builds and the convert path runs with `Provider = "None"` and with a stub adapter; no vendor type referenced from `Visa2026.Module` domain services |

---

## 11. Open engineering risks

| Risk | Note |
|------|------|
| ~~Word run splitting~~ | **Retired in E3.** Writing the token into the first `w:t` the span touches leaves the run structure untouched, so no splitting was needed. Tables and text boxes fall out of `Descendants<Paragraph>()` addressing for free |
| ~~Turkmen/Turkish normalization~~ | **Addressed in E3** by `TemplateTextNormalizer` (invariant casefold + diacritic folding), covered by `TemplateTextNormalizerTests`. E2 must reuse it |
| Runs inside hyperlinks and content controls | The gate compares `Elements<Run>()` (direct children) for `rPr` drift, so runs nested in a `w:hyperlink` are not formatting-checked. Text comparison still covers their content. Revisit if a golden document relies on it |
| False-positive matches | Short literals and values that are both a name and a place (e.g. `Mary`). §3 minimum-length and ambiguity rules are load-bearing |
| Letterhead double-match | Company name appears in both letterhead and body; letterhead must stay static (default: skip headers/footers) |
| Roster loop detection | Deciding where a repeating table starts and ends is heuristic; prefer requiring a header row match before proposing a loop |
| Excel merged cells | A merged range has one anchor cell; substituting a non-anchor cell silently does nothing |
| Provider latency | With AI on, the 90 s budget is optimistic for long documents; chunking is out of scope for v1 |

---

## 12. One-line summary

**Phase 0 is six contracts — profile-scoped placeholder set, instance value map, token writer plus diff gate, draft persistence, provider abstraction with a local-matching guarantee, and ephemeral extract/validate — after which slices E0–E9 deliver the whole officer flow with no AI vendor, and E10 adds one.**
