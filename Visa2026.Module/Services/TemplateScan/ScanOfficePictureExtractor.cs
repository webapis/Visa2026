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
}
