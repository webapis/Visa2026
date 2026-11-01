using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// After via-ministry ProcessIssued, CurrentStateLabel is empty and the AS/CO number must still
/// appear under the last step (ListView StatusLabel + workspace Badge). Regression for 6852d2ef / 8f349d6e.
/// </summary>
public sealed class ApplicationWorkspaceListProgressBadgeAndCoNumberTests
{
    [Fact]
    public void Badge_IssuedMigration_ShowsProcessNumberWhenStatusCleared()
    {
        var step = new ApplicationWorkspaceCaseProgressStep
        {
            Key = ApplicationWorkspaceProgressTimeline.MigrationKey,
            Label = ApplicationWorkspaceProgressTimeline.AsNumberReceivedLabel,
            State = "done",
            OutcomeKind = "issued",
            CurrentStateLabel = string.Empty,
            ProcessNumber = "  AS538188  ",
        };

        Assert.Equal("AS538188", ApplicationWorkspaceListProgressSteps.Badge(step));
    }

    [Fact]
    public void Badge_IssuedMigration_EmptyWhenProcessNumberMissing()
    {
        var step = new ApplicationWorkspaceCaseProgressStep
        {
            Key = ApplicationWorkspaceProgressTimeline.MigrationKey,
            State = "done",
            OutcomeKind = "issued",
            CurrentStateLabel = string.Empty,
            ProcessNumber = "   ",
        };

        Assert.Equal(string.Empty, ApplicationWorkspaceListProgressSteps.Badge(step));
    }

    [Fact]
    public void Badge_ProcessingMigration_KeepsStatusWithProcessNumber()
    {
        var step = new ApplicationWorkspaceCaseProgressStep
        {
            Key = ApplicationWorkspaceProgressTimeline.MigrationKey,
            State = "current",
            OutcomeKind = "current",
            CurrentStateLabel = "Processing",
            ProcessNumber = "7/4047",
        };

        var badge = ApplicationWorkspaceListProgressSteps.Badge(step);
        Assert.Contains("Processing", badge, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("7/4047", badge, StringComparison.Ordinal);
    }

    [Fact]
    public void FromTimeline_IssuedMigration_StatusLabelIsProcessNumber()
    {
        var steps = new[]
        {
            new ApplicationWorkspaceCaseProgressStep
            {
                Key = ApplicationWorkspaceProgressTimeline.OfficeKey,
                Label = "Office",
                State = "done",
                OutcomeKind = "ok",
                CurrentStateLabel = string.Empty,
                ProcessNumber = "SHOULD-NOT-SURFACE",
            },
            new ApplicationWorkspaceCaseProgressStep
            {
                Key = ApplicationWorkspaceProgressTimeline.MigrationKey,
                Label = ApplicationWorkspaceProgressTimeline.AsNumberReceivedLabel,
                State = "done",
                OutcomeKind = "issued",
                CurrentStateLabel = string.Empty,
                ProcessNumber = "  CO-91  ",
            },
        };

        var compact = ApplicationWorkspaceListProgressSteps.FromTimeline(steps);
        Assert.Equal(2, compact.Count);
        Assert.Equal(string.Empty, compact[0].StatusLabel);
        Assert.Equal("CO-91", compact[1].StatusLabel);
    }

    [Fact]
    public void Badge_DoneNonOffice_WithoutStatus_UsesCompletedLabel()
    {
        var step = new ApplicationWorkspaceCaseProgressStep
        {
            Key = ApplicationMigrationSubmission.TimelineKey,
            State = "done",
            OutcomeKind = "ok",
            CurrentStateLabel = string.Empty,
        };

        Assert.Equal(
            VisaUiMessages.Get("ApplicationProfileInstance.Workspace.Completed"),
            ApplicationWorkspaceListProgressSteps.Badge(step));
    }
}
