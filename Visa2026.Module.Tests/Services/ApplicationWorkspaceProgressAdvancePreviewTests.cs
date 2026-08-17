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

        Assert.Empty(ApplicationWorkspaceProgressAdvancePreview.ResultOptions("office", options));
        Assert.Equal(
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1),
            ApplicationWorkspaceProgressAdvancePreview.PreferredAdvanceCode("office", options, null));
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