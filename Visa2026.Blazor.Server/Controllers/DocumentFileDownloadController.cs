using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Visa2026.Blazor.Server.Services;

namespace Visa2026.Blazor.Server.Controllers;

[ApiController]
[Route("api/document-files")]
[Authorize]
public sealed class DocumentFileDownloadController : ControllerBase
{
    private readonly ApplicationItemDocumentFileAccess fileAccess;

    public DocumentFileDownloadController(ApplicationItemDocumentFileAccess fileAccess) =>
        this.fileAccess = fileAccess;

    [HttpGet("application-items/merged")]
    public IActionResult DownloadMergedSlot([FromQuery] string slotKey, [FromQuery] Guid[] itemIds)
    {
        if (string.IsNullOrWhiteSpace(slotKey) || itemIds == null || itemIds.Length == 0)
            return BadRequest();

        if (!fileAccess.TryGetMergedSlotPdf(itemIds, slotKey, out var file) || file == null)
            return NotFound();

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("application-items/merged/inline")]
    public IActionResult InlineMergedSlot([FromQuery] string slotKey, [FromQuery] Guid[] itemIds)
    {
        if (string.IsNullOrWhiteSpace(slotKey) || itemIds == null || itemIds.Length == 0)
            return BadRequest();

        if (!fileAccess.TryGetMergedSlotPdf(itemIds, slotKey, out var file) || file == null)
            return NotFound();

        Response.Headers.ContentDisposition = $"inline; filename=\"{SanitizeContentDispositionFileName(file.FileName)}\"";
        return File(file.Content, file.ContentType);
    }

    [HttpGet("application-items/{applicationItemId:guid}/{fileDataId:guid}")]
    public IActionResult DownloadForApplicationItem(Guid applicationItemId, Guid fileDataId)
    {
        if (!fileAccess.TryGetFile(applicationItemId, fileDataId, out var file) || file == null)
            return NotFound();

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("application-items/{applicationItemId:guid}/{fileDataId:guid}/inline")]
    public IActionResult InlineForApplicationItem(Guid applicationItemId, Guid fileDataId)
    {
        if (!fileAccess.TryGetFile(applicationItemId, fileDataId, out var file) || file == null)
            return NotFound();

        Response.Headers.ContentDisposition = $"inline; filename=\"{SanitizeContentDispositionFileName(file.FileName)}\"";
        return File(file.Content, file.ContentType);
    }

    private static string SanitizeContentDispositionFileName(string fileName)
    {
        var safe = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safe) ? "document" : safe.Replace("\"", string.Empty);
    }
}
