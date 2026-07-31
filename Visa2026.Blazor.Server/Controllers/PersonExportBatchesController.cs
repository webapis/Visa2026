using System;
using System.Linq;
using System.Security.Claims;
using DevExpress.ExpressApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Status polling and ZIP download for director hand-over exports
/// (see <c>docs/PERSON_DOSSIER.md</c> phase 4). Mirrors <see cref="PdfBatchesController"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PersonExportBatchesController : ControllerBase
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;

    public PersonExportBatchesController(INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
    }

    [HttpGet("my-latest")]
    public ActionResult<MyLatestDto> GetMyLatest()
    {
        string userName = CurrentUserName();
        if (string.IsNullOrWhiteSpace(userName))
            return Ok(new MyLatestDto());

        var userNameLower = userName.ToLower();

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<PersonExportBatch>();

        // Prefer an active job; otherwise show the most recent one so the download link stays reachable.
        var active = os.GetObjectsQuery<PersonExportBatch>()
            .Where(b => b.RequestedBy != null
                        && b.RequestedBy.ToLower() == userNameLower
                        && (b.Status == PersonExportBatchStatus.Queued || b.Status == PersonExportBatchStatus.Running))
            .OrderByDescending(b => b.CreatedOnUtc)
            .FirstOrDefault();

        var latest = active ?? os.GetObjectsQuery<PersonExportBatch>()
            .Where(b => b.RequestedBy != null && b.RequestedBy.ToLower() == userNameLower)
            .OrderByDescending(b => b.CreatedOnUtc)
            .FirstOrDefault();

        if (latest == null)
            return Ok(new MyLatestDto());

        return Ok(ToDto(latest, (Guid)os.GetKeyValue(latest)));
    }

    [HttpGet("{id:guid}/status")]
    public ActionResult<MyLatestDto> GetStatus(Guid id)
    {
        string userName = CurrentUserName();
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized();

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<PersonExportBatch>();
        var batch = os.GetObjectByKey<PersonExportBatch>(id);
        if (batch == null)
            return NotFound();

        if (!string.Equals(batch.RequestedBy, userName, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        return Ok(ToDto(batch, id));
    }

    [HttpGet("{id:guid}/zip")]
    public IActionResult DownloadZip(Guid id)
    {
        string userName = CurrentUserName();
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized();

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<PersonExportBatch>();
        var batch = os.GetObjectByKey<PersonExportBatch>(id);
        if (batch == null)
            return NotFound();

        if (!string.Equals(batch.RequestedBy, userName, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var zip = batch.ZipFile;
        if (zip?.Content == null || zip.Content.Length == 0)
            return NotFound();

        var fileName = string.IsNullOrWhiteSpace(zip.FileName) ? "Dossier.zip" : zip.FileName;
        return File(zip.Content, "application/zip", fileName);
    }

    private string CurrentUserName() =>
        User?.Identity?.Name ?? User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    private static MyLatestDto ToDto(PersonExportBatch batch, Guid id) => new()
    {
        BatchId = id,
        Status = batch.Status.ToString(),
        CreatedOnUtc = batch.CreatedOnUtc,
        PersonDisplayName = batch.PersonDisplayName,
        TotalRecords = batch.TotalRecords,
        ProcessedRecords = batch.ProcessedRecords,
        ErrorMessage = batch.ErrorMessage,
        ExportNotes = batch.ExportNotes,
        DownloadUrl = batch.Status == PersonExportBatchStatus.Completed && batch.ZipFile != null
            ? $"/api/PersonExportBatches/{id}/zip"
            : null
    };

    public sealed class MyLatestDto
    {
        public Guid? BatchId { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public string? PersonDisplayName { get; set; }
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public string? ErrorMessage { get; set; }
        /// <summary>Same text as <c>EXPORT_NOTES.txt</c> in the ZIP when the job completed.</summary>
        public string? ExportNotes { get; set; }
        public string? DownloadUrl { get; set; }
    }
}
