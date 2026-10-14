using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ReportDashboardNavigationItemsTests
{
    [Fact]
    public void TryParse_RootId_OpensOverview()
    {
        Assert.True(ReportDashboardNavigationItems.TryParse(
            ReportDashboardNavigationItems.RootId, out var showOverview, out var category));
        Assert.True(showOverview);
        Assert.Equal(default, category);
    }

    [Theory]
    [InlineData(ReportDashboardCategory.Invitation)]
    [InlineData(ReportDashboardCategory.Registration)]
    [InlineData(ReportDashboardCategory.PersonSearch)]
    [InlineData(ReportDashboardCategory.IncompletePersons)]
    public void TryParse_ChildId_OpensCategory(ReportDashboardCategory category)
    {
        var id = ReportDashboardNavigationItems.ChildId(category);
        Assert.True(ReportDashboardNavigationItems.TryParse(id, out var showOverview, out var parsed));
        Assert.False(showOverview);
        Assert.Equal(category, parsed);
        Assert.Equal(
            $@"Application/NavigationItems/Items/{ReportDashboardNavigationItems.RootId}/Items/{id}",
            ReportDashboardNavigationItems.PermissionPath(category));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Applications")]
    [InlineData("ReportDashboard_NotARealCategory")]
    [InlineData("ReportDashboard_999")]
    public void TryParse_UnknownId_ReturnsFalse(string? id)
    {
        Assert.False(ReportDashboardNavigationItems.TryParse(id, out var showOverview, out _));
        Assert.True(showOverview);
    }

    [Fact]
    public void EnglishCaption_CoversOfficerFacingCategories()
    {
        Assert.Equal("Invitation", ReportDashboardNavigationItems.EnglishCaption(ReportDashboardCategory.Invitation));
        Assert.Equal(
            "Persons with incomplete data",
            ReportDashboardNavigationItems.EnglishCaption(ReportDashboardCategory.IncompletePersons));
        Assert.Equal("Person search", ReportDashboardNavigationItems.EnglishCaption(ReportDashboardCategory.PersonSearch));
        Assert.Equal(
            "Application (direct migration)",
            ReportDashboardNavigationItems.EnglishCaption(ReportDashboardCategory.ApplicationDirectMigration));
    }

    [Fact]
    public void NavigationRequest_Apply_TogglesOverviewAndCategory()
    {
        var request = new ReportDashboardNavigationRequest();
        Assert.True(request.ShowOverview);

        request.Apply(showOverview: false, ReportDashboardCategory.WorkPermit);
        Assert.False(request.ShowOverview);
        Assert.Equal(ReportDashboardCategory.WorkPermit, request.Category);

        request.Apply(showOverview: true, ReportDashboardCategory.Invitation);
        Assert.True(request.ShowOverview);
    }
}
