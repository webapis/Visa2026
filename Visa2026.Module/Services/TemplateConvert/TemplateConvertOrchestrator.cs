using DevExpress.ExpressApp;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

public sealed class TemplateConvertAnalyzeRequest
{
    /// <summary>Must come from the same object space as <see cref="Instance"/>.</summary>
    public required ApplicationProfile Profile { get; init; }

    public required ApplicationProfileInstance Instance { get; init; }

    public required byte[] Content { get; init; }

    public required string FileName { get; init; }

    public ApplicationProfileTemplateDataScope DataScope { get; init; } = ApplicationProfileTemplateDataScope.ApplicationHeader;
}

/// <summary>Everything the convert UI needs to draw the candidate check and to run the conversion afterwards.</summary>
public sealed class TemplateConvertAnalysis
{
    public required TemplateSourceFormat Format { get; init; }

    public required ApplicationProfileTemplateKind TemplateKind { get; init; }

    public required ApplicationProfileTemplateDataScope DataScope { get; init; }

    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }

    public required ApplicationProfileInstanceValueMap ValueMap { get; init; }

    public required TemplateCandidateReport Candidate { get; init; }

    public required TemplateDocumentOutline Outline { get; init; }

    /// <summary>
    /// Local plan built at analyze time: header matches, first roster row (when a loop is planned),
    /// and any derived <c>{{#ds.rows}}</c> markers.
    /// </summary>
    public required TemplateMappingPlan DeterministicPlan { get; init; }

    /// <summary>Highlights that the deterministic plan will write (header + first roster row).</summary>
    public IReadOnlyList<HighlightRegion> ConvertibleHighlights =>
        Candidate.Highlights
            .Where(h => h.Kind == HighlightKind.Match
                && !string.IsNullOrWhiteSpace(h.Token)
                && DeterministicPlan.Substitutions.Any(s => Equals(s.Region, h.Region)))
            .ToList();

    /// <summary>
    /// True when a roster was detected but no safe loop boundaries could be derived.
    /// Blocking beats emitting a template that repeats row one without markers.
    /// </summary>
    public bool RosterLoopBlocksConversion =>
        Candidate.RosterLoopDetected && DeterministicPlan.Loops.Count == 0;

    public bool CanConvert =>
        Candidate.CanConvert && !RosterLoopBlocksConversion && DeterministicPlan.Substitutions.Count > 0;
}

public sealed class TemplateConvertOutcome
{
    public required byte[] Content { get; init; }

    public required TemplateDocumentOutline Outline { get; init; }

    public required IReadOnlyList<TokenSubstitution> Applied { get; init; }

    public required IReadOnlyList<TemplateWriteSkip> Skipped { get; init; }

    public required DiffGateResult Diff { get; init; }

    public required ResidualValueScanResult Residual { get; init; }

    public required TemplateValidationReport Validation { get; init; }

    /// <summary>Display text for the officer. Any entry here disables Approve.</summary>
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>Display text the officer can acknowledge and continue past.</summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    public bool HasErrors => Errors.Count > 0;

    public bool HasWarnings => Warnings.Count > 0;
}

public sealed class TemplateConvertSaveRequest
{
    public required IObjectSpace ObjectSpace { get; init; }

    /// <summary>The profile loaded from <see cref="ObjectSpace"/>.</summary>
    public required ApplicationProfile Profile { get; init; }

    public required string TemplateName { get; init; }

    public required ApplicationProfileTemplateKind TemplateKind { get; init; }

    public required ApplicationProfileTemplateDataScope DataScope { get; init; }

    public required ApplicationProfileTemplateCatalogScope CatalogScope { get; init; }

    public required byte[] Content { get; init; }

    public required string FileName { get; init; }
}

public interface ITemplateConvertOrchestrator
{
    /// <summary>Word or Excel from the upload name. Anything else is rejected before any service runs.</summary>
    bool TryResolveFormat(string fileName, out TemplateSourceFormat format);

    TemplateConvertAnalysis Analyze(TemplateConvertAnalyzeRequest request);

    Task<TemplateConvertOutcome> ConvertAsync(
        TemplateConvertAnalysis analysis,
        byte[] originalContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the draft from an accepted chat mapping plan (E9). Same post-write checks as
    /// <see cref="ConvertAsync"/> so Approve still sees one Errors / Warnings verdict.
    /// </summary>
    Task<TemplateConvertOutcome> ApplyPlanAsync(
        TemplateConvertAnalysis analysis,
        byte[] originalContent,
        TemplateMappingPlan plan,
        CancellationToken cancellationToken = default);

    ApplicationProfileTemplate Save(TemplateConvertSaveRequest request);
}

/// <summary>
/// Sequences the shipped convert services (placeholder set → value map → candidate check → write →
/// diff gate → residual scan → validate) so the Blazor host stays a thin shell.
/// </summary>
public sealed class TemplateConvertOrchestrator : ITemplateConvertOrchestrator
{
    private readonly IApplicationProfilePlaceholderSetService _placeholderSets;
    private readonly IApplicationProfileInstanceValueMapService _valueMaps;
    private readonly ITemplateCandidateAnalyzer _candidates;
    private readonly ITemplateDocumentOutlineReader _outlines;
    private readonly ITemplateTokenWriter _writer;
    private readonly ITemplateConversionDiffGate _diffGate;
    private readonly ITemplateResidualValueScanner _residualScanner;
    private readonly IEphemeralTemplateValidationService _validation;
    private readonly ITemplateConvertAiProvider _ai;
    private readonly ITemplateMappingPlanSanitizer _planSanitizer;
    private readonly TemplateAiConvertOptions _options;

    public TemplateConvertOrchestrator(
        IApplicationProfilePlaceholderSetService placeholderSets,
        IApplicationProfileInstanceValueMapService valueMaps,
        ITemplateCandidateAnalyzer candidates,
        ITemplateDocumentOutlineReader outlines,
        ITemplateTokenWriter writer,
        ITemplateConversionDiffGate diffGate,
        ITemplateResidualValueScanner residualScanner,
        IEphemeralTemplateValidationService validation,
        ITemplateConvertAiProvider? aiProvider = null,
        ITemplateMappingPlanSanitizer? planSanitizer = null,
        IOptions<TemplateAiConvertOptions>? options = null)
    {
        _placeholderSets = placeholderSets;
        _valueMaps = valueMaps;
        _candidates = candidates;
        _outlines = outlines;
        _writer = writer;
        _diffGate = diffGate;
        _residualScanner = residualScanner;
        _validation = validation;
        _ai = aiProvider ?? new NoneTemplateConvertAiProvider();
        _planSanitizer = planSanitizer ?? new TemplateMappingPlanSanitizer();
        _options = options?.Value ?? new TemplateAiConvertOptions();
    }

    public bool TryResolveFormat(string fileName, out TemplateSourceFormat format)
    {
        format = TemplateSourceFormat.Docx;
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return true;

        if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            format = TemplateSourceFormat.Xlsx;
            return true;
        }

        return false;
    }

    public TemplateConvertAnalysis Analyze(TemplateConvertAnalyzeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryResolveFormat(request.FileName, out var format))
            throw new ArgumentException($"Unsupported upload '{request.FileName}'. Only .docx and .xlsx convert.", nameof(request));

        var kind = format == TemplateSourceFormat.Xlsx
            ? ApplicationProfileTemplateKind.Excel
            : ApplicationProfileTemplateKind.Word;

        var placeholderSet = _placeholderSets.GetSet(new ApplicationProfilePlaceholderSetQuery
        {
            Profile = request.Profile,
            DataScope = request.DataScope,
            TemplateKind = kind,
        });

        var valueMap = _valueMaps.Build(new ApplicationProfileInstanceValueMapRequest
        {
            Instance = request.Instance,
            PlaceholderSet = placeholderSet,
            DataScope = request.DataScope,
        });

        var outline = _outlines.Read(request.Content, format);

        var candidate = _candidates.Analyze(new TemplateCandidateRequest
        {
            Content = request.Content,
            Format = format,
            ValueMap = valueMap,
        });

        var deterministicPlan = TemplateRosterLoopPlanner.Build(candidate, format);

        return new TemplateConvertAnalysis
        {
            Format = format,
            TemplateKind = kind,
            DataScope = request.DataScope,
            PlaceholderSet = placeholderSet,
            ValueMap = valueMap,
            Candidate = candidate,
            Outline = outline,
            DeterministicPlan = deterministicPlan,
        };
    }

    public async Task<TemplateConvertOutcome> ConvertAsync(
        TemplateConvertAnalysis analysis,
        byte[] originalContent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        var deterministic = analysis.DeterministicPlan;
        var plan = deterministic;
        var extraWarnings = new List<string>();

        if (analysis.Candidate.RosterLoopDetected && deterministic.Loops.Count > 0)
        {
            var extraRows = analysis.Candidate.Highlights
                .Where(static h => h.Kind == HighlightKind.Match && h.RowIndex != null)
                .Select(static h => h.RowIndex!.Value)
                .Distinct()
                .Count();
            if (extraRows > 1)
            {
                extraWarnings.Add(
                    "People rows below the first sample stay as literal text - delete them in the template editor if they are not needed.");
            }
        }

        if (_ai.IsEnabled)
        {
            try
            {
                var mappingRequest = TemplateMappingRequestBuilder.FromCandidate(
                    analysis.Format,
                    analysis.PlaceholderSet,
                    analysis.Candidate,
                    _options.RedactIdentifiersInExtract);

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.RequestTimeoutSeconds, 5, 180)));

                var proposed = await _ai.ProposeMappingAsync(mappingRequest, timeout.Token).ConfigureAwait(false);
                var sanitized = _planSanitizer.Sanitize(
                    proposed,
                    analysis.PlaceholderSet,
                    mappingRequest.Regions,
                    out var dropped);

                if (sanitized.Substitutions.Count > 0)
                {
                    // Keep local loop markers when the provider omits them (empty cells are not in the extract).
                    plan = new TemplateMappingPlan(
                        sanitized.Substitutions,
                        sanitized.Loops.Count > 0 ? sanitized.Loops : deterministic.Loops,
                        sanitized.Gaps.Count > 0 ? sanitized.Gaps : deterministic.Gaps,
                        sanitized.Rationale);
                    foreach (var drop in dropped)
                        extraWarnings.Add("AI suggestion dropped: " + drop);
                }
                else
                {
                    extraWarnings.Add("AI returned no usable mappings - using the local matches.");
                }
            }
            catch (Exception ex)
            {
                extraWarnings.Add("AI mapping was skipped (" + ex.Message + ") - using the local matches.");
                plan = deterministic;
            }
        }

        return await ApplyPlanAsync(analysis, originalContent, plan, cancellationToken, extraWarnings)
            .ConfigureAwait(false);
    }

    public async Task<TemplateConvertOutcome> ApplyPlanAsync(
        TemplateConvertAnalysis analysis,
        byte[] originalContent,
        TemplateMappingPlan plan,
        CancellationToken cancellationToken = default) =>
        await ApplyPlanAsync(analysis, originalContent, plan, cancellationToken, extraWarnings: null)
            .ConfigureAwait(false);

    private async Task<TemplateConvertOutcome> ApplyPlanAsync(
        TemplateConvertAnalysis analysis,
        byte[] originalContent,
        TemplateMappingPlan plan,
        CancellationToken cancellationToken,
        IReadOnlyList<string>? extraWarnings)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(originalContent);
        ArgumentNullException.ThrowIfNull(plan);

        var write = _writer.Apply(new TemplateTokenWriteRequest
        {
            SourceContent = originalContent,
            Format = analysis.Format,
            Substitutions = plan.Substitutions,
            Loops = plan.Loops,
        });

        var diff = _diffGate.Verify(new TemplateDiffGateRequest
        {
            OriginalContent = originalContent,
            ConvertedContent = write.Content,
            Format = analysis.Format,
            Substitutions = write.AppliedSubstitutions,
            Loops = write.AppliedLoops,
        });

        var residual = _residualScanner.Scan(write.Content, analysis.Format, BuildProbes(analysis, write.AppliedSubstitutions));

        var validation = await _validation
            .ExtractAndValidateAsync(write.Content, analysis.Format, analysis.PlaceholderSet, cancellationToken)
            .ConfigureAwait(false);

        var errors = new List<string>();
        var warnings = new List<string>();

        foreach (var issue in validation.Issues)
        {
            if (issue.Severity == TemplateValidationSeverity.Error)
                errors.Add(issue.Message);
            else
                warnings.Add(issue.Message);
        }

        if (!diff.Passed)
            errors.AddRange(diff.Violations.Select(v => $"Document changed outside the mapped fields: {v}"));

        foreach (var hit in residual.Hits)
            warnings.Add($"Case value still present in the template: \"{hit.Value}\" ({hit.Label}) at {hit.LocationHint}.");

        foreach (var skip in write.Skipped)
            warnings.Add($"Field not written ({skip.Token}): {skip.Reason}");

        if (extraWarnings != null)
            warnings.InsertRange(0, extraWarnings);

        return new TemplateConvertOutcome
        {
            Content = write.Content,
            Outline = _outlines.Read(write.Content, analysis.Format),
            Applied = write.AppliedSubstitutions,
            Skipped = write.Skipped,
            Diff = diff,
            Residual = residual,
            Validation = validation,
            Errors = errors,
            Warnings = warnings,
        };
    }

    public ApplicationProfileTemplate Save(TemplateConvertSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.TemplateName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A template name is required.", nameof(request));

        return ApplicationProfileTemplateSaveHelper.Save(new ApplicationProfileTemplateSaveRequest
        {
            ObjectSpace = request.ObjectSpace,
            Profile = request.Profile,
            TemplateName = request.TemplateName,
            TemplateKind = request.TemplateKind,
            DataScope = request.DataScope,
            CatalogScope = request.CatalogScope,
            Content = request.Content,
            FileName = request.FileName,
        });
    }

    private static IReadOnlyList<ResidualValueProbe> BuildProbes(
        TemplateConvertAnalysis analysis,
        IReadOnlyList<TokenSubstitution> applied)
    {
        var replacedTokens = applied
            .Select(static s => TemplateTokenSyntax.TryGetShortCode(s.Token, out var code) ? code : s.Token)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return analysis.ValueMap.Candidates
            .Where(c => c.RowIndex == null && replacedTokens.Contains(c.ShortCode))
            .GroupBy(static c => c.RawValue, StringComparer.Ordinal)
            .Select(g => new ResidualValueProbe(
                g.Key,
                g.First().ShortCode,
                g.First().Kind == ValueKind.Identifier ? ResidualProbeKind.Identifier : ResidualProbeKind.Text))
            .ToList();
    }
}
