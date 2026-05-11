using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace Visa2026.Module.Services;

/// <summary>
/// PdfSharpCore (MIT) merge and raster→single-page PDF for batch ZIP supporting documents
/// (passport / visa / diploma paths). Spire is not used here to avoid evaluation watermarks.
/// </summary>
internal static class SupportingDocumentsPdfSharpHelper
{
    private const double A4WidthPt = 595;
    private const double A4HeightPt = 842;
    private const double MarginPt = 20;

    /// <summary>Merges ordered PDF streams into <paramref name="output"/> (stream left open).</summary>
    public static void MergePdfStreams<TStream>(IReadOnlyList<TStream> orderedSources, Stream output)
        where TStream : Stream
    {
        ArgumentNullException.ThrowIfNull(orderedSources);
        ArgumentNullException.ThrowIfNull(output);

        using var outDoc = new PdfDocument();
        foreach (var src in orderedSources)
        {
            if (src == null)
                continue;
            src.Position = 0;
            using var input = PdfReader.Open(src, PdfDocumentOpenMode.Import);
            int count = input.PageCount;
            for (int i = 0; i < count; i++)
                outDoc.AddPage(input.Pages[i]);
        }

        outDoc.Save(output, false);
    }

    /// <summary>One A4 page with the image scaled to fit inside margins.</summary>
    public static bool TryWriteSinglePagePdfFromRasterBytes(byte[] imageBytes, Stream output, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(logger);

        if (imageBytes.Length == 0)
            return false;

        try
        {
            using var doc = new PdfDocument();
            PdfPage page = doc.AddPage();
            page.Width = XUnit.FromPoint(A4WidthPt);
            page.Height = XUnit.FromPoint(A4HeightPt);

            using var gfx = XGraphics.FromPdfPage(page);
            using XImage ximg = XImage.FromStream(() => new MemoryStream(imageBytes, writable: false));

            double pw = page.Width.Point - 2 * MarginPt;
            double ph = page.Height.Point - 2 * MarginPt;
            double imgW = ximg.PixelWidth * 72.0 / Math.Max(ximg.HorizontalResolution, 1);
            double imgH = ximg.PixelHeight * 72.0 / Math.Max(ximg.VerticalResolution, 1);
            double scale = Math.Min(pw / imgW, ph / imgH);
            double dw = imgW * scale;
            double dh = imgH * scale;
            double x = MarginPt + (pw - dw) / 2.0;
            double y = MarginPt + (ph - dh) / 2.0;

            gfx.DrawImage(ximg, x, y, dw, dh);
            doc.Save(output, false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ZIP packer: PdfSharpCore could not rasterize attachment for PDF merge slice.");
            return false;
        }
    }
}
