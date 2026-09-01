using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Locks Application / ApplicationItem ListView criteria and nav ids used for
/// ViaMinistries vs DirectToMigrationService routing.
/// </summary>
public sealed class ApplicationProgressRouteNavigationTests
{
    [Fact]
    public void Criteria_IncludeExpectedRouteEnumTokens()
    {
        Assert.Contains(
            "ApplicationProgressRouteKind,ViaMinistries#",
            ApplicationProgressRouteNavigation.CriteriaViaMinistries);
        Assert.Contains(
            "ApplicationProgressRouteKind,DirectToMigrationService#",
            ApplicationProgressRouteNavigation.CriteriaDirectMigration);

        Assert.Contains(
            "ApplicationProgressRouteKind,ViaMinistries#",
            ApplicationProgressRouteNavigation.CriteriaItemsViaMinistries);
        Assert.Contains(
            "ApplicationProgressRouteKind,DirectToMigrationService#",
            ApplicationProgressRouteNavigation.CriteriaItemsDirectMigration);
    }

    [Fact]
    public void Criteria_RequireApplicationTypeNavigation()
    {
        Assert.StartsWith(
            "ApplicationType is not null",
            ApplicationProgressRouteNavigation.CriteriaViaMinistries);
        Assert.StartsWith(
            "ApplicationType is not null",
            ApplicationProgressRouteNavigation.CriteriaDirectMigration);
        Assert.StartsWith(
            "Application is not null And Application.ApplicationType is not null",
            ApplicationProgressRouteNavigation.CriteriaItemsViaMinistries);
        Assert.StartsWith(
            "Application is not null And Application.ApplicationType is not null",
            ApplicationProgressRouteNavigation.CriteriaItemsDirectMigration);
    }

    [Fact]
    public void NavAndListViewIds_AreDistinctPerRoute()
    {
        Assert.NotEqual(
            ApplicationProgressRouteNavigation.NavItemViaMinistries,
            ApplicationProgressRouteNavigation.NavItemDirectMigration);
        Assert.NotEqual(
            ApplicationProgressRouteNavigation.ListViewViaMinistries,
            ApplicationProgressRouteNavigation.ListViewDirectMigration);
        Assert.NotEqual(
            ApplicationProgressRouteNavigation.NavItemItemsViaMinistries,
            ApplicationProgressRouteNavigation.NavItemItemsDirectMigration);
        Assert.NotEqual(
            ApplicationProgressRouteNavigation.ListViewItemsViaMinistries,
            ApplicationProgressRouteNavigation.ListViewItemsDirectMigration);
    }
}
