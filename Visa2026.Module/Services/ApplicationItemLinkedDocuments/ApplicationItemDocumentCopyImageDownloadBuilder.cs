using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

public sealed class ApplicationItemDocumentImageSource
{
    public required byte[] Content { get; init; }

    public required string FileName { get; init; }
}

/// <summary>
/// Builds an image download from document-copy source files.
/// A single picture is returned as stored. PDF pages are JPEG. Several files or pages become a ZIP.
/// </summary>
public static class ApplicationItemDocumentCopyImageDownloadBuilder
{
    public const int PdfPageDpi = 200;

    public static bool TryBuild(
        IReadOnlyList<ApplicationItemDocumentImageSource> sources,
        string archiveBaseName,
        out byte[] content,
        out string fileName,
        out string contentType) =>
        TryBuild(sources, archiveBaseName, out content, out fileName, out contentType, renderPdfPages: null);

    internal static bool TryBuild(
        IReadOnlyList<ApplicationItemDocumentImageSource> sources,
        string archiveBaseName,
        out byte[] content,
        out string fileName,
        out string contentType,
        Func<byte[], IReadOnlyList<byte[]>> renderPdfPages)
    {
        content = Array.Empty<byte>();
        fileName = string.Empty;
        contentType = string.Empty;

        if (sources == null || sources.Count == 0)
            return false;

        var render = renderPdfPages ?? RenderPdfPages;
        var parts = new List<ImagePart>();
        foreach (var source in sources)
        {
            if (source?.Content == null || source.Content.Length == 0)
                continue;

            if (DocumentFileUploadConstraints.IsLikelyPdf(source.Content))
            {
                AddPdfPages(parts, source.FileName, render(source.Content));
                continue;
            }

            if (TryGetRasterExtension(source.Content, out var signatureExtension))
                parts.Add(new ImagePart(OriginalImageFileName(source.FileName, signatureExtension), source.Content));
        }

        if (parts.Count == 0)
            return false;

        if (parts.Count == 1)
        {
            content = parts[0].Content;
            fileName = ZipEntryFileNameSanitizer.Sanitize(parts[0].FileName);
            contentType = ContentTypeFor(fileName);
            return content.Length > 0 && !string.IsNullOrWhiteSpace(fileName);
        }

        fileName = BuildArchiveFileName(archiveBaseName);
        contentType = "application/zip";
        content = BuildZip(parts);
        return content.Length > 0;
    }

    private static void AddPdfPages(List<ImagePart> parts, string sourceFileName, IReadOnlyList<byte[]> pages)
    {
        if (pages == null || pages.Count == 0)
            return;

        var baseName = BaseName(sourceFileName);
        if (pages.Count == 1)
        {
            if (pages[0] != null && pages[0].Length > 0)
                parts.Add(new ImagePart(baseName + ".jpg", pages[0]));
            return;
        }

        for (var i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            if (page == null || page.Length == 0)
                continue;

            parts.Add(new ImagePart($"{baseName}-page-{i + 1:00}.jpg", page));
        }
    }

    private static IReadOnlyList<byte[]> RenderPdfPages(byte[] pdf)
    {
        var pages = new List<byte[]>();
        try
        {
            using var document = new Spire.Pdf.PdfDocument();
            document.LoadFromBytes(pdf);
            for (var i = 0; i < document.Pages.Count; i++)
            {
                using var image = document.SaveAsImage(i, PdfPageDpi, PdfPageDpi);
                if (image == null)
                    continue;

                using var output = new MemoryStream();
                image.Save(output, ImageFormat.Jpeg);
                if (output.Length > 0)
                    pages.Add(output.ToArray());
            }
        }
        catch (Exception)
        {
            return Array.Empty<byte[]>();
        }

        return pages;
    }

    private static byte[] BuildZip(IReadOnlyList<ImagePart> parts)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var part in parts)
            {
                var entryName = ZipEntryFileNameSanitizer.EnsureUnique(part.FileName, usedNames);
                var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                entryStream.Write(part.Content, 0, part.Content.Length);
            }
        }

        return zipStream.ToArray();
    }

    private static string BuildArchiveFileName(string archiveBaseName)
    {
        var baseName = string.IsNullOrWhiteSpace(archiveBaseName)
            ? "document-copies"
            : Path.GetFileNameWithoutExtension(archiveBaseName.Trim());
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "document-copies";

        return ZipEntryFileNameSanitizer.Sanitize(baseName + "-images.zip");
    }

    private static string OriginalImageFileName(string fileName, string signatureExtension)
    {
        var name = string.IsNullOrWhiteSpace(fileName) ? "image" + signatureExtension : Path.GetFileName(fileName);
        var extension = Path.GetExtension(name);
        if (!ExtensionMatchesRaster(extension, signatureExtension))
        {
            var baseName = string.IsNullOrWhiteSpace(extension) ? name : Path.GetFileNameWithoutExtension(name);
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "image";
            name = baseName + signatureExtension;
        }

        return name;
    }

    private static bool ExtensionMatchesRaster(string fileExtension, string signatureExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
            return false;

        if (signatureExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
        {
            return fileExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || fileExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        if (signatureExtension.Equals(".tif", StringComparison.OrdinalIgnoreCase))
        {
            return fileExtension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
                || fileExtension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);
        }

        return fileExtension.Equals(signatureExtension, StringComparison.OrdinalIgnoreCase);
    }

    private static string BaseName(string fileName)
    {
        var name = string.IsNullOrWhiteSpace(fileName) ? "document" : Path.GetFileNameWithoutExtension(fileName);
        return string.IsNullOrWhiteSpace(name) ? "document" : name.Trim();
    }

    private static string ContentTypeFor(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tif" or ".tiff" => "image/tiff",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    private static bool TryGetRasterExtension(byte[] content, out string extension)
    {
        var span = content.AsSpan();
        if (IsJpeg(span))
        {
            extension = ".jpg";
            return true;
        }

        if (IsPng(span))
        {
            extension = ".png";
            return true;
        }

        if (IsGif(span))
        {
            extension = ".gif";
            return true;
        }

        if (IsBmp(span))
        {
            extension = ".bmp";
            return true;
        }

        if (IsTiff(span))
        {
            extension = ".tif";
            return true;
        }

        extension = string.Empty;
        return false;
    }

    private static bool IsJpeg(ReadOnlySpan<byte> s) =>
        s.Length >= 3 && s[0] == 0xFF && s[1] == 0xD8 && s[2] == 0xFF;

    private static bool IsPng(ReadOnlySpan<byte> s) =>
        s.Length >= 8
        && s[0] == 0x89 && s[1] == 0x50 && s[2] == 0x4E && s[3] == 0x47
        && s[4] == 0x0D && s[5] == 0x0A && s[6] == 0x1A && s[7] == 0x0A;

    private static bool IsGif(ReadOnlySpan<byte> s) =>
        s.Length >= 6
        && s[0] == (byte)'G' && s[1] == (byte)'I' && s[2] == (byte)'F' && s[3] == (byte)'8'
        && (s[4] == (byte)'7' || s[4] == (byte)'9') && s[5] == (byte)'a';

    private static bool IsBmp(ReadOnlySpan<byte> s) =>
        s.Length >= 2 && s[0] == (byte)'B' && s[1] == (byte)'M';

    private static bool IsTiff(ReadOnlySpan<byte> s) =>
        s.Length >= 4
        && ((s[0] == (byte)'I' && s[1] == (byte)'I' && s[2] == 0x2A && s[3] == 0x00)
            || (s[0] == (byte)'M' && s[1] == (byte)'M' && s[2] == 0x00 && s[3] == 0x2A));

    private sealed class ImagePart
    {
        public ImagePart(string fileName, byte[] content)
        {
            FileName = fileName;
            Content = content;
        }

        public string FileName { get; }

        public byte[] Content { get; }
    }
}
