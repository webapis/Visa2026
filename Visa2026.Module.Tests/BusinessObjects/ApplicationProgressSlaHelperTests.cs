using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressSlaHelperTests
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

        var sla = ApplicationProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.None, sla.Status);
        Assert.Null(sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsOk_WhenAwaitingFirstLegAfterOfficePreparation()
    {
        var app = BuildApplication(
            ApplicationProgressStateCodes.IsBeingPrepared,
            WorkingDaysAgo(3),
            maxDays: 10,
            warningDays: 8,
            legCount: 1,
            locationCode: ApplicationProgressLocationCodes.AtOffice);

        var sla = ApplicationProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.Ok, sla.Status);
        Assert.Null(sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsWarning_WhenPastWarningThreshold()
    {
        var app = BuildApplication(
            ApplicationProgressStateCodes.IsBeingPrepared,
            WorkingDaysAgo(9),
            maxDays: 10,
            warningDays: 8,
            legCount: 1,
            locationCode: ApplicationProgressLocationCodes.AtOffice);

        var sla = ApplicationProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.Warning, sla.Status);
        Assert.Equal(ApplicationProgressSlaCodes.Warning, sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsOverdue_WhenPastMaxDays()
    {
        var app = BuildApplication(
            ApplicationProgressStateCodes.IsBeingPrepared,
            WorkingDaysAgo(11),
            maxDays: 10,
            warningDays: 8,
            legCount: 1,
            locationCode: ApplicationProgressLocationCodes.AtOffice);

        var sla = ApplicationProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.Overdue, sla.Status);
        Assert.Equal(ApplicationProgressSlaCodes.Overdue, sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_UsesPreviousStepDate_ForLegacyReviewStartedRows()
    {
        var app = new Application
        {
            ApplicationType = new ApplicationType
            {
                ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
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
                new ApplicationProgress
                {
                    Date = WorkingDaysAgo(9),
                    State = new ApplicationState { Code = ApplicationProgressStateCodes.IsBeingPrepared },
                    Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtOffice }
                },
                new ApplicationProgress
                {
                    Date = DateTime.Today,
                    State = new ApplicationState { Code = ApplicationProgressLegCodes.ReviewStarted(1) },
                    Location = new ApplicationLocation { Code = ApplicationProgressLegCodes.AtMinistry(1) }
                }
            ],
            ApprovalLegSnapshots =
            [
                new ApplicationApprovalLegSnapshot
                {
                    Sequence = 1,
                    MinistryShortName = "Gurluşyk",
                    MaxDaysInReview = 10,
                    WarningDaysBeforeMax = 8
                }
            ]
        };

        var sla = ApplicationProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.Warning, sla.Status);
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

    private static Application BuildApplication(
        string stateCode,
        DateTime progressDate,
        int maxDays,
        int warningDays,
        int legCount,
        string? locationCode = null)
    {
        var snapshots = Enumerable.Range(1, legCount)
            .Select(sequence => new ApplicationApprovalLegSnapshot
            {
                Sequence = sequence,
                MinistryShortName = $"Leg{sequence}",
                MaxDaysInReview = maxDays,
                WarningDaysBeforeMax = warningDays
            })
            .ToList();

        return new Application
        {
            ApplicationType = new ApplicationType
            {
                ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
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
                new ApplicationProgress
                {
                    Date = progressDate,
                    State = new ApplicationState { Code = stateCode },
                    Location = new ApplicationLocation
                    {
                        Code = locationCode ?? ApplicationProgressLegCodes.AtMinistry(1)
                    }
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
