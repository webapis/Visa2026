using System.Globalization;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Classifies an instance value and derives every form a document might spell it in.
/// </summary>
/// <remarks>
/// Kind is decided from explicit short-code sets plus value shape — never from a name prefix.
/// A wrong kind only weakens matching (and is covered by tests), unlike a wrong
/// <see cref="UserReports.UserReportPlaceholderPack"/>, which silently hides a token.
/// </remarks>
public static class TemplateValueMatchKeys
{
    private static readonly HashSet<string> PersonNameCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PFN", "PLN", "PFNM", "PMNM", "ACFNM", "CHFN", "SPFNM", "RPFN", "PSEF",
    };

    private static readonly HashSet<string> IdentifierCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PPN", "PPIN", "VNUM", "AFNUM", "AFNN", "WPNM", "INVN", "ACODE", "PNAT", "PCBC", "PFAC", "AMIG",
        "CHPN", "RPPN", "EGCC",
    };

    private static readonly string[] DateInputFormats =
    [
        "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "dd.MM.yy",
    ];

    private static readonly string[] DateOutputFormats =
    [
        "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy",
    ];

    /// <summary>
    /// Turkmen month names for long-form dates ("20 awgust 2026"). Not sourced from a repo lookup —
    /// confirm against real ministry documents before relying on long-form date matching.
    /// </summary>
    private static readonly string[] TurkmenMonths =
    [
        "ýanwar", "fewral", "mart", "aprel", "maý", "iýun",
        "iýul", "awgust", "sentýabr", "oktýabr", "noýabr", "dekabr",
    ];

    public static ValueKind Classify(string shortCode, string rawValue)
    {
        if (PersonNameCodes.Contains(shortCode))
            return ValueKind.PersonName;

        if (IdentifierCodes.Contains(shortCode))
            return ValueKind.Identifier;

        if (TryParseDate(rawValue, out _))
            return ValueKind.Date;

        if (TryParseNumber(rawValue, out _))
            return ValueKind.Number;

        return ValueKind.Text;
    }

    public static IReadOnlyList<string> Build(string rawValue, ValueKind kind)
    {
        var keys = new List<string>();

        switch (kind)
        {
            case ValueKind.Identifier:
                Add(keys, TemplateTextNormalizer.NormalizeIdentifier(rawValue));
                Add(keys, TemplateTextNormalizer.NormalizeFolded(rawValue));
                break;

            case ValueKind.Date when TryParseDate(rawValue, out var date):
                foreach (var format in DateOutputFormats)
                    Add(keys, TemplateTextNormalizer.NormalizeFolded(date.ToString(format, CultureInfo.InvariantCulture)));
                Add(keys, TemplateTextNormalizer.NormalizeFolded(FormatTurkmenLongDate(date)));
                break;

            // "1,500" is 1500 in one convention and 1.5 in another, so both readings become keys
            // instead of the code picking one and being wrong half the time.
            case ValueKind.Number when TryParseNumber(rawValue, out var number):
                Add(keys, StripSeparators(rawValue));
                Add(keys, TemplateTextNormalizer.NormalizeFolded(number.ToString("0.################", CultureInfo.InvariantCulture)));
                Add(keys, TemplateTextNormalizer.NormalizeFolded(rawValue));
                break;

            case ValueKind.PersonName:
                Add(keys, TemplateTextNormalizer.NormalizeFolded(rawValue));
                Add(keys, SwapNameOrder(rawValue));
                break;

            default:
                Add(keys, TemplateTextNormalizer.NormalizeFolded(rawValue));
                break;
        }

        return keys;
    }

    public static bool TryParseDate(string? value, out DateTime date)
    {
        date = default;
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length < 6)
            return false;

        return DateTime.TryParseExact(
            trimmed,
            DateInputFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    /// <summary>Accepts both decimal marks and thousands separators, e.g. <c>1 500,50</c> and <c>1,500.50</c>.</summary>
    public static bool TryParseNumber(string? value, out decimal number)
    {
        number = 0m;
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return false;

        if (trimmed.Any(static c => !char.IsDigit(c) && c is not ('.' or ',' or ' ' or '-' or '+')))
            return false;

        var compact = trimmed.Replace(" ", string.Empty, StringComparison.Ordinal);
        var lastComma = compact.LastIndexOf(',');
        var lastDot = compact.LastIndexOf('.');

        // Whichever separator comes last is the decimal mark; the other groups thousands.
        if (lastComma >= 0 && lastDot >= 0)
        {
            compact = lastComma > lastDot
                ? compact.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.')
                : compact.Replace(",", string.Empty, StringComparison.Ordinal);
        }
        else if (lastComma >= 0)
        {
            compact = compact.Replace(',', '.');
        }

        return decimal.TryParse(compact, NumberStyles.Number, CultureInfo.InvariantCulture, out number);
    }

    public static int CountDigits(string? value) =>
        (value ?? string.Empty).Count(char.IsDigit);

    /// <summary>An unset date arrives as <c>01.01.0001</c> — a value no document ever contains.</summary>
    public static bool IsSentinelDate(string? value) =>
        TryParseDate(value, out var date) && date.Year <= 1;

    private static string StripSeparators(string value) =>
        new(value.Where(static c => c is not ('.' or ',' or ' ')).ToArray());

    private static string FormatTurkmenLongDate(DateTime date) =>
        $"{date.Day} {TurkmenMonths[date.Month - 1]} {date.Year}";

    private static string SwapNameOrder(string rawValue)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(rawValue);
        var parts = folded.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 ? $"{parts[1]} {parts[0]}" : string.Empty;
    }

    private static void Add(List<string> keys, string key)
    {
        if (key.Length >= TemplateTextNormalizer.MinimumMatchLength && !keys.Contains(key, StringComparer.Ordinal))
            keys.Add(key);
    }
}
