using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProcessNumberHelperTests
{
    [Fact]
    public void FormatDisplayCaption_WithBoth_JoinsWithSeparator()
    {
        Assert.Equal(
            "12/-7010" + ApplicationProcessNumberHelper.CaptionSeparator + "AS538188",
            ApplicationProcessNumberHelper.FormatDisplayCaption("12/-7010", "AS538188"));
    }

    [Fact]
    public void FormatDisplayCaption_WithoutProcessNumber_ReturnsApplicationNumber()
    {
        Assert.Equal("12/-7010", ApplicationProcessNumberHelper.FormatDisplayCaption("12/-7010", null));
    }

    [Fact]
    public void ResolveFromHistory_PrefersProcessStartedProcessNumber()
    {
        var started = new ApplicationProgress
        {
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessStarted },
            ProcessNumber = "AS1",
            Description = "ignored",
        };
        var issued = new ApplicationProgress
        {
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessIssued },
            ProcessNumber = "AS2",
        };

        Assert.Equal("AS1", ApplicationProcessNumberHelper.ResolveFromHistory([issued, started]));
    }

    [Fact]
    public void ResolveFromHistory_FallsBackToProcessStartedDescription()
    {
        var started = new ApplicationProgress
        {
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessStarted },
            Description = "AS538188",
        };

        Assert.Equal("AS538188", ApplicationProcessNumberHelper.ResolveFromHistory([started]));
    }

    [Fact]
    public void ResolveFromHistory_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(ApplicationProcessNumberHelper.ResolveFromHistory(null));
        Assert.Null(ApplicationProcessNumberHelper.ResolveFromHistory([]));
    }

    [Fact]
    public void ResolveFromHistory_WithoutProcessStarted_UsesAnyProcessNumber()
    {
        var issued = new ApplicationProgress
        {
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessIssued },
            ProcessNumber = "  AS9  ",
        };

        Assert.Equal("AS9", ApplicationProcessNumberHelper.ResolveFromHistory([issued]));
    }

    [Fact]
    public void ResolveFromHistory_PrefersStartedProcessNumberOverDescription()
    {
        var started = new ApplicationProgress
        {
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessStarted },
            ProcessNumber = "AS-FIELD",
            Description = "AS-LEGACY-DESC",
        };

        Assert.Equal("AS-FIELD", ApplicationProcessNumberHelper.ResolveFromHistory([started]));
    }

    [Fact]
    public void FormatDisplayCaption_Application_UsesDenormalizedProcessNumber()
    {
        var application = new Application
        {
            ApplicationNumber = "12/-7010",
            ProcessNumber = "AS538188",
        };

        Assert.Equal(
            "12/-7010" + ApplicationProcessNumberHelper.CaptionSeparator + "AS538188",
            ApplicationProcessNumberHelper.FormatDisplayCaption(application));
    }

    [Fact]
    public void FormatDisplayCaption_Application_Null_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ApplicationProcessNumberHelper.FormatDisplayCaption((Application?)null));
    }

    [Fact]
    public void FormatDisplayCaption_OnlyProcessNumber_ReturnsProcessNumber()
    {
        Assert.Equal("AS1", ApplicationProcessNumberHelper.FormatDisplayCaption(null, "AS1"));
        Assert.Equal("AS1", ApplicationProcessNumberHelper.FormatDisplayCaption("  ", " AS1 "));
    }
}