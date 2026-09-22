using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.Services.Feedback;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class UserFeedbackFileConstraintsTests
{
    [Theory]
    [InlineData("shot.png", 1024)]
    [InlineData("SHOT.JPG", 1)]
    [InlineData("a.webp", UserFeedbackFileConstraints.MaxScreenshotBytes)]
    public void TryValidateScreenshot_AcceptsAllowedImages(string fileName, long size)
    {
        Assert.True(UserFeedbackFileConstraints.TryValidateScreenshot(fileName, size, out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData("shot.png", 0, "empty")]
    [InlineData("shot.png", UserFeedbackFileConstraints.MaxScreenshotBytes + 1, "5 MB")]
    [InlineData("notes.pdf", 100, "image")]
    [InlineData("noext", 100, "image")]
    [InlineData(null, 100, "image")]
    public void TryValidateScreenshot_RejectsInvalid(string fileName, long size, string messageFragment)
    {
        Assert.False(UserFeedbackFileConstraints.TryValidateScreenshot(fileName, size, out var error));
        Assert.Contains(messageFragment, error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("doc.pdf", 2048)]
    [InlineData("sheet.XLSX", 10)]
    [InlineData("notes.txt", UserFeedbackFileConstraints.MaxAttachmentBytes)]
    public void TryValidateAttachment_AcceptsAllowedTypes(string fileName, long size)
    {
        Assert.True(UserFeedbackFileConstraints.TryValidateAttachment(fileName, size, out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData("a.exe", 100)]
    [InlineData("zip.zip", 100)]
    [InlineData("a.pdf", 0)]
    [InlineData("a.pdf", UserFeedbackFileConstraints.MaxAttachmentBytes + 1)]
    public void TryValidateAttachment_RejectsInvalid(string fileName, long size)
    {
        Assert.False(UserFeedbackFileConstraints.TryValidateAttachment(fileName, size, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void AssignFileData_SetsNameContentAndSize()
    {
        var file = new FileData();
        var bytes = new byte[] { 1, 2, 3, 4 };

        UserFeedbackFileConstraints.AssignFileData(file, "shot.png", bytes);

        Assert.Equal("shot.png", file.FileName);
        Assert.Same(bytes, file.Content);
        Assert.Equal(4, file.Size);
    }
}
