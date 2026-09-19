using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceListProgressStepsTests
{
    [Fact]
    public void Build_empty_history_office_current_legs_pending()
    {
        var profile = ViaProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };

        var steps = ApplicationWorkspaceListProgressSteps.Build(application, []);

        Assert.Equal(4, steps.Count);
        Assert.Equal(ApplicationWorkspaceProgressTimeline.OfficeKey, steps[0].Key);
        Assert.Equal("current", steps[0].State);
        Assert.Equal("current", ApplicationWorkspaceListProgressSteps.Tone(steps[0]));
        Assert.Equal("1", ApplicationWorkspaceListProgressSteps.Glyph(steps[0], 1));
        Assert.Equal("pending", steps[1].State);
        Assert.Equal("Turkmenenergo", steps[1].Label);
        Assert.Equal("pending", steps[2].State);
        Assert.Equal(ApplicationWorkspaceProgressTimeline.MigrationKey, steps[3].Key);
        Assert.Equal("pending", steps[3].State);
        Assert.Contains(" \u00b7 ", ApplicationWorkspaceListProgressSteps.FormatDisplay(steps));
    }

    [Fact]
    public void Build_direct_route_is_office_and_migration_only()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            },
            ApplicationDate = DateTime.Today,
        };

        var steps = ApplicationWorkspaceListProgressSteps.Build(application, []);

        Assert.Equal(2, steps.Count);
        Assert.Equal(ApplicationWorkspaceProgressTimeline.OfficeKey, steps[0].Key);
        Assert.Equal("current", steps[0].State);
        Assert.Equal(ApplicationWorkspaceProgressTimeline.MigrationKey, steps[1].Key);
        Assert.Equal("pending", steps[1].State);
    }

    [Fact]
    public void Build_first_leg_started_marks_office_done()
    {
        var profile = ViaProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2026, 5, 12),
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

        var steps = ApplicationWorkspaceListProgressSteps.Build(application, history);

        Assert.Equal("done", steps[0].State);
        Assert.Equal("done", ApplicationWorkspaceListProgressSteps.Tone(steps[0]));
        Assert.Equal("\u2713", ApplicationWorkspaceListProgressSteps.Glyph(steps[0], 1));
        Assert.Equal("18 May 2026", steps[0].Date);
        Assert.Equal("current", steps[1].State);
        Assert.Equal("18 May 2026", steps[1].Date);
        Assert.False(string.IsNullOrWhiteSpace(steps[1].StatusLabel));
        Assert.Equal("pending", steps[2].State);
        Assert.Equal("pending", steps[3].State);
    }

    [Fact]
    public void Build_issued_marks_all_done()
    {
        var profile = ViaProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2026, 5, 23),
        };
        var history = new ApplicationProfileInstanceProgress[]
        {
            Row(application, 1, new DateTime(2026, 5, 23), ApplicationProfileInstanceProgressStateCodes.Review1Started),
            Row(application, 2, new DateTime(2026, 5, 23), ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1)),
            Row(application, 3, new DateTime(2026, 1, 2), ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2)),
            Row(application, 4, new DateTime(2026, 1, 8), ApplicationProfileInstanceProgressStateCodes.ProcessIssued),
        };

        var steps = ApplicationWorkspaceListProgressSteps.Build(application, history);

        Assert.All(steps, s => Assert.Equal("done", s.State));
        Assert.Equal("issued", steps[^1].OutcomeKind);
        Assert.Equal("issued", ApplicationWorkspaceListProgressSteps.Tone(steps[^1]));
        Assert.Equal("\u2713", ApplicationWorkspaceListProgressSteps.Glyph(steps[^1], 4));
        Assert.Equal("08 Jan 2026", steps[^1].Date);
    }

    [Fact]
    public void Build_rejected_ministry_uses_rejected_tone()
    {
        var profile = ViaProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2026, 4, 1),
        };
        var history = new ApplicationProfileInstanceProgress[]
        {
            Row(application, 1, new DateTime(2026, 4, 1), ApplicationProfileInstanceProgressStateCodes.Review1Started),
            Row(application, 2, new DateTime(2026, 4, 9), ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1)),
            Row(application, 3, new DateTime(2026, 4, 12), ApplicationProfileInstanceProgressLegCodes.ReviewRejected(2)),
        };

        var steps = ApplicationWorkspaceListProgressSteps.Build(application, history);

        Assert.Equal("done", steps[0].State);
        Assert.Equal("done", steps[1].State);
        Assert.Equal("current", steps[2].State);
        Assert.Equal("rejected", steps[2].OutcomeKind);
        Assert.Equal("rej", ApplicationWorkspaceListProgressSteps.Tone(steps[2]));
        Assert.Equal("\u2715", ApplicationWorkspaceListProgressSteps.Glyph(steps[2], 3));
        Assert.False(string.IsNullOrWhiteSpace(steps[2].StatusLabel));
        Assert.Equal("pending", steps[3].State);
    }

    [Fact]
    public void FormatDisplay_empty_is_em_dash()
    {
        Assert.Equal("\u2014", ApplicationWorkspaceListProgressSteps.FormatDisplay(null));
        Assert.Equal("\u2014", ApplicationWorkspaceListProgressSteps.FormatDisplay([]));
    }

    private static ApplicationProfileInstanceProgress Row(
        ApplicationProfileInstance application,
        int order,
        DateTime date,
        string stateCode) =>
        new()
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = order,
            Date = date,
            State = new ApplicationState { Code = stateCode },
        };

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