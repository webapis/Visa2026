using DevExpress.ExpressApp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Blazor.Server.Services;

/// <summary>Resminamalar catalog wrapper for <see cref="IUserReportTemplateStagingService"/>.</summary>
public sealed class UserReportTemplateStagingUiService
{
    private readonly IUserReportTemplateStagingService _stagingService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TemplateEditStagingOptions _options;

    public UserReportTemplateStagingUiService(
        IUserReportTemplateStagingService stagingService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<TemplateEditStagingOptions> options)
    {
        _stagingService = stagingService;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public bool IsEnabled => _options.Enabled;

    public bool CanEditTemplates() =>
        IsEnabled && UserReportTemplateEditAccess.CanEditTemplates();

    public async Task<UserReportTemplateStagingUiExportOutcome> ExportForEditAsync(Guid templateId)
    {
        if (!CanEditTemplates())
        {
            return UserReportTemplateStagingUiExportOutcome.Fail(
                "Template edit access denied or staging is disabled.");
        }

        try
        {
            var result = await _stagingService
                .ExportForEditAsync(templateId, ResolveUserName())
                .ConfigureAwait(false);
            return UserReportTemplateStagingUiExportOutcome.Ok(result);
        }
        catch (Exception ex)
        {
            return UserReportTemplateStagingUiExportOutcome.Fail(ex.Message);
        }
    }

    /// <summary>Browser download of the stored template (.docx/.xlsx) — no staging Write gate.</summary>
    public async Task<UserReportTemplateStagingUiExportOutcome> DownloadTemplateAsync(Guid templateId)
    {
        try
        {
            var result = await _stagingService
                .TryReadTemplateFileAsync(templateId)
                .ConfigureAwait(false);
            if (result == null || result.FileContent == null || result.FileContent.Length == 0)
            {
                return UserReportTemplateStagingUiExportOutcome.Fail(
                    "Template file has no content.");
            }

            return UserReportTemplateStagingUiExportOutcome.Ok(result);
        }
        catch (Exception ex)
        {
            return UserReportTemplateStagingUiExportOutcome.Fail(ex.Message);
        }
    }

    public static UserReportTemplateStagingImportAllResult MapCollectResult(
        UserReportTemplateStagingLocalCollectJsResult collect) =>
        new()
        {
            Results = collect.Uploads
                .Select(MapStagingUploadResult)
                .ToList(),
        };

    private static UserReportTemplateStagingImportResult MapStagingUploadResult(
        UserReportTemplateStagingLocalUploadJsItem item) =>
        new()
        {
            TemplateId = item.TemplateId,
            DisplayName = item.DisplayName ?? string.Empty,
            Status = ParseStagingUploadStatus(item.Status),
            ErrorMessage = item.ErrorMessage,
        };

    private static UserReportTemplateStagingImportStatus ParseStagingUploadStatus(string? status) =>
        status switch
        {
            nameof(UserReportTemplateStagingImportStatus.Imported) => UserReportTemplateStagingImportStatus.Imported,
            nameof(UserReportTemplateStagingImportStatus.SkippedUnchanged) => UserReportTemplateStagingImportStatus.SkippedUnchanged,
            nameof(UserReportTemplateStagingImportStatus.SkippedNotFound) => UserReportTemplateStagingImportStatus.SkippedNotFound,
            _ => UserReportTemplateStagingImportStatus.Failed,
        };

    private string ResolveUserName()
    {
        try
        {
            var xafUserName = SecuritySystem.CurrentUserName;
            if (!string.IsNullOrWhiteSpace(xafUserName))
            {
                return xafUserName;
            }
        }
        catch (InvalidOperationException)
        {
        }

        return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? string.Empty;
    }
}

public sealed class UserReportTemplateStagingUiExportOutcome
{
    public bool Success { get; init; }

    public UserReportTemplateStagingExportResult? Result { get; init; }

    public string? ErrorMessage { get; init; }

    public static UserReportTemplateStagingUiExportOutcome Ok(UserReportTemplateStagingExportResult result) =>
        new() { Success = true, Result = result };

    public static UserReportTemplateStagingUiExportOutcome Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
