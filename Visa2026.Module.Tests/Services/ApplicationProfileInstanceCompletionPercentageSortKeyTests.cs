using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// ListView progress column sorts by the same 0-100 CompletionPercentage scale
/// (not appearance/state Compose keys). Cached preload must clamp and stay numeric.
/// </summary>
public sealed class ApplicationProfileInstanceCompletionPercentageSortKeyTests
{
    [Fact]
    public void ProgressListSortKey_Matches_CompletionPercentage()
    {
        var application = new ApplicationProfileInstance
        {
            ProcessNumber = null,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        application.SetListViewProgressSteps(new[]
        {
            new ApplicationWorkspaceListProgressSteps.Step("office", "Office", "done", "ok", "", ""),
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationMigrationSubmission.TimelineKey,
                "Submitted",
                "done",
                "ok",
                "",
                ""),
            new ApplicationWorkspaceListProgressSteps.Step(
                ApplicationWorkspaceProgressTimeline.MigrationKey,
                "Migration",
                "current",
                "",
                "",
                ""),
        });
        application.SetListViewResultCoverage(new[]
        {
            new ApplicationWorkspaceIssuedResultListCoverage.Chip(
                ApplicationWorkspaceIssuedRecordsCatalog.Invitation,
                "Invitation",
                1,
                2,
                false),
        });

        ApplicationProfileInstanceCompletionPercentage.ApplyTo(application);

        Assert.Equal(27, application.CompletionPercentage);
        Assert.Equal(application.CompletionPercentage, application.ProgressListSortKey);
        Assert.IsType<int>(application.CompletionPercentage);
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(0, 0)]
    [InlineData(55, 55)]
    [InlineData(100, 100)]
    [InlineData(250, 100)]
    public void SetListViewCompletionPercentage_Clamps_AndDrives_SortKey(int input, int expected)
    {
        var application = new ApplicationProfileInstance();

        application.SetListViewCompletionPercentage(input);

        Assert.Equal(expected, application.CompletionPercentage);
        Assert.Equal(expected, application.ProgressListSortKey);
    }

    [Fact]
    public void InvalidateListViewDisplayCache_Clears_Cached_Percentage()
    {
        var application = new ApplicationProfileInstance { ProcessNumber = "CO1" };
        application.SetListViewCompletionPercentage(80);
        Assert.Equal(80, application.ProgressListSortKey);

        application.InvalidateListViewDisplayCache();

        // No preloaded steps/chips → Calculate returns 0 once AS gate is considered passed via process number.
        Assert.Equal(0, application.CompletionPercentage);
        Assert.Equal(0, application.ProgressListSortKey);
    }
}
