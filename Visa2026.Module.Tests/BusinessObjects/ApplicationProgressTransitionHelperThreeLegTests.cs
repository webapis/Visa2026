using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressTransitionHelperThreeLegTests
{
    [Fact]
    public void ThreeLegProfile_AllowsFullMinistryChain()
    {
        var app = BuildThreeLegApplication();

        Assert.True(IsAllowed(app,
            ApplicationProgressStateCodes.IsBeingPrepared, ApplicationProgressLocationCodes.AtOffice,
            ApplicationProgressLegCodes.ReviewApproved(1), ApplicationProgressLegCodes.AtMinistry(1)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(1), ApplicationProgressLegCodes.AtMinistry(1),
            ApplicationProgressLegCodes.ReviewApproved(2), ApplicationProgressLegCodes.AtMinistry(2)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(2), ApplicationProgressLegCodes.AtMinistry(2),
            ApplicationProgressLegCodes.ReviewApproved(3), ApplicationProgressLegCodes.AtMinistry(3)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(3), ApplicationProgressLegCodes.AtMinistry(3),
            ApplicationProgressStateCodes.ProcessStarted, ApplicationProgressLocationCodes.AtMigrationService));
    }

    [Fact]
    public void ThreeLegProfile_AllowsRejectionFromPriorApprovedStep()
    {
        var app = BuildThreeLegApplication();

        Assert.True(IsAllowed(app,
            ApplicationProgressStateCodes.IsBeingPrepared, ApplicationProgressLocationCodes.AtOffice,
            ApplicationProgressLegCodes.ReviewRejected(1), ApplicationProgressLegCodes.AtMinistry(1)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(1), ApplicationProgressLegCodes.AtMinistry(1),
            ApplicationProgressLegCodes.ReviewRejected(2), ApplicationProgressLegCodes.AtMinistry(2)));
    }

    [Fact]
    public void ThreeLegProfile_BlocksReviewStartedTransitions()
    {
        var app = BuildThreeLegApplication();

        Assert.False(IsAllowed(app,
            ApplicationProgressStateCodes.IsBeingPrepared, ApplicationProgressLocationCodes.AtOffice,
            ApplicationProgressLegCodes.ReviewStarted(1), ApplicationProgressLegCodes.AtMinistry(1)));
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
            ShowApprovalLegProfile = true,
            MigrationSlaProfile = new ApplicationMigrationSlaProfile { MaxDaysInReview = 10 }
        };
        var profile = new ApprovalLegProfile
        {
            MinistryLegs =
            [
                new ApprovalLegProfileMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() },
                new ApprovalLegProfileMinistryLeg { Sequence = 2, ApprovingMinistry = new ApprovingMinistry() },
                new ApprovalLegProfileMinistryLeg { Sequence = 3, ApprovingMinistry = new ApprovingMinistry() }
            ]
        };

        return new Application
        {
            ApplicationType = type,
            ApprovalLegProfile = profile,
            ProgressHistory = new ObservableCollection<ApplicationProgress>()
        };
    }
}
