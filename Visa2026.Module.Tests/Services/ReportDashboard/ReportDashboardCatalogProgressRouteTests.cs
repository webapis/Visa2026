using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services.ReportDashboard;

/// <summary>
/// Dashboard application categories must filter by Application Profile progress route
/// (not legacy ApplicationType), matching XAF nav ListViews.
/// </summary>
public sealed class ReportDashboardCatalogProgressRouteTests
{
    [Fact]
    public void ApplicationProfileInstanceProgressRouteFor_maps_ministry_and_direct_categories()
    {
        Assert.Equal(
            ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ReportDashboardCatalog.ApplicationProfileInstanceProgressRouteFor(
                ReportDashboardCategory.ApplicationViaMinistry));

        Assert.Equal(
            ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            ReportDashboardCatalog.ApplicationProfileInstanceProgressRouteFor(
                ReportDashboardCategory.ApplicationDirectMigration));
    }

    [Theory]
    [InlineData(ReportDashboardCategory.Registration)]
    [InlineData(ReportDashboardCategory.Invitation)]
    [InlineData(ReportDashboardCategory.WorkPermit)]
    [InlineData(ReportDashboardCategory.PersonSearch)]
    [InlineData(ReportDashboardCategory.IncompletePersons)]
    public void ApplicationProfileInstanceProgressRouteFor_non_application_categories_are_null(
        ReportDashboardCategory category)
    {
        Assert.Null(ReportDashboardCatalog.ApplicationProfileInstanceProgressRouteFor(category));
    }

    [Fact]
    public void IsApplicationCategory_matches_route_mapped_categories_only()
    {
        Assert.True(ReportDashboardCatalog.IsApplicationCategory(ReportDashboardCategory.ApplicationViaMinistry));
        Assert.True(ReportDashboardCatalog.IsApplicationCategory(ReportDashboardCategory.ApplicationDirectMigration));
        Assert.False(ReportDashboardCatalog.IsApplicationCategory(ReportDashboardCategory.Registration));
        Assert.False(ReportDashboardCatalog.IsApplicationCategory(ReportDashboardCategory.VisaExtension));
    }
}
