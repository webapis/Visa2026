using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Visa2026.Module.BusinessObjects.Feedback;
using Visa2026.Module.Services.Feedback;

namespace Visa2026.Blazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UserFeedbackController : ControllerBase
{
    private readonly IUserFeedbackSubmitService _submitService;

    public UserFeedbackController(IUserFeedbackSubmitService submitService)
    {
        _submitService = submitService;
    }

    [HttpPost("submit")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    public async Task<ActionResult<SubmitResponse>> Submit([FromForm] SubmitForm form)
    {
        string? userName = User?.Identity?.Name ?? User?.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(form.Summary))
            return BadRequest(new { error = "Summary is required." });

        if (!Enum.TryParse(form.Type, true, out UserFeedbackType type))
            type = UserFeedbackType.Bug;

        if (!Enum.TryParse(form.Severity, true, out UserFeedbackSeverity severity))
            severity = UserFeedbackSeverity.Medium;

        Guid? contextBoId = null;
        if (!string.IsNullOrWhiteSpace(form.ContextBoId) && Guid.TryParse(form.ContextBoId, out Guid parsedId))
            contextBoId = parsedId;

        byte[]? screenshotBytes = null;
        string? screenshotFileName = null;
        if (form.Screenshot != null && form.Screenshot.Length > 0)
        {
            screenshotBytes = await ReadAllBytesAsync(form.Screenshot);
            screenshotFileName = form.Screenshot.FileName;
        }

        byte[]? attachmentBytes = null;
        string? attachmentFileName = null;
        if (form.Attachment != null && form.Attachment.Length > 0)
        {
            attachmentBytes = await ReadAllBytesAsync(form.Attachment);
            attachmentFileName = form.Attachment.FileName;
        }

        try
        {
            var input = new UserFeedbackSubmitInput
            {
                Type = type,
                Severity = severity,
                Summary = form.Summary,
                Description = form.Description ?? string.Empty,
                PageUrl = form.PageUrl ?? string.Empty,
                ViewId = form.ViewId ?? string.Empty,
                ContextBoTypeName = form.ContextBoTypeName ?? string.Empty,
                ContextBoId = contextBoId,
                ScreenshotBytes = screenshotBytes,
                ScreenshotFileName = screenshotFileName,
                AttachmentBytes = attachmentBytes,
                AttachmentFileName = attachmentFileName
            };

            Guid id = _submitService.Submit(userName, input);
            return Ok(new SubmitResponse { Id = id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        return memory.ToArray();
    }

    public sealed class SubmitForm
    {
        public string Type { get; set; } = "Bug";
        public string Severity { get; set; } = "Medium";
        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PageUrl { get; set; } = string.Empty;
        public string ViewId { get; set; } = string.Empty;
        public string ContextBoTypeName { get; set; } = string.Empty;
        public string ContextBoId { get; set; } = string.Empty;
        public IFormFile? Screenshot { get; set; }
        public IFormFile? Attachment { get; set; }
    }

    public sealed class SubmitResponse
    {
        public Guid Id { get; set; }
    }
}
