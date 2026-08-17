using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstancePersonRosterLockHelperTests
{
    [Theory]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessIssued, true)]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessRejected, true)]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, true)]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessStarted, false)]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.Review1Rejected, false)]
    public void AreResolvedLinksLocked_MatchesWorkflowTerminal(string stateCode, bool expectedLocked)
    {
        var app = new ApplicationProfileInstance
        {
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    State = new ApplicationState { Code = stateCode },
                },
            ],
        };

        Assert.Equal(expectedLocked, ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(app));
    }

    [Fact]
    public void AreResolvedLinksLocked_FalseDuringDataImportEvenWhenTerminal()
    {
        var app = new ApplicationProfileInstance
        {
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessIssued },
                },
            ],
        };

        using var scope = Visa2026.Module.Services.MigrationImport.MigrationImportContext.BeginDataImportScope();
        Assert.False(ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(app));
    }

    [Fact]
    public void AreResolvedLinksLocked_FalseWhenNoProgressHistory()
    {
        Assert.False(ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(new ApplicationProfileInstance()));
    }

    [Fact]
    public void RefreshResolvedLinks_NoOpWhenWorkflowTerminal()
    {
        var app = new ApplicationProfileInstance
        {
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessCancelled },
                },
            ],
        };
        ApplicationProfileInstancePersonResolver.RefreshResolvedLinks(objectSpace: null!, app, new Person());
    }

    [Fact]
    public void RelinkPerson_NoOpWhenWorkflowTerminal()
    {
        var app = new ApplicationProfileInstance
        {
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessCancelled },
                },
            ],
        };

        Assert.False(ApplicationProfileInstancePersonService.RelinkPerson(null!, app, new Person()));
    }
}
