using System;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonDossier;

/// <summary>
/// Person dossier progress stems use the same chip gate as ListView: ForDisplay (hide empty
/// rejection) then ShouldShowResultChips. Locks 8f229f4b / acdb1924 composition without ObjectSpace.
/// </summary>
public sealed class PersonDossierResultChipStemGateTests
{
    [Fact]
    public void StemChips_Hidden_WhileMigrationPending_EvenWithInvitationCoverage()
    {
        var compact = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationWorkspaceProgressTimeline.MigrationKey,
                "AS number received",
                "pending",
                "",
                "",
                ""),
        };
        var coverage = new[]
        {
            new ApplicationWorkspaceIssuedResultListCoverage.Chip(
                ApplicationWorkspaceIssuedRecordsCatalog.Invitation, "Invitation", 0, 1, false),
            new ApplicationWorkspaceIssuedResultListCoverage.Chip(
                ApplicationWorkspaceIssuedRecordsCatalog.Rejection, "Rejection", 0, 0, true),
        };

        var display = ApplicationWorkspaceIssuedResultListCoverage.ForDisplay(coverage);
        Assert.DoesNotContain(display, c =>
            string.Equals(c.Key, ApplicationWorkspaceIssuedRecordsCatalog.Rejection, StringComparison.OrdinalIgnoreCase));
        Assert.False(ApplicationWorkspaceListProgressSteps.ShouldShowResultChips(compact, display));
    }

    [Fact]
    public void StemChips_Visible_WhenMigrationReached_WithoutEmptyRejection()
    {
        var compact = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "1 Aug", ""),
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationWorkspaceProgressTimeline.MigrationKey,
                "AS number received",
                "done",
                "issued",
                "21 Aug",
                "AS-1"),
        };
        var coverage = new[]
        {
            new ApplicationWorkspaceIssuedResultListCoverage.Chip(
                ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, "Visa", 0, 1, false),
            new ApplicationWorkspaceIssuedResultListCoverage.Chip(
                ApplicationWorkspaceIssuedRecordsCatalog.Rejection, "Rejection", 0, 0, true),
            new ApplicationWorkspaceIssuedResultListCoverage.Chip(
                ApplicationWorkspaceIssuedRecordsCatalog.Invitation, "Invitation", 1, 1, false),
        };

        var display = ApplicationWorkspaceIssuedResultListCoverage.ForDisplay(coverage);
        Assert.Equal(2, display.Count);
        Assert.True(ApplicationWorkspaceListProgressSteps.ShouldShowResultChips(compact, display));
        Assert.Equal(ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, display[0].Key);
        Assert.Equal(ApplicationWorkspaceIssuedRecordsCatalog.Invitation, display[1].Key);
    }

    [Fact]
    public void StemChips_Hidden_WhenOnlyEmptyRejectionRemainsAfterForDisplay()
    {
        var compact = new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationWorkspaceProgressTimeline.MigrationKey,
                "AS number received",
                "current",
                "",
                "",
                ""),
        };
        var coverage = new[]
        {
            new ApplicationWorkspaceIssuedResultListCoverage.Chip(
                ApplicationWorkspaceIssuedRecordsCatalog.Rejection, "Rejection", 0, 0, true),
        };

        var display = ApplicationWorkspaceIssuedResultListCoverage.ForDisplay(coverage);
        Assert.Empty(display);
        Assert.False(ApplicationWorkspaceListProgressSteps.ShouldShowResultChips(compact, display));
    }
}
