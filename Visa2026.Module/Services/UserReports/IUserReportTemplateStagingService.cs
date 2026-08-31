namespace Visa2026.Module.Services.UserReports;

public interface IUserReportTemplateStagingService
{
    Task<UserReportTemplateStagingExportResult> ExportForEditAsync(
        Guid templateId,
        string exportedByUserName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the stored template file for a browser download (no staging / Write gate).
    /// Returns <c>null</c> when the template or file content is missing.
    /// </summary>
    Task<UserReportTemplateStagingExportResult?> TryReadTemplateFileAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);

    Task<UserReportTemplateStagingImportResult> ImportFromUploadAsync(
        Guid templateId,
        byte[] content,
        CancellationToken cancellationToken = default);
}
