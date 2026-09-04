using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Finds inline / anchored pictures in a Word paragraph. Tiny icons (both extents under 8mm)
/// are ignored so decorative glyphs are not treated as person-photo slots.
/// </summary>
internal static class WordInlinePictureLocator
{
    public const long EmuPerMillimetre = 36000L;

    public const long MinimumPhotoExtentEmu = 8L * EmuPerMillimetre;

    public static IReadOnlyList<Drawing> Enumerate(Paragraph paragraph)
    {
        ArgumentNullException.ThrowIfNull(paragraph);
        var drawings = new List<Drawing>();
        foreach (var drawing in paragraph.Descendants<Drawing>())
        {
            if (!IsQualifyingPicture(drawing))
                continue;
            drawings.Add(drawing);
        }

        return drawings;
    }

    public static int TextOffsetBefore(Paragraph paragraph, Drawing drawing)
    {
        ArgumentNullException.ThrowIfNull(paragraph);
        ArgumentNullException.ThrowIfNull(drawing);

        var offset = 0;
        foreach (var node in paragraph.Descendants())
        {
            if (ReferenceEquals(node, drawing))
                return offset;
            if (node is Text text)
                offset += (text.Text ?? string.Empty).Length;
        }

        return offset;
    }

    internal static bool IsQualifyingPicture(Drawing drawing)
    {
        if (!drawing.Descendants<PIC.Picture>().Any())
            return false;
        if (!drawing.Descendants<A.Blip>().Any())
            return false;

        if (!TryGetExtents(drawing, out var cx, out var cy))
            return true;

        if (cx <= 0 || cy <= 0)
            return true;

        return cx >= MinimumPhotoExtentEmu || cy >= MinimumPhotoExtentEmu;
    }

    internal static bool TryGetExtents(Drawing drawing, out long cx, out long cy)
    {
        cx = 0;
        cy = 0;

        var inline = drawing.Descendants<DW.Inline>().FirstOrDefault();
        if (inline?.Extent?.Cx?.HasValue == true && inline.Extent.Cy?.HasValue == true)
        {
            cx = inline.Extent.Cx.Value;
            cy = inline.Extent.Cy.Value;
            return true;
        }

        var anchor = drawing.Descendants<DW.Anchor>().FirstOrDefault();
        if (anchor?.Extent?.Cx?.HasValue == true && anchor.Extent.Cy?.HasValue == true)
        {
            cx = anchor.Extent.Cx.Value;
            cy = anchor.Extent.Cy.Value;
            return true;
        }

        var transform = drawing.Descendants<A.Extents>().FirstOrDefault();
        if (transform?.Cx?.HasValue == true && transform.Cy?.HasValue == true)
        {
            cx = transform.Cx.Value;
            cy = transform.Cy.Value;
            return true;
        }

        return false;
    }
}
