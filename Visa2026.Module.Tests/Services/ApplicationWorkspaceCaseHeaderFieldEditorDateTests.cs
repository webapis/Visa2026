using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Edge cases for calendar binding on case-header date fields (Value stays yyyy-MM-dd for save).
/// </summary>
public class ApplicationWorkspaceCaseHeaderFieldEditorDateTests
{
    [Fact]
    public void EditorDate_ParsesExactIsoDateOnly()
    {
        var field = new ApplicationWorkspaceCaseHeaderField
        {
            Kind = ApplicationWorkspaceCaseHeaderFieldKind.Date,
            Value = "2024-08-25",
        };

        Assert.Equal(new DateTime(2024, 8, 25), field.EditorDate);
    }

    [Theory]
    [InlineData("25.08.2024")]
    [InlineData("2024/08/25")]
    [InlineData("2024-8-25")]
    [InlineData("08-25-2024")]
    [InlineData("not-a-date")]
    [InlineData("")]
    [InlineData("  ")]
    public void EditorDate_RejectsNonExactIsoValues(string value)
    {
        var field = new ApplicationWorkspaceCaseHeaderField
        {
            Kind = ApplicationWorkspaceCaseHeaderFieldKind.Date,
            Value = value,
        };

        Assert.Null(field.EditorDate);
    }

    [Theory]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldKind.Text)]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldKind.ShortText)]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldKind.Lookup)]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldKind.CommaSeparatedMultiSelect)]
    public void EditorDate_NullWhenKindIsNotDate(ApplicationWorkspaceCaseHeaderFieldKind kind)
    {
        var field = new ApplicationWorkspaceCaseHeaderField
        {
            Kind = kind,
            Value = "2024-08-25",
        };

        Assert.Null(field.EditorDate);
    }

    [Fact]
    public void FormatEditorDate_NullOrValueRoundTripsToIsoOrEmpty()
    {
        Assert.Equal(string.Empty, ApplicationWorkspaceCaseHeaderField.FormatEditorDate(null));
        Assert.Equal(
            "2024-08-25",
            ApplicationWorkspaceCaseHeaderField.FormatEditorDate(new DateTime(2024, 8, 25, 15, 30, 0)));
    }
}
