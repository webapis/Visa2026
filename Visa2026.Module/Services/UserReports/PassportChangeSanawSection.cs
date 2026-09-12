#nullable enable

using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Passport-change sanaw has two stacked tables: Kiçirak / previous booklet, then Täze / new.
/// Both tables use the same column headers; section titles choose current vs previous passport tokens.
/// </summary>
public static class PassportChangeSanawSection
{
    private static readonly Regex CurrentPassportCode = new(
        @"(?<![A-Za-z])(PPN|PPIS|PPED|PPTP|PPAT|PPCC|PPCT)(?![A-Za-z])",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Dictionary<string, string> CurrentToPrevious = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PPN"] = "PRPN",
        ["PPIS"] = "PRIS",
        ["PPED"] = "PRED",
        ["PPTP"] = "PRTP",
        ["PPAT"] = "PRAT",
        ["PPCC"] = "PRCC",
        ["PPCT"] = "PRCT",
        ["Passport_Number"] = "PreviousPassport_Number",
        ["Passport_IssueDateText"] = "PreviousPassport_IssueDateText",
        ["Passport_ExpirationDateText"] = "PreviousPassport_ExpirationDateText",
        ["Passport_TypeTm"] = "PreviousPassport_TypeTm",
        ["Passport_Authority"] = "PreviousPassport_Authority",
        ["Passport_CountryCode"] = "PreviousPassport_CountryCode",
        ["Passport_CountryTm"] = "PreviousPassport_CountryTm",
    };

    private static readonly Regex StackedRosterToken = new(
        @"\{\{\.(?:PPN|PPIS|PPED|PPTP|PPAT|PPCC|PPCT|PRPN|PRIS|PRED|PRTP|PRAT|PRCC|PRCT|PLN|PFNM|Person_LastName|Person_FirstName|Passport_|PreviousPassport_)",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsPreviousBand(IXLWorksheet sheet, int dataRow)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if (dataRow <= 1)
            return false;

        var sawNew = false;
        for (var row = dataRow - 1; row >= Math.Max(1, dataRow - 12); row--)
        {
            var folded = FoldRow(sheet, row);
            if (folded.Length == 0)
                continue;

            if (LooksLikeNewPassportTitle(folded))
                sawNew = true;
            if (LooksLikePreviousPassportTitle(folded))
                return !sawNew;
        }

        return false;
    }

    public static bool IsNewBand(IXLWorksheet sheet, int dataRow)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if (dataRow <= 1)
            return false;

        for (var row = dataRow - 1; row >= Math.Max(1, dataRow - 12); row--)
        {
            var folded = FoldRow(sheet, row);
            if (folded.Length == 0)
                continue;

            if (LooksLikeNewPassportTitle(folded))
                return true;
            if (LooksLikePreviousPassportTitle(folded))
                return false;
        }

        return false;
    }

    public static bool IsRosterBand(IXLWorksheet sheet, int dataRow) =>
        IsPreviousBand(sheet, dataRow) || IsNewBand(sheet, dataRow);

    public static bool LooksLikeStackedRosterToken(string? cellText) =>
        !string.IsNullOrWhiteSpace(cellText) && StackedRosterToken.IsMatch(cellText);

    /// <summary>
    /// Already-approved templates often keep <c>{{.PPN}}</c> on both tables.
    /// Under a Kiçirak / previous title, fill current-passport keys from previous booklet values.
    /// </summary>
    public static void OverlayCurrentPassportFromPrevious(IDictionary<string, object> row)
    {
        ArgumentNullException.ThrowIfNull(row);

        foreach (var (current, previous) in CurrentToPrevious)
        {
            if (row.TryGetValue(previous, out var value))
                row[current] = value ?? string.Empty;
        }
    }

    public static bool LooksLikePreviousPassportTitle(string? folded)
    {
        var text = folded ?? string.Empty;
        if (text.Contains("kicirak", StringComparison.Ordinal)
            || text.Contains("kici pasport", StringComparison.Ordinal)
            || text.Contains("onki pasport", StringComparison.Ordinal)
            || text.Contains("previouspassport", StringComparison.Ordinal)
            || text.Contains("oldpassport", StringComparison.Ordinal))
        {
            return true;
        }

        return text.Contains("onki", StringComparison.Ordinal)
            && text.Contains("pasport", StringComparison.Ordinal);
    }

    public static bool LooksLikeNewPassportTitle(string? folded)
    {
        var text = folded ?? string.Empty;
        if (text.Contains("taze pasport", StringComparison.Ordinal)
            || text.Contains("newpassport", StringComparison.Ordinal))
        {
            return true;
        }

        return text.Contains("taze", StringComparison.Ordinal)
            && text.Contains("pasport", StringComparison.Ordinal);
    }

    public static string RemapTokenToPrevious(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return token;

        return CurrentPassportCode.Replace(token, match =>
            CurrentToPrevious.TryGetValue(match.Value, out var mapped) ? mapped : match.Value);
    }

    public static bool TryMapRowKeyToPrevious(string key, out string previousKey)
    {
        previousKey = key;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (CurrentToPrevious.TryGetValue(key.Trim(), out var mapped))
        {
            previousKey = mapped;
            return true;
        }

        return false;
    }

    public static string FoldRow(IXLWorksheet sheet, int rowNumber)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var last = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;
        var parts = new List<string>();
        for (var column = 1; column <= last; column++)
        {
            var text = sheet.Cell(rowNumber, column).GetFormattedString()?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                parts.Add(text);
        }

        return TemplateTextNormalizer.NormalizeFolded(string.Join(' ', parts));
    }
}
