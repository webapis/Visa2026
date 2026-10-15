using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Expired / Cancelled / Used Invitation catalog routing must stay distinct from Active Invitation.
/// </summary>
public sealed class ReportDashboardInvitationLifecycleCatalogTests
{
    [Theory]
    [InlineData("expired-invitation", true)]
    [InlineData("cancelled-invitation", false)]
    [InlineData("used", false)]
    [InlineData("used-by-period-category-type", false)]
    [InlineData("ready-by-project", false)]
    [InlineData(null, false)]
    public void IsInvitationExpiredSubReport_OnlyExpiredKey(string? key, bool expected) =>
        Assert.Equal(expected, ReportDashboardCatalog.IsInvitationExpiredSubReport(key));

    [Theory]
    [InlineData("cancelled-invitation", true)]
    [InlineData("expired-invitation", false)]
    [InlineData("used", false)]
    [InlineData("ready-by-project", false)]
    [InlineData(null, false)]
    public void IsInvitationCancelledSubReport_OnlyCancelledKey(string? key, bool expected) =>
        Assert.Equal(expected, ReportDashboardCatalog.IsInvitationCancelledSubReport(key));

    [Theory]
    [InlineData("used", true)]
    [InlineData("used-by-period-category-type", true)]
    [InlineData("expired-invitation", false)]
    [InlineData("cancelled-invitation", false)]
    [InlineData("ready-by-project", false)]
    [InlineData(null, false)]
    public void IsInvitationUsedSubReport_AcceptsUsedKeys(string? key, bool expected) =>
        Assert.Equal(expected, ReportDashboardCatalog.IsInvitationUsedSubReport(key));

    [Fact]
    public void ResolveListViewTarget_MapsLifecycleTabsToDedicatedViews()
    {
        var expired = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.Invitation, "expired-invitation");
        Assert.Equal("VwRdInvitationExpired_ListView", expired.ListViewId);
        Assert.Equal(typeof(VwRdInvitationExpired), expired.ListViewType);

        var cancelled = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.Invitation, "cancelled-invitation");
        Assert.Equal("VwRdInvitationCancelled_ListView", cancelled.ListViewId);
        Assert.Equal(typeof(VwRdInvitationCancelled), cancelled.ListViewType);

        var used = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.Invitation, "used");
        Assert.Equal("VwRdInvitationUsed_ListView", used.ListViewId);
        Assert.Equal(typeof(VwRdInvitationUsed), used.ListViewType);

        var usedChart = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.Invitation, "used-by-period-category-type");
        Assert.Equal("VwRdInvitationUsed_ListView", usedChart.ListViewId);
        Assert.Equal(typeof(VwRdInvitationUsed), usedChart.ListViewType);
    }

    [Fact]
    public void EnglishTableHeaders_CancelledUsesMigrationLayoutWithoutApprovals()
    {
        var headers = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.Invitation, "cancelled-invitation");

        Assert.Equal(
            ["Full name", "Invitation #", "Issued date", "Expires", "Period", "App #", "Date", "Migration"],
            headers);
        Assert.DoesNotContain("Approval 1", headers);
        Assert.DoesNotContain("Visa #", headers);
    }

    [Fact]
    public void EnglishTableHeaders_UsedAppendsVisaColumnsAndOptionalChart()
    {
        var plain = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.Invitation, "used");
        Assert.Contains("Visa #", plain);
        Assert.Contains("Issued date", plain);
        Assert.Contains("Place", plain);
        Assert.Contains("Start date", plain);
        Assert.Contains("End date", plain);
        Assert.DoesNotContain("Period · Category · Type", plain);
        Assert.Equal(plain.Length - 5, ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.Invitation, "expired-invitation").Length);

        var chart = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.Invitation, "used-by-period-category-type");
        Assert.Equal("Period · Category · Type", chart[^1]);
        Assert.Equal(plain.Length + 1, chart.Length);
    }

    [Fact]
    public void ExcelTemplateCanonicalKey_MapsLifecycleAndUsedTwin()
    {
        Assert.Equal("expired-invitation", ReportDashboardExcelTemplateService.CanonicalKey("expired-invitation"));
        Assert.Equal("cancelled-invitation", ReportDashboardExcelTemplateService.CanonicalKey("cancelled-invitation"));
        Assert.Equal("used", ReportDashboardExcelTemplateService.CanonicalKey("used"));
        Assert.Equal("used", ReportDashboardExcelTemplateService.CanonicalKey("used-by-period-category-type"));
        Assert.True(ReportDashboardExcelTemplateService.Supports("expired-invitation"));
        Assert.True(ReportDashboardExcelTemplateService.Supports("cancelled-invitation"));
        Assert.True(ReportDashboardExcelTemplateService.Supports("used-by-period-category-type"));
        Assert.False(ReportDashboardExcelTemplateService.Supports("unknown-tab"));
    }

    [Fact]
    public void BuildListCriteria_LifecycleTabsUseArchivedGateNotPersonPath()
    {
        var expired = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.Invitation,
            projectKey: null,
            statusLabel: null,
            subReport: "expired-invitation");
        Assert.Contains("[IsArchived] = False", expired);
        Assert.DoesNotContain("Person is not null", expired);

        var cancelled = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.Employees,
            ReportDashboardCategory.Invitation,
            projectKey: null,
            statusLabel: null,
            subReport: "cancelled-invitation");
        Assert.Contains("[IsArchived] = False", cancelled);
        Assert.Contains("[PersonRoleCode]", cancelled);

        var used = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.Invitation,
            projectKey: null,
            statusLabel: null,
            subReport: "used");
        Assert.Contains("[IsArchived] = False", used);
    }
}
