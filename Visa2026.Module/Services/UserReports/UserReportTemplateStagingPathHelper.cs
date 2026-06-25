using System.Text.RegularExpressions;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <summary>Builds UNC staging paths and Office open URLs for template edit workflow.</summary>
public static partial class UserReportTemplateStagingPathHelper
{
    private static readonly Regex InvalidFileNameChars = InvalidFileNameCharsRegex();

    /// <summary>Returns the configured UNC share root (trimmed, no trailing slash).</summary>
    public static string ResolveStagingUncRoot(TemplateEditStagingOptions options)
    {
        var configured = options.StagingRootUnc?.Trim().TrimEnd('\\', '/') ?? string.Empty;
        if (string.IsNullOrEmpty(configured))
            throw new InvalidOperationException("TemplateEditStaging:StagingRootUnc is not configured.");

        if (!IsUncPath(configured))
        {
            throw new InvalidOperationException(
                "TemplateEditStaging:StagingRootUnc must be a UNC path (\\\\server\\share). " +
                "Use StagingLocalPath for server-side file I/O on IIS.");
        }

        return configured;
    }

    /// <summary>Root path for export/import file I/O (local path when configured, else UNC).</summary>
    public static string ResolveStagingIoRoot(TemplateEditStagingOptions options)
    {
        var local = options.StagingLocalPath?.Trim().TrimEnd('\\', '/') ?? string.Empty;
        if (!string.IsNullOrEmpty(local))
            return local;

        return ResolveStagingUncRoot(options);
    }

    public static string ResolveStagingRoot(TemplateEditStagingOptions options) =>
        ResolveStagingIoRoot(options);

    public static bool IsUncPath(string path) =>
        !string.IsNullOrWhiteSpace(path) && path.TrimStart().StartsWith(@"\\", StringComparison.Ordinal);

    public static string GetExtension(TemplateOutputFormat format) =>
        format == TemplateOutputFormat.Excel ? ".xlsx" : ".docx";

    public static string SanitizeTemplateName(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return "template";

        var trimmed = templateName.Trim();
        var sanitized = InvalidFileNameChars.Replace(trimmed, "_");
        sanitized = sanitized.Trim('_', '.', ' ');
        if (sanitized.Length > 80)
            sanitized = sanitized[..80].TrimEnd('_', '.', ' ');

        return string.IsNullOrWhiteSpace(sanitized) ? "template" : sanitized;
    }

    public static string BuildDocumentFileName(
        TemplateEditStagingOptions options,
        Guid templateId,
        string templateName,
        TemplateOutputFormat outputFormat)
    {
        var pattern = string.IsNullOrWhiteSpace(options.FileNamePattern)
            ? "{templateId}{extension}"
            : options.FileNamePattern;

        var extension = GetExtension(outputFormat);
        var safeName = SanitizeTemplateName(templateName);
        var templateIdD = templateId.ToString("D");
        var templateIdN = templateId.ToString("N");

        return pattern
            .Replace("{templateId}", templateIdN, StringComparison.OrdinalIgnoreCase)
            .Replace("{templateIdN}", templateIdN, StringComparison.OrdinalIgnoreCase)
            .Replace("{templateIdD}", templateIdD, StringComparison.OrdinalIgnoreCase)
            .Replace("{safeName}", safeName, StringComparison.OrdinalIgnoreCase)
            .Replace("{extension}", extension, StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildDocumentPath(
        TemplateEditStagingOptions options,
        Guid templateId,
        string templateName,
        TemplateOutputFormat outputFormat)
    {
        var root = ResolveStagingIoRoot(options);
        var fileName = BuildDocumentFileName(options, templateId, templateName, outputFormat);
        return Path.Combine(root, fileName);
    }

    public static string BuildMetaFilePath(string documentPath) => documentPath + ".meta.json";

    public static string BuildUncPath(TemplateEditStagingOptions options, string documentFileName)
    {
        var root = ResolveStagingUncRoot(options);
        return Path.Combine(root, documentFileName);
    }

    /// <summary>
    /// Office protocol URL for UNC paths. Uses a percent-encoded UNC in the <c>u</c> parameter
    /// (<c>ms-word:ofe|u|%5C%5Cserver%5Cshare%5Cfile.docx</c>) — not <c>file://</c>, which Word maps to <c>\\\server\...</c>.
    /// </summary>
    public static string? TryBuildOfficeOpenUrl(string uncPath, TemplateOutputFormat outputFormat)
    {
        if (string.IsNullOrWhiteSpace(uncPath) || !IsUncPath(uncPath))
            return null;

        var protocol = outputFormat == TemplateOutputFormat.Excel ? "ms-excel" : "ms-word";
        return $"{protocol}:ofe|u|{Uri.EscapeDataString(uncPath.Trim())}";
    }

    [GeneratedRegex(@"[<>:""/\\|?*\x00-\x1F]")]
    private static partial Regex InvalidFileNameCharsRegex();
}
