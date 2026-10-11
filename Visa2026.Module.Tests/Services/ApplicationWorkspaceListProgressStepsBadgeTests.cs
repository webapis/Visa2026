using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceListProgressStepsBadgeTests
{
    [Fact]
    public void Badge_prefers_current_state_label()
    {
        var step = new ApplicationWorkspaceCaseProgressStep
        {
            Key = "leg-1",
            State = "current",
            CurrentStateLabel = "Submitted",
        };

        Assert.Equal(
            ApplicationProfileLocalization.ProgressStepLabel("Submitted"),
            ApplicationWorkspaceListProgressSteps.Badge(step));
    }

    [Fact]
    public void Badge_done_non_office_without_label_is_completed()
    {
        var step = new ApplicationWorkspaceCaseProgressStep
        {
            Key = "leg-1",
            State = "done",
            CurrentStateLabel = string.Empty,
        };

        Assert.Equal(
            VisaUiMessages.Get("ApplicationProfileInstance.Workspace.Completed"),
            ApplicationWorkspaceListProgressSteps.Badge(step));
    }

    [Fact]
    public void Badge_done_office_without_label_is_empty()
    {
        var step = new ApplicationWorkspaceCaseProgressStep
        {
            Key = ApplicationWorkspaceProgressTimeline.OfficeKey,
            State = "done",
            CurrentStateLabel = string.Empty,
        };

        Assert.Equal(string.Empty, ApplicationWorkspaceListProgressSteps.Badge(step));
    }

    [Fact]
    public void Badge_current_without_label_is_in_progress()
    {
        var step = new ApplicationWorkspaceCaseProgressStep
        {
            Key = ApplicationWorkspaceProgressTimeline.OfficeKey,
            State = "current",
            CurrentStateLabel = "  ",
        };

        Assert.Equal(
            VisaUiMessages.Get("ApplicationProfileInstance.Workspace.InProgress"),
            ApplicationWorkspaceListProgressSteps.Badge(step));
    }

    [Fact]
    public void Badge_pending_without_label_is_pending()
    {
        var step = new ApplicationWorkspaceCaseProgressStep
        {
            Key = ApplicationWorkspaceProgressTimeline.MigrationKey,
            State = "pending",
        };

        Assert.Equal(
            VisaUiMessages.Get("ApplicationProfileInstance.Workspace.Pending"),
            ApplicationWorkspaceListProgressSteps.Badge(step));
    }

    [Fact]
    public void FromTimeline_hides_status_on_done_office_even_when_label_exists()
    {
        var profile = ViaProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2026, 5, 12),
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        var history = new ApplicationProfileInstanceProgress[]
        {
            new()
            {
                ID = Guid.NewGuid(),
                ApplicationProfileInstance = application,
                Order = 1,
                Date = new DateTime(2026, 5, 18),
                State = new ApplicationState
                {
                    Code = ApplicationProfileInstanceProgressStateCodes.Review1Started,
                    NameTm = "Submitted",
                },
            },
        };

        var timeline = ApplicationWorkspaceListProgressSteps.BuildTimeline(application, history);
        var steps = ApplicationWorkspaceListProgressSteps.FromTimeline(timeline);

        Assert.Equal("done", steps[0].State);
        Assert.False(string.IsNullOrWhiteSpace(timeline[0].CurrentStateLabel));
        Assert.Equal(string.Empty, steps[0].StatusLabel);
        Assert.Equal("current", steps[1].State);
        Assert.False(string.IsNullOrWhiteSpace(steps[1].StatusLabel));
    }

    [Fact]
    public void FromTimeline_keeps_status_for_rejected_current_leg()
    {
        var profile = ViaProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2026, 4, 1),
        };
        var history = new ApplicationProfileInstanceProgress[]
        {
            new()
            {
                ID = Guid.NewGuid(),
                ApplicationProfileInstance = application,
                Order = 1,
                Date = new DateTime(2026, 4, 1),
                State = new ApplicationState
                {
                    Code = ApplicationProfileInstanceProgressStateCodes.Review1Started,
                },
            },
            new()
            {
                ID = Guid.NewGuid(),
                ApplicationProfileInstance = application,
                Order = 2,
                Date = new DateTime(2026, 4, 12),
                State = new ApplicationState
                {
                    Code = ApplicationProfileInstanceProgressLegCodes.ReviewRejected(1),
                    NameTm = "Rejected",
                },
            },
        };

        var steps = ApplicationWorkspaceListProgressSteps.Build(application, history);

        Assert.Equal("current", steps[1].State);
        Assert.Equal("rejected", steps[1].OutcomeKind);
        Assert.False(string.IsNullOrWhiteSpace(steps[1].StatusLabel));
    }

    private static ApplicationProfile ViaProfile()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        profile.ApprovalLegs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 1,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Turkmenenergo", NameTm = "Turkmenenergo" },
        });
        profile.ApprovalLegs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 2,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Energetika", NameTm = "Energetika" },
        });
        return profile;
    }
}
