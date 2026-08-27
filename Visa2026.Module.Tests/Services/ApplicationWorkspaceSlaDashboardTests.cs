using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceSlaDashboardTests
{
    [Fact]
    public void Issued_HidesRemainingClock_AndMarksEveryDeadlineComplete()
    {
        var profile = ViaMinistryProfile(ministryDays: 4, migrationDays: 30);
        var application = IssuedApplication(profile, new DateTime(2026, 8, 25), new DateTime(2026, 8, 27));
        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);
        var chrome = new ApplicationWorkspaceCaseChrome
        {
            StartedOn = "25 Aug 2026",
            SlaDaysRemaining = 8,
        };

        var sla = ApplicationWorkspaceSlaDashboardBuilder.Build(application, profile, default, chrome, steps);
        var header = ApplicationWorkspaceSlaDashboardBuilder.WithHeaderRemaining(chrome, sla);

        Assert.True(sla.IsTerminal);
        Assert.Equal("issued", sla.ProcessOutcome);
        Assert.Equal("Issued", sla.CaseStatus);
        Assert.Null(sla.CaseDaysRemaining);
        Assert.Null(sla.CurrentStepDaysRemaining);
        Assert.Null(header.SlaDaysRemaining);
        Assert.Equal("27 Aug 2026", sla.ExpectedCompletionDate);
        Assert.True(sla.Deadlines.Count >= 4);
        Assert.All(sla.Deadlines, row =>
        {
            Assert.Equal("completed", row.Status);
            Assert.False(row.IsCurrent);
            Assert.Equal("—", row.DaysLeft);
            Assert.Null(row.DaysLeftNumber);
        });
        Assert.True(string.IsNullOrEmpty(sla.AlertMessage));
    }

    [Fact]
    public void InProcess_HeaderMatchesCurrentDeadline_AndOverallUsesMigrationClock()
    {
        var profile = ViaMinistryProfile(ministryDays: 4, migrationDays: 30);
        var start = DateTime.Today;
        while (!WorkingDaysHelper.IsWorkingDay(start))
            start = start.AddDays(-1);

        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = start,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);
        var chrome = new ApplicationWorkspaceCaseChrome { StartedOn = start.ToString("dd MMM yyyy") };
        var sla = ApplicationWorkspaceSlaDashboardBuilder.Build(application, profile, default, chrome, steps);
        var header = ApplicationWorkspaceSlaDashboardBuilder.WithHeaderRemaining(chrome, sla);
        var current = sla.Deadlines.Single(d => d.IsCurrent);
        var elapsed = WorkingDaysHelper.CountWorkingDaysInclusive(start, DateTime.Today);

        Assert.False(sla.IsTerminal);
        Assert.Equal("inprocess", sla.ProcessOutcome);
        Assert.Equal(30, sla.TotalSlaDays);
        Assert.Equal(elapsed, sla.ElapsedDays);
        Assert.Equal(Math.Max(0, 30 - elapsed), sla.CaseDaysRemaining);
        Assert.Equal("Office preparation", current.Step);
        Assert.Equal("inprogress", current.Status);
        Assert.Equal(current.DaysLeftNumber, sla.CurrentStepDaysRemaining);
        Assert.Equal(sla.CurrentStepDaysRemaining, header.SlaDaysRemaining);
        Assert.NotEqual(sla.CaseDaysRemaining, sla.CurrentStepDaysRemaining);
        Assert.Equal(Math.Max(0, 4 - elapsed), sla.CurrentStepDaysRemaining);
    }

    private static ApplicationProfile ViaMinistryProfile(int ministryDays, int migrationDays)
    {
        var profile = new ApplicationProfile
        {
            Name = "Test profile",
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            MinistrySlaDays = ministryDays,
            MigrationSlaDays = migrationDays,
        };
        profile.ApprovalLegs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 1,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Turkmenenergo", NameTm = "Turkmenenergo" },
        });
        profile.ApprovalLegs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 2,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Finansdyly", NameTm = "Finansdyly" },
        });
        return profile;
    }

    private static ApplicationProfileInstance IssuedApplication(
        ApplicationProfile profile,
        DateTime started,
        DateTime issued)
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = started,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 1,
            Date = started,
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1),
                NameTm = "Approved",
            },
        });
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 2,
            Date = started,
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2),
                NameTm = "Approved",
            },
        });
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 3,
            Date = issued,
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
                NameTm = "Issued",
            },
        });
        return application;
    }
}
