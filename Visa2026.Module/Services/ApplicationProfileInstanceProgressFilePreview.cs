using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;

namespace Visa2026.Module.Services;

public sealed class ApplicationProfileInstanceProgressFilePreviewResult
{
    public required byte[] Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }
}

/// <summary>
/// One file is previewed as itself. Several files on a step become one PDF.
/// </summary>
public static class ApplicationProfileInstanceProgressFilePreview
{
    public static bool TryBuild(
        IReadOnlyList<(string Name, byte[] Content)> files,
        out ApplicationProfileInstanceProgressFilePreviewResult? result,
        out string? error)
    {
        result = null;
        error = null;
        var ready = files?
            .Where(file => file.Content is { Length: > 0 })
            .ToList() ?? [];
        if (ready.Count == 0)
        {
            error = "The file is empty.";
            return false;
        }

        if (ready.Count == 1)
        {
            var one = ready[0];
            var name = string.IsNullOrWhiteSpace(one.Name) ? "file" : one.Name.Trim();
            result = new ApplicationProfileInstanceProgressFilePreviewResult
            {
                Content = one.Content,
                FileName = name,
                ContentType = ContentType(name),
            };
            return true;
        }

        var streams = new List<MemoryStream>();
        try
        {
            foreach (var file in ready)
            {
                if (IsPdf(file.Content))
                {
                    streams.Add(new MemoryStream(file.Content, writable: false));
                    continue;
                }

                var page = new MemoryStream();
                if (!SupportingDocumentsPdfSharpHelper.TryWriteSinglePagePdfFromRasterBytes(
                        file.Content,
                        page,
                        NullLogger.Instance))
                {
                    error = "One of the files could not be added to the preview.";
                    return false;
                }

                page.Position = 0;
                streams.Add(page);
            }

            using var merged = new MemoryStream();
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(streams, merged);
            result = new ApplicationProfileInstanceProgressFilePreviewResult
            {
                Content = merged.ToArray(),
                FileName = "progress-files.pdf",
                ContentType = "application/pdf",
            };
            return result.Content.Length > 0;
        }
        catch (Exception)
        {
            error = "One of the files could not be added to the preview.";
            return false;
        }
        finally
        {
            foreach (var stream in streams)
                stream.Dispose();
        }
    }

    public static bool IsPdf(byte[] content) =>
        content.Length >= 5
        && content[0] == (byte)'%'
        && content[1] == (byte)'P'
        && content[2] == (byte)'D'
        && content[3] == (byte)'F';

    public static string ContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty);
        return ext.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tif" or ".tiff" => "image/tiff",
            _ => "application/octet-stream",
        };
    }
}
