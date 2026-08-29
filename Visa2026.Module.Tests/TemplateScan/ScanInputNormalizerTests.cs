using Microsoft.Extensions.Options;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanInputNormalizerTests
{
    [Fact]
    public void Normalize_Png_ReadsDimensions()
    {
        var (normalizer, _, _, _) = ScanTestServiceFactory.Create();
        var png = ScanTestImageFactory.CreatePngWithDimensions(800, 1200);

        var result = normalizer.Normalize(new ScanNormalizeRequest
        {
            Content = png,
            FileName = "form.png",
        });

        Assert.Equal(ScanSourceKind.Image, result.SourceKind);
        Assert.Single(result.Pages);
        Assert.Equal(800, result.Pages[0].WidthPx);
        Assert.Equal(1200, result.Pages[0].HeightPx);
    }

    [Fact]
    public void Normalize_PdfExceedingMaxPages_Throws()
    {
        var (normalizer, _, _, _) = ScanTestServiceFactory.Create(maxPdfPages: 5);
        var pdf = ScanTestImageFactory.CreatePdf(6);

        var ex = Assert.Throws<ScanNormalizationException>(() => normalizer.Normalize(new ScanNormalizeRequest
        {
            Content = pdf,
            FileName = "form.pdf",
        }));

        Assert.Equal(ScanSuitabilityIssueCode.TooManyPages, ex.Code);
    }

    [Fact]
    public void Normalize_PdfWithinLimit_ReturnsPageMetadata()
    {
        var (normalizer, _, _, _) = ScanTestServiceFactory.Create(maxPdfPages: 5);
        var pdf = ScanTestImageFactory.CreatePdf(2);

        var result = normalizer.Normalize(new ScanNormalizeRequest
        {
            Content = pdf,
            FileName = "form.pdf",
        });

        Assert.Equal(ScanSourceKind.Pdf, result.SourceKind);
        Assert.Equal(2, result.Pages.Count);
    }
}
