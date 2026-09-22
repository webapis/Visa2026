using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// ListView / dashboard primary state: empty history implies office; latest State.Code otherwise.
/// </summary>
public sealed class ApplicationProgressPrimaryStateCodeResolverTests
{
    [Fact]
    public void Resolve_NullApplication_ReturnsNull()
    {
        Assert.Null(ApplicationProgressPrimaryStateCodeResolver.Resolve(null));
    }

    [Fact]
    public void ResolveFromLatest_Null_ImpliesIsBeingPrepared()
    {
        var code = ApplicationProgressPrimaryStateCodeResolver.ResolveFromLatest(null);

        Assert.Equal(ApplicationProgressStateCodes.IsBeingPrepared, code);
    }

    [Fact]
    public void ResolveFromLatest_EmptyStateCode_ImpliesIsBeingPrepared()
    {
        var latest = new ApplicationProgress
        {
            State = new ApplicationState { Code = "   " }
        };

        var code = ApplicationProgressPrimaryStateCodeResolver.ResolveFromLatest(latest);

        Assert.Equal(ApplicationProgressStateCodes.IsBeingPrepared, code);
    }

    [Fact]
    public void ResolveFromLatest_UsesTrimmedStateCode()
    {
        var latest = new ApplicationProgress
        {
            State = new ApplicationState { Code = " PROCESS_ISSUED " }
        };

        var code = ApplicationProgressPrimaryStateCodeResolver.ResolveFromLatest(latest);

        Assert.Equal("PROCESS_ISSUED", code);
    }

    [Fact]
    public void Resolve_UsesLatestFromProgressHistory()
    {
        var older = new ApplicationProgress
        {
            Order = 1,
            Date = new System.DateTime(2026, 1, 1),
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessStarted }
        };
        var newer = new ApplicationProgress
        {
            Order = 2,
            Date = new System.DateTime(2026, 2, 1),
            State = new ApplicationState { Code = ApplicationProgressStateCodes.ProcessIssued }
        };
        var application = new Application
        {
            ProgressHistory = new ObservableCollection<ApplicationProgress> { older, newer }
        };

        var code = ApplicationProgressPrimaryStateCodeResolver.Resolve(application);

        Assert.Equal(ApplicationProgressStateCodes.ProcessIssued, code);
    }

    [Fact]
    public void Resolve_EmptyHistory_ImpliesIsBeingPrepared()
    {
        var application = new Application
        {
            ProgressHistory = new ObservableCollection<ApplicationProgress>()
        };

        var code = ApplicationProgressPrimaryStateCodeResolver.Resolve(application);

        Assert.Equal(ApplicationProgressStateCodes.IsBeingPrepared, code);
    }
}
