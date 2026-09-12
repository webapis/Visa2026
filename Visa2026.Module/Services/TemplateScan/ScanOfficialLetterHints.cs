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

    /// <summary>
    /// Printed visa-cancel count: <c>1 (bir) sany wizasyny ýatyrmak</c>.
    /// Not person count (<c>daşary ýurt raýaty</c>) and not visa period (<c>N aý</c>).
    /// </summary>
    public static bool LooksLikeCancelVisaCount(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        if (folded.Length < 4 || !folded.Contains("wiza", StringComparison.Ordinal))
            return false;

        return folded.Contains("yatyr", StringComparison.Ordinal);
    }

    /// <summary>Printed person-count phrase: <c>daşary ýurt raýaty</c>.</summary>
    public static bool LooksLikePersonCountPhrase(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        if (folded.Length < 4)
            return false;
        if (folded.Contains("rayat", StringComparison.Ordinal))
            return true;

        return folded.Contains("dasary yurt", StringComparison.Ordinal)
            && (folded.Contains("adam", StringComparison.Ordinal)
                || folded.Contains("sany", StringComparison.Ordinal));
    }

    /// <summary>
    /// Printed work-permit cancel count: <c>1 (bir) sany iş rugsatnamasyny ýatyrmak</c>.
    /// </summary>
    public static bool LooksLikeCancelWorkPermitCount(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        if (folded.Length < 4 || !folded.Contains("yatyr", StringComparison.Ordinal))
            return false;

        return folded.Contains("rugsat", StringComparison.Ordinal)
            || folded.Contains("rugsad", StringComparison.Ordinal);
    }

    /// <summary>
    /// When both person and visa-cancel phrases appear, the earlier phrase wins
    /// so the first <c>1 (bir)</c> stays TPCNT and the one before <c>wizasy ýatyrmak</c> is CVCNT.
    /// </summary>
    public static bool PrefersCancelVisaCount(string? text)
    {
        if (!LooksLikeCancelVisaCount(text))
            return false;
        if (LooksLikeCancelWorkPermitCount(text) && WorkPermitPhraseStartsBeforeVisa(text))
            return false;
        if (!LooksLikePersonCountPhrase(text))
            return true;

        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        var personIdx = FirstIndex(folded, "rayat", "dasary yurt");
        var visaIdx = folded.IndexOf("wiza", StringComparison.Ordinal);
        return visaIdx >= 0 && (personIdx < 0 || visaIdx < personIdx);
    }

    /// <summary>
    /// When visa-cancel and work-permit-cancel phrases appear, the earlier document wins
    /// so <c>wizasy</c> stays CVCNT and <c>iş rugsatnamasyny ýatyrmak</c> is CWCNT.
    /// Person <c>daşary ýurt raýaty</c> still wins when it is first.
    /// </summary>
    public static bool PrefersCancelWorkPermitCount(string? text)
    {
        if (!LooksLikeCancelWorkPermitCount(text))
            return false;
        if (LooksLikeCancelVisaCount(text) && !WorkPermitPhraseStartsBeforeVisa(text))
            return false;
        if (!LooksLikePersonCountPhrase(text))
            return true;

        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        var personIdx = FirstIndex(folded, "rayat", "dasary yurt");
        var wpIdx = FirstWorkPermitPhraseIndex(folded);
        return wpIdx >= 0 && (personIdx < 0 || wpIdx < personIdx);
    }

    public static bool PrefersDocumentCancelCount(string? text) =>
        PrefersCancelWorkPermitCount(text) || PrefersCancelVisaCount(text);

    public static (string CountCode, string WordsCode) ResolveCountTokenCodes(string? text)
    {
        if (PrefersCancelWorkPermitCount(text))
            return ("CWCNT", "CWCTX");
        if (PrefersCancelVisaCount(text))
            return ("CVCNT", "CVCTX");
        return ("TPCNT", "TPCTX");
    }

    /// <summary>
    /// Nearby text is a person or document-cancel count caption, so an isolated
    /// <c>3</c> / <c>üç</c> yellow can be mapped without the pair in one span.
    /// </summary>
    public static bool LooksLikeCountContext(string? text) =>
        LooksLikePersonCountPhrase(text)
        || LooksLikeCancelVisaCount(text)
        || LooksLikeCancelWorkPermitCount(text);

    private static bool WorkPermitPhraseStartsBeforeVisa(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        var visaIdx = folded.IndexOf("wiza", StringComparison.Ordinal);
        var wpIdx = FirstWorkPermitPhraseIndex(folded);
        return wpIdx >= 0 && (visaIdx < 0 || wpIdx < visaIdx);
    }

    private static int FirstWorkPermitPhraseIndex(string folded) =>
        FirstIndex(folded, "rugsat", "rugsad");

    private static int FirstIndex(string folded, params string[] needles)
    {
        var best = -1;
        foreach (var needle in needles)
        {
            var i = folded.IndexOf(needle, StringComparison.Ordinal);
            if (i >= 0 && (best < 0 || i < best))
                best = i;
        }

        return best;
    }
}