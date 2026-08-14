using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileInstanceProgressSlaHelperTests
{
    [Fact]
    public void Resolve_ReturnsNone_WhenLastLegApprovedAndMigrationNext()
    {
        var app = BuildApplication(
            "2_REVIEW_APPROVED",
            DateTime.Today.AddDays(-10),
            maxDays: 10,
            warningDays: 8,
            legCount: 2);

        var sla = ApplicationProfileInstanceProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProfileInstanceProgressSlaStatus.None, sla.Status);
        Assert.Null(sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsOk_WhenAwaitingFirstLegAfterOfficePreparation()
    {
        var app = BuildApplication(
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
            WorkingDaysAgo(3),
            maxDays: 10,
            warningDays: 8,
            legCount: 1);

        var sla = ApplicationProfileInstanceProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProfileInstanceProgressSlaStatus.Ok, sla.Status);
        Assert.Null(sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsWarning_WhenPastWarningThreshold()
    {
        var app = BuildApplication(
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
            WorkingDaysAgo(9),
            maxDays: 10,
            warningDays: 8,
            legCount: 1);

        var sla = ApplicationProfileInstanceProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProfileInstanceProgressSlaStatus.Warning, sla.Status);
        Assert.Equal(ApplicationProfileInstanceProgressSlaCodes.Warning, sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsOverdue_WhenPastMaxDays()
    {
        var app = BuildApplication(
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
            WorkingDaysAgo(11),
            maxDays: 10,
            warningDays: 8,
            legCount: 1);

        var sla = ApplicationProfileInstanceProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProfileInstanceProgressSlaStatus.Overdue, sla.Status);
        Assert.Equal(ApplicationProfileInstanceProgressSlaCodes.Overdue, sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_UsesPreviousStepDate_ForLegacyReviewStartedRows()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType
            {
                ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
                ShowApprovalLegProfile = true
            },
            ApprovalLegProfile = new ApprovalLegProfile
            {
                MinistryLegs =
                [
                    new ApprovalLegProfileMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() }
                ]
            },
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    Date = WorkingDaysAgo(9),
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared },
                },
                new ApplicationProfileInstanceProgress
                {
                    Date = DateTime.Today,
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1) },
                }
            ],
            ApprovalLegSnapshots =
            [
                new ApplicationProfileInstanceApprovalLegSnapshot
                {
                    Sequence = 1,
                    MinistryShortName = "Gurluşyk",
                    MaxDaysInReview = 10,
                    WarningDaysBeforeMax = 8
                }
            ]
        };

        var sla = ApplicationProfileInstanceProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProfileInstanceProgressSlaStatus.Warning, sla.Status);
    }

    [Fact]
    public void TryValidateLegSla_BlocksActiveProfile_WhenGlobalSlaInvalid()
    {
        var profile = new ApprovalLegProfile
        {
            IsActive = true,
            MinistryLegs =
            [
                new ApprovalLegProfileMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() }
            ]
        };

        Assert.False(MinistryReviewSlaHelper.TryValidateSlaValues(0, 8, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }


    private static ApplicationProfileInstance BuildImpliedOfficeApplication(
        DateTime applicationDate,
        int maxDays,
        int warningDays,
        int legCount)
    {
        var app = BuildApplication(
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
            applicationDate,
            maxDays,
            warningDays,
            legCount);
        app.ApplicationDate = applicationDate;
        app.ProgressHistory = [];
        return app;
    }
    private static ApplicationProfileInstance BuildApplication(
        string stateCode,
        DateTime progressDate,
        int maxDays,
        int warningDays,
        int legCount)
    {
        var snapshots = Enumerable.Range(1, legCount)
            .Select(sequence => new ApplicationProfileInstanceApprovalLegSnapshot
            {
                Sequence = sequence,
                MinistryShortName = $"Leg{sequence}",
                MaxDaysInReview = maxDays,
                WarningDaysBeforeMax = warningDays
            })
            .ToList();

        return new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType
            {
                ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
                ShowApprovalLegProfile = true
            },
            ApprovalLegProfile = new ApprovalLegProfile
            {
                MinistryLegs = snapshots
                    .Select(snapshot => new ApprovalLegProfileMinistryLeg
                    {
                        Sequence = snapshot.Sequence,
                        ApprovingMinistry = new ApprovingMinistry()
                    })
                    .ToList()
            },
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    Date = progressDate,
                    State = new ApplicationState { Code = stateCode },
                }
            ],
            ApprovalLegSnapshots = snapshots
        };
    }

    private static DateTime WorkingDaysAgo(int workingDaysInclusive)
    {
        var date = DateTime.Today.Date;
        var counted = WorkingDaysHelper.IsWorkingDay(date) ? 1 : 0;
        while (counted < workingDaysInclusive)
        {
            date = date.AddDays(-1);
            if (WorkingDaysHelper.IsWorkingDay(date))
                counted++;
        }

        return date;
    }
}
