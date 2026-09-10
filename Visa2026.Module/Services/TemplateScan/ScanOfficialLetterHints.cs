#nullable enable

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Cover-letter yellows that are the printed block itself (addressee / branch
/// director title), not a left-side form caption next to a sample value.
/// </summary>
public static class ScanOfficialLetterHints
{
    public static bool LooksLikeMigrationAddressee(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        if (folded.Length < 8)
            return false;

        if (folded.Contains("dowlet migrasi", StringComparison.Ordinal)
            || (folded.Contains("migrasi", StringComparison.Ordinal)
                && folded.Contains("gullug", StringComparison.Ordinal)))
            return true;

        return folded.Contains("mudirine", StringComparison.Ordinal)
            && (folded.Contains("migrasi", StringComparison.Ordinal)
                || folded.Contains("mudirlig", StringComparison.Ordinal));
    }

    public static bool LooksLikeBranchDirectorTitle(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        if (folded.Length < 6)
            return false;
        if (LooksLikeMigrationAddressee(text)
            || folded.Contains("mudirine", StringComparison.Ordinal)
            || folded.Contains("mudirlig", StringComparison.Ordinal))
            return false;

        if (folded.Contains("sahamca", StringComparison.Ordinal)
            && folded.Contains("mudiri", StringComparison.Ordinal))
            return true;

        return folded.Equals("mudiri", StringComparison.Ordinal)
            || folded.Equals("sahamcasynyn mudiri", StringComparison.Ordinal);
    }

    public static bool LooksLikeLetterBlock(string? text) =>
        LooksLikeMigrationAddressee(text) || LooksLikeBranchDirectorTitle(text);
}