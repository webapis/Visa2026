#nullable enable

using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanChatImageAttachmentTests
{
    [Fact]
    public void TryCreate_png_succeeds()
    {
        var png = ScanTestImageFactory.CreatePngWithDimensions(80, 60);

        Assert.True(ScanChatImageAttachment.TryCreate(png, "line12.png", 1_000_000, out var image, out var error));
        Assert.Equal(string.Empty, error);
        Assert.Equal(80, image.WidthPx);
        Assert.Equal(60, image.HeightPx);
        Assert.Equal("image/png", image.MimeType);
        Assert.True(ScanVisionImageDataUrl.TryGetDataUrl(image, out var url));
        Assert.StartsWith("data:image/png;base64,", url, StringComparison.Ordinal);
    }

    [Fact]
    public void TryCreate_rejects_pdf_name()
    {
        var png = ScanTestImageFactory.CreatePngWithDimensions(80, 60);

        Assert.False(ScanChatImageAttachment.TryCreate(png, "form.pdf", 1_000_000, out _, out var error));
        Assert.Contains("PNG or JPG", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryCreate_rejects_oversize()
    {
        var png = ScanTestImageFactory.CreatePngWithDimensions(80, 60);

        Assert.False(ScanChatImageAttachment.TryCreate(png, "x.png", 10, out _, out var error));
        Assert.Contains("too large", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DataUrl_skips_one_by_one_placeholder()
    {
        var page = new ScanPageImage
        {
            PageIndex = 0,
            PngBytes = ScanTestImageFactory.CreatePngWithDimensions(1, 1),
            WidthPx = 1,
            HeightPx = 1,
        };

        Assert.False(ScanVisionImageDataUrl.TryGetDataUrl(page, out _));
    }
}