# Engineering spec (Phase 0) — AI convert existing Word/Excel → profile templates

> **Status:** Phase 0 — contracts proposed · **§8 decisions locked 2026-08-20** · not implemented · slices **E0–E10** tracked in [`IMPLEMENTATION_PLAN.md`](../.cursor/skills/visa2026-application-profile/IMPLEMENTATION_PLAN.md)
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

```csharp
public interface IApplicationProfilePlaceholderSetService
{
    ApplicationProfilePlaceholderSet GetSet(ApplicationProfilePlaceholderSetQuery query);
}

public sealed class ApplicationProfilePlaceholderSetQuery
{
    public required Guid ApplicationProfileId { get; init; }
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
    WrongRootBoType,
    StructuralUnsupportedForKind,
}
```

**Derivation (in order):**

| Step | Rule |
|------|------|
| 1 | Start from `IUserReportPlaceholderCatalogService.GetEntries` with `RootBoType = ApplicationProfileInstance` |
| 2 | Map data scope → `UserReportPlaceholderScope`: `ApplicationHeader` → `Header`, `PeopleM2M` → `Row`, `Both` → `Both` |
| 3 | Drop entries whose person pack is disabled by the profile's `RequirePerson*` toggles → `PersonPackDisabled` |
| 4 | Drop structural markers unsupported for `TemplateKind` (e.g. `{{IMAGE:…}}` in Excel) → `StructuralUnsupportedForKind` |
| 5 | Compute `Fingerprint` = SHA-256 of sorted allowed short codes (record on the draft and in audit) |

**Required prerequisite — do not guess packs from string prefixes.** Add an explicit `PackKey` to each entry in `Resources/UserReportPlaceholderCatalog.json` and to `UserReportPlaceholderCatalogEntry`, then map `PackKey` → `RequirePerson*` in one table. Prefix matching on canonical paths is brittle and will silently leak tokens, which breaks Q13.

**Exclusions are returned, not swallowed** — the officer-facing "gap" explanation and the developer gap packet both need the reason.

---

## 3. E2 — Instance value map (unblocks L7 matching, P4)

```csharp
public interface IApplicationProfileInstanceValueMapService
{
    Task<ApplicationProfileInstanceValueMap> BuildAsync(
        Guid applicationProfileInstanceId,
        ApplicationProfileTemplateDataScope dataScope,
        CancellationToken cancellationToken = default);
}

public sealed class ApplicationProfileInstanceValueMap
{
    public required IReadOnlyDictionary<string, string?> Header { get; init; }
    public required IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows { get; init; }

    /// <summary>Inverted index for literal → token matching.</summary>
    public required IReadOnlyList<ValueCandidate> Candidates { get; init; }
}

public sealed record ValueCandidate(
    string Token,
    string RawValue,
    string NormalizedValue,
    ValueKind Kind,
    int? RowIndex);

public enum ValueKind { Text, Date, Number, Identifier, PersonName }
```

**Implementation:** wrap the existing statics — `UserReportMergeDataHelper.BuildApplicationHeaderDictionary`, `.GetActiveApplicationItems` / `ApplicationRosterHelper.GetMergeLineItems`, and the row builders. **Must not** require a `UserReportTemplate`; the whole point is that no template exists yet.

**Normalization is the hard part.** Matching Turkmen and Turkish documents against database values fails without it:

| Kind | Normalization for comparison |
|------|------------------------------|
| All | Trim, collapse internal whitespace, NFC, casefold with **invariant** culture (never `tr-TR` — dotted/dotless `i` will corrupt comparisons) |
| Text / PersonName | Fold Turkmen and Turkish diacritics (`ç ş ň ý ü ö ä ğ ı İ`) to an ASCII form for a secondary match key; keep the raw value for display. Try both orders for names (`Amanov Dowletmyrat` and `Dowletmyrat Amanov`) |
| Date | Parse and re-render in every candidate format: `dd.MM.yyyy`, `d.M.yyyy`, `dd/MM/yyyy`, `yyyy-MM-dd`, plus Turkmen month names. Match on the parsed date, not the string |
| Number | Strip thousands separators; both `,` and `.` decimal marks |
| Identifier | Strip spaces and hyphens (passport `T 12345678` = `T12345678`) |

**Minimum literal length.** Reject candidates whose normalized value is shorter than 3 characters or is a bare small integer — otherwise `"1"` or `"Mary"` (a Turkmen city **and** a name) produces false highlights. Record rejected-as-ambiguous separately so the suitability score is honest.

---

## 4. E3 — Local token writer + diff gate (unblocks L8, Q10, Q4)

### 4.1 Writer

```csharp
public interface ITemplateTokenWriter
{
    Task<TokenWriteResult> ApplyAsync(TemplateTokenWriteRequest request, CancellationToken cancellationToken = default);
}

public sealed class TemplateTokenWriteRequest
{
    public required byte[] SourceContent { get; init; }
    public required TemplateSourceFormat Format { get; init; }          // Docx | Xlsx
    public required IReadOnlyList<TokenSubstitution> Substitutions { get; init; }
    public required IReadOnlyList<LoopMarker> Loops { get; init; }
}

public sealed record TokenSubstitution(DocumentRegion Region, string Token);
public sealed record LoopMarker(DocumentRegion Start, DocumentRegion End, string CollectionToken);

public abstract record DocumentRegion
{
    public sealed record WordSpan(string ParagraphId, int Start, int Length, WordPart Part) : DocumentRegion;
    public sealed record ExcelCell(string SheetName, string CellRef) : DocumentRegion;
}

public enum WordPart { Body, Header, Footer, TableCell, TextBox }

public sealed record TokenWriteResult(byte[] Content, int Applied, IReadOnlyList<string> Skipped);
```

**Word rules (OpenXml):**

| Rule | Detail |
|------|--------|
| Offsets are paragraph-relative over concatenated run text | A visible phrase is routinely split across several `<w:r>`; resolve the span to a run range first |
| Split, never merge | Split boundary runs and copy the original `w:rPr` onto each fragment; the token inherits the formatting of the **first** run in the span |
| Preserve `xml:space="preserve"` | Required or leading/trailing spaces vanish |
| Never touch | `styles.xml`, `numbering.xml`, `theme/`, `sectPr`, `rsid*` attributes, image parts, content controls |
| Headers/footers | Reachable but **default to not substituting** — letterhead is static content (L7 "unmatched literals / static") |

**Excel rules (ClosedXML):** set the cell **value** only. Do not touch number formats, styles, column widths, merged ranges, conditional formatting, defined names, or formulas. A cell holding a formula is never a substitution target.

### 4.2 Diff gate

```csharp
public interface ITemplateConversionDiffGate
{
    DiffGateResult Verify(
        byte[] original,
        byte[] converted,
        TemplateSourceFormat format,
        IReadOnlyList<TokenSubstitution> expected);
}

public sealed record DiffGateResult(bool Passed, IReadOnlyList<string> Violations);
```

Runs **after** the writer, on every convert, including the no-AI path. Any violation fails the convert (Q10).

| Must be identical | Word | Excel |
|-------------------|------|-------|
| Structure | Part list, paragraph count, table/row/cell counts, section count | Sheet count + names, used range, merged ranges |
| Formatting | `styles.xml`, `numbering.xml`, theme, `sectPr`, per-run `rPr` outside substituted spans | Cell styles, number formats, column widths, conditional formatting |
| Media | Image part count + SHA-256 per part | Same |
| Formulas | — | Every formula string |
| Text | Equal after masking each expected region with its token | Equal per cell after masking |

### 4.3 Residual value scan (Q4)

Before commit, assert that **no** `ValueCandidate.NormalizedValue` of `Kind` `Identifier` or `PersonName` survives anywhere in the committed bytes. A leftover passport number in a saved master is a data-protection defect, not a cosmetic one.

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

## 6. E5 — AI provider abstraction (unblocks L11, Q7, Q14)

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
| `NoneTemplateConvertAiProvider` | Default. `IsEnabled = false`; returns the deterministic plan unchanged; chat replies "AI assistance is turned off." Ships first and is the only adapter needed for Phase 0 |
| One real adapter | Added later behind the same interface. No vendor type may appear outside its adapter assembly/folder (Q14) |

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

## 7. E6 — Extract/Validate on ephemeral bytes + warning tier

`IUserReportTemplateMaintenanceService` requires a persisted template id, and `PlaceholderValidationResult` has no severity — but product spec §6.1 needs a soft-warning tier.

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
    bool HasHardFailure);

public sealed record TemplateValidationIssue(
    string Message, TemplateValidationSeverity Severity, string? Token);

public enum TemplateValidationSeverity { Error, Warning }
```

Wraps the existing stream extractors and validators, adds the L10 set check and the severity split. No DB row. Severity mapping follows product spec §6.1: unknown token, broken loop, unsupported IMAGE, empty extract, corrupt OOXML → `Error`; pack-disabled reference and low-confidence leftover literal → `Warning`.

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

**Sequencing gate:** E1 starts **after** profile slice 10 (Person M2M / Wave 2b F5 heal) lands. E4 adds a table; do not interleave a new BO with the outstanding `ApplicationProfileInstancePerson` heal.

| # | Slice | Depends on | Needs AI? |
|---|-------|-----------|-----------|
| E0 | §8 decisions locked (**done**); golden set still outstanding | — | No |
| E1 | Profile-scoped placeholder set + `PackKey` in catalog JSON | E-D5, slice 10 heal | No |
| E2 | Instance value map + normalization + ambiguity rejection | — | No |
| E3 | Token writer (Word + Excel) + diff gate + residual scan | — | No |
| E4 | Draft BO + EF mapping + permissions + expiry sweep | — | No |
| E5 | Candidate check: suitability score + highlight regions | E1, E2 | No |
| E6 | Ephemeral extract/validate + severity tier | E1 | No |
| E7 | Modal shell (Upload → Candidate check → Converting → Preview → Done), deterministic path end to end, commit via bridge | E1–E6 | No |
| E8 | Provider abstraction + `None` adapter + sanitizer + Q7 / Q13 / Q14 tests | E7 | No |
| E9 | Preview chat panel (accept / reject copy) against `None` adapter | E8 | No |
| E10 | First real adapter + per-slot flag + Demo pilot | E8, E9 | **Yes** |

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
| Word run splitting | Highest-effort item in E3. Budget real time for tables, nested tables, and text boxes |
| Turkmen/Turkish normalization | Invariant casefold is mandatory; `tr-TR` casing will corrupt `i`/`İ` comparisons |
| False-positive matches | Short literals and values that are both a name and a place (e.g. `Mary`). §3 minimum-length and ambiguity rules are load-bearing |
| Letterhead double-match | Company name appears in both letterhead and body; letterhead must stay static (default: skip headers/footers) |
| Roster loop detection | Deciding where a repeating table starts and ends is heuristic; prefer requiring a header row match before proposing a loop |
| Excel merged cells | A merged range has one anchor cell; substituting a non-anchor cell silently does nothing |
| Provider latency | With AI on, the 90 s budget is optimistic for long documents; chunking is out of scope for v1 |

---

## 12. One-line summary

**Phase 0 is six contracts — profile-scoped placeholder set, instance value map, token writer plus diff gate, draft persistence, provider abstraction with a local-matching guarantee, and ephemeral extract/validate — after which slices E0–E9 deliver the whole officer flow with no AI vendor, and E10 adds one.**
