using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Visa2026.Module.Services;

/// <summary>Normalizes names used inside <see cref="System.IO.Compression.ZipArchive"/> entries (flat, no path segments).</summary>
public static class ZipEntryFileNameSanitizer
{
    public static string Sanitize(string fileName, int maxLength = 120)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "report.bin";

        // Zip treats '/' and '\' as directory separators — keep a single flat file name.
        var leaf = fileName.Replace('\\', '/');
        var slash = leaf.LastIndexOf('/');
        if (slash >= 0 && slash < leaf.Length - 1)
            leaf = leaf[(slash + 1)..];

        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(leaf
            .Trim()
            .Select(ch => invalid.Contains(ch) || ch is '/' or '\\' ? '_' : ch)
            .ToArray())
            .Trim('_', ' ');

        if (string.IsNullOrEmpty(sanitized))
            sanitized = "report.bin";

        if (sanitized.Length > maxLength)
        {
            var ext = Path.GetExtension(sanitized);
            var baseName = Path.GetFileNameWithoutExtension(sanitized);
            int keep = Math.Max(8, maxLength - ext.Length);
            if (baseName.Length > keep)
                baseName = baseName[..keep].TrimEnd('_', ' ');
            sanitized = baseName + ext;
        }

        return sanitized;
    }

    public static string EnsureUnique(string fileName, ISet<string> usedNames)
    {
        fileName = Sanitize(fileName);
        if (usedNames.Add(fileName))
            return fileName;

        string ext = Path.GetExtension(fileName);
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        for (int i = 2; i < 1000; i++)
        {
            string candidate = $"{baseName}_{i}{ext}";
            if (usedNames.Add(candidate))
                return candidate;
        }

        string fallback = $"{baseName}_{Guid.NewGuid():N}{ext}";
        usedNames.Add(fallback);
        return fallback;
    }
}
