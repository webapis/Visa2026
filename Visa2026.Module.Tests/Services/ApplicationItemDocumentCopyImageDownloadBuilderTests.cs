using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationItemDocumentCopyImageDownloadBuilderTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];
    private static readonly byte[] Png =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00
    ];

    private static readonly byte[] Pdf = "%PDF-1.4 fake"u8.ToArray();

    [Fact]
    public void SingleJpeg_ReturnsOriginalBytesAndName()
    {
        var ok = ApplicationItemDocumentCopyImageDownloadBuilder.TryBuild(
            [Source(Jpeg, "passport-scan.jpg")],
            "Passport",
            out var content,
            out var fileName,
            out var contentType);

        Assert.True(ok);
        Assert.Equal(Jpeg, content);
        Assert.Equal("passport-scan.jpg", fileName);
        Assert.Equal("image/jpeg", contentType);
    }

    [Fact]
    public void JpegNamedPdf_UsesImageExtension()
    {
        var ok = ApplicationItemDocumentCopyImageDownloadBuilder.TryBuild(
            [Source(Jpeg, "scan.pdf")],
            "Passport",
            out var content,
            out var fileName,
            out var contentType);

        Assert.True(ok);
        Assert.Equal(Jpeg, content);
        Assert.Equal("scan.jpg", fileName);
        Assert.Equal("image/jpeg", contentType);
    }

    [Fact]
    public void TwoImages_ReturnsZipOfOriginals()
    {
        var ok = ApplicationItemDocumentCopyImageDownloadBuilder.TryBuild(
            [Source(Jpeg, "passport.jpg"), Source(Png, "visa.png")],
            "Passport",
            out var content,
            out var fileName,
            out var contentType);

        Assert.True(ok);
        Assert.Equal("Passport-images.zip", fileName);
        Assert.Equal("application/zip", contentType);
        var entries = ReadZip(content);
        Assert.Equal(new[] { "passport.jpg", "visa.png" }, entries.Keys.OrderBy(k => k, StringComparer.Ordinal));
        Assert.Equal(Jpeg, entries["passport.jpg"]);
        Assert.Equal(Png, entries["visa.png"]);
    }

    [Fact]
    public void SinglePagePdf_ReturnsOneJpeg()
    {
        var page = new byte[] { 0xFF, 0xD8, 0xFF, 0x01 };
        var ok = ApplicationItemDocumentCopyImageDownloadBuilder.TryBuild(
            [Source(Pdf, "diploma.pdf")],
            "Education",
            out var content,
            out var fileName,
            out var contentType,
            _ => new[] { page });

        Assert.True(ok);
        Assert.Equal(page, content);
        Assert.Equal("diploma.jpg", fileName);
        Assert.Equal("image/jpeg", contentType);
    }

    [Fact]
    public void MultiPagePdf_ReturnsZipOfPageJpegs()
    {
        var page1 = new byte[] { 0xFF, 0xD8, 0xFF, 0x01 };
        var page2 = new byte[] { 0xFF, 0xD8, 0xFF, 0x02 };
        var ok = ApplicationItemDocumentCopyImageDownloadBuilder.TryBuild(
            [Source(Pdf, "passport.pdf")],
            "Passport",
            out var content,
            out var fileName,
            out var contentType,
            _ => new[] { page1, page2 });

        Assert.True(ok);
        Assert.Equal("Passport-images.zip", fileName);
        Assert.Equal("application/zip", contentType);
        var entries = ReadZip(content);
        Assert.Equal(page1, entries["passport-page-01.jpg"]);
        Assert.Equal(page2, entries["passport-page-02.jpg"]);
    }

    [Fact]
    public void ImageAndPdf_ReturnsZipWithOriginalAndPage()
    {
        var page = new byte[] { 0xFF, 0xD8, 0xFF, 0x11 };
        var ok = ApplicationItemDocumentCopyImageDownloadBuilder.TryBuild(
            [Source(Jpeg, "photo.jpg"), Source(Pdf, "form.pdf")],
            "Visa",
            out var content,
            out var fileName,
            out _,
            _ => new[] { page });

        Assert.True(ok);
        Assert.Equal("Visa-images.zip", fileName);
        var entries = ReadZip(content);
        Assert.Equal(Jpeg, entries["photo.jpg"]);
        Assert.Equal(page, entries["form.jpg"]);
    }

    [Fact]
    public void UnrecognizedBytes_ReturnsFalse()
    {
        var ok = ApplicationItemDocumentCopyImageDownloadBuilder.TryBuild(
            [Source([0x01, 0x02, 0x03, 0x04], "notes.bin")],
            "Passport",
            out _,
            out _,
            out _);

        Assert.False(ok);
    }

    private static ApplicationItemDocumentImageSource Source(byte[] content, string fileName) =>
        new()
        {
            Content = content,
            FileName = fileName
        };

    private static Dictionary<string, byte[]> ReadZip(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries)
        {
            using var entryStream = entry.Open();
            using var copy = new MemoryStream();
            entryStream.CopyTo(copy);
            entries[entry.FullName] = copy.ToArray();
        }

        return entries;
    }
}
