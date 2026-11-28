namespace Visa2026.Module.Services.UserReports;

public interface IUserReportTemplateStagingService
{
    Task<UserReportTemplateStagingExportResult> ExportForEditAsync(
        Guid templateId,
        string exportedByUserName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Same bytes as <see cref="ExportForEditAsync"/> for an authenticated HTTP request.
    /// Skips <c>SecuritySystem</c> — that context exists only on a Blazor circuit.
    /// The controller is <c>[Authorize]</c>.
    /// </summary>
    Task<UserReportTemplateStagingExportResult> ExportForAuthorizedHttpAsync(
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
