using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationMigrationSlaHelperTests
{
    [Fact]
    public void Resolve_ReturnsNone_WhenNotAtMigrationService()
    {
        var app = BuildApplication(
            ApplicationProgressStateCodes.Review1Started,
            ApplicationProgressLocationCodes.AtMinistry1,
            WorkingDaysAgo(5),
            maxDays: 10,
            warningDays: 8);

        var sla = ApplicationMigrationSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.None, sla.Status);
        Assert.Null(sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsOk_WhenWithinLimit()
    {
        var app = BuildApplication(
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressLocationCodes.AtMigrationService,
            WorkingDaysAgo(3),
            maxDays: 10,
            warningDays: 8);

        var sla = ApplicationMigrationSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.Ok, sla.Status);
        Assert.Null(sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsWarning_WhenPastWarningThreshold()
    {
        var app = BuildApplication(
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressLocationCodes.AtMigrationService,
            WorkingDaysAgo(9),
            maxDays: 10,
            warningDays: 8);

        var sla = ApplicationMigrationSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.Warning, sla.Status);
        Assert.Equal(ApplicationProgressSlaCodes.Warning, sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsOverdue_WhenPastMaxDays()
    {
        var app = BuildApplication(
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressLocationCodes.AtMigrationService,
            WorkingDaysAgo(11),
            maxDays: 10,
            warningDays: 8);

        var sla = ApplicationMigrationSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.Overdue, sla.Status);
        Assert.Equal(ApplicationProgressSlaCodes.Overdue, sla.AppearanceStateCode);
    }

    [Fact]
    public void Resolve_ReturnsNone_WhenProfileMissingMaxDays()
    {
        var app = new Application
        {
            ApplicationType = new ApplicationType
            {
                MigrationSlaProfile = new ApplicationMigrationSlaProfile()
            },
            ProgressHistory =
            [
                new ApplicationProgress
                {
                    Date = WorkingDaysAgo(5),
                    State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessStarted },
                    Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtMigrationService }
                }
            ]
        };

        var sla = ApplicationMigrationSlaHelper.Resolve(app);

        Assert.Equal(ApplicationProgressSlaStatus.None, sla.Status);
    }

    [Fact]
    public void IsMigrationServiceProcessStartedStep_RequiresBothStateAndLocation()
    {
        Assert.True(ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressLocationCodes.AtMigrationService));
        Assert.False(ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressLocationCodes.AtOffice));
    }

    private static Application BuildApplication(
        string stateCode,
        string locationCode,
        DateTime progressDate,
        int maxDays,
        int warningDays)
    {
        return new Application
        {
            ApplicationType = new ApplicationType
            {
                MigrationSlaProfile = new ApplicationMigrationSlaProfile
                {
                    Code = "UP-TO-TWO-WEEKS",
                    NameTm = "2 hepdä çenli",
                    MaxDaysInReview = maxDays,
                    WarningDaysBeforeMax = warningDays
                }
            },
            ProgressHistory =
            [
                new ApplicationProgress
                {
                    Date = progressDate,
                    State = new ApplicationState { Code = stateCode },
                    Location = new ApplicationLocation { Code = locationCode }
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
