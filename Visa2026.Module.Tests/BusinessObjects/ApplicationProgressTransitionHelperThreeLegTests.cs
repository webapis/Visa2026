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

        Assert.True(IsAllowedFirst(app, ApplicationProgressLegCodes.ReviewStarted(1)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewStarted(1),
            ApplicationProgressLegCodes.ReviewApproved(1)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(1),
            ApplicationProgressLegCodes.ReviewApproved(2)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(2),
            ApplicationProgressLegCodes.ReviewApproved(3)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(3),
            ApplicationProgressStateCodes.ProcessStarted));
    }

    [Fact]
    public void ThreeLegProfile_AllowsRejectionFromPriorStep()
    {
        var app = BuildThreeLegApplication();

        Assert.True(IsAllowedFirst(app, ApplicationProgressLegCodes.ReviewRejected(1)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewStarted(1),
            ApplicationProgressLegCodes.ReviewRejected(1)));

        Assert.True(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(1),
            ApplicationProgressLegCodes.ReviewRejected(2)));
    }

    [Fact]
    public void ThreeLegProfile_BlocksPrepAsFirstStep()
    {
        var app = BuildThreeLegApplication();
        Assert.False(IsAllowedFirst(app, ApplicationProgressStateCodes.IsBeingPrepared));
    }

    [Fact]
    public void ThreeLegProfile_BlocksLaterReviewStartedTransitions()
    {
        var app = BuildThreeLegApplication();

        Assert.False(IsAllowed(app,
            ApplicationProgressLegCodes.ReviewApproved(1),
            ApplicationProgressLegCodes.ReviewStarted(2)));
    }

    private static bool IsAllowedFirst(Application app, string toState)
    {
        app.ProgressHistory = new ObservableCollection<ApplicationProgress>();
        var current = new ApplicationProgress
        {
            Application = app,
            State = new ApplicationState { Code = toState },
            Date = DateTime.Today
        };
        return ApplicationProgressTransitionHelper.TryValidateProgressStep(current, null, out _);
    }

    private static bool IsAllowed(Application app, string fromState, string toState)
    {
        app.ProgressHistory = new ObservableCollection<ApplicationProgress>
        {
            new()
            {
                Application = app,
                State = new ApplicationState { Code = fromState },
                Date = DateTime.Today.AddDays(-1)
            }
        };

        var current = new ApplicationProgress
        {
            Application = app,
            State = new ApplicationState { Code = toState },
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
