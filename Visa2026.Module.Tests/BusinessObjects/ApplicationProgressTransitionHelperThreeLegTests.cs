using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressTransitionHelperThreeLegTests
{
    [Fact]
    public void ThreeLegContract_AllowsFullMinistryChain()
    {
        var app = BuildThreeLegApplication();

        Assert.True(IsAllowed(app,
            ApplicationProgressStateCodes.IsBeingPrepared, ApplicationProgressLocationCodes.AtOffice,
            ApplicationProgressLegCodes.ReviewStarted(1), ApplicationProgressLegCodes.AtMinistry(1)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(1), ApplicationProgressLegCodes.AtMinistry(1),
            ApplicationProgressLegCodes.ReviewStarted(2), ApplicationProgressLegCodes.AtMinistry(2)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(2), ApplicationProgressLegCodes.AtMinistry(2),
            ApplicationProgressLegCodes.ReviewStarted(3), ApplicationProgressLegCodes.AtMinistry(3)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(3), ApplicationProgressLegCodes.AtMinistry(3),
            ApplicationProgressStateCodes.ProcessStarted, ApplicationProgressLocationCodes.AtMigrationService));
    }

    private static bool IsAllowed(
        Application app,
        string fromState,
        string fromLocation,
        string toState,
        string toLocation)
    {
        app.ProgressHistory = new ObservableCollection<ApplicationProgress>
        {
            new()
            {
                Application = app,
                State = new ApplicationState { Code = fromState },
                Location = new ApplicationLocation { Code = fromLocation },
                Date = DateTime.Today.AddDays(-1)
            }
        };

        var current = new ApplicationProgress
        {
            Application = app,
            State = new ApplicationState { Code = toState },
            Location = new ApplicationLocation { Code = toLocation },
            Date = DateTime.Today
        };

        return ApplicationProgressTransitionHelper.TryValidateProgressStep(current, null, out _);
    }

    private static Application BuildThreeLegApplication()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var contract = new ProjectContract
        {
            MinistryLegs =
            [
                new ProjectContractMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() },
                new ProjectContractMinistryLeg { Sequence = 2, ApprovingMinistry = new ApprovingMinistry() },
                new ProjectContractMinistryLeg { Sequence = 3, ApprovingMinistry = new ApprovingMinistry() }
            ]
        };

        return new Application
        {
            ApplicationType = type,
            ProjectContract = contract,
            ProgressHistory = new ObservableCollection<ApplicationProgress>()
        };
    }
}
