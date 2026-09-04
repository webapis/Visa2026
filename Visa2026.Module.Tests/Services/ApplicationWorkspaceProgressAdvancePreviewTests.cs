using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceProgressAdvancePreviewTests
{
    [Fact]
    public void IsResultForStep_SameMinistryDecision_IsTrue()
    {
        Assert.True(ApplicationWorkspaceProgressAdvancePreview.IsResultForStep(
            "leg-2",
            ApplicationProfileInstanceProgressLegCodes.ReviewRejected(2)));
        Assert.True(ApplicationWorkspaceProgressAdvancePreview.IsResultForStep(
            "leg-2",
            ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2)));
    }

    [Fact]
    public void IsResultForStep_Cancelled_IsTrueOnCurrentSlot()
    {
        Assert.True(ApplicationWorkspaceProgressAdvancePreview.IsResultForStep(
            "office",
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled));
        Assert.True(ApplicationWorkspaceProgressAdvancePreview.IsResultForStep(
            "leg-1",
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled));
        Assert.True(ApplicationWorkspaceProgressAdvancePreview.IsResultForStep(
            "migration",
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled));
    }

    [Fact]
    public void IsResultForStep_Submitted_IsTrueOnOfficeOnly()
    {
        Assert.True(ApplicationWorkspaceProgressAdvancePreview.IsResultForStep(
            "office",
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1)));
        Assert.True(ApplicationWorkspaceProgressAdvancePreview.IsResultForStep(
            "office",
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted));
        Assert.False(ApplicationWorkspaceProgressAdvancePreview.IsResultForStep(
            "leg-1",
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1)));
        Assert.False(ApplicationWorkspaceProgressAdvancePreview.IsOfficeSubmitted(
            "leg-1",
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1)));
    }

    [Fact]
    public void IsResultForStep_NextMinistry_IsFalse()
    {
        Assert.False(ApplicationWorkspaceProgressAdvancePreview.IsResultForStep(
            "leg-2",
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(3)));
    }

    [Fact]
    public void IsResultForStep_StartedOnSameMinistry_IsFalse()
    {
        Assert.False(ApplicationWorkspaceProgressAdvancePreview.IsResultForStep(
            "leg-2",
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(2)));
    }

    [Fact]
    public void PreferredAdvanceCode_Office_UsesFirstMinistryStarted()
    {
        var options = new[]
        {
            new ApplicationWorkspaceCaseProgressAdvanceOption
            {
                StateCode = ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1),
                Label = "Submitted",
            },
            new ApplicationWorkspaceCaseProgressAdvanceOption
            {
                StateCode = ApplicationProfileInstanceProgressLegCodes.ReviewRejected(1),
                Label = "Disapproved",
            },
            new ApplicationWorkspaceCaseProgressAdvanceOption
            {
                StateCode = ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
                Label = "Cancelled",
            },
        };

        var officeResults = ApplicationWorkspaceProgressAdvancePreview.ResultOptions("office", options);
        Assert.Contains(
            officeResults,
            o => o.StateCode == ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1));
        Assert.Equal(
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1),
            officeResults[0].StateCode);
        Assert.Contains(
            officeResults,
            o => o.StateCode == ApplicationProfileInstanceProgressStateCodes.ProcessCancelled);
        Assert.Equal(
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
            officeResults[^1].StateCode);
        Assert.DoesNotContain(
            officeResults,
            o => o.StateCode == ApplicationProfileInstanceProgressLegCodes.ReviewRejected(1));
        Assert.Equal(
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1),
            ApplicationWorkspaceProgressAdvancePreview.PreferredAdvanceCode("office", options, null));
    }

    [Fact]
    public void PreferredAdvanceCode_Ministry_DefaultsToApprovedNotCancelled()
    {
        var options = new[]
        {
            new ApplicationWorkspaceCaseProgressAdvanceOption
            {
                StateCode = ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2),
                Label = "Approved",
            },
            new ApplicationWorkspaceCaseProgressAdvanceOption
            {
                StateCode = ApplicationProfileInstanceProgressLegCodes.ReviewRejected(2),
                Label = "Disapproved",
            },
            new ApplicationWorkspaceCaseProgressAdvanceOption
            {
                StateCode = ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
                Label = "Cancelled",
            },
        };

        var results = ApplicationWorkspaceProgressAdvancePreview.ResultOptions("leg-2", options);
        Assert.Equal(3, results.Count);
        Assert.Equal(ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, results[^1].StateCode);
        Assert.Equal(
            ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2),
            ApplicationWorkspaceProgressAdvancePreview.PreferredAdvanceCode("leg-2", options, null));
        Assert.Equal(
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
            ApplicationWorkspaceProgressAdvancePreview.PreferredAdvanceCode(
                "leg-2",
                options,
                ApplicationProfileInstanceProgressStateCodes.ProcessCancelled));
    }

    [Fact]
    public void OutcomeKind_Rejected_IsRejected()
    {
        Assert.Equal(
            "rejected",
            ApplicationWorkspaceProgressAdvancePreview.OutcomeKind(
                ApplicationProfileInstanceProgressLegCodes.ReviewRejected(2)));
    }
}