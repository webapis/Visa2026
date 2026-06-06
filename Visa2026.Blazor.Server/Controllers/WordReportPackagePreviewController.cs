using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Visa2026.Blazor.Server.Services;

namespace Visa2026.Blazor.Server.Controllers;

[ApiController]
[Route("api/word-report-packages")]
[Authorize]
public sealed class WordReportPackagePreviewController : ControllerBase
{
    private readonly ApplicationWordReportPackageFileAccess fileAccess;

    public WordReportPackagePreviewController(ApplicationWordReportPackageFileAccess fileAccess)
    {
        this.fileAccess = fileAccess;
    }

    [HttpGet("applications/{applicationId:guid}/preview")]
    public async Task<IActionResult> DownloadPreview(Guid applicationId, [FromQuery] string entryKey)
    {
        if (string.IsNullOrWhiteSpace(entryKey))
            return BadRequest();

        var file = await fileAccess.TryGeneratePreviewAsync(applicationId, entryKey).ConfigureAwait(false);
        if (file == null)
            return NotFound();

        return File(file.Content, file.ContentType, file.FileName);
    }
}
