#nullable enable

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Printed-label role for a Word yellow mark (letter captions, not the sample name).
/// </summary>
public enum ScanLetterRole
{
    Unknown = 0,
    Applicant = 1,
    Signatory = 2,
    Wekil = 3,
}

public static class ScanLetterRoleHint
{
    public static ScanLetterRole FromNearbyText(params string?[] fragments)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(
            string.Join(" ", fragments.Where(static f => !string.IsNullOrWhiteSpace(f))));
        if (folded.Length == 0)
            return ScanLetterRole.Unknown;

        if (LooksLikeWekil(folded))
            return ScanLetterRole.Wekil;
        if (LooksLikeSignatory(folded))
            return ScanLetterRole.Signatory;
        if (LooksLikeApplicant(folded))
            return ScanLetterRole.Applicant;

        return ScanLetterRole.Unknown;
    }

    public static bool LooksLikeWekil(string? foldedOrRaw)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(foldedOrRaw);
        return folded.Contains("wekil", StringComparison.Ordinal)
            || folded.Contains("ygtyyarly wezipeli", StringComparison.Ordinal)
            || folded.Contains("representativ", StringComparison.Ordinal)
            || folded.Contains("authorized represent", StringComparison.Ordinal);
    }

    private static bool LooksLikeSignatory(string folded) =>
        folded.Contains("yolbascy", StringComparison.Ordinal)
        || folded.Contains("gol cekiji", StringComparison.Ordinal)
        || folded.Contains("signatory", StringComparison.Ordinal)
        || folded.Contains("company head", StringComparison.Ordinal);

    private static bool LooksLikeApplicant(string folded) =>
        folded.Contains("doglan senesi", StringComparison.Ordinal)
        || folded.Contains("atasynyn ady", StringComparison.Ordinal);
}
