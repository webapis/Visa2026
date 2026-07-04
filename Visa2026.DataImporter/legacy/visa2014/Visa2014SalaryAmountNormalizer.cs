using System.Globalization;
using System.Text.RegularExpressions;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Normalizes legacy <c>dbo.Salary.Detail</c> text into Visa2026 <c>EmployeeSalary.Amount</c> (max 32 chars).
/// </summary>
internal static partial class Visa2014SalaryAmountNormalizer
{
    private const int MaxAmountLength = 32;

    [GeneratedRegex(@"(?<!\d)(\d{1,3}(?:[.\s]\d{3})+(?:[.,]\d{2})?|\d{1,3}(?:[.,]\d{3})+[.,]\d{2}|\d{4,6}(?:[.,]\d{2})?)(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex AmountTokenRegex();

    internal static bool TryNormalize(string? rawDetail, out string amount, out string parseNote)
    {
        amount = string.Empty;
        parseNote = "empty";

        if (string.IsNullOrWhiteSpace(rawDetail))
            return false;

        var trimmed = CollapseSpaces(rawDetail.Trim());
        if (string.IsNullOrEmpty(trimmed))
            return false;

        if (trimmed.Length <= MaxAmountLength && LooksLikePlainAmount(trimmed))
        {
            amount = NormalizeSeparators(trimmed);
            parseNote = string.Equals(amount, trimmed, StringComparison.Ordinal) ? "plain" : "normalized_separators";
            return !string.IsNullOrWhiteSpace(amount);
        }

        var extracted = ExtractBestAmountToken(trimmed);
        if (string.IsNullOrWhiteSpace(extracted))
        {
            parseNote = "no_amount_token";
            return false;
        }

        amount = NormalizeSeparators(extracted);
        if (amount.Length > MaxAmountLength)
        {
            parseNote = "extracted_truncated";
            amount = amount[..MaxAmountLength];
        }
        else
        {
            parseNote = extracted != trimmed ? "extracted_from_sentence" : "plain";
        }

        return !string.IsNullOrWhiteSpace(amount);
    }

    /// <summary>
    /// Çalik import sign-off: all salaries import as USD (legacy Detail may say dtm but has no currency column).
    /// </summary>
    internal static string ResolveCurrency(string? rawDetail) => "USD";

    private static string? ExtractBestAmountToken(string text)
    {
        var matches = AmountTokenRegex().Matches(text);
        if (matches.Count == 0)
            return null;

        string? best = null;
        decimal bestScore = decimal.MinValue;
        foreach (Match match in matches)
        {
            var token = CollapseSpaces(match.Value.Trim());
            if (!TryParseAmountValue(token, out var value))
                continue;

            if (value > bestScore)
            {
                bestScore = value;
                best = token;
            }
        }

        return best;
    }

    private static bool LooksLikePlainAmount(string text)
    {
        if (ContainsLettersOutsideAmountContext(text))
            return false;

        return TryParseAmountValue(text, out _);
    }

    private static bool ContainsLettersOutsideAmountContext(string text)
    {
        foreach (var ch in text)
        {
            if (char.IsLetter(ch))
                return true;
        }

        return false;
    }

    private static string CollapseSpaces(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizeSeparators(string token)
    {
        var collapsed = CollapseSpaces(token);
        var lastComma = collapsed.LastIndexOf(',');
        var lastDot = collapsed.LastIndexOf('.');

        if (lastComma > lastDot && collapsed.Length - lastComma == 3)
        {
            // European thousands with comma decimals: 1.667,00 → 1.667.00
            var integerPart = collapsed[..lastComma].Replace(",", string.Empty).Replace(" ", string.Empty);
            var fraction = collapsed[(lastComma + 1)..];
            return $"{integerPart}.{fraction}";
        }

        return collapsed.Replace(" ", string.Empty);
    }

    private static bool TryParseAmountValue(string token, out decimal value)
    {
        value = 0;
        var normalized = NormalizeSeparators(token);
        var digitsOnly = normalized.Replace(".", string.Empty).Replace(",", string.Empty);
        if (digitsOnly.Length == 0 || !digitsOnly.All(char.IsDigit))
            return false;

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return true;

        if (decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("tr-TR"), out value))
            return true;

        return decimal.TryParse(digitsOnly, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
