using System.Text;
using PdfSharpCore.Pdf;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstanceProgressFilePreviewTests
{
    [Theory]
    [InlineData(new byte[] { }, false)]
    [InlineData(new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F' }, false)]
    [InlineData(new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-' }, true)]
    [InlineData(new byte[] { (byte)'%', (byte)'p', (byte)'d', (byte)'f', (byte)'-' }, false)]
    public void IsPdf_RequiresPercentPdfPrefix(byte[] content, bool expected) =>
        Assert.Equal(expected, ApplicationProfileInstanceProgressFilePreview.IsPdf(content));

    [Theory]
    [InlineData("letter.PDF", "application/pdf")]
    [InlineData("scan.png", "image/png")]
    [InlineData("scan.JPG", "image/jpeg")]
    [InlineData("scan.jpeg", "image/jpeg")]
    [InlineData("scan.gif", "image/gif")]
    [InlineData("scan.bmp", "image/bmp")]
    [InlineData("scan.tif", "image/tiff")]
    [InlineData("scan.tiff", "image/tiff")]
    [InlineData("scan.bin", "application/octet-stream")]
    [InlineData("", "application/octet-stream")]
    public void ContentType_MapsKnownExtensions(string fileName, string expected) =>
        Assert.Equal(expected, ApplicationProfileInstanceProgressFilePreview.ContentType(fileName));

    [Fact]
    public void TryBuild_RejectsEmptyPayload()
    {
        var ok = ApplicationProfileInstanceProgressFilePreview.TryBuild(
            [( "a.pdf", Array.Empty<byte>() ), ( "b.pdf", null! )],
            out var result,
            out var error);

        Assert.False(ok);
        Assert.Null(result);
        Assert.Equal("The file is empty.", error);
    }

    [Fact]
    public void TryBuild_SingleFile_ReturnsOriginalBytesAndName()
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.4 single");
        var ok = ApplicationProfileInstanceProgressFilePreview.TryBuild(
            [("  letter.PDF  ", bytes)],
            out var result,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Same(bytes, result!.Content);
        Assert.Equal("letter.PDF", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
    }

    [Fact]
    public void TryBuild_BlankSingleName_DefaultsToFile()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var ok = ApplicationProfileInstanceProgressFilePreview.TryBuild(
            [("   ", bytes)],
            out var result,
            out _);

        Assert.True(ok);
        Assert.Equal("file", result!.FileName);
        Assert.Equal("application/octet-stream", result.ContentType);
    }

    [Fact]
    public void TryBuild_MultiplePdfs_MergesIntoProgressFilesPdf()
    {
        var first = MinimalPdf("one");
        var second = MinimalPdf("two");

        var ok = ApplicationProfileInstanceProgressFilePreview.TryBuild(
            [("a.pdf", first), ("b.pdf", second)],
            out var result,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("progress-files.pdf", result!.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.True(result.Content.Length > 0);
        Assert.True(ApplicationProfileInstanceProgressFilePreview.IsPdf(result.Content));
    }

    [Fact]
    public void TryBuild_InvalidRasterAmongFiles_FailsCleanly()
    {
        var ok = ApplicationProfileInstanceProgressFilePreview.TryBuild(
            [("ok.pdf", MinimalPdf("ok")), ("bad.bin", new byte[] { 1, 2, 3, 4 })],
            out var result,
            out var error);

        Assert.False(ok);
        Assert.Null(result);
        Assert.Equal("One of the files could not be added to the preview.", error);
    }

    private static byte[] MinimalPdf(string label)
    {
        using var doc = new PdfDocument();
        doc.Info.Title = label;
        doc.AddPage();
        using var stream = new MemoryStream();
        doc.Save(stream, false);
        return stream.ToArray();
    }
}
