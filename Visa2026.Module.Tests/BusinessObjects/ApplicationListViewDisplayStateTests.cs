using System.Linq;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationListViewDisplayStateTests
{
    [Fact]
    public void Resolve_WithoutSla_UsesPrimaryStateForRowAppearance()
    {
        var latest = new ApplicationProgress
        {
            ID = Guid.NewGuid(),
            Date = DateTime.Today,
            Order = 1,
            State = new ApplicationState
            {
                Code = ApplicationProgressStateCodes.ProcessIssued
            }
        };
        var application = new Application
        {
            LatestProgressId = latest.ID,
            LatestProgress = latest,
            ProgressHistory = [latest]
        };

        var state = ApplicationListViewDisplayState.Resolve(application);

        Assert.Equal(ApplicationProgressStateCodes.ProcessIssued, state.PrimaryStateCode);
        Assert.Equal(ApplicationProgressStateCodes.ProcessIssued, state.ListRowAppearanceStateCode);
        Assert.Equal(string.Empty, state.ProgressSlaAppearanceCode);
        Assert.Contains("visa-progress-row--state-", state.ListRowCssClass, StringComparison.Ordinal);
        Assert.Contains("visa-progress-row", state.ListRowCssClass, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_WhenProgressSlaOverdue_PrefersSlaOverPrimaryState()
    {
        var application = BuildViaMinistryApplication(
            ApplicationProgressStateCodes.IsBeingPrepared,
            WorkingDaysAgo(11),
            maxDays: 10,
            warningDays: 8);

        var state = ApplicationListViewDisplayState.Resolve(application);

        Assert.Equal(ApplicationProgressStateCodes.IsBeingPrepared, state.PrimaryStateCode);
        Assert.Equal(ApplicationProgressSlaCodes.Overdue, state.ProgressSlaAppearanceCode);
        Assert.Equal(ApplicationProgressSlaCodes.Overdue, state.ListRowAppearanceStateCode);
        Assert.Equal(
            $"visa-progress-row--state-{ApplicationProgressSlaCodes.Overdue} visa-progress-row",
            state.ListRowCssClass);
    }

    [Fact]
    public void Resolve_WhenProgressSlaWarning_PrefersWarningAppearance()
    {
        var application = BuildViaMinistryApplication(
            ApplicationProgressStateCodes.IsBeingPrepared,
            WorkingDaysAgo(9),
            maxDays: 10,
            warningDays: 8);

        var state = ApplicationListViewDisplayState.Resolve(application);

        Assert.Equal(ApplicationProgressSlaCodes.Warning, state.ListRowAppearanceStateCode);
        Assert.False(string.IsNullOrWhiteSpace(state.ProgressSlaStatement));
    }

    private static Application BuildViaMinistryApplication(
        string stateCode,
        DateTime progressDate,
        int maxDays,
        int warningDays)
    {
        var snapshots = new[]
        {
            new ApplicationApprovalLegSnapshot
            {
                Sequence = 1,
                MinistryShortName = "Leg1",
                MaxDaysInReview = maxDays,
                WarningDaysBeforeMax = warningDays
            }
        };

        return new Application
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
                    new ApprovalLegProfileMinistryLeg
                    {
                        Sequence = 1,
                        ApprovingMinistry = new ApprovingMinistry()
                    }
                ]
            },
            ProgressHistory =
            [
                new ApplicationProgress
                {
                    Date = progressDate,
                    State = new ApplicationState { Code = stateCode },
                }
            ],
            ApprovalLegSnapshots = snapshots.ToList()
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
