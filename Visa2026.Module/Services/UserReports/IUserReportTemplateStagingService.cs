namespace Visa2026.Module.Services.UserReports;

public interface IUserReportTemplateStagingService
{
    Task<UserReportTemplateStagingExportResult> ExportForEditAsync(
        Guid templateId,
        string exportedByUserName,
        CancellationToken cancellationToken = default);

    Task<UserReportTemplateStagingImportResult> TryImportAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);

    Task<UserReportTemplateStagingImportAllResult> ImportAllChangedAsync(
        CancellationToken cancellationToken = default);
}
