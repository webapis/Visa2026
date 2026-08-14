using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationWorkflowTerminalTests
{
    [Theory]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, true)]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessRejected, true)]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessIssued, true)]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessStarted, false)]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.Review1Rejected, false)]
    public void WorkflowTerminalFlags_ReflectLatestProgressState(
        string stateCode,
        bool expectedTerminal)
    {
        var app = BuildApplication(stateCode);

        Assert.Equal(expectedTerminal, app.IsWorkflowTerminal);
        Assert.Equal(expectedTerminal, ApplicationProfileInstanceProgressProfileResolver.IsWorkflowTerminal(app));
    }

    [Fact]
    public void IsProcessTerminalStateCode_MatchesProcessTerminalsOnly()
    {
        Assert.True(ApplicationProfileInstanceProgressProfileResolver.IsProcessTerminalStateCode(
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled));
        Assert.True(ApplicationProfileInstanceProgressProfileResolver.IsProcessTerminalStateCode(
            ApplicationProfileInstanceProgressStateCodes.ProcessRejected));
        Assert.True(ApplicationProfileInstanceProgressProfileResolver.IsProcessTerminalStateCode(
            ApplicationProfileInstanceProgressStateCodes.ProcessIssued));
        Assert.False(ApplicationProfileInstanceProgressProfileResolver.IsProcessTerminalStateCode(
            ApplicationProfileInstanceProgressStateCodes.Review1Rejected));
    }

    private static ApplicationProfileInstance BuildApplication(string stateCode) =>
        new()
        {
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    Date = new DateTime(2024, 6, 1),
                    State = new ApplicationState { Code = stateCode },
                }
            ]
        };
}