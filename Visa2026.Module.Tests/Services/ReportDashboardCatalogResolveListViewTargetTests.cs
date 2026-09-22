using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Open ListView must open the dedicated <c>VwRd*</c> view for Incomplete / Person search /
/// ministry and visa-extension sub-reports — wrong id/type yields an empty or wrong BO list.
/// </summary>
public sealed class ReportDashboardCatalogResolveListViewTargetTests
{
    [Fact]
    public void IncompletePersons_UsesMissingAreaView()
    {
        var (id, type) = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.IncompletePersons);

        Assert.Equal("VwRdIncompletePersonsByMissingArea_ListView", id);
        Assert.Equal(typeof(VwRdIncompletePersonsByMissingArea), type);
    }

    [Fact]
    public void PersonSearch_UsesPersonSearchView()
    {
        var (id, type) = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.PersonSearch);

        Assert.Equal("VwRdPersonSearch_ListView", id);
        Assert.Equal(typeof(VwRdPersonSearch), type);
    }

    [Theory]
    [InlineData(
        ReportDashboardCatalog.AppViaMinistryInvitationOnProcessKey,
        "VwRdApplicationViaMinistryInvitationOnProcess_ListView",
        typeof(VwRdApplicationViaMinistryInvitationOnProcess))]
    [InlineData(
        ReportDashboardCatalog.AppViaMinistryInvitationOnProcessVKey,
        "VwRdApplicationViaMinistryInvitationOnProcessByPeriodCategoryType_ListView",
        typeof(VwRdApplicationViaMinistryInvitationOnProcessByPeriodCategoryType))]
    [InlineData(
        ReportDashboardCatalog.AppViaMinistryInvitationCompletedKey,
        "VwRdApplicationViaMinistryInvitationCompleted_ListView",
        typeof(VwRdApplicationViaMinistryInvitationCompleted))]
    [InlineData(
        ReportDashboardCatalog.AppViaMinistryInvitationCompletedVKey,
        "VwRdApplicationViaMinistryInvitationCompletedByPeriodCategoryType_ListView",
        typeof(VwRdApplicationViaMinistryInvitationCompletedByPeriodCategoryType))]
    [InlineData(
        ReportDashboardCatalog.AppViaMinistryVisaExtOnProcessKey,
        "VwRdApplicationViaMinistryVisaExtensionOnProcess_ListView",
        typeof(VwRdApplicationViaMinistryVisaExtensionOnProcess))]
    [InlineData(
        ReportDashboardCatalog.AppViaMinistryVisaExtOnProcessVKey,
        "VwRdApplicationViaMinistryVisaExtensionOnProcessByPeriodCategoryType_ListView",
        typeof(VwRdApplicationViaMinistryVisaExtensionOnProcessByPeriodCategoryType))]
    [InlineData(
        ReportDashboardCatalog.AppViaMinistryVisaExtCompletedKey,
        "VwRdApplicationViaMinistryVisaExtensionCompleted_ListView",
        typeof(VwRdApplicationViaMinistryVisaExtensionCompleted))]
    [InlineData(
        ReportDashboardCatalog.AppViaMinistryVisaExtCompletedVKey,
        "VwRdApplicationViaMinistryVisaExtensionCompletedByPeriodCategoryType_ListView",
        typeof(VwRdApplicationViaMinistryVisaExtensionCompletedByPeriodCategoryType))]
    [InlineData(
        ReportDashboardCatalog.AppViaMinistryOtherOnProcessKey,
        "VwRdApplicationViaMinistryOtherOnProcess_ListView",
        typeof(VwRdApplicationViaMinistryOtherOnProcess))]
    [InlineData(
        ReportDashboardCatalog.AppViaMinistryOtherCompletedKey,
        "VwRdApplicationViaMinistryOtherCompleted_ListView",
        typeof(VwRdApplicationViaMinistryOtherCompleted))]
    public void ApplicationViaMinistry_MapsDedicatedRdViews(
        string subReport, string expectedId, Type expectedType)
    {
        var (id, type) = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.ApplicationViaMinistry, subReport);

        Assert.Equal(expectedId, id);
        Assert.Equal(expectedType, type);
    }

    [Theory]
    [InlineData(
        ReportDashboardCatalog.AppDirectOnProcessAKey,
        "VwRdApplicationDirectMigrationOnProcessA_ListView",
        typeof(VwRdApplicationDirectMigrationOnProcessA))]
    [InlineData(
        ReportDashboardCatalog.AppDirectProcessCompleteKey,
        "VwRdApplicationDirectMigrationProcessComplete_ListView",
        typeof(VwRdApplicationDirectMigrationProcessComplete))]
    public void ApplicationDirectMigration_MapsDedicatedRdViews(
        string subReport, string expectedId, Type expectedType)
    {
        var (id, type) = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.ApplicationDirectMigration, subReport);

        Assert.Equal(expectedId, id);
        Assert.Equal(expectedType, type);
    }

    [Theory]
    [InlineData("active-by-project", "VwRdVisaActiveByProject_ListView", typeof(VwRdVisaActiveByProject))]
    [InlineData("by-period-category-type", "VwRdVisaActiveByPeriodCategoryType_ListView", typeof(VwRdVisaActiveByPeriodCategoryType))]
    [InlineData("by-category", "VwRdVisaActiveByPeriodCategoryType_ListView", typeof(VwRdVisaActiveByPeriodCategoryType))]
    [InlineData("extension-required", "VwRdVisaExtensionRequired_ListView", typeof(VwRdVisaExtensionRequired))]
    [InlineData("by-days-remaining", "VwRdVisaByDaysRemaining_ListView", typeof(VwRdVisaByDaysRemaining))]
    [InlineData("on-extension", "VwRdVisaOnExtension_ListView", typeof(VwRdVisaOnExtension))]
    [InlineData("app-progress", "VwRdVisaOnExtension_ListView", typeof(VwRdVisaOnExtension))]
    [InlineData("extension-result", "VwRdVisaExtensionResult_ListView", typeof(VwRdVisaExtensionResult))]
    public void VisaExtension_MapsDedicatedRdViews(
        string subReport, string expectedId, Type expectedType)
    {
        var (id, type) = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.VisaExtension, subReport);

        Assert.Equal(expectedId, id);
        Assert.Equal(expectedType, type);
    }

    [Fact]
    public void UnknownViaMinistrySubReport_FallsBackToCategoryListView()
    {
        var (id, type) = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.ApplicationViaMinistry, "not-a-rd-key");

        Assert.Equal(ApplicationProgressRouteNavigation.ListViewViaMinistries, id);
        Assert.Equal(typeof(Application), type);
    }

    [Fact]
    public void InvitationCategory_UsesInvitationItemListView()
    {
        var (id, type) = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.Invitation);

        Assert.Equal("InvitationItem_ListView", id);
        Assert.Equal(typeof(InvitationItem), type);
    }
}
