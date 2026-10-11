using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceProgressTimelineOfficerNotesTests
{
    [Fact]
    public void Build_empty_history_shows_office_notes_only_on_current_office()
    {
        var profile = ViaProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            OfficePreparationNotes = "Prepare dossier pack",
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("current", steps[0].State);
        Assert.Equal("Prepare dossier pack", steps[0].OfficerNotes);
        Assert.All(steps.Skip(1), step => Assert.Equal(string.Empty, step.OfficerNotes));
    }

    [Fact]
    public void Build_first_leg_started_hides_office_notes_and_shows_current_leg_description()
    {
        var profile = ViaProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2026, 5, 12),
            OfficePreparationNotes = "Must stay on office only",
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 1,
            Date = new DateTime(2026, 5, 18),
            Description = "Waiting on ministry reply",
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.Review1Started,
                NameTm = "Submitted",
            },
        });

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("done", steps[0].State);
        Assert.Equal(string.Empty, steps[0].OfficerNotes);
        Assert.Equal("current", steps[1].State);
        Assert.Equal("Waiting on ministry reply", steps[1].OfficerNotes);
        Assert.Equal(string.Empty, steps[2].OfficerNotes);
        Assert.Equal(string.Empty, steps[3].OfficerNotes);
    }

    [Fact]
    public void Build_pending_and_done_legs_never_surface_row_description()
    {
        var profile = ViaProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2026, 4, 1),
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 1,
            Date = new DateTime(2026, 4, 2),
            Description = "Office submit note",
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.Review1Started,
            },
        });
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 2,
            Date = new DateTime(2026, 4, 9),
            Description = "First ministry decided",
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1),
            },
        });

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("done", steps[0].State);
        Assert.Equal(string.Empty, steps[0].OfficerNotes);
        Assert.Equal("done", steps[1].State);
        // Row description stays on the resolved link, but the UI must not show notes on done slots.
        Assert.Equal(string.Empty, steps[1].OfficerNotes);
        Assert.Equal("current", steps[2].State);
        Assert.Equal(string.Empty, steps[2].OfficerNotes);
        Assert.Equal(string.Empty, steps[3].OfficerNotes);
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
