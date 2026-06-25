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

    [HttpPost("{templateId:guid}/staging/upload")]
    [RequestSizeLimit(52_428_800)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
    public async Task<ActionResult<StagingImportResponse>> Upload(
        Guid templateId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (templateId == Guid.Empty)
            return BadRequest();

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Template file is required." });

        try
        {
            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
            var result = await _stagingService
                .ImportFromUploadAsync(templateId, memory.ToArray(), cancellationToken)
                .ConfigureAwait(false);
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
        catch (Exception ex)
        {
            return Ok(StagingImportResponse.From(new UserReportTemplateStagingImportResult
            {
                TemplateId = templateId,
                DisplayName = string.Empty,
                Status = UserReportTemplateStagingImportStatus.Failed,
                ErrorMessage = ex.Message,
            }));
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

    public string? SourceContentHashSha256 { get; init; }

    public string OutputFormat { get; init; } = string.Empty;

    public static StagingExportResponse From(UserReportTemplateStagingExportResult result) =>
        new()
        {
            TemplateId = result.TemplateId,
            DisplayName = result.DisplayName,
            DocumentFileName = result.DocumentFileName,
            SourceContentHashSha256 = result.SourceContentHashSha256,
            OutputFormat = result.OutputFormat.ToString(),
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
