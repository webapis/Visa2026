using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationLatestProgressSyncTests
{
    [Fact]
    public void Apply_PersistsLatestDisplayAndTerminalFlags()
    {
        var application = new ApplicationProfileInstance();
        var latest = new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            Date = new DateTime(2024, 6, 2),
            Order = 2,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessRejected },
        };

        ApplicationLatestProgressSyncHelper.Apply(application, latest);

        Assert.Equal(latest.ID, application.LatestProgressId);
        Assert.Equal(ApplicationProfileInstanceProgressStateCodes.ProcessRejected, application.LatestPrimaryStateCode);
        Assert.False(string.IsNullOrWhiteSpace(application.LatestProgressDisplay));
    }

    [Fact]
    public void ResolveLatestForDisplay_UsesLatestProgressNavigation()
    {
        var latest = new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            Order = 2,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessStarted },
        };
        var application = new ApplicationProfileInstance
        {
            LatestProgressId = latest.ID,
            LatestProgress = latest,
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress { ID = Guid.NewGuid(), Order = 1 },
                latest,
            ],
        };

        var resolved = ApplicationLatestProgressSyncHelper.ResolveLatestForDisplay(application);

        Assert.Same(latest, resolved);
    }
}
