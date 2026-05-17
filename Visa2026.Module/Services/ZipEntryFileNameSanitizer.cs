using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Visa2026.Module.Services;

/// <summary>Normalizes names used inside <see cref="System.IO.Compression.ZipArchive"/> entries (flat, no path segments).</summary>
public static class ZipEntryFileNameSanitizer
{
    /// <summary>
    /// Builds a zip entry name such as <c>Personnel list (seed)_3_-433.docx</c>.
    /// Application numbers like <c>3/-433</c> are flattened to <c>3_-433</c>, not treated as folders.
    /// </summary>
    public static string BuildReportEntryName(string reportLabel, string applicationNumber, string extension)
    {
        string label = SanitizeReportLabel(reportLabel);
        string app = FlattenApplicationNumber(applicationNumber);
        string ext = extension.StartsWith('.') ? extension : "." + extension;
        return Sanitize($"{label}_{app}{ext}", maxLength: 120);
    }

    public static string FlattenApplicationNumber(string applicationNumber)
    {
        if (string.IsNullOrWhiteSpace(applicationNumber))
            return "APP";

        return Sanitize(applicationNumber, maxLength: 48);
    }

    private static string SanitizeReportLabel(string reportLabel)
    {
        if (string.IsNullOrWhiteSpace(reportLabel))
            return "Report";

        string label = reportLabel.Trim();
        string ext = Path.GetExtension(label);
        if (!string.IsNullOrEmpty(ext)
            && (ext.Equals(".docx", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)))
        {
            label = Path.GetFileNameWithoutExtension(label);
        }

        return Sanitize(label, maxLength: 80);
    }

    public static string Sanitize(string fileName, int maxLength = 120)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "report.bin";

        // Flatten slashes in application numbers (e.g. "3/-433") — do not keep only the segment after "/".
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);
        foreach (var ch in fileName.Trim())
        {
            if (ch is '/' or '\\')
                builder.Append('_');
            else if (invalid.Contains(ch))
                builder.Append('_');
            else
                builder.Append(ch);
        }

        var sanitized = CollapseUnderscores(builder.ToString()).Trim('_', ' ');

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

    private static string CollapseUnderscores(string value)
    {
        while (value.Contains("__", StringComparison.Ordinal))
            value = value.Replace("__", "_", StringComparison.Ordinal);
        return value;
    }
}
