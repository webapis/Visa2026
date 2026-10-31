using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Result pills stem from the last progress hand-off. Anchor choice must stay aligned with
/// dossier/ListView merge (migration node when present; otherwise migration-submitted).
/// </summary>
public sealed class ApplicationWorkspaceListProgressResultChipAnchorTests
{
    [Fact]
    public void IsResultChipAnchor_UsesMigration_WhenTimelineIncludesMigration()
    {
        var steps = new[]
        {
            Step("office", "done"),
            Step(ApplicationMigrationSubmission.TimelineKey, "done"),
            Step(ApplicationWorkspaceProgressTimeline.MigrationKey, "current"),
        };

        Assert.True(ApplicationWorkspaceListProgressSteps.IsResultChipAnchor(
            steps,
            steps[^1]));
        Assert.False(ApplicationWorkspaceListProgressSteps.IsResultChipAnchor(
            steps,
            steps[1]));
        Assert.False(ApplicationWorkspaceListProgressSteps.IsResultChipAnchor(
            steps,
            Step(ApplicationWorkspaceProgressTimeline.MigrationKey, "pending")));
    }

    [Fact]
    public void IsResultChipAnchor_UsesHandIn_WhenDirectRouteHasNoMigrationNode()
    {
        var steps = new[]
        {
            Step("office", "done"),
            Step(ApplicationMigrationSubmission.TimelineKey, "done"),
        };

        Assert.True(ApplicationWorkspaceListProgressSteps.IsResultChipAnchor(
            steps,
            steps[^1]));
        Assert.False(ApplicationWorkspaceListProgressSteps.IsResultChipAnchor(
            steps,
            steps[0]));
    }

    [Fact]
    public void IsResultChipAnchor_False_ForNullOrEmpty()
    {
        var step = Step(ApplicationWorkspaceProgressTimeline.MigrationKey, "current");
        Assert.False(ApplicationWorkspaceListProgressSteps.IsResultChipAnchor(null, step));
        Assert.False(ApplicationWorkspaceListProgressSteps.IsResultChipAnchor(
            Array.Empty<ApplicationWorkspaceCaseProgressStep>(),
            step));
    }

    [Fact]
    public void ShouldShowResultChips_False_WhileMigrationStillPending()
    {
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationMigrationSubmission.TimelineKey,
                "Submitted",
                "done",
                "ok",
                "10 Aug",
                ""),
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationWorkspaceProgressTimeline.MigrationKey,
                "At the migration service",
                "pending",
                "",
                "",
                ""),
        };
        var chips = new[]
        {
            new ApplicationWorkspaceIssuedResultListCoverage.Chip(
                ApplicationWorkspaceIssuedRecordsCatalog.Invitation,
                "Invitation",
                0,
                2,
                false),
        };

        Assert.False(ApplicationWorkspaceListProgressSteps.ShouldShowResultChips(steps, chips));
    }

    [Theory]
    [InlineData("current")]
    [InlineData("done")]
    public void ShouldShowResultChips_True_WhenMigrationReached_WithIssuedDocument(string migrationState)
    {
        var steps = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationWorkspaceProgressTimeline.MigrationKey,
                "At the migration service",
                migrationState,
                "issued",
                "21 Aug",
                "Issued"),
        };
        var chips = new[]
        {
            new ApplicationWorkspaceIssuedResultListCoverage.Chip(
                ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa,
                "Issued visa",
                1,
                2,
                false),
        };

        Assert.True(ApplicationWorkspaceListProgressSteps.ShouldShowResultChips(steps, chips));
    }

    private static ApplicationWorkspaceCaseProgressStep Step(string key, string state) =>
        new()
        {
            Key = key,
            State = state,
            Label = key,
            CurrentStateLabel = state,
        };
}
