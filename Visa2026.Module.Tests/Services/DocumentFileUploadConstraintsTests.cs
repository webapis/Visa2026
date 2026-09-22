using System;
using System.Collections.Generic;
using System.Text;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class DocumentFileUploadConstraintsTests
{
    [Fact]
    public void TryValidate_NullFile_IsValid()
    {
        Assert.True(DocumentFileUploadConstraints.TryValidate(null, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_EmptyFile_ReturnsError()
    {
        var file = new FileData { FileName = "scan.pdf", Content = Array.Empty<byte>() };

        Assert.False(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.Equal("The file is empty.", error);
    }

    [Theory]
    [InlineData("malware.exe")]
    [InlineData("noextension")]
    [InlineData("notes.txt")]
    public void TryValidate_DisallowedExtension_ReturnsError(string fileName)
    {
        var file = CreateFile(fileName, Encoding.ASCII.GetBytes("%PDF-1.4 enough"));

        Assert.False(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.Contains("not allowed", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryValidate_PdfMagicWithPdfExtension_IsValid()
    {
        var file = CreateFile("form.PDF", Encoding.ASCII.GetBytes("%PDF-1.7........"));

        Assert.True(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_PngBytesWithPdfExtension_IsContentMismatch()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
        var file = CreateFile("fake.pdf", png);

        Assert.False(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.Contains("does not match", error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("photo.JPG", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 })]
    [InlineData("photo.jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 })]
    [InlineData("icon.png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })]
    [InlineData("anim.gif", new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a', 0, 0 })]
    [InlineData("scan.bmp", new byte[] { (byte)'B', (byte)'M', 0, 0, 0, 0, 0, 0 })]
    [InlineData("scan.tif", new byte[] { (byte)'I', (byte)'I', 0x2A, 0x00, 0, 0, 0, 0 })]
    [InlineData("scan.tiff", new byte[] { (byte)'M', (byte)'M', 0x00, 0x2A, 0, 0, 0, 0 })]
    public void TryValidate_MatchingRasterMagic_IsValid(string fileName, byte[] content)
    {
        var file = CreateFile(fileName, content);

        Assert.True(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_ContentTooShortToSniff_DoesNotBlock()
    {
        var file = CreateFile("tiny.pdf", new byte[] { 1, 2, 3 });

        Assert.True(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_ContentNull_DoesNotBlockWhenSizePositive()
    {
        var file = new FileData { FileName = "deferred.pdf", Size = 128, Content = null };

        Assert.True(DocumentFileUploadConstraints.TryValidate(file, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void IsLikelyPdf_RequiresPercentPdfPrefix()
    {
        Assert.True(DocumentFileUploadConstraints.IsLikelyPdf(Encoding.ASCII.GetBytes("%PDF-1.4")));
        Assert.False(DocumentFileUploadConstraints.IsLikelyPdf(Encoding.ASCII.GetBytes("PDF-1.4")));
        Assert.False(DocumentFileUploadConstraints.IsLikelyPdf(ReadOnlySpan<byte>.Empty));
    }

    private static FileData CreateFile(string fileName, byte[] content) =>
        new()
        {
            FileName = fileName,
            Content = content,
            Size = content.Length
        };
}
