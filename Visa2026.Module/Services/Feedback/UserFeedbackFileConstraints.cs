using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.Services.Feedback;

public static class UserFeedbackFileConstraints
{
    public const int MaxScreenshotBytes = 5 * 1024 * 1024;
    public const int MaxAttachmentBytes = 10 * 1024 * 1024;

    public static readonly HashSet<string> ScreenshotExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp"
    };

    public static readonly HashSet<string> AttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp",
        ".pdf", ".docx", ".xlsx", ".txt"
    };

    public static bool TryValidateScreenshot(string fileName, long size, out string? error)
    {
        error = null;
        if (size <= 0)
        {
            error = "The screenshot file is empty.";
            return false;
        }

        if (size > MaxScreenshotBytes)
        {
            error = "Screenshot must be 5 MB or smaller.";
            return false;
        }

        string ext = Path.GetExtension(fileName ?? string.Empty);
        if (string.IsNullOrEmpty(ext) || !ScreenshotExtensions.Contains(ext))
        {
            error = "Screenshot must be an image (.png, .jpg, .gif, .webp, or .bmp).";
            return false;
        }

        return true;
    }

    public static bool TryValidateAttachment(string fileName, long size, out string? error)
    {
        error = null;
        if (size <= 0)
        {
            error = "The attachment file is empty.";
            return false;
        }

        if (size > MaxAttachmentBytes)
        {
            error = "Attachment must be 10 MB or smaller.";
            return false;
        }

        string ext = Path.GetExtension(fileName ?? string.Empty);
        if (string.IsNullOrEmpty(ext) || !AttachmentExtensions.Contains(ext))
        {
            error = "Attachment type is not allowed.";
            return false;
        }

        return true;
    }

    public static void AssignFileData(FileData fileData, string fileName, byte[] content)
    {
        fileData.FileName = fileName;
        fileData.Content = content;
        fileData.Size = (int)content.LongLength;
    }
}
