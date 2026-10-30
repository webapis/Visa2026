using System;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationWorkspaceListProgressMigrationGateTests
{
    [Fact]
    public void HasReachedMigrationService_False_WhenMigrationPending()
    {
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step("migration", "At the migration service", "pending", "", "", ""),
        };

        Assert.False(ApplicationWorkspaceListProgressSteps.HasReachedMigrationService(steps));
    }

    [Theory]
    [InlineData("current")]
    [InlineData("done")]
    public void HasReachedMigrationService_True_WhenMigrationCurrentOrDone(string state)
    {
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step("migration", "At the migration service", state, "issued", "21 Aug", "Issued"),
        };

        Assert.True(ApplicationWorkspaceListProgressSteps.HasReachedMigrationService(steps));
    }

    [Fact]
    public void HasReachedMigrationService_False_WhenNoMigrationStep()
    {
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step("submission", "Submitted", "current", "", "11 Aug", ""),
        };

        Assert.False(ApplicationWorkspaceListProgressSteps.HasReachedMigrationService(steps));
    }

    [Fact]
    public void HasReachedMigrationService_False_WhenEmpty()
    {
        Assert.False(ApplicationWorkspaceListProgressSteps.HasReachedMigrationService(
            Array.Empty<ApplicationWorkspaceListProgressSteps.Step>()));
        Assert.False(ApplicationWorkspaceListProgressSteps.HasReachedMigrationService(
            (System.Collections.Generic.IReadOnlyList<ApplicationWorkspaceListProgressSteps.Step>?)null));
    }

    [Fact]
    public void ShouldShowResultChips_DirectHandIn_WhenInvitationProduced()
    {
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step("migration-submitted", "Submitted to migration service", "done", "ok", "2 Aug", ""),
        };
        var chips = new[]
        {
            new ApplicationWorkspaceIssuedResultListCoverage.Chip("invitation", "Invitation", 1, 1, false),
            new ApplicationWorkspaceIssuedResultListCoverage.Chip("issuedVisa", "Issued visa", 1, 1, false),
        };

        Assert.True(ApplicationWorkspaceListProgressSteps.ShouldShowResultChips(steps, chips));
    }

    [Fact]
    public void ShouldShowResultChips_False_ForRegistrationRejectionOnly()
    {
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step("migration-submitted", "Submitted to migration service", "done", "ok", "2 Aug", ""),
        };
        var chips = new[]
        {
            new ApplicationWorkspaceIssuedResultListCoverage.Chip("rejection", "Rejection", 0, 0, true),
        };

        Assert.False(ApplicationWorkspaceListProgressSteps.ShouldShowResultChips(steps, chips));
    }

    [Fact]
    public void ShouldShowResultChips_False_WhileHandInStillPending()
    {
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "current", "", "", ""),
            new ApplicationWorkspaceListProgressSteps.Step("migration-submitted", "Submitted to migration service", "pending", "", "", ""),
        };
        var chips = new[]
        {
            new ApplicationWorkspaceIssuedResultListCoverage.Chip("invitation", "Invitation", 0, 1, false),
        };

        Assert.False(ApplicationWorkspaceListProgressSteps.ShouldShowResultChips(steps, chips));
    }
}