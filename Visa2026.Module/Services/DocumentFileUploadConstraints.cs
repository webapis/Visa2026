using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.Services;

/// <summary>
/// Central rules for user-uploaded <see cref="FileData"/> on <see cref="BusinessObjects.DocumentBase"/> rows
/// (passport/visa/education scans, etc.). Size limits remain on <see cref="BusinessObjects.SystemSettings"/>.
/// </summary>
public static class DocumentFileUploadConstraints
{
    /// <summary>Extensions aligned with the supporting-document ZIP packer (PDF + common raster scans).</summary>
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".gif", ".bmp"
    };

    public const string AllowedExtensionsDisplay = ".pdf, .png, .jpg, .jpeg, .tif, .tiff, .gif, .bmp";

    public const int MinBytesForFormatCheck = 8;

    /// <summary>Returns <c>true</c> when <paramref name="file"/> passes extension + non-empty content sniff (when bytes are available).</summary>
    public static bool TryValidate(FileData? file, out string? errorMessage)
    {
        errorMessage = null;
        if (file == null)
            return true;

        if (file.Size <= 0)
        {
            errorMessage = "The file is empty.";
            return false;
        }

        string ext = Path.GetExtension(file.FileName ?? string.Empty);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            errorMessage =
                $"This file type is not allowed. Use one of: {string.Join(", ", AllowedExtensions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}.";
            return false;
        }

        byte[]? content = file.Content;
        if (content == null || content.Length < MinBytesForFormatCheck)
        {
            // Bytes not loaded or too small to classify — do not block save; size / empty rules still apply.
            return true;
        }

        if (!ContentMatchesDeclaredExtension(ext, content))
        {
            errorMessage =
                "The file content does not match its extension (for example, a non-PDF renamed to .pdf). Re-export or save in the correct format.";
            return false;
        }

        return true;
    }

    private static bool ContentMatchesDeclaredExtension(string ext, byte[] content)
    {
        ReadOnlySpan<byte> span = content.AsSpan();
        ext = ext.ToLowerInvariant();
        return ext switch
        {
            ".pdf" => IsLikelyPdf(span),
            ".png" => IsPng(span),
            ".jpg" or ".jpeg" => IsJpeg(span),
            ".gif" => IsGif(span),
            ".bmp" => IsBmp(span),
            ".tif" or ".tiff" => IsTiff(span),
            _ => false
        };
    }

    public static bool IsLikelyPdf(ReadOnlySpan<byte> content) =>
        content.Length >= 5
        && content[0] == (byte)'%'
        && content[1] == (byte)'P'
        && content[2] == (byte)'D'
        && content[3] == (byte)'F';

    private static bool IsPng(ReadOnlySpan<byte> s) =>
        s.Length >= 8
        && s[0] == 0x89 && s[1] == 0x50 && s[2] == 0x4E && s[3] == 0x47
        && s[4] == 0x0D && s[5] == 0x0A && s[6] == 0x1A && s[7] == 0x0A;

    private static bool IsJpeg(ReadOnlySpan<byte> s) =>
        s.Length >= 3 && s[0] == 0xFF && s[1] == 0xD8 && s[2] == 0xFF;

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
}
