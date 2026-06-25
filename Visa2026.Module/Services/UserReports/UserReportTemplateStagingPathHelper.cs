using System.Text.RegularExpressions;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <summary>Builds local sandbox file names for template edit workflow.</summary>
public static partial class UserReportTemplateStagingPathHelper
{
    private static readonly Regex InvalidFileNameChars = InvalidFileNameCharsRegex();

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

    [GeneratedRegex(@"[<>:""/\\|?*\x00-\x1F]")]
    private static partial Regex InvalidFileNameCharsRegex();
}
