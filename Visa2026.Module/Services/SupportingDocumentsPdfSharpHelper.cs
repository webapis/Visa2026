using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Spire.Pdf.Graphics;

namespace Visa2026.Module.Services;

/// <summary>
/// PdfSharpCore (MIT) merge and raster→PDF pages for batch ZIP supporting documents.
/// PdfSharpCore <see cref="XImage"/> often fails on TIFF and some scans; Spire.PDF (already used for XFA forms)
/// decodes those rasters as a fallback before the merged PDF is built with PdfSharpCore.
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
    /// <param name="landscape">When true, page is A4 landscape (842×595 pt); default is portrait.</param>
    public static bool TryWriteSinglePagePdfFromRasterBytes(byte[] imageBytes, Stream output, ILogger logger, bool landscape = false)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(logger);

        if (imageBytes.Length == 0)
            return false;

        double pageWidthPt = landscape ? A4HeightPt : A4WidthPt;
        double pageHeightPt = landscape ? A4WidthPt : A4HeightPt;

        try
        {
            using var doc = new PdfDocument();
            PdfPage page = doc.AddPage();
            page.Width = XUnit.FromPoint(pageWidthPt);
            page.Height = XUnit.FromPoint(pageHeightPt);

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
            logger.LogDebug(ex, "ZIP packer: PdfSharpCore could not decode raster for merge slice; trying Spire fallback.");
        }

        return TryWriteRasterPdfViaSpire(imageBytes, output, logger, landscape);
    }

    /// <summary>
    /// Spire-backed raster→PDF (TIFF/JPEG/PNG and similar). Output is normal PDF suitable for <see cref="MergePdfStreams"/>.
    /// </summary>
    private static bool TryWriteRasterPdfViaSpire(byte[] imageBytes, Stream output, ILogger logger, bool landscape)
    {
        try
        {
            using var imageStream = new MemoryStream(imageBytes, writable: false);
            using var spireDoc = new Spire.Pdf.PdfDocument();
            Spire.Pdf.PdfPageBase page = landscape
                ? spireDoc.Pages.Add(
                    new System.Drawing.SizeF((float)A4HeightPt, (float)A4WidthPt),
                    new PdfMargins((float)MarginPt))
                : spireDoc.Pages.Add(Spire.Pdf.PdfPageSize.A4, new PdfMargins((float)MarginPt));

            PdfImage img = PdfImage.FromStream(imageStream);
            float pw = page.Canvas.ClientSize.Width;
            float ph = page.Canvas.ClientSize.Height;
            float scale = Math.Min(pw / img.Width, ph / img.Height);
            float dw = img.Width * scale;
            float dh = img.Height * scale;
            float x = (pw - dw) / 2f;
            float y = (ph - dh) / 2f;
            page.Canvas.DrawImage(img, x, y, dw, dh);

            spireDoc.SaveToStream(output, Spire.Pdf.FileFormat.PDF);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ZIP packer: Spire could not rasterize attachment for PDF merge slice.");
            return false;
        }
    }
}
