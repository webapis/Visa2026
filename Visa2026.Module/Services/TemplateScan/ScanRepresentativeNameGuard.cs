#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// <c>RPFN</c> is the tenant <c>AuthorizedRepresentative</c> (wekil) slot — not a roster <c>Person</c>.
/// A printed wekil caption next to a person-shaped yellow maps to <c>RPFN</c> even when the sample
/// name is fictitious. Isolated person-shaped yellows stay <c>PFN</c> unless they exactly match the wekil.
/// </summary>
public static class ScanRepresentativeNameGuard
{
    public const string RepresentativeFullNameCode = "RPFN";
    public const string PersonFullNameCode = "PFN";

    public static ScanDetectedFieldDraft RewriteDraft(
        ScanDetectedFieldDraft draft,
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ValueCandidate>? instanceCandidates,
        HashSet<string>? usedHeaderCodes = null)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(placeholderSet);

        var nearbyRole = ScanLetterRoleHint.FromNearbyText(draft.NearbyLabel, draft.ColumnHeader);
        if (nearbyRole == ScanLetterRole.Wekil
            && ScanShapeTokenMatcher.LooksLikePersonFullName(draft.LabelText ?? string.Empty))
            return ForceRepresentative(draft, placeholderSet, usedHeaderCodes);

        if (!ShouldRewriteToPersonFullName(
                draft.LabelText,
                draft.ProposedToken,
                placeholderSet,
                instanceCandidates))
            return draft;

        if (TemplateTokenSyntax.TryGetShortCode(draft.ProposedToken, out var oldCode)
            && oldCode.Equals(RepresentativeFullNameCode, StringComparison.OrdinalIgnoreCase))
            usedHeaderCodes?.Remove(RepresentativeFullNameCode);

        if (!placeholderSet.Contains(PersonFullNameCode))
            return draft;

        var entry = placeholderSet.Allowed.First(e =>
            string.Equals(e.ShortCode, PersonFullNameCode, StringComparison.OrdinalIgnoreCase));

        return CopyDraft(
            draft,
            entry.BuildWordToken(UserReportPlaceholderScope.Row),
            ScanFieldScope.Row);
    }

    private static ScanDetectedFieldDraft ForceRepresentative(
        ScanDetectedFieldDraft draft,
        ApplicationProfilePlaceholderSet placeholderSet,
        HashSet<string>? usedHeaderCodes)
    {
        if (!placeholderSet.Contains(RepresentativeFullNameCode))
            return draft;

        if (TemplateTokenSyntax.TryGetShortCode(draft.ProposedToken, out var code)
            && code.Equals(RepresentativeFullNameCode, StringComparison.OrdinalIgnoreCase))
            return draft;

        if (code != null
            && code.Equals(PersonFullNameCode, StringComparison.OrdinalIgnoreCase))
            usedHeaderCodes?.Remove(PersonFullNameCode);

        var entry = placeholderSet.Allowed.First(e =>
            string.Equals(e.ShortCode, RepresentativeFullNameCode, StringComparison.OrdinalIgnoreCase));
        var token = entry.BuildWordToken(UserReportPlaceholderScope.Header);
        usedHeaderCodes?.Add(RepresentativeFullNameCode);
        return CopyDraft(
            draft,
            token,
            ScanFieldScope.Header,
            [
                new ScanTokenAlternative(
                    token,
                    RepresentativeFullNameCode,
                    90,
                    "Printed wekil caption — Authorized Representative, not roster Person"),
            ]);
    }

    private static ScanDetectedFieldDraft CopyDraft(
        ScanDetectedFieldDraft draft,
        string proposedToken,
        ScanFieldScope scope,
        IReadOnlyList<ScanTokenAlternative>? alternatives = null) =>
        new()
        {
            FieldId = draft.FieldId,
            PageIndex = draft.PageIndex,
            LabelText = draft.LabelText,
            ProposedToken = proposedToken,
            Confidence = draft.Confidence,
            Scope = scope,
            Box = draft.Box,
            SourceRegion = draft.SourceRegion,
            ColumnHeader = draft.ColumnHeader,
            NearbyLabel = draft.NearbyLabel,
            Alternatives = alternatives ?? draft.Alternatives,
        };

    public static IReadOnlyList<ScanDetectedFieldDraft> RewriteDrafts(
        IReadOnlyList<ScanDetectedFieldDraft> drafts,
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ValueCandidate>? instanceCandidates,
        HashSet<string>? usedHeaderCodes = null)
    {
        ArgumentNullException.ThrowIfNull(drafts);
        if (drafts.Count == 0)
            return drafts;

        var list = new List<ScanDetectedFieldDraft>(drafts.Count);
        foreach (var draft in drafts)
            list.Add(RewriteDraft(draft, placeholderSet, instanceCandidates, usedHeaderCodes));
        return list;
    }

    public static ScanFieldPlanProposal RewriteProposal(
        ScanFieldPlanProposal proposal,
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ValueCandidate>? instanceCandidates)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        return new ScanFieldPlanProposal
        {
            Fields = RewriteDrafts(proposal.Fields, placeholderSet, instanceCandidates),
            StaticRegions = proposal.StaticRegions,
            Gaps = proposal.Gaps,
            PendingQuestions = proposal.PendingQuestions,
            Rationale = proposal.Rationale,
            Source = proposal.Source,
            YellowHighlightCount = proposal.YellowHighlightCount,
        };
    }

    /// <summary>
    /// Keep <c>RPFN</c> only when the yellow text exactly matches the catalog wekil
    /// example (and/or instance <c>RPFN</c> when that example is absent) and is not also a roster <c>PFN</c>.
    /// </summary>
    public static bool ShouldKeepRepresentativeFullName(
        string? yellowText,
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ValueCandidate>? instanceCandidates)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        if (!ScanShapeTokenMatcher.LooksLikePersonFullName(yellowText ?? string.Empty))
            return true;

        if (IsExactRosterPerson(yellowText, instanceCandidates))
            return false;

        var yellowId = TemplateTextNormalizer.NormalizeIdentifier(yellowText);
        var catalogWekil = TemplateTextNormalizer.NormalizeIdentifier(
            placeholderSet.Allowed
                .FirstOrDefault(e => e.ShortCode.Equals(RepresentativeFullNameCode, StringComparison.OrdinalIgnoreCase))
                ?.ExampleValue);
        if (catalogWekil.Length >= TemplateTextNormalizer.MinimumMatchLength
            && yellowId.Length >= TemplateTextNormalizer.MinimumMatchLength
            && !string.Equals(catalogWekil, yellowId, StringComparison.Ordinal))
            return false;

        return IsExactWekilName(yellowText, placeholderSet, instanceCandidates);
    }

    public static bool ShouldRewriteToPersonFullName(
        string? yellowText,
        string? proposedToken,
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ValueCandidate>? instanceCandidates)
    {
        if (string.IsNullOrWhiteSpace(proposedToken)
            || !TemplateTokenSyntax.TryGetShortCode(proposedToken, out var code)
            || !code.Equals(RepresentativeFullNameCode, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!ScanShapeTokenMatcher.LooksLikePersonFullName(yellowText ?? string.Empty))
            return false;

        return !ShouldKeepRepresentativeFullName(yellowText, placeholderSet, instanceCandidates);
    }

    public static bool IsExactWekilName(
        string? yellowText,
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ValueCandidate>? instanceCandidates)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        var id = TemplateTextNormalizer.NormalizeIdentifier(yellowText);
        if (id.Length < TemplateTextNormalizer.MinimumMatchLength)
            return false;

        if (instanceCandidates != null)
        {
            foreach (var candidate in instanceCandidates)
            {
                if (!candidate.ShortCode.Equals(RepresentativeFullNameCode, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(
                        TemplateTextNormalizer.NormalizeIdentifier(candidate.RawValue),
                        id,
                        StringComparison.Ordinal))
                    return true;
            }
        }

        var example = placeholderSet.Allowed
            .FirstOrDefault(e => e.ShortCode.Equals(RepresentativeFullNameCode, StringComparison.OrdinalIgnoreCase))
            ?.ExampleValue;
        return example != null
            && string.Equals(TemplateTextNormalizer.NormalizeIdentifier(example), id, StringComparison.Ordinal);
    }

    private static bool IsExactRosterPerson(
        string? yellowText,
        IReadOnlyList<ValueCandidate>? instanceCandidates)
    {
        if (instanceCandidates == null || instanceCandidates.Count == 0)
            return false;

        var id = TemplateTextNormalizer.NormalizeIdentifier(yellowText);
        if (id.Length < TemplateTextNormalizer.MinimumMatchLength)
            return false;

        foreach (var candidate in instanceCandidates)
        {
            if (!candidate.ShortCode.Equals(PersonFullNameCode, StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.Equals(
                    TemplateTextNormalizer.NormalizeIdentifier(candidate.RawValue),
                    id,
                    StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}