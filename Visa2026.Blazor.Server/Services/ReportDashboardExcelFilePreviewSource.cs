using System;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Serves a generated Report Dashboard Excel sheet as PDF in #visa-preview-slot; Download keeps .xlsx.
/// </summary>
public sealed class ReportDashboardExcelFilePreviewSource : IFilePreviewSource
{
    public const string Key = "report-dashboard-excel";

    private readonly IReportDashboardExcelPreviewStore _store;
    private readonly ApplicationWordReportOfficePreviewPdfConverter _converter;

    public ReportDashboardExcelFilePreviewSource(
        IReportDashboardExcelPreviewStore store,
        ApplicationWordReportOfficePreviewPdfConverter converter)
    {
        _store = store;
        _converter = converter;
    }

    public string SourceType => Key;

    public Task<FilePreviewResult?> TryLoadAsync(Guid objectId)
    {
        if (!_store.TryGet(objectId, out var entry))
            return Task.FromResult<FilePreviewResult?>(null);

        var pdf = _converter.TryConvertToPdf(entry.XlsxContent, entry.FileName);
        if (pdf == null || pdf.Length == 0)
            return Task.FromResult<FilePreviewResult?>(null);

        var pdfName = Path.ChangeExtension(entry.FileName, ".pdf") ?? "report.pdf";
        return Task.FromResult<FilePreviewResult?>(new FilePreviewResult
        {
            Content = pdf,
            // Drawer title uses FileName; keep the report label, with .pdf so preview kind resolves.
            FileName = string.IsNullOrWhiteSpace(entry.DisplayName)
                ? pdfName
                : entry.DisplayName.Trim() + ".pdf",
            ContentType = "application/pdf",
            DownloadContent = entry.XlsxContent,
            DownloadFileName = entry.FileName,
            DownloadContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        });
    }
}