using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace Visa2026.Module.Services;

/// <summary>
/// PdfSharpCore (MIT) merge and raster→PDF pages for batch ZIP supporting documents and
/// document-copies preview. PdfSharpCore <see cref="XImage"/> often fails on TIFF and some
/// scans; those are decoded via System.Drawing and redrawn with PdfSharpCore so Spire.PDF is
/// not used to save pages (unlicensed Spire stamps a red evaluation warning on every page).
/// </summary>
internal static class SupportingDocumentsPdfSharpHelper
{
    private const double A4WidthPt = 595;
    private const double A4HeightPt = 842;
    private const double MarginPt = 20;

    /// <summary>
    /// One A4 printable page used in document-copies section preview when a person has no scan
    /// for the requested document type. Drawn with PdfSharpCore (no Spire watermark).
    /// </summary>
    public static bool TryWriteMissingDocumentCopyPlaceholderPage(
        Stream output,
        string personName,
        string documentTypeLabel,
        string title,
        string personCaption,
        string documentTypeCaption)
    {
        ArgumentNullException.ThrowIfNull(output);

        title = string.IsNullOrWhiteSpace(title) ? "File is not uploaded yet" : title.Trim();
        personCaption = string.IsNullOrWhiteSpace(personCaption) ? "Person" : personCaption.Trim();
        documentTypeCaption = string.IsNullOrWhiteSpace(documentTypeCaption) ? "Document type" : documentTypeCaption.Trim();
        personName = string.IsNullOrWhiteSpace(personName) ? "—" : personName.Trim();
        documentTypeLabel = string.IsNullOrWhiteSpace(documentTypeLabel) ? "—" : documentTypeLabel.Trim();

        try
        {
            using var doc = new PdfDocument();
            PdfPage page = doc.AddPage();
            page.Width = A4WidthPt;
            page.Height = A4HeightPt;
            using (var gfx = XGraphics.FromPdfPage(page))
            {
                var titleFont = new XFont("Arial", 20, XFontStyle.Bold);
                var labelFont = new XFont("Arial", 12, XFontStyle.Regular);
                var valueFont = new XFont("Arial", 14, XFontStyle.Bold);
                var warnBrush = new XSolidBrush(XColor.FromArgb(245, 158, 11));
                var textBrush = new XSolidBrush(XColor.FromArgb(30, 30, 30));
                var mutedBrush = new XSolidBrush(XColor.FromArgb(90, 90, 90));

                double y = 180;

                // Warning triangle mark
                gfx.DrawString("!", titleFont, warnBrush,
                    new XRect(0, y, A4WidthPt, 28), XStringFormats.TopCenter);
                y += 48;

                gfx.DrawString(title, titleFont, textBrush,
                    new XRect(MarginPt, y, A4WidthPt - 2 * MarginPt, 36), XStringFormats.TopCenter);
                y += 64;

                gfx.DrawString(personCaption, labelFont, mutedBrush,
                    new XRect(MarginPt, y, A4WidthPt - 2 * MarginPt, 20), XStringFormats.TopCenter);
                y += 22;
                gfx.DrawString(personName, valueFont, textBrush,
                    new XRect(MarginPt, y, A4WidthPt - 2 * MarginPt, 24), XStringFormats.TopCenter);
                y += 40;

                gfx.DrawString(documentTypeCaption, labelFont, mutedBrush,
                    new XRect(MarginPt, y, A4WidthPt - 2 * MarginPt, 20), XStringFormats.TopCenter);
                y += 22;
                gfx.DrawString(documentTypeLabel, valueFont, textBrush,
                    new XRect(MarginPt, y, A4WidthPt - 2 * MarginPt, 24), XStringFormats.TopCenter);
            }

            doc.Save(output, false);
            return output.Length > 0;
        }
        catch
        {
            return false;
        }
    }

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
            using var sourceStream = new MemoryStream(pdfBytes, writable: false);
            using var form = XPdfForm.FromStream(sourceStream);
            if (form.PageCount == 0)
                return false;

            using var dest = new PdfDocument();
            for (int pageNumber = 1; pageNumber <= form.PageCount; pageNumber++)
            {
                form.PageNumber = pageNumber;
                DocumentCopyDrawRect rect = layout.Measure(form.PointWidth, form.PointHeight);
                PdfPage page = dest.AddPage();
                page.Width = XUnit.FromPoint(rect.PageWidthPt);
                page.Height = XUnit.FromPoint(rect.PageHeightPt);
                using var gfx = XGraphics.FromPdfPage(page);
                DrawPdfSharpImage(gfx, form, rect);
            }

            dest.Save(output, false);
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

        if (TryWriteFitA4RasterCore(imageBytes, output, pageWidthPt, pageHeightPt))
            return true;

        logger.LogDebug("ZIP packer: PdfSharpCore could not decode raster for merge slice; trying System.Drawing fallback.");
        if (!TryDecodeRasterToPng(imageBytes, out var png))
        {
            logger.LogWarning("ZIP packer: could not rasterize attachment for PDF merge slice.");
            return false;
        }

        return TryWriteFitA4RasterCore(png, output, pageWidthPt, pageHeightPt);
    }

    private static bool TryWriteFitA4RasterCore(
        byte[] imageBytes,
        Stream output,
        double pageWidthPt,
        double pageHeightPt)
    {
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
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryWriteDocumentSizedRaster(
        byte[] imageBytes,
        Stream output,
        ILogger logger,
        DocumentCopyPrintLayout layout)
    {
        if (TryWriteDocumentSizedRasterCore(imageBytes, output, layout))
            return true;

        logger.LogDebug(
            "Document copies: PdfSharpCore could not place raster at {Size}; trying System.Drawing.",
            layout.Size);
        if (!TryDecodeRasterToPng(imageBytes, out var png))
        {
            logger.LogWarning("Document copies: could not place raster at {Size}.", layout.Size);
            return false;
        }

        return TryWriteDocumentSizedRasterCore(png, output, layout);
    }

    private static bool TryWriteDocumentSizedRasterCore(
        byte[] imageBytes,
        Stream output,
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
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Decode TIFF/odd rasters via System.Drawing into PNG that PdfSharpCore can place.
    /// Avoids Spire.PDF SaveToStream, which stamps the evaluation warning when unlicensed.
    /// </summary>
    private static bool TryDecodeRasterToPng(byte[] imageBytes, out byte[] png)
    {
        png = Array.Empty<byte>();
        try
        {
            using var input = new MemoryStream(imageBytes, writable: false);
            using var image = Image.FromStream(input);
            using var output = new MemoryStream();
            image.Save(output, ImageFormat.Png);
            png = output.ToArray();
            return png.Length > 0;
        }
        catch (Exception)
        {
            return false;
        }
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
    /// One A4 page per visa page: the passport scan fills one half and the visa scan fills the other.
    /// Landscape (the visa default) puts the passport on the left. Portrait stacks the passport above the visa.
    /// </summary>
    public static bool TryWriteVisaBesidePassport(
        byte[] passportBytes,
        byte[] visaBytes,
        Stream output,
        ILogger logger,
        DocumentCopyPrintLayout layout)
    {
        ArgumentNullException.ThrowIfNull(passportBytes);
        ArgumentNullException.ThrowIfNull(visaBytes);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(logger);

        if (passportBytes.Length == 0 || visaBytes.Length == 0)
            return false;

        ScanHandle? passport = null;
        ScanHandle? visa = null;
        try
        {
            if (!TryOpenScan(passportBytes, logger, out passport)
                || !TryOpenScan(visaBytes, logger, out visa)
                || passport.PageCount == 0
                || visa.PageCount == 0)
                return false;

            bool portrait = layout.Orientation == DocumentCopyPageOrientation.Portrait;
            double pageW = portrait ? DocumentCopyPrintLayout.A4WidthPt : DocumentCopyPrintLayout.A4HeightPt;
            double pageH = portrait ? DocumentCopyPrintLayout.A4HeightPt : DocumentCopyPrintLayout.A4WidthPt;

            using var dest = new PdfDocument();
            for (int i = 0; i < visa.PageCount; i++)
            {
                PdfPage page = dest.AddPage();
                page.Width = XUnit.FromPoint(pageW);
                page.Height = XUnit.FromPoint(pageH);
                SplitHalves(
                    pageW,
                    pageH,
                    portrait,
                    out double leftX,
                    out double leftY,
                    out double leftW,
                    out double leftH,
                    out double rightX,
                    out double rightY,
                    out double rightW,
                    out double rightH);

                passport.NaturalSize(0, out double passportW, out double passportH);
                visa.NaturalSize(i, out double visaW, out double visaH);
                DocumentCopyDrawRect left = FitInBox(
                    leftX, leftY, leftW, leftH, passportW, passportH, layout.RotationDegrees);
                DocumentCopyDrawRect right = FitInBox(
                    rightX, rightY, rightW, rightH, visaW, visaH, layout.RotationDegrees);
                using var gfx = XGraphics.FromPdfPage(page);
                passport.Draw(gfx, 0, left);
                visa.Draw(gfx, i, right);
            }

            dest.Save(output, false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Document copies: could not place the visa beside its passport.");
            return false;
        }
        finally
        {
            passport?.Dispose();
            visa?.Dispose();
        }
    }

    internal static bool TryReadPdfPageSize(
        byte[] pdf,
        out double widthPt,
        out double heightPt,
        out int pageCount)
    {
        widthPt = 0;
        heightPt = 0;
        pageCount = 0;
        if (pdf == null || pdf.Length == 0)
            return false;

        using var stream = new MemoryStream(pdf, writable: false);
        using PdfDocument doc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        if (doc.PageCount == 0)
            return false;

        pageCount = doc.PageCount;
        widthPt = doc.Pages[0].Width.Point;
        heightPt = doc.Pages[0].Height.Point;
        return true;
    }

    private static bool TryOpenScan(byte[] bytes, ILogger logger, out ScanHandle handle)
    {
        handle = null!;
        try
        {
            if (DocumentFileUploadConstraints.IsLikelyPdf(bytes))
            {
                var stream = new MemoryStream(bytes, writable: false);
                try
                {
                    var form = XPdfForm.FromStream(stream);
                    if (form.PageCount == 0)
                    {
                        form.Dispose();
                        stream.Dispose();
                        return false;
                    }

                    handle = ScanHandle.FromPdf(form, stream);
                    return true;
                }
                catch
                {
                    stream.Dispose();
                    throw;
                }
            }

            if (TryOpenRasterScan(bytes, out handle))
                return true;

            return false;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Document copies: could not open a scan for the visa-and-passport page.");
            return false;
        }
    }

    private static bool TryOpenRasterScan(byte[] bytes, out ScanHandle handle)
    {
        handle = null!;
        try
        {
            var stream = new MemoryStream(bytes, writable: false);
            var image = XImage.FromStream(() => new MemoryStream(bytes, writable: false));
            handle = ScanHandle.FromImage(image, stream);
            return true;
        }
        catch (Exception)
        {
            // fall through to System.Drawing
        }

        if (!TryDecodeRasterToPng(bytes, out var png))
            return false;

        try
        {
            var stream = new MemoryStream(png, writable: false);
            var image = XImage.FromStream(() => new MemoryStream(png, writable: false));
            handle = ScanHandle.FromImage(image, stream);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void SplitHalves(
        double pageW,
        double pageH,
        bool portrait,
        out double firstX,
        out double firstY,
        out double firstW,
        out double firstH,
        out double secondX,
        out double secondY,
        out double secondW,
        out double secondH)
    {
        const double margin = 16;
        const double gap = 12;
        if (portrait)
        {
            firstW = secondW = pageW - 2 * margin;
            firstH = secondH = (pageH - 2 * margin - gap) / 2.0;
            firstX = secondX = margin;
            firstY = margin;
            secondY = margin + firstH + gap;
            return;
        }

        firstH = secondH = pageH - 2 * margin;
        firstW = secondW = (pageW - 2 * margin - gap) / 2.0;
        firstY = secondY = margin;
        firstX = margin;
        secondX = margin + firstW + gap;
    }

    private static DocumentCopyDrawRect FitInBox(
        double boxX,
        double boxY,
        double boxW,
        double boxH,
        double sourceWidth,
        double sourceHeight,
        int rotationDegrees)
    {
        int rotation = rotationDegrees % 360;
        if (rotation < 0)
            rotation += 360;
        rotation = rotation / 90 * 90;

        double srcW = sourceWidth <= 0 ? 1 : sourceWidth;
        double srcH = sourceHeight <= 0 ? 1 : sourceHeight;
        if (rotation % 180 != 0)
            (srcW, srcH) = (srcH, srcW);

        double scale = Math.Min(boxW / srcW, boxH / srcH);
        double drawW = srcW * scale;
        double drawH = srcH * scale;
        return new DocumentCopyDrawRect(
            0,
            0,
            boxX + (boxW - drawW) / 2.0,
            boxY + (boxH - drawH) / 2.0,
            drawW,
            drawH,
            rotation);
    }

    private sealed class ScanHandle : IDisposable
    {
        private readonly XPdfForm? form;
        private readonly XImage? image;
        private readonly MemoryStream? keepAlive;

        private ScanHandle(XPdfForm? form, XImage? image, MemoryStream? keepAlive)
        {
            this.form = form;
            this.image = image;
            this.keepAlive = keepAlive;
        }

        public static ScanHandle FromPdf(XPdfForm form, MemoryStream stream) => new(form, null, stream);

        public static ScanHandle FromImage(XImage image, MemoryStream stream) => new(null, image, stream);

        public int PageCount => form?.PageCount ?? (image == null ? 0 : 1);

        public void NaturalSize(int pageIndex, out double width, out double height)
        {
            if (image != null)
            {
                width = image.PixelWidth * 72.0 / Math.Max(image.HorizontalResolution, 1);
                height = image.PixelHeight * 72.0 / Math.Max(image.VerticalResolution, 1);
                return;
            }

            form!.PageNumber = pageIndex + 1;
            width = form.PointWidth;
            height = form.PointHeight;
        }

        public void Draw(XGraphics gfx, int pageIndex, DocumentCopyDrawRect rect)
        {
            if (image != null)
            {
                DrawPdfSharpImage(gfx, image, rect);
                return;
            }

            form!.PageNumber = pageIndex + 1;
            DrawPdfSharpImage(gfx, form, rect);
        }

        public void Dispose()
        {
            form?.Dispose();
            image?.Dispose();
            keepAlive?.Dispose();
        }
    }
}