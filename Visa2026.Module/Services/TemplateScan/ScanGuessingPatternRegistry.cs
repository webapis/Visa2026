#nullable enable

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Names the layout-specific guessing patterns and which nearby text each one uses.
/// Ranking still goes through <see cref="ScanSurroundPlaceholderPattern"/> so every
/// file can apply more than one pattern (caption + left label + shape).
/// </summary>
public static class ScanGuessingPatternRegistry
{
    public static IReadOnlyList<ScanGuessingPatternKind> All { get; } =
    [
        ScanGuessingPatternKind.OfficialLetter,
        ScanGuessingPatternKind.CaptionUnderLine,
        ScanGuessingPatternKind.LeftLabelForm,
        ScanGuessingPatternKind.InlineProse,
        ScanGuessingPatternKind.ExcelColumnHeader,
    ];

    public static IReadOnlyList<ScanGuessingPatternKind> Detect(
        string? yellowText,
        string? nearbyLabel,
        string? columnHeader,
        ScanSourceKind sourceKind)
    {
        var kinds = new List<ScanGuessingPatternKind>(3);
        if (sourceKind == ScanSourceKind.Excel)
        {
            kinds.Add(ScanGuessingPatternKind.ExcelColumnHeader);
            return kinds;
        }

        var nearby = string.Join(
            " ",
            new[] { nearbyLabel, columnHeader }.Where(static s => !string.IsNullOrWhiteSpace(s)));
        var caption = ScanFormCaptionHints.ExtractParentheticalList(nearby);
        if (!string.IsNullOrWhiteSpace(caption))
            kinds.Add(ScanGuessingPatternKind.CaptionUnderLine);
        else if (ScanFormFieldLabelHints.LooksLikeFormFieldLabel(nearbyLabel)
                 || ScanFormFieldLabelHints.LooksLikeFormFieldLabel(Stem(nearby)))
            kinds.Add(ScanGuessingPatternKind.LeftLabelForm);
        else if (!string.IsNullOrWhiteSpace(nearbyLabel) || LooksLikeInlineTitle(yellowText, nearby))
            kinds.Add(ScanGuessingPatternKind.InlineProse);

        if (LooksLikeOfficialLetterMark(yellowText))
            kinds.Insert(0, ScanGuessingPatternKind.OfficialLetter);

        return kinds.Count == 0
            ? [ScanGuessingPatternKind.InlineProse]
            : kinds;
    }

    internal static bool LooksLikeOfficialLetterMark(string? yellowText)
    {
        var text = yellowText?.Trim() ?? string.Empty;
        if (text.Length == 0)
            return false;

        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        return folded.Contains("tertipde", StringComparison.Ordinal)
            || folded.Contains("gezeklik", StringComparison.Ordinal)
            || System.Text.RegularExpressions.Regex.IsMatch(text, @"№?\s*\d+\s*/\s*-?\s*\d+")
            || System.Text.RegularExpressions.Regex.IsMatch(text, @"\b\d+\s*\([^)]+\)\s*aý\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool LooksLikeInlineTitle(string? yellowText, string nearby)
    {
        var foldedYellow = TemplateTextNormalizer.NormalizeFolded(yellowText);
        var foldedNearby = TemplateTextNormalizer.NormalizeFolded(nearby);
        return foldedYellow.StartsWith("mudiri ", StringComparison.Ordinal)
            || foldedNearby.Contains("isgar", StringComparison.Ordinal)
            || foldedNearby.Contains("is beriji", StringComparison.Ordinal)
            || foldedNearby.Contains("pasport", StringComparison.Ordinal);
    }

    private static string Stem(string nearby)
    {
        if (string.IsNullOrWhiteSpace(nearby))
            return string.Empty;
        var without = System.Text.RegularExpressions.Regex.Replace(nearby, @"\([^)]*\)", " ");
        return TemplateTextNormalizer.NormalizeFolded(without);
    }
}