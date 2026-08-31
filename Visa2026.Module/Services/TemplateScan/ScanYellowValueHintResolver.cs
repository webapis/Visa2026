#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Maps yellow Office cell/span text to library placeholders by matching case instance values
/// (same value map as Convert). Used for roster Excel and filled samples where literals are person data.
/// </summary>
public static class ScanYellowValueHintResolver
{
    private static readonly Dictionary<string, int> ShortCodePreference = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PNAT"] = 0,
        ["PCBC"] = 1,
        ["PFAC"] = 2,
        ["PDBT"] = 0,
        ["ADAT"] = 10,
        ["EGIY"] = 0,
        ["EGLV"] = 5,
        ["EGSP"] = 1,
        ["PFAD"] = 0,
        ["PFWC"] = 1,
        ["ADRS"] = 2,
    };

    public static IReadOnlyList<ScanDetectedFieldDraft> Resolve(
        string? yellowText,
        int pageIndex,
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyList<ValueCandidate> valueCandidates,
        HashSet<string> usedHeaderShortCodes,
        DocumentRegion? sourceRegion = null)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        ArgumentNullException.ThrowIfNull(valueCandidates);
        ArgumentNullException.ThrowIfNull(usedHeaderShortCodes);

        var text = TemplateTextNormalizer.Normalize(yellowText);
        if (text.Length == 0 || valueCandidates.Count == 0)
            return Array.Empty<ScanDetectedFieldDraft>();

        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        var identifier = TemplateTextNormalizer.NormalizeIdentifier(text);
        if (folded.Length < TemplateTextNormalizer.MinimumMatchLength
            && identifier.Length < TemplateTextNormalizer.MinimumMatchLength)
            return Array.Empty<ScanDetectedFieldDraft>();

        var matches = valueCandidates
            .Where(c => MatchesWholeCell(folded, identifier, c))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<ScanDetectedFieldDraft>();

        var winner = PickBest(matches);
        if (winner == null
            || !TemplateTokenSyntax.TryGetShortCode(winner.Token, out var code)
            || !placeholderSet.Contains(code))
            return Array.Empty<ScanDetectedFieldDraft>();

        var entry = placeholderSet.Allowed.First(e =>
            string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));

        var usageScope = ResolveUsageScope(entry, winner);
        var isRow = usageScope == UserReportPlaceholderScope.Row;
        if (!isRow && !usedHeaderShortCodes.Add(code))
            return Array.Empty<ScanDetectedFieldDraft>();

        return
        [
            new ScanDetectedFieldDraft
            {
                FieldId = Guid.NewGuid().ToString("N"),
                PageIndex = pageIndex,
                LabelText = yellowText?.Trim() ?? text,
                ProposedToken = entry.BuildWordToken(usageScope),
                Confidence = ScanFieldConfidence.High,
                Scope = isRow ? ScanFieldScope.Row : ScanFieldScope.Header,
                Box = ScanBoundingBox.FullPage,
                SourceRegion = sourceRegion,
            },
        ];
    }

    private static UserReportPlaceholderScope ResolveUsageScope(
        UserReportPlaceholderCatalogEntry entry,
        ValueCandidate winner)
    {
        return entry.Scope switch
        {
            UserReportPlaceholderScope.Row => UserReportPlaceholderScope.Row,
            UserReportPlaceholderScope.Header => UserReportPlaceholderScope.Header,
            _ => winner.RowIndex.HasValue
                ? UserReportPlaceholderScope.Row
                : UserReportPlaceholderScope.Header,
        };
    }

    private static bool MatchesWholeCell(string folded, string identifier, ValueCandidate candidate)
    {
        foreach (var key in candidate.MatchKeys)
        {
            if (key.Length == 0)
                continue;

            if (string.Equals(folded, key, StringComparison.Ordinal)
                || string.Equals(identifier, key, StringComparison.Ordinal))
                return true;
        }

        var rawFolded = TemplateTextNormalizer.NormalizeFolded(candidate.RawValue);
        var rawIdentifier = TemplateTextNormalizer.NormalizeIdentifier(candidate.RawValue);
        if (rawFolded.Length >= TemplateTextNormalizer.MinimumMatchLength)
        {
            if (string.Equals(folded, rawFolded, StringComparison.Ordinal)
                || string.Equals(identifier, rawIdentifier, StringComparison.Ordinal))
                return true;

            if (TryLongTextContains(rawFolded, folded) || TryLongTextContains(folded, rawFolded))
                return true;
        }

        return false;
    }

    private static bool TryLongTextContains(string longer, string shorter)
    {
        if (shorter.Length < TemplateTextNormalizer.MinimumMatchLength || longer.Length < shorter.Length)
            return false;

        return longer.Contains(shorter, StringComparison.Ordinal);
    }

    private static ValueCandidate? PickBest(IReadOnlyList<ValueCandidate> matches)
    {
        if (matches.Count == 0)
            return null;

        return matches
            .OrderBy(static c => ShortCodePreference.GetValueOrDefault(c.ShortCode, 50))
            .ThenByDescending(static c => c.RowIndex.HasValue)
            .ThenByDescending(static c => c.Kind == ValueKind.PersonName)
            .ThenByDescending(static c => c.RawValue?.Length ?? 0)
            .ThenBy(static c => c.ShortCode, StringComparer.OrdinalIgnoreCase)
            .First();
    }
}
