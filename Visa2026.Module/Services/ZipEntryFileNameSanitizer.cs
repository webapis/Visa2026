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
    /// Builds a flat zip entry name from a report label (e.g. <c>GT-15_Elyasow_uzt.docx</c>).
    /// Application number and date belong on the outer <c>Resminamalar_…_….zip</c> name only.
    /// </summary>
    public static string BuildReportEntryName(string reportLabel, string extension)
    {
        string label = SanitizeReportLabel(reportLabel);
        string ext = extension.StartsWith('.') ? extension : "." + extension;
        return Sanitize($"{label}{ext}", maxLength: 120);
    }

    /// <summary>
    /// Strips <c>_{applicationNumber}</c> and optional <c>_yyyyMMdd</c> suffixes from legacy
    /// User report template display names for bundle entries.
    /// </summary>
    public static string ToBundleEntryName(string reportFileName, string applicationNumber)
    {
        string ext = Path.GetExtension(reportFileName);
        string baseName = Path.GetFileNameWithoutExtension(reportFileName);
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "Report";

        string flatApp = FlattenApplicationNumber(applicationNumber);
        if (!string.Equals(flatApp, "APP", StringComparison.Ordinal))
        {
            string appSuffix = "_" + flatApp;
            if (baseName.EndsWith(appSuffix, StringComparison.OrdinalIgnoreCase))
            {
                baseName = baseName[..^appSuffix.Length];
            }
            else
            {
                string appAndUnderscore = appSuffix + "_";
                int idx = baseName.LastIndexOf(appAndUnderscore, StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                {
                    string datePart = baseName[(idx + appAndUnderscore.Length)..];
                    if (datePart.Length == 8 && datePart.All(char.IsDigit))
                        baseName = baseName[..idx];
                }
            }
        }

        return Sanitize(baseName + ext, maxLength: 120);
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
