using Microsoft.Extensions.Options;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanInputNormalizerTests
{
    [Fact]
    public void Normalize_Docx_SetsWordSource()
    {
        var (normalizer, _, _, _) = ScanTestServiceFactory.Create();
        var bytes = ScanOfficeYellowExtractorTests.CreateWordFixture("hello");

        var result = normalizer.Normalize(new ScanNormalizeRequest
        {
            Content = bytes,
            FileName = "marked.docx",
        });

        Assert.Equal(ScanSourceKind.Word, result.SourceKind);
        Assert.NotNull(result.OfficePackageBytes);
        Assert.True(result.IsOfficeSource);
    }

    [Fact]
    public void Normalize_Png_ThrowsRetired()
    {
        var (normalizer, _, _, _) = ScanTestServiceFactory.Create();
        var png = ScanTestImageFactory.CreatePngWithDimensions(800, 1200);

        var ex = Assert.Throws<NotSupportedException>(() => normalizer.Normalize(new ScanNormalizeRequest
        {
            Content = png,
            FileName = "form.png",
        }));

        Assert.Contains("retired", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Normalize_Pdf_ThrowsRetired()
    {
        var (normalizer, _, _, _) = ScanTestServiceFactory.Create(maxPdfPages: 5);
        var pdf = ScanTestImageFactory.CreatePdf(2);

        var ex = Assert.Throws<NotSupportedException>(() => normalizer.Normalize(new ScanNormalizeRequest
        {
            Content = pdf,
            FileName = "form.pdf",
        }));

        Assert.Contains("retired", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}