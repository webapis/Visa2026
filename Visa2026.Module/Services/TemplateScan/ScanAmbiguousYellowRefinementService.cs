#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanAmbiguousYellowRefinementService
{
    Task<ScanFieldPlanProposal> RefineAsync(
        ScanFieldPlanProposal proposal,
        ScanFieldPlanBuildRequest buildRequest,
        CancellationToken cancellationToken = default);
}

public sealed class ScanAmbiguousYellowRefinementService : IScanAmbiguousYellowRefinementService
{
    private readonly ITemplateScanAiProvider _provider;
    private readonly TemplateAiScanOptions _options;

    public ScanAmbiguousYellowRefinementService(
        ITemplateScanAiProvider provider,
        Microsoft.Extensions.Options.IOptions<TemplateAiScanOptions> options)
    {
        _provider = provider;
        _options = options?.Value ?? new TemplateAiScanOptions();
    }

    public async Task<ScanFieldPlanProposal> RefineAsync(
        ScanFieldPlanProposal proposal,
        ScanFieldPlanBuildRequest buildRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentNullException.ThrowIfNull(buildRequest);

        if (!_options.Enabled
            || !_options.RefineAmbiguousYellowWithAi
            || !_provider.IsEnabled)
            return proposal;

        var ambiguous = ScanAmbiguousYellowGate.SelectForRefinement(proposal.Fields, _options);
        if (ambiguous.Count == 0)
            return proposal;

        var contexts = ScanYellowMarkContextBuilder.Build(
            buildRequest.Ingest.Input.OfficePackageBytes,
            buildRequest.Ingest.Input.SourceKind,
            ambiguous);

        var marks = ambiguous.Select(draft =>
        {
            contexts.TryGetValue(draft.FieldId, out var context);
            return new ScanAmbiguousYellowMark
            {
                FieldId = draft.FieldId,
                YellowText = draft.LabelText,
                ColumnHeader = draft.ColumnHeader,
                SurroundingSnippet = context?.SurroundingSnippet,
                PrintedLabel = context?.PrintedLabel ?? draft.ColumnHeader,
                SheetName = context?.SheetName,
                HeaderRow = context?.HeaderRow,
                Scope = draft.Scope,
                LocalProposedToken = draft.ProposedToken,
                LocalCandidates = draft.Alternatives,
            };
        }).ToList();

        ScanAmbiguousYellowRefinementResult aiResult;
        try
        {
            aiResult = await _provider.RefineAmbiguousYellowMarksAsync(
                new ScanAmbiguousYellowRefinementRequest
                {
                    Playbook = buildRequest.Ingest.Playbook,
                    PlaceholderSet = buildRequest.PlaceholderSet,
                    SourceKind = buildRequest.Ingest.Input.SourceKind,
                    Marks = marks,
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return proposal;
        }

        if (aiResult.Marks.Count == 0)
            return proposal;

        var byId = aiResult.Marks.ToDictionary(static m => m.FieldId, StringComparer.Ordinal);
        var updated = proposal.Fields
            .Select(draft => ApplyAiResult(draft, byId, buildRequest.PlaceholderSet))
            .Select(draft => ScanRepresentativeNameGuard.RewriteDraft(
                draft,
                buildRequest.PlaceholderSet,
                buildRequest.ValueCandidates))
            .ToList();

        return new ScanFieldPlanProposal
        {
            Fields = updated,
            StaticRegions = proposal.StaticRegions,
            Gaps = proposal.Gaps,
            PendingQuestions = proposal.PendingQuestions,
            Rationale = AppendRationale(proposal.Rationale, aiResult.Rationale),
            Source = proposal.Source + "+" + aiResult.Source,
            YellowHighlightCount = proposal.YellowHighlightCount,
        };
    }

    private static ScanDetectedFieldDraft ApplyAiResult(
        ScanDetectedFieldDraft draft,
        IReadOnlyDictionary<string, ScanAmbiguousYellowMarkResult> aiById,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        if (!aiById.TryGetValue(draft.FieldId, out var ai))
            return draft;

        var token = SanitizeToken(ai.ProposedToken, placeholderSet);
        if (token == null && ai.Candidates.Count > 0)
            token = SanitizeToken(ai.Candidates[0].Token, placeholderSet);

        var alternatives = ai.Candidates.Count > 0
            ? ai.Candidates
            : draft.Alternatives;

        if (token == null)
        {
            return CopyDraft(draft, draft.ProposedToken, draft.Confidence,
                MergeAlternatives(draft.Alternatives, alternatives));
        }

        return CopyDraft(draft, token, ai.Confidence,
            MergeAlternatives(alternatives, draft.Alternatives));
    }

    private static ScanDetectedFieldDraft CopyDraft(
        ScanDetectedFieldDraft draft,
        string? proposedToken,
        ScanFieldConfidence confidence,
        IReadOnlyList<ScanTokenAlternative> alternatives) =>
        new()
        {
            FieldId = draft.FieldId,
            PageIndex = draft.PageIndex,
            LabelText = draft.LabelText,
            ProposedToken = proposedToken,
            Confidence = confidence,
            Scope = draft.Scope,
            Box = draft.Box,
            SourceRegion = draft.SourceRegion,
            ColumnHeader = draft.ColumnHeader,
            NearbyLabel = draft.NearbyLabel,
            Alternatives = alternatives,
        };

    private static string? SanitizeToken(string? token, ApplicationProfilePlaceholderSet placeholderSet)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var trimmed = token.Trim();
        if (trimmed.Contains("{{", StringComparison.Ordinal))
        {
            if (ContainsOnlyAllowedTokens(trimmed, placeholderSet))
                return ScanLibraryTokenRewriter.Rewrite(trimmed, placeholderSet);
            return null;
        }

        if (!TemplateTokenSyntax.TryGetShortCode(trimmed, out var code)
            || !placeholderSet.Contains(code))
            return null;

        var entry = placeholderSet.Allowed.First(e =>
            string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));
        var scope = entry.Scope == UserReportPlaceholderScope.Row
            ? UserReportPlaceholderScope.Row
            : UserReportPlaceholderScope.Header;
        return entry.BuildWordToken(scope);
    }

    private static bool ContainsOnlyAllowedTokens(string cellTemplate, ApplicationProfilePlaceholderSet placeholderSet)
    {
        var index = 0;
        var found = false;
        while (index < cellTemplate.Length)
        {
            var start = cellTemplate.IndexOf("{{", index, StringComparison.Ordinal);
            if (start < 0)
                break;

            var end = cellTemplate.IndexOf("}}", start + 2, StringComparison.Ordinal);
            if (end < 0)
                return false;

            var raw = cellTemplate[(start + 2)..end].Trim();
            if (raw.StartsWith('#') || raw.StartsWith('/'))
                raw = raw[1..].Trim();

            if (!TemplateTokenSyntax.TryGetShortCode("{{" + raw + "}}", out var code)
                || !placeholderSet.Contains(code))
                return false;

            found = true;
            index = end + 2;
        }

        return found;
    }

    private static IReadOnlyList<ScanTokenAlternative> MergeAlternatives(
        IReadOnlyList<ScanTokenAlternative> primary,
        IReadOnlyList<ScanTokenAlternative> secondary)
    {
        return primary
            .Concat(secondary)
            .GroupBy(static a => a.ShortCode, StringComparer.OrdinalIgnoreCase)
            .Select(static g => g.OrderByDescending(static a => a.ScorePercent).First())
            .OrderByDescending(static a => a.ScorePercent)
            .Take(6)
            .ToList();
    }

    private static string? AppendRationale(string? existing, string? added)
    {
        if (string.IsNullOrWhiteSpace(added))
            return existing;

        return string.IsNullOrWhiteSpace(existing) ? added : existing + "; " + added;
    }
}
