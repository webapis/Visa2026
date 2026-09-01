#nullable enable

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Builds writer substitutions from Review tokens. Recovers OpenXML addresses from
/// yellow marks when <see cref="ScanDetectedField.SourceRegion"/> was dropped in merge.
/// </summary>
public static class ScanYellowSubstitutionBinder
{
    public static IReadOnlyList<TokenSubstitution> Bind(
        IReadOnlyList<ScanDetectedField> fields,
        byte[] officeBytes,
        ScanSourceKind sourceKind,
        TemplateSourceFormat format)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(officeBytes);

        var substitutions = new List<TokenSubstitution>();
        var unmatched = new List<ScanDetectedField>();

        foreach (var field in fields)
        {
            if (!TryGetWritableToken(field.ProposedToken, out var token))
                continue;

            if (field.SourceRegion is null)
            {
                unmatched.Add(field);
                continue;
            }

            if (format == TemplateSourceFormat.Xlsx
                && field.SourceRegion is DocumentRegion.ExcelCell excelCell
                && !ScanExcelWorkbookPolicy.IsOnFirstWorksheet(officeBytes, excelCell.SheetName))
                continue;

            substitutions.Add(new TokenSubstitution(field.SourceRegion, token));
        }

        if (unmatched.Count == 0)
            return substitutions;

        var yellows = new ScanOfficeYellowExtractor().Extract(officeBytes, sourceKind).ToList();
        foreach (var field in unmatched)
        {
            if (!TryGetWritableToken(field.ProposedToken, out var token))
                continue;

            var yellow = TakeYellow(yellows, field.LabelText);
            if (yellow == null)
                continue;

            if (format == TemplateSourceFormat.Xlsx
                && yellow.Region is DocumentRegion.ExcelCell excelCell
                && !ScanExcelWorkbookPolicy.IsOnFirstWorksheet(officeBytes, excelCell.SheetName))
                continue;

            substitutions.Add(new TokenSubstitution(yellow.Region, token));
        }

        return substitutions;
    }

    internal static bool TryGetWritableToken(string? proposedToken, out string token)
    {
        token = string.Empty;
        if (string.IsNullOrWhiteSpace(proposedToken))
            return false;

        var trimmed = proposedToken.Trim();
        if (trimmed.Contains("{{", StringComparison.Ordinal)
            || TemplateTokenSyntax.TryGetShortCode(trimmed, out _))
        {
            token = trimmed;
            return true;
        }

        return false;
    }

    internal static ScanOfficeYellowSpan? TakeYellow(List<ScanOfficeYellowSpan> yellows, string? labelText)
    {
        var want = TemplateTextNormalizer.NormalizeIdentifier(labelText);
        if (want.Length < TemplateTextNormalizer.MinimumMatchLength)
            return null;

        var index = yellows.FindIndex(y =>
            string.Equals(TemplateTextNormalizer.NormalizeIdentifier(y.Text), want, StringComparison.Ordinal));
        if (index < 0)
            return null;

        var match = yellows[index];
        yellows.RemoveAt(index);
        return match;
    }
}
