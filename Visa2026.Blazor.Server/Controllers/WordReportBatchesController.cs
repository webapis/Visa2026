using DevExpress.ExpressApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class WordReportBatchesController : ControllerBase
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;

    public WordReportBatchesController(INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
    }

    [HttpGet("my-latest")]
    public ActionResult<WordReportBatchStatusDto> GetMyLatest()
    {
        string userName = User?.Identity?.Name
            ?? User?.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userName))
            return Ok(new WordReportBatchStatusDto());

        var userNameLower = userName.ToLowerInvariant();

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<WordReportGenerationBatch>();

        var active = os.GetObjectsQuery<WordReportGenerationBatch>()
            .Where(b => b.RequestedBy != null
                        && b.RequestedBy.ToLower() == userNameLower
                        && (b.Status == WordReportGenerationBatchStatus.Queued
                            || b.Status == WordReportGenerationBatchStatus.Running))
            .OrderByDescending(b => b.CreatedOnUtc)
            .FirstOrDefault();

        var latest = active ?? os.GetObjectsQuery<WordReportGenerationBatch>()
            .Where(b => b.RequestedBy != null && b.RequestedBy.ToLower() == userNameLower)
            .OrderByDescending(b => b.CreatedOnUtc)
            .FirstOrDefault();

        if (latest == null)
            return Ok(new WordReportBatchStatusDto());

        var id = (Guid)os.GetKeyValue(latest)!;
        return Ok(ToDto(id, latest));
    }

    [HttpGet("{id:guid}/status")]
    public ActionResult<WordReportBatchStatusDto> GetStatus(Guid id)
    {
        string userName = User?.Identity?.Name
            ?? User?.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized();

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<WordReportGenerationBatch>();
        var batch = os.GetObjectByKey<WordReportGenerationBatch>(id);
        if (batch == null)
            return NotFound();

        if (!string.Equals(batch.RequestedBy, userName, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        return Ok(ToDto(id, batch));
    }

    [HttpGet("{id:guid}/zip")]
    public IActionResult DownloadZip(Guid id)
    {
        string userName = User?.Identity?.Name
            ?? User?.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized();

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<WordReportGenerationBatch>();
        var batch = os.GetObjectByKey<WordReportGenerationBatch>(id);
        if (batch == null)
            return NotFound();

        if (!string.Equals(batch.RequestedBy, userName, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var zip = batch.ZipFile;
        if (zip == null || zip.Content == null || zip.Content.Length == 0)
            return NotFound();

        var fileName = string.IsNullOrWhiteSpace(zip.FileName) ? "Resminamalar.zip" : zip.FileName;
        return File(zip.Content, "application/zip", fileName);
    }

    private static WordReportBatchStatusDto ToDto(Guid id, WordReportGenerationBatch batch) =>
        new()
        {
            BatchId = id,
            Status = batch.Status.ToString(),
            CreatedOnUtc = batch.CreatedOnUtc,
            TotalItems = batch.TotalReports,
            ProcessedItems = batch.ProcessedReports,
            ErrorMessage = batch.ErrorMessage,
            DownloadUrl = batch.Status == WordReportGenerationBatchStatus.Completed && batch.ZipFile != null
                ? $"/api/WordReportBatches/{id}/zip"
                : null
        };

    public sealed class WordReportBatchStatusDto
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
