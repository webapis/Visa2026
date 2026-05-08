using DevExpress.ExpressApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PdfBatchesController : ControllerBase
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;

    public PdfBatchesController(INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
    }

    [HttpGet("my-latest")]
    public ActionResult<MyLatestDto> GetMyLatest()
    {
        // In Web/API pipeline, rely on ASP.NET Core principal.
        string userName = User?.Identity?.Name
            ?? User?.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userName))
            return Ok(new MyLatestDto());
        var userNameLower = userName.ToLower();

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<PdfGenerationBatch>();

        // Prefer active jobs. If none, show the most recent completed/failed job (so user sees download).
        var active = os.GetObjectsQuery<PdfGenerationBatch>()
            .Where(b => b.RequestedBy != null
                        && b.RequestedBy.ToLower() == userNameLower
                        && (b.Status == PdfGenerationBatchStatus.Queued || b.Status == PdfGenerationBatchStatus.Running))
            .OrderByDescending(b => b.CreatedOnUtc)
            .FirstOrDefault();

        var latest = active ?? os.GetObjectsQuery<PdfGenerationBatch>()
            .Where(b => b.RequestedBy != null && b.RequestedBy.ToLower() == userNameLower)
            .OrderByDescending(b => b.CreatedOnUtc)
            .FirstOrDefault();

        if (latest == null)
            return Ok(new MyLatestDto());

        var id = (Guid)os.GetKeyValue(latest);
        return Ok(new MyLatestDto
        {
            BatchId = id,
            Status = latest.Status.ToString(),
            CreatedOnUtc = latest.CreatedOnUtc,
            TotalItems = latest.TotalItems,
            ProcessedItems = latest.ProcessedItems,
            ErrorMessage = latest.ErrorMessage,
            DownloadUrl = latest.Status == PdfGenerationBatchStatus.Completed && latest.ZipFile != null
                ? $"/api/PdfBatches/{id}/zip"
                : null
        });
    }

    [HttpGet("{id:guid}/status")]
    public ActionResult<MyLatestDto> GetStatus(Guid id)
    {
        string userName = User?.Identity?.Name
            ?? User?.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized();

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<PdfGenerationBatch>();
        var batch = os.GetObjectByKey<PdfGenerationBatch>(id);
        if (batch == null)
            return NotFound();

        if (!string.Equals(batch.RequestedBy, userName, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        return Ok(new MyLatestDto
        {
            BatchId = id,
            Status = batch.Status.ToString(),
            CreatedOnUtc = batch.CreatedOnUtc,
            TotalItems = batch.TotalItems,
            ProcessedItems = batch.ProcessedItems,
            ErrorMessage = batch.ErrorMessage,
            DownloadUrl = batch.Status == PdfGenerationBatchStatus.Completed && batch.ZipFile != null
                ? $"/api/PdfBatches/{id}/zip"
                : null
        });
    }

    [HttpGet("{id:guid}/zip")]
    public IActionResult DownloadZip(Guid id)
    {
        string userName = User?.Identity?.Name
            ?? User?.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized();

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<PdfGenerationBatch>();
        var batch = os.GetObjectByKey<PdfGenerationBatch>(id);
        if (batch == null)
            return NotFound();

        // Basic ownership gate. (Admins can still get it via My PDF Jobs in UI.)
        if (!string.Equals(batch.RequestedBy, userName, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var zip = batch.ZipFile;
        if (zip == null || zip.Content == null || zip.Content.Length == 0)
            return NotFound();

        var fileName = string.IsNullOrWhiteSpace(zip.FileName) ? "Selected.zip" : zip.FileName;
        return File(zip.Content, "application/zip", fileName);
    }

    public sealed class MyLatestDto
    {
        public Guid? BatchId { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public string? ErrorMessage { get; set; }
        public string? DownloadUrl { get; set; }
    }
}

