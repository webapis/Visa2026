using System.IO;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Blazor.Server.Services;

internal static class OfficeFilePreviewResultFactory
{
    public static FilePreviewResult? FromOfficeOrPdf(
        ApplicationWordReportOfficePreviewPdfConverter converter,
        byte[]? content,
        string? fileName)
    {
        if (content == null || content.Length == 0)
            return null;

        var name = string.IsNullOrWhiteSpace(fileName) ? "template.docx" : fileName.Trim();
        var ext = Path.GetExtension(name).ToLowerInvariant();
        if (ext == ".pdf")
        {
            return new FilePreviewResult
            {
                Content = content,
                FileName = name,
                ContentType = "application/pdf",
            };
        }

        var pdf = converter.TryConvertToPdf(content, name);
        if (pdf == null || pdf.Length == 0)
            return null;

        return new FilePreviewResult
        {
            Content = pdf,
            FileName = Path.ChangeExtension(name, ".pdf"),
            ContentType = "application/pdf",
        };
    }
}