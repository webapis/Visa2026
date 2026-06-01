using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressPrimaryStateCodeResolverTests
{
    [Fact]
    public void Resolve_NoProgress_ReturnsNull()
    {
        var app = new Application { ProgressHistory = new List<ApplicationProgress>() };

        Assert.Null(ApplicationProgressPrimaryStateCodeResolver.Resolve(app));
        Assert.Equal(string.Empty, app.PrimaryStateCode);
    }

    [Fact]
    public void Resolve_ProcessIssued_ReturnsProcessIssued()
    {
        var app = CreateAppWithLatest(
            ApplicationProgressStateCodes.ProcessIssued,
            ApplicationProgressLocationCodes.AtMigrationService);

        Assert.Equal(ApplicationProgressStateCodes.ProcessIssued, ApplicationProgressPrimaryStateCodeResolver.Resolve(app));
    }

    [Fact]
    public void Resolve_ProcessStartedAtMigration_ReturnsProcessStarted()
    {
        var app = CreateAppWithLatest(
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressLocationCodes.AtMigrationService);

        Assert.Equal(ApplicationProgressStateCodes.ProcessStarted, ApplicationProgressPrimaryStateCodeResolver.Resolve(app));
    }

    [Fact]
    public void Resolve_NonTerminalAtOffice_ReturnsAtOffice()
    {
        var app = CreateAppWithLatest(
            ApplicationProgressStateCodes.ProcessStarted,
            ApplicationProgressLocationCodes.AtOffice);

        Assert.Equal(ApplicationProgressLocationCodes.AtOffice, ApplicationProgressPrimaryStateCodeResolver.Resolve(app));
    }

    [Fact]
    public void Resolve_ReviewRejected_ReturnsStateCode()
    {
        var app = CreateAppWithLatest(
            ApplicationProgressStateCodes.Review1Rejected,
            ApplicationProgressLocationCodes.AtMinistry1);

        Assert.Equal(ApplicationProgressStateCodes.Review1Rejected, ApplicationProgressPrimaryStateCodeResolver.Resolve(app));
    }

    [Fact]
    public void ResolveDisplayName_IncludesStateAndLocation()
    {
        var state = new ApplicationState
        {
            Code = ApplicationProgressStateCodes.ProcessStarted,
            LocalizationKey = ApplicationProgressStateCodes.ProcessStarted,
            NameTm = "İŞLENMEKDE"
        };
        var location = new ApplicationLocation
        {
            Code = ApplicationProgressLocationCodes.AtMigrationService,
            LocalizationKey = ApplicationProgressLocationCodes.AtMigrationService,
            NameTm = "MIGRASIÝA"
        };
        var app = new Application
        {
            ProgressHistory = new List<ApplicationProgress>
            {
                new()
                {
                    State = state,
                    Location = location,
                    Date = DateTime.Today
                }
            }
        };

        var display = ApplicationProgressPrimaryStateCodeResolver.ResolveDisplayName(app);

        Assert.Contains(LookupLocalization.GetDisplayName(state), display, StringComparison.Ordinal);
        Assert.Contains(LookupLocalization.GetDisplayName(location), display, StringComparison.Ordinal);
        Assert.Contains("@", display, StringComparison.Ordinal);
        Assert.Equal(display, app.CurrentState);
    }

    private static Application CreateAppWithLatest(string stateCode, string locationCode) =>
        new()
        {
            ProgressHistory = new List<ApplicationProgress>
            {
                new()
                {
                    Application = null!,
                    State = new ApplicationState { Code = stateCode },
                    Location = new ApplicationLocation { Code = locationCode },
                    Date = DateTime.Today
                }
            }
        };
}
