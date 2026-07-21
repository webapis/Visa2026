using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationWorkflowTerminalTests
{
    [Theory]
    [InlineData(ApplicationProgressStateCodes.ProcessCancelled, true)]
    [InlineData(ApplicationProgressStateCodes.ProcessRejected, true)]
    [InlineData(ApplicationProgressStateCodes.ProcessIssued, true)]
    [InlineData(ApplicationProgressStateCodes.ProcessStarted, false)]
    [InlineData(ApplicationProgressStateCodes.Review1Rejected, false)]
    public void WorkflowTerminalFlags_ReflectLatestProgressState(
        string stateCode,
        bool expectedTerminal)
    {
        var app = BuildApplication(stateCode);

        Assert.Equal(expectedTerminal, app.IsWorkflowTerminal);
        Assert.Equal(expectedTerminal, ApplicationProgressProfileResolver.IsWorkflowTerminal(app));
    }

    [Fact]
    public void IsProcessTerminalStateCode_MatchesProcessTerminalsOnly()
    {
        Assert.True(ApplicationProgressProfileResolver.IsProcessTerminalStateCode(
            ApplicationProgressStateCodes.ProcessCancelled));
        Assert.True(ApplicationProgressProfileResolver.IsProcessTerminalStateCode(
            ApplicationProgressStateCodes.ProcessRejected));
        Assert.True(ApplicationProgressProfileResolver.IsProcessTerminalStateCode(
            ApplicationProgressStateCodes.ProcessIssued));
        Assert.False(ApplicationProgressProfileResolver.IsProcessTerminalStateCode(
            ApplicationProgressStateCodes.Review1Rejected));
    }

    private static Application BuildApplication(string stateCode) =>
        new()
        {
            ProgressHistory =
            [
                new ApplicationProgress
                {
                    Date = new DateTime(2024, 6, 1),
                    State = new ApplicationState { Code = stateCode },
                }
            ]
        };
}