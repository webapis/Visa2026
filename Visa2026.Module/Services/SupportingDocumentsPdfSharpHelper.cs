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
    public static bool TryWriteSinglePagePdfFromRasterBytes(byte[] imageBytes, Stream output, ILogger logger, bool landscape = false) =>
        TryWriteSinglePagePdfFromRasterBytes(imageBytes, output, logger, DocumentCopyPrintLayout.FitA4, landscape);

    /// <summary>
    /// One page for a raster scan. Passport and visa layouts keep A4 paper and draw the scan at document size.
    /// </summary>
    public static bool TryWriteSinglePagePdfFromRasterBytes(
        byte[] imageBytes,
        Stream output,
        ILogger logger,
        DocumentCopyPrintLayout layout,
        bool landscape = false)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(logger);

        if (imageBytes.Length == 0)
            return false;

        if (!layout.UsesDocumentSize)
            return TryWriteFitA4Raster(imageBytes, output, logger, landscape);

        return TryWriteDocumentSizedRaster(imageBytes, output, logger, layout);
    }

    /// <summary>
    /// Writes one preview/package slice. PDF bytes are copied as-is for A4 documents.
    /// Passport and visa PDFs are placed at document size; rasters use <see cref="TryWriteSinglePagePdfFromRasterBytes"/>.
    /// </summary>
    public static bool TryWriteSlice(
        byte[] content,
        Stream output,
        ILogger logger,
        DocumentCopyPrintLayout layout,
        bool fitA4Landscape = false)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(logger);

        if (content.Length == 0)
            return false;

        if (DocumentFileUploadConstraints.IsLikelyPdf(content))
        {
            if (layout.UsesDocumentSize)
            {
                var placed = new MemoryStream();
                if (TryWritePdfPagesAtPrintLayout(content, placed, logger, layout) && placed.Length > 0)
                {
                    placed.Position = 0;
                    placed.CopyTo(output);
                    return true;
                }

                logger.LogWarning(
                    "Document copies: could not place PDF at {Size}; using the original pages.",
                    layout.Size);
            }

            output.Write(content, 0, content.Length);
            return true;
        }

        return TryWriteSinglePagePdfFromRasterBytes(content, output, logger, layout, fitA4Landscape);
    }

    /// <summary>Places each page of an existing PDF at the layout's document size on A4.</summary>
    public static bool TryWritePdfPagesAtPrintLayout(
        byte[] pdfBytes,
        Stream output,
        ILogger logger,
        DocumentCopyPrintLayout layout)
    {
        if (!layout.UsesDocumentSize)
            return false;

        try
        {
            using var source = new Spire.Pdf.PdfDocument();
            source.LoadFromBytes(pdfBytes);
            if (source.Pages.Count == 0)
                return false;

            using var dest = new Spire.Pdf.PdfDocument();
            for (int i = 0; i < source.Pages.Count; i++)
            {
                Spire.Pdf.PdfPageBase sourcePage = source.Pages[i];
                var rect = layout.Measure(sourcePage.Size.Width, sourcePage.Size.Height);
                Spire.Pdf.PdfPageBase page = dest.Pages.Add(
                    new System.Drawing.SizeF((float)rect.PageWidthPt, (float)rect.PageHeightPt),
                    new PdfMargins(0));
                PdfTemplate template = sourcePage.CreateTemplate();
                DrawSpireTemplate(page, template, rect);
            }

            dest.SaveToStream(output, Spire.Pdf.FileFormat.PDF);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Document copies: could not reflow PDF pages to {Size}.", layout.Size);
            return false;
        }
    }

    private static bool TryWriteFitA4Raster(byte[] imageBytes, Stream output, ILogger logger, bool landscape)
    {
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

        return TryWriteRasterPdfViaSpire(imageBytes, output, logger, landscape, DocumentCopyPrintLayout.FitA4);
    }

    private static bool TryWriteDocumentSizedRaster(
        byte[] imageBytes,
        Stream output,
        ILogger logger,
        DocumentCopyPrintLayout layout)
    {
        try
        {
            using var doc = new PdfDocument();
            using XImage ximg = XImage.FromStream(() => new MemoryStream(imageBytes, writable: false));
            double imgW = ximg.PixelWidth * 72.0 / Math.Max(ximg.HorizontalResolution, 1);
            double imgH = ximg.PixelHeight * 72.0 / Math.Max(ximg.VerticalResolution, 1);
            DocumentCopyDrawRect rect = layout.Measure(imgW, imgH);

            PdfPage page = doc.AddPage();
            page.Width = XUnit.FromPoint(rect.PageWidthPt);
            page.Height = XUnit.FromPoint(rect.PageHeightPt);
            using var gfx = XGraphics.FromPdfPage(page);
            DrawPdfSharpImage(gfx, ximg, rect);
            doc.Save(output, false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Document copies: PdfSharpCore could not place raster at {Size}; trying Spire.", layout.Size);
        }

        return TryWriteRasterPdfViaSpire(imageBytes, output, logger, landscape: false, layout);
    }

    private static void DrawPdfSharpImage(XGraphics gfx, XImage image, DocumentCopyDrawRect rect)
    {
        XGraphicsState state = gfx.Save();
        double centerX = rect.X + rect.Width / 2.0;
        double centerY = rect.Y + rect.Height / 2.0;
        gfx.TranslateTransform(centerX, centerY);
        gfx.RotateTransform(rect.RotationDegrees);
        bool quarterTurn = rect.RotationDegrees % 180 != 0;
        double drawW = quarterTurn ? rect.Height : rect.Width;
        double drawH = quarterTurn ? rect.Width : rect.Height;
        gfx.DrawImage(image, -drawW / 2.0, -drawH / 2.0, drawW, drawH);
        gfx.Restore(state);
    }

    /// <summary>
    /// Spire-backed raster→PDF (TIFF/JPEG/PNG and similar). Output is normal PDF suitable for <see cref="MergePdfStreams"/>.
    /// </summary>
    private static bool TryWriteRasterPdfViaSpire(
        byte[] imageBytes,
        Stream output,
        ILogger logger,
        bool landscape,
        DocumentCopyPrintLayout layout)
    {
        try
        {
            using var imageStream = new MemoryStream(imageBytes, writable: false);
            using var spireDoc = new Spire.Pdf.PdfDocument();
            PdfImage img = PdfImage.FromStream(imageStream);

            if (!layout.UsesDocumentSize)
            {
                Spire.Pdf.PdfPageBase fitPage = landscape
                    ? spireDoc.Pages.Add(
                        new System.Drawing.SizeF((float)A4HeightPt, (float)A4WidthPt),
                        new PdfMargins((float)MarginPt))
                    : spireDoc.Pages.Add(Spire.Pdf.PdfPageSize.A4, new PdfMargins((float)MarginPt));

                float pw = fitPage.Canvas.ClientSize.Width;
                float ph = fitPage.Canvas.ClientSize.Height;
                float scale = Math.Min(pw / img.Width, ph / img.Height);
                float dw = img.Width * scale;
                float dh = img.Height * scale;
                float x = (pw - dw) / 2f;
                float y = (ph - dh) / 2f;
                fitPage.Canvas.DrawImage(img, x, y, dw, dh);
            }
            else
            {
                DocumentCopyDrawRect rect = layout.Measure(img.Width, img.Height);
                Spire.Pdf.PdfPageBase page = spireDoc.Pages.Add(
                    new System.Drawing.SizeF((float)rect.PageWidthPt, (float)rect.PageHeightPt),
                    new PdfMargins(0));
                DrawSpireImage(page, img, rect);
            }

            spireDoc.SaveToStream(output, Spire.Pdf.FileFormat.PDF);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ZIP packer: Spire could not rasterize attachment for PDF merge slice.");
            return false;
        }
    }

    private static void DrawSpireImage(Spire.Pdf.PdfPageBase page, PdfImage image, DocumentCopyDrawRect rect)
    {
        bool quarterTurn = rect.RotationDegrees % 180 != 0;
        float drawW = (float)(quarterTurn ? rect.Height : rect.Width);
        float drawH = (float)(quarterTurn ? rect.Width : rect.Height);
        float centerX = (float)(rect.X + rect.Width / 2.0);
        float centerY = (float)(rect.Y + rect.Height / 2.0);
        PdfGraphicsState state = page.Canvas.Save();
        page.Canvas.TranslateTransform(centerX, centerY);
        page.Canvas.RotateTransform(rect.RotationDegrees);
        page.Canvas.DrawImage(image, -drawW / 2f, -drawH / 2f, drawW, drawH);
        page.Canvas.Restore(state);
    }

    private static void DrawSpireTemplate(Spire.Pdf.PdfPageBase page, PdfTemplate template, DocumentCopyDrawRect rect)
    {
        bool quarterTurn = rect.RotationDegrees % 180 != 0;
        float drawW = (float)(quarterTurn ? rect.Height : rect.Width);
        float drawH = (float)(quarterTurn ? rect.Width : rect.Height);
        float centerX = (float)(rect.X + rect.Width / 2.0);
        float centerY = (float)(rect.Y + rect.Height / 2.0);
        PdfGraphicsState state = page.Canvas.Save();
        page.Canvas.TranslateTransform(centerX, centerY);
        page.Canvas.RotateTransform(rect.RotationDegrees);
        page.Canvas.DrawTemplate(template, new System.Drawing.PointF(-drawW / 2f, -drawH / 2f), new System.Drawing.SizeF(drawW, drawH));
        page.Canvas.Restore(state);
    }
}
