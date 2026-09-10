#nullable enable

using DocumentFormat.OpenXml.Packaging;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Treats a Word body inline picture as a person-photo merge slot. Header/footer pictures
/// and tiny icons are ignored. Image parts stay in the package so the Convert diff gate
/// can still hash them after the drawing is replaced by <c>{{IMAGE:Person_Photo}}</c>.
/// </summary>
internal static class ScanOfficePictureExtractor
{
    public static IReadOnlyList<DocumentRegion.WordDrawing> Extract(byte[] officeBytes)
    {
        ArgumentNullException.ThrowIfNull(officeBytes);
        if (officeBytes.Length < 64)
            return Array.Empty<DocumentRegion.WordDrawing>();

        using var stream = new MemoryStream(officeBytes, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        var results = new List<DocumentRegion.WordDrawing>();

        foreach (var addressed in WordTemplateAddressing.EnumerateParagraphs(document))
        {
            if (addressed.Part != WordPart.Body)
                continue;

            var drawings = WordInlinePictureLocator.Enumerate(addressed.Paragraph);
            for (var i = 0; i < drawings.Count; i++)
            {
                var offset = WordInlinePictureLocator.TextOffsetBefore(addressed.Paragraph, drawings[i]);
                results.Add(new DocumentRegion.WordDrawing(addressed.Address, i, offset));
            }
        }

        return results;
    }

    /// <summary>
    /// Body portraits as Review/Generate slots. Photos are drawings, not yellow highlighter.
    /// </summary>
    public static IReadOnlyList<ScanOfficeYellowSpan> AsYellowSpans(byte[] officeBytes)
    {
        var slots = Extract(officeBytes);
        if (slots.Count == 0)
            return Array.Empty<ScanOfficeYellowSpan>();

        var spans = new List<ScanOfficeYellowSpan>(slots.Count);
        foreach (var slot in slots)
        {
            spans.Add(new ScanOfficeYellowSpan
            {
                Text = "Person photo",
                Region = slot,
                PageIndex = 0,
            });
        }

        return spans;
    }

    public static IReadOnlyList<ScanOfficeYellowSpan> MergeInto(
        IReadOnlyList<ScanOfficeYellowSpan> yellows,
        byte[]? officeBytes,
        ScanSourceKind sourceKind)
    {
        yellows ??= Array.Empty<ScanOfficeYellowSpan>();
        if (sourceKind != ScanSourceKind.Word || officeBytes is not { Length: > 64 })
            return yellows;

        var photos = AsYellowSpans(officeBytes);
        if (photos.Count == 0)
            return yellows;

        var merged = new List<ScanOfficeYellowSpan>(yellows.Count + photos.Count);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var yellow in yellows)
        {
            merged.Add(yellow);
            var key = ScanDocumentRegionKey.ForRegion(yellow.Region);
            if (key != null)
                keys.Add(key);
        }

        foreach (var photo in photos)
        {
            var key = ScanDocumentRegionKey.ForRegion(photo.Region);
            if (key != null && !keys.Add(key))
                continue;
            merged.Add(photo);
        }

        return merged;
    }

    public static bool IsPersonPhotoToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;
        if (token.Contains("IMAGE:", StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var code in TemplateTokenSyntax.GetShortCodes(token))
        {
            if (code.Equals("PPH", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
