using System;

namespace Visa2026.Module.Services;

/// <summary>
/// How a passport or visa scan is placed for preview and print.
/// The PDF page stays A4 so office printers keep the scale. Passport and visa
/// scans default to a landscape sheet: the scan keeps its original size and
/// sits on the left, with the right side left blank. It is scaled down only
/// when it would not fit the sheet.
/// </summary>
public enum DocumentCopyPrintSize
{
    /// <summary>Scale the scan to fit the A4 page. Used for letters and other A4 copies.</summary>
    FitA4 = 0,

    /// <summary>ICAO TD3 / ID-3 passport page, 125 × 88 mm.</summary>
    PassportPage = 1,

    /// <summary>Open passport (two ID-3 pages), 176 × 125 mm.</summary>
    PassportOpen = 2,

    /// <summary>Visa sticker, 120 × 80 mm.</summary>
    Visa = 3,
}

/// <summary>Which edge of the document size is the long side. Auto follows the scan after rotation.</summary>
public enum DocumentCopyPageOrientation
{
    Auto = 0,
    Portrait = 1,
    Landscape = 2,
}

/// <summary>Page placement for one document-copy slice.</summary>
public readonly record struct DocumentCopyPrintLayout(
    DocumentCopyPrintSize Size,
    int RotationDegrees,
    DocumentCopyPageOrientation Orientation)
{
    public const double A4WidthPt = 595;
    public const double A4HeightPt = 842;
    public const double FitMarginPt = 20;
    public const double MillimetersPerInch = 25.4;

    public const double PassportLongMm = 125;
    public const double PassportShortMm = 88;
    public const double PassportOpenLongMm = 176;
    public const double PassportOpenShortMm = 125;
    public const double VisaLongMm = 120;
    public const double VisaShortMm = 80;

    public static DocumentCopyPrintLayout FitA4 { get; } = new(
        DocumentCopyPrintSize.FitA4,
        0,
        DocumentCopyPageOrientation.Auto);

    public bool UsesDocumentSize => Size != DocumentCopyPrintSize.FitA4;

    public static double MillimetersToPoints(double millimeters) =>
        millimeters * 72.0 / MillimetersPerInch;

    public static DocumentCopyPrintLayout DefaultFor(string? key)
    {
        if (IsVisaKey(key))
            return new(DocumentCopyPrintSize.Visa, 0, DocumentCopyPageOrientation.Landscape);

        if (IsPassportKey(key))
            return new(DocumentCopyPrintSize.PassportPage, 0, DocumentCopyPageOrientation.Landscape);

        return FitA4;
    }

    public static bool AppliesTo(string? key) => IsVisaKey(key) || IsPassportKey(key);

    public DocumentCopyPrintLayout Rotate(int deltaDegrees)
    {
        int next = RotationDegrees + deltaDegrees;
        next %= 360;
        if (next < 0)
            next += 360;

        return this with { RotationDegrees = next };
    }

    public DocumentCopyPrintLayout WithSize(DocumentCopyPrintSize size) =>
        this with { Size = size };

    public DocumentCopyPrintLayout WithOrientation(DocumentCopyPageOrientation orientation) =>
        this with { Orientation = orientation };

    /// <summary>
    /// Axis-aligned rectangle where the scan is drawn, in PDF points, origin at the top left.
    /// For a document-size layout, <paramref name="sourceWidth"/> and <paramref name="sourceHeight"/>
    /// are the scan's original size in points. Fit-to-A4 only needs a shared unit.
    /// </summary>
    public DocumentCopyDrawRect Measure(double sourceWidth, double sourceHeight, bool a4Landscape = false)
    {
        double srcW = sourceWidth <= 0 ? 1 : sourceWidth;
        double srcH = sourceHeight <= 0 ? 1 : sourceHeight;
        int rotation = NormalizeQuarterTurn(RotationDegrees);
        if (rotation % 180 != 0)
            (srcW, srcH) = (srcH, srcW);

        if (!UsesDocumentSize)
        {
            double pageW = a4Landscape ? A4HeightPt : A4WidthPt;
            double pageH = a4Landscape ? A4WidthPt : A4HeightPt;
            double contentW = pageW - 2 * FitMarginPt;
            double contentH = pageH - 2 * FitMarginPt;
            double scale = Math.Min(contentW / srcW, contentH / srcH);
            double drawW = srcW * scale;
            double drawH = srcH * scale;
            return new DocumentCopyDrawRect(
                pageW,
                pageH,
                FitMarginPt + (contentW - drawW) / 2.0,
                FitMarginPt + (contentH - drawH) / 2.0,
                drawW,
                drawH,
                rotation);
        }

        bool landscapePage = Orientation == DocumentCopyPageOrientation.Landscape;
        double sheetW = landscapePage ? A4HeightPt : A4WidthPt;
        double sheetH = landscapePage ? A4WidthPt : A4HeightPt;
        double maxW = sheetW - 2 * FitMarginPt;
        double maxH = sheetH - 2 * FitMarginPt;
        double fit = srcW > maxW || srcH > maxH
            ? Math.Min(maxW / srcW, maxH / srcH)
            : 1;
        double visW = srcW * fit;
        double visH = srcH * fit;
        return new DocumentCopyDrawRect(
            sheetW,
            sheetH,
            FitMarginPt,
            sheetH - FitMarginPt - visH,
            visW,
            visH,
            rotation);
    }

    public static bool IsVisaKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (key.Contains("/Visa:", StringComparison.OrdinalIgnoreCase))
            return true;

        if (key.StartsWith("Visa.", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("VisaDocument:", StringComparison.OrdinalIgnoreCase))
            return true;

        return key.Equals("Visa", StringComparison.OrdinalIgnoreCase)
            || key.Equals("CurrentVisas", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPassportKey(string? key)
    {
        if (IsVisaKey(key) || string.IsNullOrWhiteSpace(key))
            return false;

        return key.StartsWith("Passport", StringComparison.OrdinalIgnoreCase)
            || key.Equals("CurrentPassports", StringComparison.OrdinalIgnoreCase);
    }

    private static int NormalizeQuarterTurn(int degrees)
    {
        int next = degrees % 360;
        if (next < 0)
            next += 360;

        return next / 90 * 90;
    }
}

/// <summary>Where to draw a scan on the PDF page. Width and height are the size after rotation.</summary>
public readonly record struct DocumentCopyDrawRect(
    double PageWidthPt,
    double PageHeightPt,
    double X,
    double Y,
    double Width,
    double Height,
    int RotationDegrees);
