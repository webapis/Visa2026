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
    public async Task<IActionResult> DownloadPreview(
        Guid applicationId,
        [FromQuery] string entryKey,
        [FromQuery] string? itemIds = null)
    {
        if (string.IsNullOrWhiteSpace(entryKey))
            return BadRequest();

        var applicationItemIds = ParseItemIds(itemIds);
        var bundle = await fileAccess.TryBuildPreviewBundleAsync(applicationId, entryKey, applicationItemIds)
            .ConfigureAwait(false);
        if (bundle == null || bundle.Originals.Count == 0)
            return NotFound();

        if (bundle.PdfContent is { Length: > 0 } pdfContent)
            return File(pdfContent, "application/pdf", bundle.PdfFileName);

        var file = bundle.Original;
        return File(file.Content, file.ContentType, file.FileName);
    }

    private static IReadOnlyList<Guid>? ParseItemIds(string? itemIds)
    {
        if (string.IsNullOrWhiteSpace(itemIds))
            return null;

        var parsed = itemIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        return parsed.Count == 0 ? null : parsed;
    }
}
