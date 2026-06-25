using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Blazor.Server.Services;

/// <summary>Resminamalar catalog wrapper for <see cref="IUserReportTemplateStagingService"/>.</summary>
public sealed class UserReportTemplateStagingUiService
{
    private readonly IUserReportTemplateStagingService _stagingService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJSRuntime _jsRuntime;
    private readonly TemplateEditStagingOptions _options;

    public UserReportTemplateStagingUiService(
        IUserReportTemplateStagingService stagingService,
        IHttpContextAccessor httpContextAccessor,
        IJSRuntime jsRuntime,
        IOptions<TemplateEditStagingOptions> options)
    {
        _stagingService = stagingService;
        _httpContextAccessor = httpContextAccessor;
        _jsRuntime = jsRuntime;
        _options = options.Value;
    }

    public bool IsEnabled => _options.Enabled;

    public bool IsLocalFolderMode =>
        _options.Enabled && _options.Mode == TemplateEditStagingMode.LocalFolder;

    public bool IsShareMode =>
        _options.Enabled && _options.Mode == TemplateEditStagingMode.Share;

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

    public async Task<UserReportTemplateStagingUiImportAllOutcome> ImportAllChangedAsync()
    {
        if (!CanEditTemplates())
        {
            return UserReportTemplateStagingUiImportAllOutcome.Fail(
                "Template edit access denied or staging is disabled.");
        }

        try
        {
            var result = await _stagingService.ImportAllChangedAsync().ConfigureAwait(false);
            return UserReportTemplateStagingUiImportAllOutcome.Ok(result);
        }
        catch (Exception ex)
        {
            return UserReportTemplateStagingUiImportAllOutcome.Fail(ex.Message);
        }
    }

    public async Task<UserReportTemplateStagingUiImportAllOutcome> ImportLocalFolderChangedAsync(
        IReadOnlyCollection<Guid> templateIds)
    {
        if (!CanEditTemplates() || !IsLocalFolderMode)
        {
            return UserReportTemplateStagingUiImportAllOutcome.Fail(
                "Template edit access denied or local-folder staging is disabled.");
        }

        try
        {
            var collect = await _jsRuntime
                .InvokeAsync<UserReportTemplateStagingLocalCollectJsResult>(
                    "visaTemplateStagingLocal.collectChangedUploads",
                    templateIds.Select(id => id.ToString()).ToArray())
                .ConfigureAwait(false);

            var results = new List<UserReportTemplateStagingImportResult>();
            foreach (var item in collect.Uploads)
            {
                if (string.Equals(item.Status, "SkippedUnchanged", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new UserReportTemplateStagingImportResult
                    {
                        TemplateId = item.TemplateId,
                        DisplayName = string.Empty,
                        Status = UserReportTemplateStagingImportStatus.SkippedUnchanged,
                    });
                    continue;
                }

                if (string.Equals(item.Status, "SkippedNotFound", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new UserReportTemplateStagingImportResult
                    {
                        TemplateId = item.TemplateId,
                        DisplayName = string.Empty,
                        Status = UserReportTemplateStagingImportStatus.SkippedNotFound,
                    });
                    continue;
                }

                if (string.Equals(item.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new UserReportTemplateStagingImportResult
                    {
                        TemplateId = item.TemplateId,
                        DisplayName = string.Empty,
                        Status = UserReportTemplateStagingImportStatus.Failed,
                        ErrorMessage = item.ErrorMessage,
                    });
                    continue;
                }

                if (!string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(item.FileBase64))
                {
                    continue;
                }

                var bytes = Convert.FromBase64String(item.FileBase64);
                var importResult = await _stagingService
                    .ImportFromUploadAsync(item.TemplateId, bytes)
                    .ConfigureAwait(false);
                results.Add(importResult);

                if (importResult.Status == UserReportTemplateStagingImportStatus.Imported
                    && !string.IsNullOrWhiteSpace(item.FileName)
                    && !string.IsNullOrWhiteSpace(item.ContentHash))
                {
                    await _jsRuntime
                        .InvokeVoidAsync(
                            "visaTemplateStagingLocal.markImported",
                            item.FileName,
                            item.ContentHash)
                        .ConfigureAwait(false);
                }
            }

            return UserReportTemplateStagingUiImportAllOutcome.Ok(
                new UserReportTemplateStagingImportAllResult { Results = results });
        }
        catch (Exception ex)
        {
            return UserReportTemplateStagingUiImportAllOutcome.Fail(ex.Message);
        }
    }

    private string ResolveUserName() =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
        ?? string.Empty;
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

public sealed class UserReportTemplateStagingUiImportAllOutcome
{
    public bool Success { get; init; }

    public UserReportTemplateStagingImportAllResult? Result { get; init; }

    public string? ErrorMessage { get; init; }

    public static UserReportTemplateStagingUiImportAllOutcome Ok(UserReportTemplateStagingImportAllResult result) =>
        new() { Success = true, Result = result };

    public static UserReportTemplateStagingUiImportAllOutcome Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
