using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Caption formatting and ResolveFromHistory fallbacks for process numbers
/// (required on Visa / denormalized on Application after recent schema work).
/// </summary>
public sealed class ApplicationProcessNumberHelperCaptionAndFallbackTests
{
    [Fact]
    public void FormatDisplayCaption_OnlyProcessNumber_ReturnsProcessNumber()
    {
        Assert.Equal("AS538188", ApplicationProcessNumberHelper.FormatDisplayCaption(null, "AS538188"));
        Assert.Equal("AS538188", ApplicationProcessNumberHelper.FormatDisplayCaption("  ", " AS538188 "));
    }

    [Fact]
    public void FormatDisplayCaption_BothBlank_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ApplicationProcessNumberHelper.FormatDisplayCaption(null, null));
        Assert.Equal(string.Empty, ApplicationProcessNumberHelper.FormatDisplayCaption("  ", "\t"));
    }

    [Fact]
    public void ResolveFromHistory_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(ApplicationProcessNumberHelper.ResolveFromHistory(null));
        Assert.Null(ApplicationProcessNumberHelper.ResolveFromHistory([]));
    }

    [Fact]
    public void ResolveFromHistory_NoProcessStarted_UsesAnyProcessNumber()
    {
        var issued = new ApplicationProgress
        {
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessIssued },
            ProcessNumber = " AS9 ",
        };

        Assert.Equal("AS9", ApplicationProcessNumberHelper.ResolveFromHistory([issued]));
    }

    [Fact]
    public void FormatDisplayCaption_Application_PrefersDenormalizedProcessNumber()
    {
        var application = new Application
        {
            ApplicationNumber = "12/-7010",
            ProcessNumber = "AS-DENORM",
            ProgressHistory =
            [
                new ApplicationProgress
                {
                    State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessStarted },
                    ProcessNumber = "AS-HISTORY",
                },
            ],
        };

        Assert.Equal(
            "12/-7010" + ApplicationProcessNumberHelper.CaptionSeparator + "AS-DENORM",
            ApplicationProcessNumberHelper.FormatDisplayCaption(application));
    }

    [Fact]
    public void FormatDisplayCaption_Application_FallsBackToHistoryWhenDenormalizedMissing()
    {
        var application = new Application
        {
            ApplicationNumber = "12/-7010",
            ProcessNumber = null,
            ProgressHistory =
            [
                new ApplicationProgress
                {
                    State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessStarted },
                    ProcessNumber = "AS-HISTORY",
                },
            ],
        };

        Assert.Equal(
            "12/-7010" + ApplicationProcessNumberHelper.CaptionSeparator + "AS-HISTORY",
            ApplicationProcessNumberHelper.FormatDisplayCaption(application));
    }

    [Fact]
    public void FormatDisplayCaption_NullApplication_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ApplicationProcessNumberHelper.FormatDisplayCaption((Application?)null));
    }
}
