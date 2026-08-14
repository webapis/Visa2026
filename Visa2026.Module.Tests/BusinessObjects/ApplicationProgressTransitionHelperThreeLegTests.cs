using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileInstanceProgressTransitionHelperThreeLegTests
{
    [Fact]
    public void ThreeLegProfile_AllowsFullMinistryChain()
    {
        var app = BuildThreeLegApplication();

        Assert.True(IsAllowedFirst(app, ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1)));

        Assert.True(IsAllowed(app,
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1),
            ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1)));

        Assert.True(IsAllowed(app,
            ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1),
            ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2)));

        Assert.True(IsAllowed(app,
            ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2),
            ApplicationProfileInstanceProgressLegCodes.ReviewApproved(3)));

        Assert.True(IsAllowed(app,
            ApplicationProfileInstanceProgressLegCodes.ReviewApproved(3),
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted));
    }

    [Fact]
    public void ThreeLegProfile_AllowsRejectionFromPriorStep()
    {
        var app = BuildThreeLegApplication();

        Assert.True(IsAllowedFirst(app, ApplicationProfileInstanceProgressLegCodes.ReviewRejected(1)));

        Assert.True(IsAllowed(app,
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1),
            ApplicationProfileInstanceProgressLegCodes.ReviewRejected(1)));

        Assert.True(IsAllowed(app,
            ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1),
            ApplicationProfileInstanceProgressLegCodes.ReviewRejected(2)));
    }

    [Fact]
    public void ThreeLegProfile_BlocksPrepAsFirstStep()
    {
        var app = BuildThreeLegApplication();
        Assert.False(IsAllowedFirst(app, ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared));
    }

    [Fact]
    public void ThreeLegProfile_BlocksLaterReviewStartedTransitions()
    {
        var app = BuildThreeLegApplication();

        Assert.False(IsAllowed(app,
            ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1),
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(2)));
    }

    private static bool IsAllowedFirst(ApplicationProfileInstance app, string toState)
    {
        app.ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>();
        var current = new ApplicationProfileInstanceProgress
        {
            ApplicationProfileInstance = app,
            State = new ApplicationState { Code = toState },
            Date = DateTime.Today
        };
        return ApplicationProfileInstanceProgressTransitionHelper.TryValidateProgressStep(current, null, out _);
    }

    private static bool IsAllowed(ApplicationProfileInstance app, string fromState, string toState)
    {
        app.ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>
        {
            new()
            {
                ApplicationProfileInstance = app,
                State = new ApplicationState { Code = fromState },
                Date = DateTime.Today.AddDays(-1)
            }
        };

        var current = new ApplicationProfileInstanceProgress
        {
            ApplicationProfileInstance = app,
            State = new ApplicationState { Code = toState },
            Date = DateTime.Today
        };

        return ApplicationProfileInstanceProgressTransitionHelper.TryValidateProgressStep(current, null, out _);
    }

    private static ApplicationProfileInstance BuildThreeLegApplication()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
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

        return new ApplicationProfileInstance
        {
            ApplicationType = type,
            ApprovalLegProfile = profile,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>()
        };
    }
}
