using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationMigrationSlaHelperTests
{
    [Fact]
    public void Resolve_ReturnsNone_WhenNotProcessStarted()
    {
        var app = BuildApplication(
            ApplicationProfileInstanceProgressStateCodes.Review1Started,
            WorkingDaysAgo(5),
            maxDays: 10);

        var sla = ApplicationMigrationSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProfileInstanceProgressSlaStatus.None, sla.Status);
        Assert.Null(sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsOk_WhenWithinLimit()
    {
        var app = BuildApplication(
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
            WorkingDaysAgo(3),
            maxDays: 10);

        var sla = ApplicationMigrationSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProfileInstanceProgressSlaStatus.Ok, sla.Status);
        Assert.Null(sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsOverdue_WhenPastMaxDays()
    {
        var app = BuildApplication(
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
            WorkingDaysAgo(11),
            maxDays: 10);

        var sla = ApplicationMigrationSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProfileInstanceProgressSlaStatus.Overdue, sla.Status);
        Assert.Equal(ApplicationProfileInstanceProgressSlaCodes.Overdue, sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsNone_WhenProfileMissingMaxDays()
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { MigrationSlaDays = 0 },
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    Date = WorkingDaysAgo(5),
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessStarted },
                }
            ]
        };

        var sla = ApplicationMigrationSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProfileInstanceProgressSlaStatus.None, sla.Status);
    }

    [Fact]
    public void IsMigrationServiceProcessStartedStep_IsStateOnly()
    {
        Assert.True(ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted));
        Assert.False(ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared));
    }

    private static ApplicationProfileInstance BuildApplication(
        string stateCode,
        DateTime progressDate,
        int maxDays)
    {
        return new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { MigrationSlaDays = maxDays },
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    Date = progressDate,
                    State = new ApplicationState { Code = stateCode },
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
