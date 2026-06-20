using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Blazor.Server.Controllers;

[ApiController]
[Route("api/user-report-templates")]
[Authorize]
public sealed class UserReportTemplateStagingController : ControllerBase
{
    private readonly IUserReportTemplateStagingService _stagingService;

    public UserReportTemplateStagingController(IUserReportTemplateStagingService stagingService) =>
        _stagingService = stagingService;

    [HttpPost("{templateId:guid}/staging/export")]
    public async Task<ActionResult<StagingExportResponse>> Export(Guid templateId, CancellationToken cancellationToken)
    {
        if (templateId == Guid.Empty)
            return BadRequest();

        try
        {
            var result = await _stagingService
                .ExportForEditAsync(templateId, ResolveUserName(), cancellationToken)
                .ConfigureAwait(false);
            return Ok(StagingExportResponse.From(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("staging/import-all")]
    public async Task<ActionResult<StagingImportAllResponse>> ImportAll(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stagingService.ImportAllChangedAsync(cancellationToken).ConfigureAwait(false);
            return Ok(StagingImportAllResponse.From(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{templateId:guid}/staging/import")]
    public async Task<ActionResult<StagingImportResponse>> ImportOne(Guid templateId, CancellationToken cancellationToken)
    {
        if (templateId == Guid.Empty)
            return BadRequest();

        try
        {
            var result = await _stagingService.TryImportAsync(templateId, cancellationToken).ConfigureAwait(false);
            return Ok(StagingImportResponse.From(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private string ResolveUserName() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
}

public sealed class StagingExportResponse
{
    public Guid TemplateId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string DocumentFileName { get; init; } = string.Empty;

    public string UncPath { get; init; } = string.Empty;

    public string? OfficeOpenUrl { get; init; }

    public static StagingExportResponse From(UserReportTemplateStagingExportResult result) =>
        new()
        {
            TemplateId = result.TemplateId,
            DisplayName = result.DisplayName,
            DocumentFileName = result.DocumentFileName,
            UncPath = result.UncPath,
            OfficeOpenUrl = result.OfficeOpenUrl,
        };
}

public sealed class StagingImportResponse
{
    public Guid TemplateId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public bool ExtractValidateRan { get; init; }

    public int? InvalidPlaceholderCount { get; init; }

    public static StagingImportResponse From(UserReportTemplateStagingImportResult result) =>
        new()
        {
            TemplateId = result.TemplateId,
            DisplayName = result.DisplayName,
            Status = result.Status.ToString(),
            ErrorMessage = result.ErrorMessage,
            ExtractValidateRan = result.ExtractValidateRan,
            InvalidPlaceholderCount = result.InvalidPlaceholderCount,
        };
}

public sealed class StagingImportAllResponse
{
    public int ImportedCount { get; init; }

    public int SkippedUnchangedCount { get; init; }

    public int SkippedNotFoundCount { get; init; }

    public int FailedCount { get; init; }

    public IReadOnlyList<StagingImportResponse> Results { get; init; } = Array.Empty<StagingImportResponse>();

    public static StagingImportAllResponse From(UserReportTemplateStagingImportAllResult result) =>
        new()
        {
            ImportedCount = result.ImportedCount,
            SkippedUnchangedCount = result.SkippedUnchangedCount,
            SkippedNotFoundCount = result.SkippedNotFoundCount,
            FailedCount = result.FailedCount,
            Results = result.Results.Select(StagingImportResponse.From).ToList(),
        };
}
