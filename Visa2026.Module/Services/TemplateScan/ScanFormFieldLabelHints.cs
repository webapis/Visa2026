#nullable enable

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Left-side form field names without a parenthetical caption (Sahsy kagyzy, Excel headers,
/// contract footer labels). Catalog ScoreHeader still runs; these prefers lock the obvious codes.
/// </summary>
public static class ScanFormFieldLabelHints
{
    public static IReadOnlyList<string> PreferCodes(string? nearbyLabel, ScanLetterRole role)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(nearbyLabel);
        if (folded.Length < 3)
            return Array.Empty<string>();

        if (folded.Contains("gelmegin", StringComparison.Ordinal))
            return ["RGEL"];

        if (folded.Contains("cagyran tarap", StringComparison.Ordinal)
            || folded.Contains("cagyrjan tarap", StringComparison.Ordinal))
            return ["ACNAM"];

        if (folded.Contains("sahsy belgi", StringComparison.Ordinal)
            || folded.Equals("sahsy belgisi", StringComparison.Ordinal))
            return ["PPIN"];

        if (folded.Contains("rayatlyg", StringComparison.Ordinal)
            && !folded.Contains("doglan", StringComparison.Ordinal))
            return ["PNAT"];

        if (folded.Contains("jynsy", StringComparison.Ordinal))
            return ["PGND"];

        if (folded.Contains("masgala", StringComparison.Ordinal))
            return ["PVFM"];

        if (folded.Contains("dasary", StringComparison.Ordinal)
            && (folded.Contains("salgy", StringComparison.Ordinal) || folded.Contains("yasa", StringComparison.Ordinal)))
            return ["PFWC", "PFAD", "PFAC"];

        if (folded.Contains("onki islan", StringComparison.Ordinal)
            || folded.Contains("onki isleyen", StringComparison.Ordinal))
            return ["PWTM"];

        if (folded.Contains("hunar", StringComparison.Ordinal)
            && !folded.Contains("bilim", StringComparison.Ordinal))
            return ["EGSP"];

        if (folded.Contains("wezipe", StringComparison.Ordinal)
            || folded.Equals("wezipesi", StringComparison.Ordinal))
            return role == ScanLetterRole.Signatory ? ["ACPOS", "POSN"] : ["POSN"];

        if (folded.Contains("doglan senesi we yeri", StringComparison.Ordinal)
            || (folded.Contains("doglan", StringComparison.Ordinal) && folded.Contains("yeri", StringComparison.Ordinal)))
            return ["PDBT", "PCBC", "PBPL"];

        if (folded.Contains("bilimi", StringComparison.Ordinal)
            || folded.Contains("okan yeri", StringComparison.Ordinal))
            return ["EGLV", "EGCC", "EGIN"];

        if (folded.Contains("pasport", StringComparison.Ordinal)
            && (folded.Contains("belgi", StringComparison.Ordinal) || folded.Contains("mohlet", StringComparison.Ordinal)))
        {
            return role switch
            {
                ScanLetterRole.Signatory => ["CHPN", "CHPD", "CHPE"],
                ScanLetterRole.Wekil => ["RPPN", "RPPA", "RPPH"],
                _ => ["PPN", "PPIS", "PPED"],
            };
        }

        if (folded.Contains("familiyasy", StringComparison.Ordinal)
            && folded.Contains("ady", StringComparison.Ordinal))
            return [ScanFormCaptionHints.RemapByRole("PFN", role)];

        if (folded.Equals("familiyasy", StringComparison.Ordinal))
            return ["PLN"];

        if (folded.Equals("ady", StringComparison.Ordinal))
            return ["PFNM"];

        if (folded.Contains("aylyk", StringComparison.Ordinal)
            || folded.Contains("aylygy", StringComparison.Ordinal)
            || folded.Contains("salary", StringComparison.Ordinal))
            return ["CSAL"];

        if (folded.Contains("is beriji", StringComparison.Ordinal)
            || folded.Equals("mudiri", StringComparison.Ordinal))
            return ["CHFN", "ACFNM"];

        if (folded.Equals("isgar", StringComparison.Ordinal)
            || folded.Equals("isgari", StringComparison.Ordinal))
            return ["PFN"];

        return Array.Empty<string>();
    }

    public static bool LooksLikeFormFieldLabel(string? text)
    {
        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length is < 3 or > 120)
            return false;

        var folded = TemplateTextNormalizer.NormalizeFolded(trimmed);
        return folded.Contains("rayatlyg", StringComparison.Ordinal)
            || folded.Contains("sahsy belgi", StringComparison.Ordinal)
            || folded.Contains("bilim", StringComparison.Ordinal)
            || folded.Contains("hunar", StringComparison.Ordinal)
            || folded.Contains("wezipe", StringComparison.Ordinal)
            || folded.Contains("masgala", StringComparison.Ordinal)
            || folded.Contains("salgy", StringComparison.Ordinal)
            || folded.Contains("onki islan", StringComparison.Ordinal)
            || folded.Contains("gelmegin", StringComparison.Ordinal)
            || folded.Contains("jynsy", StringComparison.Ordinal)
            || folded.Contains("okan yeri", StringComparison.Ordinal)
            || folded.Contains("familiyasy", StringComparison.Ordinal)
            || folded.Contains("doglan", StringComparison.Ordinal)
            || folded.Contains("pasport", StringComparison.Ordinal)
            || folded.Contains("cagyran tarap", StringComparison.Ordinal)
            || folded.Equals("isgar", StringComparison.Ordinal)
            || folded.Contains("is beriji", StringComparison.Ordinal)
            || folded.Equals("mudiri", StringComparison.Ordinal);
    }
}