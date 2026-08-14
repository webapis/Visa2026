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

    [Fact]
    public void Apply_NullLatest_ClearsDenormalizedFieldsAndProcessNumber()
    {
        var application = new Application
        {
            LatestProgressId = Guid.NewGuid(),
            LatestPrimaryStateCode = ApplicationProgressStateCodes.ProcessStarted,
            LatestProgressDisplay = "started",
            ProcessNumber = "AS-OLD",
            ProgressHistory = [],
        };

        ApplicationLatestProgressSyncHelper.Apply(application, latest: null);

        Assert.Null(application.LatestProgressId);
        Assert.Null(application.LatestProgress);
        Assert.Null(application.LatestPrimaryStateCode);
        Assert.Null(application.LatestProgressDisplay);
        Assert.Null(application.ProcessNumber);
    }

    [Fact]
    public void Apply_EmptyLatestId_UpdatesScalarsWithoutLinkingNavigation()
    {
        var application = new Application();
        var latest = new ApplicationProgress
        {
            ID = Guid.Empty,
            Date = new DateTime(2024, 6, 2, 0, 0, 0),
            Order = 1,
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessStarted },
            ProcessNumber = "AS-NEW",
        };

        ApplicationLatestProgressSyncHelper.Apply(application, latest);

        Assert.Equal(ApplicationProgressStateCodes.ProcessStarted, application.LatestPrimaryStateCode);
        Assert.Equal("AS-NEW", application.ProcessNumber);
        Assert.Null(application.LatestProgressId);
        Assert.Null(application.LatestProgress);
    }

    [Fact]
    public void ResolveLatestForDisplay_NullApplication_ReturnsNull()
    {
        Assert.Null(ApplicationLatestProgressSyncHelper.ResolveLatestForDisplay(null));
    }

    [Fact]
    public void ResolveLatestForDisplay_FallsBackToHistoryOrderWhenPointerMissing()
    {
        var older = new ApplicationProgress
        {
            ID = Guid.NewGuid(),
            Order = 1,
            Date = new DateTime(2024, 1, 1, 0, 0, 0),
            State = new ApplicationState { Code = ApplicationProgressStateCodes.IsBeingPrepared },
        };
        var newer = new ApplicationProgress
        {
            ID = Guid.NewGuid(),
            Order = 2,
            Date = new DateTime(2024, 2, 1, 0, 0, 0),
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessStarted },
        };
        var application = new Application
        {
            ProgressHistory = [older, newer],
        };

        var resolved = ApplicationLatestProgressSyncHelper.ResolveLatestForDisplay(application);

        Assert.Same(newer, resolved);
    }

    [Fact]
    public void SyncProcessNumber_NullApplication_IsNoOp()
    {
        ApplicationLatestProgressSyncHelper.SyncProcessNumber(null);
    }

    [Fact]
    public void Sync_NullApplication_IsNoOp()
    {
        ApplicationLatestProgressSyncHelper.Sync(null);
    }
}
