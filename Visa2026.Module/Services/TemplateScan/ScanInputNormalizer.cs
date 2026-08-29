#nullable enable

using Microsoft.Extensions.Options;
using Spire.Pdf;

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanInputNormalizer
{
    ScanNormalizedInput Normalize(ScanNormalizeRequest request);
}

public sealed class ScanInputNormalizer : IScanInputNormalizer
{
    private readonly IOptions<TemplateAiScanOptions> _options;

    public ScanInputNormalizer(IOptions<TemplateAiScanOptions> options)
    {
        _options = options;
    }

    public ScanNormalizedInput Normalize(ScanNormalizeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Content is not { Length: > 0 })
            throw new ArgumentException("Scan content is empty.", nameof(request));

        var fileName = request.FileName ?? string.Empty;
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".docx" => NormalizeOffice(request, ScanSourceKind.Word),
            ".xlsx" => NormalizeOffice(request, ScanSourceKind.Excel),
            ".png" or ".jpg" or ".jpeg" or ".pdf" => throw new NotSupportedException(
                "PNG, JPG, and PDF uploads are retired. Use a yellow-marked Word (.docx) or Excel (.xlsx) file."),
            _ => throw new NotSupportedException(
                $"Unsupported format '{extension}'. Use Word (.docx) or Excel (.xlsx) with yellow highlights."),
        };
    }

    private ScanNormalizedInput NormalizeImage(ScanNormalizeRequest request, string extension)
    {
        if (!ScanImageDimensionReader.TryReadImageDimensions(request.Content, out var width, out var height))
            throw new InvalidOperationException("Could not read image dimensions from the upload.");

        var pngBytes = extension == ".png"
            ? request.Content
            : request.Content;

        return new ScanNormalizedInput
        {
            SourceKind = ScanSourceKind.Image,
            Pages =
            [
                new ScanPageImage
                {
                    PageIndex = 0,
                    PngBytes = pngBytes,
                    WidthPx = width,
                    HeightPx = height,
                },
            ],
            OriginalByteLength = request.Content.LongLength,
            FileName = request.FileName,
        };
    }


    private static ScanNormalizedInput NormalizeOffice(ScanNormalizeRequest request, ScanSourceKind kind)
    {
        return new ScanNormalizedInput
        {
            SourceKind = kind,
            Pages =
            [
                new ScanPageImage
                {
                    PageIndex = 0,
                    PngBytes = ScanRasterPlaceholder.OneByOneWhitePng,
                    WidthPx = 800,
                    HeightPx = 1100,
                },
            ],
            OriginalByteLength = request.Content.LongLength,
            FileName = request.FileName,
            OfficePackageBytes = request.Content,
        };
    }
    private ScanNormalizedInput NormalizePdf(ScanNormalizeRequest request)
    {
        using var document = new PdfDocument();
        document.LoadFromBytes(request.Content);

        var totalPages = document.Pages.Count;
        if (totalPages == 0)
            throw new InvalidOperationException("The PDF contains no pages.");

        var maxPages = Math.Max(1, _options.Value.MaxPdfPages);
        if (totalPages > maxPages)
        {
            throw new ScanNormalizationException(
                ScanSuitabilityIssueCode.TooManyPages,
                $"The PDF has {totalPages} pages; the maximum for this release is {maxPages}. Select fewer pages or split the file.");
        }

        var selected = ResolveSelectedPages(request.SelectedPages, totalPages);
        var pages = new List<ScanPageImage>(selected.Count);

        foreach (var pageNumber in selected)
        {
            var pageIndex = pageNumber - 1;
            var page = document.Pages[pageIndex];
            var size = page.Size;
            var width = (int)Math.Round(size.Width);
            var height = (int)Math.Round(size.Height);
            if (width <= 0)
                width = 595;
            if (height <= 0)
                height = 842;

            pages.Add(new ScanPageImage
            {
                PageIndex = pageIndex,
                PngBytes = ScanRasterPlaceholder.OneByOneWhitePng,
                WidthPx = width,
                HeightPx = height,
            });
        }

        return new ScanNormalizedInput
        {
            SourceKind = ScanSourceKind.Pdf,
            Pages = pages,
            OriginalByteLength = request.Content.LongLength,
            FileName = request.FileName,
        };
    }

    private static IReadOnlyList<int> ResolveSelectedPages(IReadOnlyList<int>? selectedPages, int totalPages)
    {
        if (selectedPages == null || selectedPages.Count == 0)
            return Enumerable.Range(1, totalPages).ToList();

        var distinct = selectedPages
            .Where(p => p >= 1 && p <= totalPages)
            .Distinct()
            .OrderBy(static p => p)
            .ToList();

        if (distinct.Count == 0)
            throw new ArgumentException("No valid PDF pages were selected.");

        return distinct;
    }
}

public sealed class ScanNormalizationException : Exception
{
    public ScanNormalizationException(ScanSuitabilityIssueCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public ScanSuitabilityIssueCode Code { get; }
}

internal static class ScanRasterPlaceholder
{
    internal static readonly byte[] OneByOneWhitePng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82,
    ];
}
