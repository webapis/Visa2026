#nullable enable

using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    /// Printed invitation-cancel count: <c>3 (üç) sany çakylygyny ýatyrmak</c>.
    /// </summary>
    public static bool LooksLikeCancelInvitationCount(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        if (folded.Length < 4 || !folded.Contains("yatyr", StringComparison.Ordinal))
            return false;

        return folded.Contains("cakyly", StringComparison.Ordinal)
            || folded.Contains("invitation", StringComparison.Ordinal);
    }

    /// <summary>
    /// Printed trip length: <c>2 (iki) gün möhlet</c>. Not person count
    /// (<c>daşary ýurt raýaty</c>) and not visa period (<c>N aý</c>).
    /// </summary>
    public static bool LooksLikeDurationCount(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        if (folded.Length < 3)
            return false;

        return folded.Contains("gun", StringComparison.Ordinal)
            && (folded.Contains("mohlet", StringComparison.Ordinal)
                || folded.Contains("cenli", StringComparison.Ordinal)
                || folded.Contains("is sapary", StringComparison.Ordinal));
    }

    /// <summary>
    /// When person count and duration appear, the earlier phrase wins
    /// so the first <c>1 (bir)</c> stays TPCNT and <c>2 (iki) gün</c> is BTDCNT.
    /// </summary>
    public static bool PrefersDurationCount(string? text)
    {
        if (!LooksLikeDurationCount(text))
            return false;
        if (!LooksLikePersonCountPhrase(text)
            && !LooksLikeCancelVisaCount(text)
            && !LooksLikeCancelWorkPermitCount(text)
            && !LooksLikeCancelInvitationCount(text))
            return true;

        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        var gunIdx = folded.IndexOf("gun", StringComparison.Ordinal);
        if (gunIdx < 0)
            return false;

        var personIdx = FirstIndex(folded, "rayat", "dasary yurt");
        var visaIdx = folded.IndexOf("wiza", StringComparison.Ordinal);
        var wpIdx = FirstWorkPermitPhraseIndex(folded);
        var invitationIdx = FirstInvitationPhraseIndex(folded);
        return (personIdx < 0 || gunIdx < personIdx)
            && (visaIdx < 0 || gunIdx < visaIdx)
            && (wpIdx < 0 || gunIdx < wpIdx)
            && (invitationIdx < 0 || gunIdx < invitationIdx);
    }

    /// <summary>
    /// Letter dates: <c>12.02.2026-den</c> → start, <c>13.02.2026-ne çenli</c> → end,
    /// otherwise application date (ADAT).
    /// </summary>
    public static string ResolveLetterDateTokenCode(
        string? afterText,
        string? nearbyLabel = null,
        ISet<string>? usedShortCodes = null)
    {
        if (LooksLikeTripStartDate(afterText) || LooksLikeTripStartDate(nearbyLabel))
            return "BTSD";
        if (LooksLikeTripEndDate(afterText) || LooksLikeTripEndDate(nearbyLabel))
            return "BTED";
        if (usedShortCodes != null
            && usedShortCodes.Contains("ADAT")
            && !LooksLikeCompanyRegistrationDate(afterText)
            && !LooksLikeCompanyRegistrationDate(nearbyLabel))
        {
            return usedShortCodes.Contains("BTSD") ? "BTED" : "BTSD";
        }

        return "ADAT";
    }

    private static bool LooksLikeCompanyRegistrationDate(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        return folded.Contains("hasaba alys", StringComparison.Ordinal)
            || folded.Contains("tescil", StringComparison.Ordinal);
    }

    public static bool LooksLikeTripStartDate(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        if (folded.Length == 0)
            return false;

        var trimmed = folded.TrimStart('-', ' ', '\t');
        return trimmed.StartsWith("den", StringComparison.Ordinal)
            || trimmed.StartsWith("dan", StringComparison.Ordinal);
    }

    public static bool LooksLikeTripEndDate(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        if (folded.Length == 0)
            return false;

        var trimmed = folded.TrimStart('-', ' ', '\t');
        return trimmed.StartsWith("ne", StringComparison.Ordinal)
            || trimmed.StartsWith("cenli", StringComparison.Ordinal);
    }

    public static bool LooksLikePurposeParagraph(string? text, string? nearbyLabel = null)
    {
        var nearby = TemplateTextNormalizer.NormalizeFolded(nearbyLabel);
        if (nearby.Contains("maksady", StringComparison.Ordinal))
            return (text?.Trim().Length ?? 0) >= 12;

        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        return folded.Contains("maksady", StringComparison.Ordinal) && folded.Length >= 24;
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
        if (LooksLikeCancelInvitationCount(text) && InvitationPhraseStartsBeforeVisa(text))
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
        if (LooksLikeCancelInvitationCount(text) && !WorkPermitPhraseStartsBeforeInvitation(text))
            return false;
        if (!LooksLikePersonCountPhrase(text))
            return true;

        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        var personIdx = FirstIndex(folded, "rayat", "dasary yurt");
        var wpIdx = FirstWorkPermitPhraseIndex(folded);
        return wpIdx >= 0 && (personIdx < 0 || wpIdx < personIdx);
    }

    /// <summary>
    /// When invitation-cancel and WP/visa phrases appear, the earlier document wins
    /// so <c>iş rugsatnamasyny</c> stays CWCNT and <c>çakylygyny ýatyrmak</c> is CICNT.
    /// </summary>
    public static bool PrefersCancelInvitationCount(string? text)
    {
        if (!LooksLikeCancelInvitationCount(text))
            return false;
        if (LooksLikeCancelWorkPermitCount(text) && WorkPermitPhraseStartsBeforeInvitation(text))
            return false;
        if (LooksLikeCancelVisaCount(text) && !InvitationPhraseStartsBeforeVisa(text))
            return false;
        if (!LooksLikePersonCountPhrase(text))
            return true;

        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        var personIdx = FirstIndex(folded, "rayat", "dasary yurt");
        var invitationIdx = FirstInvitationPhraseIndex(folded);
        return invitationIdx >= 0 && (personIdx < 0 || invitationIdx < personIdx);
    }

    public static bool PrefersDocumentCancelCount(string? text) =>
        PrefersCancelWorkPermitCount(text)
        || PrefersCancelVisaCount(text)
        || PrefersCancelInvitationCount(text);

    public static (string CountCode, string WordsCode) ResolveCountTokenCodes(string? text)
    {
        if (PrefersCancelWorkPermitCount(text))
            return ("CWCNT", "CWCTX");
        if (PrefersCancelVisaCount(text))
            return ("CVCNT", "CVCTX");
        if (PrefersCancelInvitationCount(text))
            return ("CICNT", "CICTX");
        if (PrefersDurationCount(text))
            return ("BTDCNT", "BTDCTX");
        return ("TPCNT", "TPCTX");
    }

    /// <summary>
    /// Isolated yellow <c>2</c> / <c>iki</c> in the same sentence as person
    /// <c>1 (bir)</c>: the first pair keeps TPCNT; the later mark next to
    /// <c>gün</c> is BTDCNT (not a duplicate person count).
    /// </summary>
    public static (string CountCode, string WordsCode) ResolveIsolatedCountTokenCodes(
        string? text,
        ISet<string>? usedShortCodes)
    {
        var pair = ResolveCountTokenCodes(text);
        if (usedShortCodes == null
            || !usedShortCodes.Contains(pair.CountCode))
            return pair;

        if (PrefersCancelWorkPermitCount(text) && !usedShortCodes.Contains("CWCNT"))
            return ("CWCNT", "CWCTX");
        if (PrefersCancelVisaCount(text) && !usedShortCodes.Contains("CVCNT"))
            return ("CVCNT", "CVCTX");
        if (PrefersCancelInvitationCount(text) && !usedShortCodes.Contains("CICNT"))
            return ("CICNT", "CICTX");
        if (LooksLikeDurationHint(text) && !usedShortCodes.Contains("BTDCNT"))
            return ("BTDCNT", "BTDCTX");

        return pair;
    }

    public static bool LooksLikeDurationHint(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        return folded.Contains("gun", StringComparison.Ordinal)
            || folded.Contains("mohlet", StringComparison.Ordinal);
    }

    public static bool LooksLikeIsolatedCountDigit(string? text)
    {
        var inner = (text ?? string.Empty).Trim();
        return inner.Length is >= 1 and <= 3 && inner.All(char.IsDigit);
    }

    public static bool LooksLikeIsolatedCountWords(string? text)
    {
        var inner = (text ?? string.Empty).Trim().Trim('(', ')').Trim();
        if (inner.Length == 0)
            return false;

        var folded = TemplateTextNormalizer.NormalizeFolded(inner);
        return IsolatedCountWordFolds.Contains(folded);
    }

    public static bool LooksLikeIsolatedCountMark(string? text) =>
        LooksLikeIsolatedCountDigit(text) || LooksLikeIsolatedCountWords(text);

    /// <summary>
    /// Keep only this <c>N (words)</c> pair’s caption. A later
    /// <c>2 (iki) gün</c> in the same sentence must not steal person
    /// <c>1 (bir)</c> (or the reverse).
    /// </summary>
    public static string? ClipImmediateCountContext(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var raw = text.Trim();
        var leading = LeadingCountPair.Match(raw);
        var searchFrom = leading.Success ? leading.Length : 0;
        if (searchFrom >= raw.Length)
            return raw;

        var later = LaterCountPair.Match(raw, searchFrom);
        if (!later.Success)
            return raw;

        var clipped = raw[..later.Index].Trim();
        return clipped.Length > 0 ? clipped : raw;
    }

    private static readonly HashSet<string> IsolatedCountWordFolds = new(StringComparer.Ordinal)
    {
        "nol", "bir", "iki", "uc", "dort", "bas", "alty", "yedi", "sekiz", "dokuz",
        "on", "on bir", "on iki", "on uc", "on dort", "on bas", "on alty", "on yedi",
        "on sekiz", "on dokuz",
        "yigrimi", "otuz", "kyrk", "elli", "altmys", "yetmis", "segsen", "togsan",
    };

    private static readonly Regex LeadingCountPair = new(
        @"^\s*(?:\d{1,3}\s*)?\([^)]{0,40}\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LaterCountPair = new(
        @"\d{1,3}\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Nearby text is a person or document-cancel count caption, so an isolated
    /// <c>3</c> / <c>üç</c> yellow can be mapped without the pair in one span.
    /// </summary>
    public static bool LooksLikeCountContext(string? text) =>
        LooksLikePersonCountPhrase(text)
        || LooksLikeCancelVisaCount(text)
        || LooksLikeCancelWorkPermitCount(text)
        || LooksLikeCancelInvitationCount(text)
        || LooksLikeDurationCount(text);

    private static bool WorkPermitPhraseStartsBeforeVisa(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        var visaIdx = folded.IndexOf("wiza", StringComparison.Ordinal);
        var wpIdx = FirstWorkPermitPhraseIndex(folded);
        return wpIdx >= 0 && (visaIdx < 0 || wpIdx < visaIdx);
    }

    private static bool WorkPermitPhraseStartsBeforeInvitation(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        var invitationIdx = FirstInvitationPhraseIndex(folded);
        var wpIdx = FirstWorkPermitPhraseIndex(folded);
        return wpIdx >= 0 && (invitationIdx < 0 || wpIdx < invitationIdx);
    }

    private static bool InvitationPhraseStartsBeforeVisa(string? text)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        var visaIdx = folded.IndexOf("wiza", StringComparison.Ordinal);
        var invitationIdx = FirstInvitationPhraseIndex(folded);
        return invitationIdx >= 0 && (visaIdx < 0 || invitationIdx < visaIdx);
    }

    private static int FirstWorkPermitPhraseIndex(string folded) =>
        FirstIndex(folded, "rugsat", "rugsad");

    private static int FirstInvitationPhraseIndex(string folded) =>
        FirstIndex(folded, "cakyly", "invitation");

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