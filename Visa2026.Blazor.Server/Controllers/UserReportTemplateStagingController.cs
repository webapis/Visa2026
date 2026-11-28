using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Visa2026.Blazor.Server.Services;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Blazor.Server.Controllers;

[ApiController]
[Route("api/user-report-templates")]
[Authorize]
public sealed class UserReportTemplateStagingController : ControllerBase
{
    private readonly IUserReportTemplateStagingService _stagingService;
    private readonly TemplateEditStagingOptions _options;
    private readonly TemplateEditDesktopBroker _desktopBroker;
    private readonly TemplateEditRibbonTokenService _ribbonTokens;

    public UserReportTemplateStagingController(
        IUserReportTemplateStagingService stagingService,
        IOptions<TemplateEditStagingOptions> options,
        TemplateEditDesktopBroker desktopBroker,
        TemplateEditRibbonTokenService ribbonTokens)
    {
        _stagingService = stagingService;
        _options = options.Value;
        _desktopBroker = desktopBroker;
        _ribbonTokens = ribbonTokens;
    }

    [HttpPost("{templateId:guid}/staging/export")]
    public async Task<ActionResult<StagingExportResponse>> Export(Guid templateId, CancellationToken cancellationToken)
    {
        if (templateId == Guid.Empty)
            return BadRequest();

        try
        {
            var result = await _stagingService
                .ExportForAuthorizedHttpAsync(templateId, ResolveUserName(), cancellationToken)
                .ConfigureAwait(false);
            if (result.FileContent == null || result.FileContent.Length == 0)
                return BadRequest(new { error = "Template file has no content." });

            return Ok(StagingExportResponse.From(result, _options, HttpContext));
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

    /// <summary>Development: the browser is on this PC, so the site creates the template folder.</summary>
    [HttpPost("{templateId:guid}/staging/write-local")]
    public async Task<ActionResult<TemplateEditMachineWriteResponse>> WriteLocal(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        if (templateId == Guid.Empty)
            return BadRequest();

        try
        {
            return Ok(await _desktopBroker
                .WriteOnThisMachineAsync(HttpContext, templateId, ResolveUserName(), cancellationToken)
                .ConfigureAwait(false));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Development: read the folder on this PC and import changed templates.</summary>
    [HttpPost("staging/import-local")]
    public async Task<ActionResult<TemplateEditMachineImportResponse>> ImportLocal(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _desktopBroker
                .ImportOnThisMachineAsync(HttpContext, cancellationToken)
                .ConfigureAwait(false));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Production: opener on the officer PC downloads and opens the template.</summary>
    [HttpPost("{templateId:guid}/staging/desktop-open")]
    public ActionResult<TemplateEditDesktopLaunchResponse> DesktopOpen(Guid templateId)
    {
        if (templateId == Guid.Empty)
            return BadRequest();

        if (!_options.Enabled)
            return BadRequest(new { error = "Template staging edit is disabled." });

        return Ok(_desktopBroker.CreateLaunch(HttpContext, "open", templateId, ResolveUserName()));
    }

    /// <summary>Production: opener on the officer PC uploads saved templates.</summary>
    [HttpPost("staging/desktop-sync")]
    public ActionResult<TemplateEditDesktopLaunchResponse> DesktopSync()
    {
        if (!_options.Enabled)
            return BadRequest(new { error = "Template staging edit is disabled." });

        return Ok(_desktopBroker.CreateLaunch(HttpContext, "sync", Guid.Empty, ResolveUserName()));
    }

    [AllowAnonymous]
    [HttpGet("staging/desktop/{token}")]
    public async Task<ActionResult<TemplateEditDesktopFilePayload>> DesktopFile(
        string token,
        CancellationToken cancellationToken)
    {
        var payload = await _desktopBroker.ReadOpenFileAsync(token, cancellationToken).ConfigureAwait(false);
        return payload == null ? NotFound() : Ok(payload);
    }

    [AllowAnonymous]
    [HttpPost("staging/desktop/{token}/upload")]
    [RequestSizeLimit(52_428_800)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
    public async Task<ActionResult<StagingImportResponse>> DesktopUpload(
        string token,
        [FromQuery] Guid templateId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Template file is required." });

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        var result = await _desktopBroker
            .ImportDesktopUploadAsync(token, templateId, memory.ToArray(), cancellationToken)
            .ConfigureAwait(false);
        if (result == null)
            return NotFound();

        return Ok(StagingImportResponse.From(result));
    }

    /// <summary>Word/Excel Home-tab Sync. The token covers every template this officer edits.</summary>
    [AllowAnonymous]
    [HttpPost("staging/ribbon-sync")]
    [RequestSizeLimit(52_428_800)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
    public async Task<ActionResult<StagingImportResponse>> RibbonSync(
        [FromForm] Guid templateId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return BadRequest(new { error = "Template staging edit is disabled." });

        var token = Request.Headers["X-Visa2026-Ribbon-Token"].ToString();
        if (!_ribbonTokens.TryValidateHost(token, out _))
            return Unauthorized();

        if (templateId == Guid.Empty)
            return BadRequest(new { error = "Template id is required." });

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Template file is required." });

        if (file.Length > _options.MaxFileSizeBytes)
            return BadRequest(new { error = "Uploaded file exceeds maximum size." });

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        var result = await _stagingService
            .ImportFromUploadAsync(templateId, memory.ToArray(), cancellationToken)
            .ConfigureAwait(false);
        return Ok(StagingImportResponse.From(result));
    }

    [AllowAnonymous]
    [HttpPost("staging/desktop/{token}/complete")]
    public ActionResult DesktopComplete(string token, [FromBody] JsonElement result)
    {
        return _desktopBroker.Complete(token, result) ? Ok() : NotFound();
    }

    [HttpGet("staging/desktop/{token}/result")]
    public ActionResult DesktopResult(string token)
    {
        if (!_desktopBroker.TryReadResult(token, ResolveUserName(), out var completed, out var result))
            return NotFound();

        return Ok(new { completed, result });
    }

    /// <summary>One zip for this PC: Office trust, the Edit opener, and the Word/Excel Sync button.</summary>
    [HttpGet("staging/pc-setup")]
    public IActionResult DownloadPcSetup()
    {
        // SecuritySystem.IsGranted needs the Blazor circuit. This GET is a plain HTTP download.
        // [Authorize] on the controller already requires a signed-in officer. The button is shown
        // only when that officer can edit templates.
        if (!_options.Enabled)
            return NotFound();

        if (!TemplateEditPcSetupPackage.TryNormalizeHost(Request.Host.Host, out var serverHost))
            return BadRequest();

        try
        {
            var bytes = TemplateEditPcSetupPackage.Create(serverHost);
            return File(bytes, "application/zip", TemplateEditPcSetupPackage.FileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
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

    public string FileBase64 { get; init; } = string.Empty;

    /// <summary>Absolute path when this process is the officer PC. Empty from a remote IIS host.</summary>
    public string? SuggestedLocalFilePath { get; init; }

    /// <summary>Path to copy. Explorer expands <c>%LOCALAPPDATA%</c> when that form is used.</summary>
    public string CopyPath { get; init; } = string.Empty;

    public string FolderLeafName { get; init; } = string.Empty;

    public static StagingExportResponse From(
        UserReportTemplateStagingExportResult result,
        TemplateEditStagingOptions options,
        HttpContext http) =>
        new()
        {
            TemplateId = result.TemplateId,
            DisplayName = result.DisplayName,
            DocumentFileName = result.DocumentFileName,
            SourceContentHashSha256 = result.SourceContentHashSha256,
            OutputFormat = result.OutputFormat.ToString(),
            FileBase64 = Convert.ToBase64String(result.FileContent ?? Array.Empty<byte>()),
            SuggestedLocalFilePath = TemplateEditStagingPathHints.OfficeOpenPath(
                options,
                http,
                result.DocumentFileName),
            CopyPath = TemplateEditStagingPathHints.CopyPath(options, http, result.DocumentFileName),
            FolderLeafName = TemplateEditStagingPathHints.FolderLeaf(options),
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
