using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationLatestProgressSyncTests
{
    [Fact]
    public void Apply_PersistsLatestDisplayAndTerminalFlags()
    {
        var application = new Application();
        var latest = new ApplicationProgress
        {
            ID = Guid.NewGuid(),
            Date = new DateTime(2024, 6, 2),
            Order = 2,
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessRejected },
        };

        ApplicationLatestProgressSyncHelper.Apply(application, latest);

        Assert.Equal(latest.ID, application.LatestProgressId);
        Assert.Equal(ApplicationProgressStateCodes.ProcessRejected, application.LatestPrimaryStateCode);
        Assert.True(application.LatestIsRejected);
        Assert.False(application.LatestIsCancelled);
        Assert.False(string.IsNullOrWhiteSpace(application.LatestProgressDisplay));
    }

    [Fact]
    public void ResolveLatestForDisplay_UsesLatestProgressNavigation()
    {
        var latest = new ApplicationProgress
        {
            ID = Guid.NewGuid(),
            Order = 2,
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessStarted },
        };
        var application = new Application
        {
            LatestProgressId = latest.ID,
            LatestProgress = latest,
            ProgressHistory =
            [
                new ApplicationProgress { ID = Guid.NewGuid(), Order = 1 },
                latest,
            ],
        };

        var resolved = ApplicationLatestProgressSyncHelper.ResolveLatestForDisplay(application);

        Assert.Same(latest, resolved);
    }
}
