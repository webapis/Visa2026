using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Builds an E-D1-safe <see cref="TemplateMappingRequest"/> from the deterministic candidate report.
/// Masks identifier-like previews when configured so a future cloud adapter cannot see passport numbers.
/// </summary>
public static class TemplateMappingRequestBuilder
{
    public static TemplateMappingRequest FromCandidate(
        TemplateSourceFormat format,
        ApplicationProfilePlaceholderSet placeholderSet,
        TemplateCandidateReport candidate,
        bool redactIdentifiers = true)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        ArgumentNullException.ThrowIfNull(candidate);

        var regions = candidate.Highlights
            .Select(h =>
            {
                var kind = InferKind(h.MatchedText);
                return new DocumentExtractRegion(
                    h.Region,
                    MaskPreview(h.MatchedText, kind, redactIdentifiers),
                    kind,
                    h.RowIndex);
            })
            .ToList();

        var preMatched = candidate.Highlights
            .Where(static h => h.Kind == HighlightKind.Match && !string.IsNullOrWhiteSpace(h.Token))
            .Select(h => new DeterministicMatch(h.Region, h.Token!, h.ShortCode ?? string.Empty))
            .ToList();

        var allowed = placeholderSet.Allowed
            .Select(static e => new AllowedToken(e.ShortCode, e.LabelEn, e.Scope))
            .ToList();

        return new TemplateMappingRequest
        {
            Format = format,
            Regions = regions,
            AllowedTokens = allowed,
            PlaceholderSetFingerprint = placeholderSet.Fingerprint,
            PreMatched = preMatched,
        };
    }

    public static string MaskPreview(string text, ValueKind? kind, bool redactIdentifiers)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (!redactIdentifiers)
            return text;

        if (kind != ValueKind.Identifier && !LooksLikeIdentifier(text))
            return text;

        if (text.Length <= 4)
            return new string('*', text.Length);

        return string.Concat(text.AsSpan(0, 2), new string('*', text.Length - 4), text.AsSpan(text.Length - 2));
    }

    public static ValueKind? InferKind(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (LooksLikeIdentifier(text))
            return ValueKind.Identifier;

        return ValueKind.Text;
    }

    /// <summary>Six or more digits and digits are at least half the characters - passport / case-number shaped.</summary>
    private static bool LooksLikeIdentifier(string text)
    {
        var digits = 0;
        foreach (var ch in text)
        {
            if (char.IsDigit(ch))
                digits++;
        }

        return digits >= 6 && digits * 2 >= text.Length;
    }
}