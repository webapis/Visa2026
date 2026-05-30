using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.Feedback;

namespace Visa2026.Module.Services.Feedback;

public sealed class UserFeedbackSubmitService : IUserFeedbackSubmitService
{
    private readonly INonSecuredObjectSpaceFactory _objectSpaceFactory;

    public UserFeedbackSubmitService(INonSecuredObjectSpaceFactory objectSpaceFactory)
    {
        _objectSpaceFactory = objectSpaceFactory;
    }

    public Guid Submit(string userName, UserFeedbackSubmitInput input)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new InvalidOperationException("User is not authenticated.");

        if (string.IsNullOrWhiteSpace(input.Summary))
            throw new ArgumentException("Summary is required.", nameof(input));

        using var objectSpace = _objectSpaceFactory.CreateNonSecuredObjectSpace<UserFeedback>();

        var user = objectSpace.GetObjectsQuery<ApplicationUser>()
            .FirstOrDefault(u => u.UserName.ToLower() == userName.ToLower());
        if (user == null)
            throw new InvalidOperationException($"User '{userName}' was not found.");

        var feedback = objectSpace.CreateObject<UserFeedback>();
        feedback.FeedbackType = input.Type;
        feedback.Severity = input.Severity;
        feedback.Summary = input.Summary.Trim();
        feedback.Description = input.Description?.Trim() ?? string.Empty;
        feedback.PageUrl = Truncate(input.PageUrl, 500);
        feedback.ViewId = Truncate(input.ViewId, 200);
        feedback.ContextBoTypeName = Truncate(input.ContextBoTypeName, 300);
        feedback.ContextBoId = input.ContextBoId;
        feedback.AppVersion = Visa2026Module.VersionDisplay;
        feedback.SubmittedBy = user;
        feedback.SubmittedAt = DateTime.Now;
        feedback.Status = UserFeedbackStatus.New;

        if (input.ScreenshotBytes is { Length: > 0 })
        {
            string fileName = string.IsNullOrWhiteSpace(input.ScreenshotFileName)
                ? "screenshot.png"
                : input.ScreenshotFileName;
            if (!UserFeedbackFileConstraints.TryValidateScreenshot(fileName, input.ScreenshotBytes.LongLength, out string? screenshotError))
                throw new ArgumentException(screenshotError);

            var screenshot = objectSpace.CreateObject<FileData>();
            UserFeedbackFileConstraints.AssignFileData(screenshot, fileName, input.ScreenshotBytes);
            feedback.Screenshot = screenshot;
        }

        if (input.AttachmentBytes is { Length: > 0 })
        {
            string fileName = string.IsNullOrWhiteSpace(input.AttachmentFileName)
                ? "attachment.bin"
                : input.AttachmentFileName;
            if (!UserFeedbackFileConstraints.TryValidateAttachment(fileName, input.AttachmentBytes.LongLength, out string? attachmentError))
                throw new ArgumentException(attachmentError);

            var attachment = objectSpace.CreateObject<FileData>();
            UserFeedbackFileConstraints.AssignFileData(attachment, fileName, input.AttachmentBytes);
            feedback.Attachment = attachment;
        }

        objectSpace.CommitChanges();
        return (Guid)objectSpace.GetKeyValue(feedback);
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        value = value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
