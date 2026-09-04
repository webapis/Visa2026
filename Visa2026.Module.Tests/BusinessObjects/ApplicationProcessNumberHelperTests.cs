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

    [Fact]
    public void TryAssign_RejectsEmptyAfterProcessStarted()
    {
        var application = new ApplicationProfileInstance
        {
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessStarted },
                    ProcessNumber = "AS1",
                },
            ],
            ProcessNumber = "AS1",
        };

        Assert.False(ApplicationProcessNumberHelper.TryAssign(null, application, "  ", requireWhenVisible: true, out var error));
        Assert.Contains("required", error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("AS1", application.ProcessNumber);
    }

    [Fact]
    public void TryAssign_AllowsEmptyBeforeSubmitted()
    {
        var application = new ApplicationProfileInstance();
        Assert.True(ApplicationProcessNumberHelper.TryAssign(null, application, null, requireWhenVisible: true, out var error));
        Assert.Null(error);
        Assert.Null(application.ProcessNumber);
    }

    [Fact]
    public void CopyForIssuedDocument_PrefersInstance()
    {
        var application = new ApplicationProfileInstance { ProcessNumber = " AS538188 " };
        Assert.Equal("AS538188", ApplicationProcessNumberHelper.CopyForIssuedDocument(application));
    }

    [Fact]
    public void ApplyToVisa_CopiesInstanceProcessNumber()
    {
        var visa = new Visa { VisaNumber = "V-1" };
        ApplicationProcessNumberHelper.ApplyToVisa(visa, new ApplicationProfileInstance { ProcessNumber = "AS9" });
        Assert.Equal("AS9", visa.ProcessNumber);
    }
}