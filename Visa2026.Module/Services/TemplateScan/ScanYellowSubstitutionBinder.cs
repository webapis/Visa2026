#nullable enable

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Builds writer substitutions from Review tokens. Locked rows keep their yellow:
/// exact OpenXML key, then the same paragraph/cell slot. Never steal a yellow by sample text.
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

        var yellows = ScanOfficePictureExtractor.MergeInto(
            new ScanOfficeYellowExtractor().Extract(officeBytes, sourceKind),
            officeBytes,
            sourceKind).ToList();
        var claimed = new HashSet<string>(StringComparer.Ordinal);
        var substitutions = new List<TokenSubstitution>();
        var unmatchedText = new List<ScanDetectedField>();
        var unmatchedImages = new List<ScanDetectedField>();

        foreach (var field in fields)
        {
            if (!TryGetWritableToken(field.ProposedToken, out var token))
                continue;

            if (format == TemplateSourceFormat.Xlsx
                && field.SourceRegion is DocumentRegion.ExcelCell excelCell
                && !ScanExcelWorkbookPolicy.IsOnFirstWorksheet(officeBytes, excelCell.SheetName))
                continue;

            if (ScanOfficePictureExtractor.IsPersonPhotoToken(token))
            {
                var drawingKey = ScanDocumentRegionKey.ForField(field);
                var drawing = FindYellow(yellows, drawingKey);
                if (drawing?.Region is DocumentRegion.WordDrawing)
                {
                    var yellowKey = ScanDocumentRegionKey.ForRegion(drawing.Region) ?? drawingKey;
                    if (claimed.Add(yellowKey))
                    {
                        substitutions.Add(new TokenSubstitution(drawing.Region, token));
                        continue;
                    }
                }

                unmatchedImages.Add(field);
                continue;
            }

            var key = ScanDocumentRegionKey.ForField(field);
            var yellow = FindYellow(yellows, key);
            if (yellow != null && yellow.Region is not DocumentRegion.WordDrawing)
            {
                var yellowKey = ScanDocumentRegionKey.ForRegion(yellow.Region) ?? key;
                if (!claimed.Add(yellowKey))
                {
                    unmatchedText.Add(field);
                    continue;
                }

                substitutions.Add(new TokenSubstitution(yellow.Region, token));
                continue;
            }

            unmatchedText.Add(field);
        }

        var unmatchedNoRegion = unmatchedText
            .Where(static f => f.SourceRegion == null)
            .ToList();
        unmatchedText = unmatchedText
            .Where(static f => f.SourceRegion != null)
            .ToList();

        foreach (var group in unmatchedText.GroupBy(static f => ScanDocumentRegionKey.AlignGroupKey(f.SourceRegion), StringComparer.Ordinal))
        {
            var available = yellows
                .Where(y => y.Region is not DocumentRegion.WordDrawing)
                .Where(y => string.Equals(
                    ScanDocumentRegionKey.AlignGroupKey(y.Region),
                    group.Key,
                    StringComparison.Ordinal))
                .Where(y =>
                {
                    var k = ScanDocumentRegionKey.ForRegion(y.Region);
                    return k == null || !claimed.Contains(k);
                })
                .OrderBy(static y => ScanDocumentRegionKey.OrderInGroup(y.Region))
                .ToList();

            var orderedFields = group
                .OrderBy(static f => ScanDocumentRegionKey.OrderInGroup(f.SourceRegion))
                .ToList();

            var count = Math.Min(available.Count, orderedFields.Count);
            for (var i = 0; i < count; i++)
            {
                if (!TryGetWritableToken(orderedFields[i].ProposedToken, out var token))
                    continue;

                var yellow = available[i];
                if (format == TemplateSourceFormat.Xlsx
                    && yellow.Region is DocumentRegion.ExcelCell excelCell
                    && !ScanExcelWorkbookPolicy.IsOnFirstWorksheet(officeBytes, excelCell.SheetName))
                    continue;

                var yellowKey = ScanDocumentRegionKey.ForRegion(yellow.Region);
                if (yellowKey != null && !claimed.Add(yellowKey))
                    continue;

                substitutions.Add(new TokenSubstitution(yellow.Region, token));
            }
        }

        var leftoverText = yellows
            .Where(static y => y.Region is not DocumentRegion.WordDrawing)
            .Where(y =>
            {
                var k = ScanDocumentRegionKey.ForRegion(y.Region);
                return k == null || !claimed.Contains(k);
            })
            .ToList();
        var leftoverTextIndex = 0;
        foreach (var field in unmatchedNoRegion)
        {
            if (leftoverTextIndex >= leftoverText.Count)
                break;
            if (!TryGetWritableToken(field.ProposedToken, out var token))
                continue;

            var yellow = leftoverText[leftoverTextIndex++];
            if (format == TemplateSourceFormat.Xlsx
                && yellow.Region is DocumentRegion.ExcelCell excelCell
                && !ScanExcelWorkbookPolicy.IsOnFirstWorksheet(officeBytes, excelCell.SheetName))
                continue;

            var yellowKey = ScanDocumentRegionKey.ForRegion(yellow.Region);
            if (yellowKey != null && !claimed.Add(yellowKey))
                continue;

            substitutions.Add(new TokenSubstitution(yellow.Region, token));
        }

        var leftoverDrawings = yellows
            .Where(static y => y.Region is DocumentRegion.WordDrawing)
            .Where(y =>
            {
                var k = ScanDocumentRegionKey.ForRegion(y.Region);
                return k == null || !claimed.Contains(k);
            })
            .OrderBy(static y => ((DocumentRegion.WordDrawing)y.Region).ParagraphAddress, StringComparer.Ordinal)
            .ThenBy(static y => ((DocumentRegion.WordDrawing)y.Region).DrawingIndex)
            .ToList();

        var drawingIndex = 0;
        foreach (var field in unmatchedImages)
        {
            if (drawingIndex >= leftoverDrawings.Count)
                break;
            if (!TryGetWritableToken(field.ProposedToken, out var token))
                continue;

            var drawing = leftoverDrawings[drawingIndex++];
            var yellowKey = ScanDocumentRegionKey.ForRegion(drawing.Region);
            if (yellowKey != null && !claimed.Add(yellowKey))
                continue;

            substitutions.Add(new TokenSubstitution(drawing.Region, token));
        }

        return SplitCompoundsAcrossUnclaimedYellows(substitutions, yellows, claimed);
    }

    /// <summary>
    /// One locked compound on the first yellow of a cell must not swallow sibling yellows.
    /// Write one short code per leftover mark in that paragraph/cell.
    /// </summary>
    private static IReadOnlyList<TokenSubstitution> SplitCompoundsAcrossUnclaimedYellows(
        List<TokenSubstitution> substitutions,
        IReadOnlyList<ScanOfficeYellowSpan> yellows,
        HashSet<string> claimed)
    {
        var claimedNow = new HashSet<string>(claimed, StringComparer.Ordinal);
        var result = new List<TokenSubstitution>(substitutions.Count);
        foreach (var substitution in substitutions)
        {
            var codes = TemplateTokenSyntax.GetShortCodes(substitution.Token);
            if (codes.Count <= 1)
            {
                result.Add(substitution);
                continue;
            }

            var group = ScanDocumentRegionKey.AlignGroupKey(substitution.Region);
            var extras = yellows
                .Where(static y => y.Region is not DocumentRegion.WordDrawing)
                .Where(y => string.Equals(
                    ScanDocumentRegionKey.AlignGroupKey(y.Region),
                    group,
                    StringComparison.Ordinal))
                .Where(y =>
                {
                    var key = ScanDocumentRegionKey.ForRegion(y.Region);
                    if (key == null || claimedNow.Contains(key))
                        return false;
                    return !SameRegion(y.Region, substitution.Region);
                })
                .OrderBy(static y => ScanDocumentRegionKey.OrderInGroup(y.Region))
                .ToList();

            if (extras.Count == 0)
            {
                result.Add(substitution);
                continue;
            }

            var take = Math.Min(codes.Count, extras.Count + 1);
            result.Add(new TokenSubstitution(substitution.Region, WrapBare(codes[0])));
            for (var i = 1; i < take; i++)
            {
                var yellow = extras[i - 1];
                var yellowKey = ScanDocumentRegionKey.ForRegion(yellow.Region);
                if (yellowKey != null)
                    claimedNow.Add(yellowKey);
                result.Add(new TokenSubstitution(yellow.Region, WrapBare(codes[i])));
            }
        }

        return result;
    }

    private static bool SameRegion(DocumentRegion? left, DocumentRegion? right)
    {
        var a = ScanDocumentRegionKey.ForRegion(left);
        var b = ScanDocumentRegionKey.ForRegion(right);
        return a != null && string.Equals(a, b, StringComparison.Ordinal);
    }

    private static string WrapBare(string code) => "{{." + code + "}}";


    internal static bool TryGetWritableToken(string? proposedToken, out string token)
    {
        token = string.Empty;
        var stripped = ScanCompoundYellowParts.StripEmptyPartMarkers(proposedToken);
        if (string.IsNullOrWhiteSpace(stripped))
            return false;

        var trimmed = stripped.Trim();
        if (trimmed.Contains("{{", StringComparison.Ordinal)
            || TemplateTokenSyntax.TryGetShortCode(trimmed, out _))
        {
            token = trimmed;
            return true;
        }

        return false;
    }

    private static ScanOfficeYellowSpan? FindYellow(IReadOnlyList<ScanOfficeYellowSpan> yellows, string key)
    {
        foreach (var yellow in yellows)
        {
            var yellowKey = ScanDocumentRegionKey.ForRegion(yellow.Region);
            if (yellowKey != null && string.Equals(yellowKey, key, StringComparison.Ordinal))
                return yellow;
        }

        return null;
    }
}
