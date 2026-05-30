using System;
using Visa2026.Module.BusinessObjects.Feedback;

namespace Visa2026.Module.Services.Feedback;

public sealed class UserFeedbackSubmitInput
{
    public UserFeedbackType Type { get; set; } = UserFeedbackType.Bug;
    public UserFeedbackSeverity Severity { get; set; } = UserFeedbackSeverity.Medium;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public string ViewId { get; set; } = string.Empty;
    public string ContextBoTypeName { get; set; } = string.Empty;
    public Guid? ContextBoId { get; set; }
    public byte[]? ScreenshotBytes { get; set; }
    public string? ScreenshotFileName { get; set; }
    public byte[]? AttachmentBytes { get; set; }
    public string? AttachmentFileName { get; set; }
}
