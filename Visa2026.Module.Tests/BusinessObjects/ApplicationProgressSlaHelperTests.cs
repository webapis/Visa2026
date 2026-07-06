using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressSlaHelperTests
{
    [Fact]
    public void Resolve_ReturnsNone_WhenNotInReviewStarted()
    {
        var app = BuildApplication(
            "1_REVIEW_APPROVED",
            DateTime.Today.AddDays(-10),
            maxDays: 10,
            warningDays: 8);

        var sla = ApplicationProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.None, sla.Status);
        Assert.Null(sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsOk_WhenWithinLimit()
    {
        var app = BuildApplication(
            ApplicationProgressLegCodes.ReviewStarted(1),
            WorkingDaysAgo(3),
            maxDays: 10,
            warningDays: 8);

        var sla = ApplicationProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.Ok, sla.Status);
        Assert.Null(sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsWarning_WhenPastWarningThreshold()
    {
        var app = BuildApplication(
            ApplicationProgressLegCodes.ReviewStarted(1),
            WorkingDaysAgo(9),
            maxDays: 10,
            warningDays: 8);

        var sla = ApplicationProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.Warning, sla.Status);
        Assert.Equal(ApplicationProgressSlaCodes.Warning, sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsOverdue_WhenPastMaxDays()
    {
        var app = BuildApplication(
            ApplicationProgressLegCodes.ReviewStarted(1),
            WorkingDaysAgo(11),
            maxDays: 10,
            warningDays: 8);

        var sla = ApplicationProgressSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.Overdue, sla.Status);
        Assert.Equal(ApplicationProgressSlaCodes.Overdue, sla.AppearanceStateCode);
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
        int warningDays)
    {
        return new Application
        {
            ProgressHistory =
            [
                new ApplicationProgress
                {
                    Date = progressDate,
                    State = new ApplicationState { Code = stateCode }
                }
            ],
            ApprovalLegSnapshots =
            [
                new ApplicationApprovalLegSnapshot
                {
                    Sequence = 1,
                    MinistryShortName = "Gurluşyk",
                    MaxDaysInReview = maxDays,
                    WarningDaysBeforeMax = warningDays
                }
            ]
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
