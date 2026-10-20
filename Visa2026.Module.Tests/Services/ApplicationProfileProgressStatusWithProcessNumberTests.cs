using Visa2026.Module.Localization;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// CO (AS) number is appended to the progress status chip (list stepper + Badge).
/// </summary>
public class ApplicationProfileProgressStatusWithProcessNumberTests
{
    [Fact]
    public void ProgressStatusWithProcessNumber_BlankStatus_ReturnsEmptyOrWhitespaceAsGiven()
    {
        Assert.Equal(string.Empty, ApplicationProfileLocalization.ProgressStatusWithProcessNumber(null, "CO1"));
        // Whitespace-only status is treated as blank for appending, but returned unchanged.
        Assert.Equal("  ", ApplicationProfileLocalization.ProgressStatusWithProcessNumber("  ", "CO1"));
    }

    [Fact]
    public void ProgressStatusWithProcessNumber_WithoutNumber_ReturnsStatusOnly()
    {
        Assert.Equal("Processing", ApplicationProfileLocalization.ProgressStatusWithProcessNumber("Processing", null));
        Assert.Equal("Processing", ApplicationProfileLocalization.ProgressStatusWithProcessNumber("Processing", "  "));
    }

    [Fact]
    public void ProgressStatusWithProcessNumber_AppendsTrimmedNumberWithSlash()
    {
        Assert.Equal(
            "Processing /CO000004",
            ApplicationProfileLocalization.ProgressStatusWithProcessNumber("  Processing  ", "  CO000004  "));
    }
}
