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
        var started = new ApplicationProfileInstanceProgress
        {
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessStarted },
            ProcessNumber = "AS1",
            Description = "ignored",
        };
        var issued = new ApplicationProfileInstanceProgress
        {
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessIssued },
            ProcessNumber = "AS2",
        };

        Assert.Equal("AS1", ApplicationProcessNumberHelper.ResolveFromHistory([issued, started]));
    }

    [Fact]
    public void ResolveFromHistory_FallsBackToProcessStartedDescription()
    {
        var started = new ApplicationProfileInstanceProgress
        {
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessStarted },
            Description = "AS538188",
        };

        Assert.Equal("AS538188", ApplicationProcessNumberHelper.ResolveFromHistory([started]));
    }
}