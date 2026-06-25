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

    public bool CanEditTemplates() =>
        IsEnabled && UserReportTemplateEditAccess.CanEditTemplates();

    public string LocalSandboxRelativePath =>
        string.IsNullOrWhiteSpace(_options.LocalFolderSubfolderName)
            ? @"Visa2026\TemplateEdit"
            : _options.LocalFolderSubfolderName.Trim().Trim('\\', '/');

    /// <summary>
    /// Best-effort Windows path for Office <c>ms-word:</c> / <c>ms-excel:</c> open URLs when FSA cannot expose a real path.
    /// Uses the signed-in user name to build <c>%LOCALAPPDATA%\{relative}</c> on the officer PC.
    /// </summary>
    public string BuildSuggestedLocalFolderPathHint()
    {
        var relative = LocalSandboxRelativePath;
        var userName = ResolveUserName();
        if (!string.IsNullOrWhiteSpace(userName))
        {
            var shortName = userName;
            var slash = userName.IndexOf('\\');
            if (slash >= 0 && slash < userName.Length - 1)
            {
                shortName = userName[(slash + 1)..];
            }
            else
            {
                var at = userName.IndexOf('@');
                if (at > 0)
                {
                    shortName = userName[..at];
                }
            }

            return Path.Combine($@"C:\Users\{shortName}\AppData\Local", relative);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            relative);
    }

    /// <summary>
    /// Best-effort full path from the folder name returned by the browser picker (leaf segment only).
    /// AppData default when the picked folder matches <see cref="LocalSandboxRelativePath"/> leaf; otherwise Documents.
    /// </summary>
    public string BuildLocalFolderPathHint(string? pickedFolderName)
    {
        if (string.IsNullOrWhiteSpace(pickedFolderName))
        {
            return BuildSuggestedLocalFolderPathHint();
        }

        var relativeLeaf = LocalSandboxRelativePath
            .Split('/', '\\')
            .LastOrDefault(segment => !string.IsNullOrWhiteSpace(segment))
            ?? "TemplateEdit";

        if (string.Equals(pickedFolderName.Trim(), relativeLeaf, StringComparison.OrdinalIgnoreCase))
        {
            return BuildSuggestedLocalFolderPathHint();
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            pickedFolderName.Trim());
    }

    /// <summary>Updates IndexedDB path hint from the configured FSA folder name (for Copy path / ms-word:).</summary>
    public async Task SyncLocalFolderPathHintAsync()
    {
        try
        {
            var folderName = await _jsRuntime
                .InvokeAsync<string>("visaTemplateStagingLocal.getFolderName")
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(folderName))
            {
                return;
            }

            var pathHint = BuildLocalFolderPathHint(folderName);
            await _jsRuntime
                .InvokeAsync<bool>("visaTemplateStagingLocal.setFolderPathHint", pathHint)
                .ConfigureAwait(false);
        }
        catch (JSException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
    }

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

    public async Task<UserReportTemplateStagingUiImportAllOutcome> ImportSandboxChangedAsync(
        IReadOnlyCollection<Guid> templateIds)
    {
        if (!CanEditTemplates())
        {
            return UserReportTemplateStagingUiImportAllOutcome.Fail(
                "Template edit access denied or staging is disabled.");
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
