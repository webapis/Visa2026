using System.Security.Claims;
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
