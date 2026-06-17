using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Visa2026.Blazor.Server.Services;

namespace Visa2026.Blazor.Server.Controllers;

[ApiController]
[Route("api/application-progress")]
[Authorize]
public sealed class ApplicationProgressMinistryLetterPreviewController : ControllerBase
{
    private readonly ApplicationProgressMinistryLetterFileAccess fileAccess;

    public ApplicationProgressMinistryLetterPreviewController(ApplicationProgressMinistryLetterFileAccess fileAccess) =>
        this.fileAccess = fileAccess;

    [HttpGet("applications/{applicationId:guid}/progress/{progressId:guid}/ministry-letter/inline")]
    public IActionResult InlineMinistryLetter(Guid applicationId, Guid progressId)
    {
        if (!fileAccess.TryGetFile(applicationId, progressId, out var file) || file == null)
            return NotFound();

        Response.Headers.ContentDisposition =
            $"inline; filename=\"{SanitizeContentDispositionFileName(file.FileName)}\"";
        return File(file.Content, file.ContentType);
    }

    [HttpGet("applications/{applicationId:guid}/progress/{progressId:guid}/ministry-letter")]
    public IActionResult DownloadMinistryLetter(Guid applicationId, Guid progressId)
    {
        if (!fileAccess.TryGetFile(applicationId, progressId, out var file) || file == null)
            return NotFound();

        return File(file.Content, file.ContentType, file.FileName);
    }

    private static string SanitizeContentDispositionFileName(string fileName)
    {
        var safe = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safe) ? "ministry-letter.pdf" : safe.Replace("\"", string.Empty);
    }
}
