#nullable enable

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Left-side form field names without a parenthetical caption (Sahsy kagyzy, Forma 16
/// numbered 1–15 labels, Excel headers, contract footer labels). Catalog ScoreHeader
/// still runs; these prefers lock the obvious codes.
/// </summary>
public static class ScanFormFieldLabelHints
{
    public static IReadOnlyList<string> PreferCodes(string? nearbyLabel, ScanLetterRole role)
    {
        var folded = StripLeadingItemNumber(TemplateTextNormalizer.NormalizeFolded(nearbyLabel));
        if (folded.Length < 3)
            return Array.Empty<string>();

        if (ScanOfficialLetterHints.LooksLikeMigrationAddressee(nearbyLabel))
            return ["MSRV"];

        if (ScanOfficialLetterHints.LooksLikeBranchDirectorTitle(nearbyLabel)
            || (folded.Contains("sahamca", StringComparison.Ordinal)
                && folded.Contains("mudiri", StringComparison.Ordinal)
                && !folded.Contains("mudirine", StringComparison.Ordinal)))
            return ["ACPOS"];

        if (folded.Contains("tertipde", StringComparison.Ordinal))
            return ["Urgency_NameTm"];

        if (folded.Contains("gezeklik", StringComparison.Ordinal))
            return ["VCAT"];

        if (folded.Contains("gelmegin", StringComparison.Ordinal))
            return ["RGEL"];

        if (folded.Contains("kabul edyan", StringComparison.Ordinal)
            || folded.Contains("kabul ediji", StringComparison.Ordinal))
            return ["ACNAM", "ACADR"];

        if (folded.Contains("giren", StringComparison.Ordinal) && folded.Contains("yeri", StringComparison.Ordinal))
            return ["TRCK"];

        if (folded.Contains("giren", StringComparison.Ordinal)
            && (folded.Contains("wagt", StringComparison.Ordinal) || folded.Contains("senesi", StringComparison.Ordinal)))
            return ["TRDT"];

        if (folded.Contains("baslanyan", StringComparison.Ordinal)
            || folded.Contains("visa start", StringComparison.Ordinal)
            || (folded.Contains("wiza", StringComparison.Ordinal)
                && folded.Contains("baslangyc", StringComparison.Ordinal)))
            return ["VSTD"];

        if (folded.Contains("wiza", StringComparison.Ordinal)
            && folded.Contains("berlen", StringComparison.Ordinal)
            && (folded.Contains("senesi", StringComparison.Ordinal) || folded.Contains("mohlet", StringComparison.Ordinal)))
            return ["VISD", "VSTD", "VEDT"];

        if (folded.Contains("wiza", StringComparison.Ordinal)
            && folded.Contains("berlen", StringComparison.Ordinal)
            && folded.Contains("yeri", StringComparison.Ordinal))
            return ["VPLC"];

        if (folded.Contains("wiza", StringComparison.Ordinal)
            && (folded.Contains("dereje", StringComparison.Ordinal)
                || folded.Contains("gornus", StringComparison.Ordinal)
                || folded.Contains("gorunsi", StringComparison.Ordinal)
                || folded.Contains("belgi", StringComparison.Ordinal)))
            return ["VCTM", "VTYP", "VNUM"];

        if (folded.Contains("bolyan yeri", StringComparison.Ordinal)
            || folded.Contains("bolyan yer", StringComparison.Ordinal)
            || (folded.Contains("turkmenistan", StringComparison.Ordinal)
                && folded.Contains("bolyan", StringComparison.Ordinal)))
            return ["ADRS"];

        if (folded.Contains("oy salgy", StringComparison.Ordinal)
            || folded.Equals("oy salgysy", StringComparison.Ordinal))
            return ["PFAC", "PFAD", "PFWC"];

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

        if (folded.Contains("is saparyna baryan yer", StringComparison.Ordinal)
            || folded.Contains("baryan yer", StringComparison.Ordinal)
            || folded.Contains("is saparynda boljak", StringComparison.Ordinal)
            || folded.Contains("business trip address", StringComparison.Ordinal)
            || folded.Contains("business trip destination", StringComparison.Ordinal))
            return ["BTAD"];

        if (folded.Contains("dasary", StringComparison.Ordinal)
            && (folded.Contains("salgy", StringComparison.Ordinal) || folded.Contains("yasa", StringComparison.Ordinal)))
            return ["PFAC", "PFAD", "PFWC"];

        if ((folded.Contains("turkmenistan", StringComparison.Ordinal)
                || folded.Contains("yasayan", StringComparison.Ordinal)
                || folded.Contains("yasayys", StringComparison.Ordinal)
                || folded.Contains("ikamet", StringComparison.Ordinal)
                || folded.Contains("residence", StringComparison.Ordinal))
            && (folded.Contains("salgy", StringComparison.Ordinal)
                || folded.Contains("adres", StringComparison.Ordinal)
                || folded.Contains("ikamet", StringComparison.Ordinal)))
            return ["ADRS"];

        if (folded.Contains("salgy", StringComparison.Ordinal)
            && !folded.Contains("yuridiki", StringComparison.Ordinal)
            && !folded.Contains("karhana", StringComparison.Ordinal))
            return ["ADRS"];

        if (folded.Contains("onki islan", StringComparison.Ordinal)
            || folded.Contains("onki isleyen", StringComparison.Ordinal))
            return ["PWTM"];

        if (folded.Contains("hunar", StringComparison.Ordinal)
            && !folded.Contains("bilim", StringComparison.Ordinal))
            return ["EGSP"];

        if (folded.Contains("wezipe", StringComparison.Ordinal)
            || folded.Equals("wezipesi", StringComparison.Ordinal))
            return role == ScanLetterRole.Signatory ? ["ACPOS", "POSN"] : ["POSN"];

        if (folded.Contains("doglan senesi we yeri", StringComparison.Ordinal))
            return ["PDBT", "PCBT", "PBPL"];

        if (folded.Contains("doglan senesi", StringComparison.Ordinal)
            && !folded.Contains("yeri", StringComparison.Ordinal)
            && !folded.Contains("yurdy", StringComparison.Ordinal))
            return ["PDBT"];

        if (folded.Contains("doglan", StringComparison.Ordinal)
            && (folded.Contains("yeri", StringComparison.Ordinal) || folded.Contains("yurdy", StringComparison.Ordinal)))
            return ["PCBC", "PBPL"];

        if (folded.Contains("bilimi we okan yeri", StringComparison.Ordinal)
            || (folded.Contains("bilimi", StringComparison.Ordinal)
                && folded.Contains("okan yeri", StringComparison.Ordinal)))
            return ["EGLV", "EGIN"];

        if (folded.Contains("okan yeri", StringComparison.Ordinal)
            && !folded.Contains("bilimi", StringComparison.Ordinal))
            return ["EGIN"];

        if (folded.Contains("bilimi", StringComparison.Ordinal))
            return ["EGLV", "EGCC", "EGIN"];

        if (folded.Contains("pasport", StringComparison.Ordinal)
            && folded.Contains("belgi", StringComparison.Ordinal)
            && !folded.Contains("mohlet", StringComparison.Ordinal))
        {
            return role switch
            {
                ScanLetterRole.Signatory => ["CHPN", "CHPD", "CHPE"],
                ScanLetterRole.Wekil => ["RPPN", "RPPA", "RPPH"],
                _ => ["PPN", "PPED"],
            };
        }

        if (folded.Contains("pasport", StringComparison.Ordinal)
            && folded.Contains("mohlet", StringComparison.Ordinal))
        {
            return role switch
            {
                ScanLetterRole.Signatory => ["CHPD", "CHPE"],
                ScanLetterRole.Wekil => ["RPPD"],
                _ => ["PPIS", "PPED"],
            };
        }

        if (folded.Contains("familiyasy", StringComparison.Ordinal)
            && folded.Contains("ady", StringComparison.Ordinal))
            return [ScanFormCaptionHints.RemapByRole("PFN", role)];

        if (folded.Equals("familiyasy", StringComparison.Ordinal))
            return ["PLN"];

        if (folded.Equals("ady", StringComparison.Ordinal))
            return ["PFNM"];

        if (folded.Contains("sertnama", StringComparison.Ordinal)
            && (folded.Contains("mohlet", StringComparison.Ordinal)
                || folded.Contains("hereket", StringComparison.Ordinal)
                || folded.Contains("zahmet", StringComparison.Ordinal)))
            return ["CSDT", "CEDT"];

        if (folded.Contains("aylyk", StringComparison.Ordinal)
            || folded.Contains("aylygy", StringComparison.Ordinal)
            || folded.Contains("salary", StringComparison.Ordinal)
            || folded.Contains("waluta", StringComparison.Ordinal)
            || folded.Contains("currency", StringComparison.Ordinal))
            return ["CSAL", "CCUR"];

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
        if (trimmed.Length is < 3 or > 160)
            return false;

        var folded = StripLeadingItemNumber(TemplateTextNormalizer.NormalizeFolded(trimmed));
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
            || folded.Equals("ady", StringComparison.Ordinal)
            || folded.Contains("familiyasy", StringComparison.Ordinal)
            || folded.Contains("doglan", StringComparison.Ordinal)
            || folded.Contains("pasport", StringComparison.Ordinal)
            || folded.Contains("cagyran tarap", StringComparison.Ordinal)
            || folded.Equals("isgar", StringComparison.Ordinal)
            || folded.Contains("is beriji", StringComparison.Ordinal)
            || folded.Equals("mudiri", StringComparison.Ordinal)
            || ScanOfficialLetterHints.LooksLikeBranchDirectorTitle(trimmed)
            || ScanOfficialLetterHints.LooksLikeMigrationAddressee(trimmed)
            || folded.Contains("wiza", StringComparison.Ordinal)
            || folded.Contains("giren", StringComparison.Ordinal)
            || folded.Contains("kabul edyan", StringComparison.Ordinal)
            || folded.Contains("bolyan yeri", StringComparison.Ordinal)
            || folded.Contains("oy salgy", StringComparison.Ordinal)
            || folded.Contains("baryan yer", StringComparison.Ordinal)
            || folded.Contains("is saparynda boljak", StringComparison.Ordinal);
    }

    internal static string StripLeadingItemNumber(string folded)
    {
        if (string.IsNullOrEmpty(folded))
            return folded ?? string.Empty;

        var i = 0;
        while (i < folded.Length && char.IsDigit(folded[i]))
            i++;
        if (i is < 1 or > 2)
            return folded;
        if (i >= folded.Length)
            return folded;
        if (folded[i] is not ('.' or ')' or ':'))
            return folded;
        i++;
        while (i < folded.Length && folded[i] == ' ')
            i++;
        return i >= folded.Length ? folded : folded[i..];
    }
}