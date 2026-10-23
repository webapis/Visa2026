using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Invitation Process split into short vs long visa-period tabs must keep distinct
/// sheet names, ListViews, Excel twin key, and IsShortVisaPeriod list criteria.
/// </summary>
public sealed class InvitationProcessShortLongCatalogTests
{
    [Theory]
    [InlineData("in-process-long", true)]
    [InlineData("IN-PROCESS-LONG", true)]
    [InlineData("in-process", false)]
    [InlineData("in-process-by-period-category-type", false)]
    [InlineData(null, false)]
    public void IsInvitationProcessLongSubReport_MatchesLongKeyOnly(string? key, bool expected)
    {
        Assert.Equal(expected, ReportDashboardCatalog.IsInvitationProcessLongSubReport(key));
        Assert.Equal(
            expected || key is "in-process" or "in-process-by-period-category-type",
            ReportDashboardCatalog.IsInvitationProcessSubReport(key));
    }

    [Fact]
    public void InvitationProcessSheetName_UsesShortAndLongCaptions()
    {
        Assert.Equal("işlemde olanlar kısa dönem", ReportDashboardCatalog.InvitationProcessSheetName("in-process"));
        Assert.Equal(
            "işlemde olanlar uzun dönem",
            ReportDashboardCatalog.InvitationProcessSheetName(ReportDashboardCatalog.InvitationInProcessLongKey));
        Assert.Null(ReportDashboardCatalog.InvitationProcessSheetName("ready-by-project"));
    }

    [Fact]
    public void SubReports_IncludeBothShortAndLongProcessTabs()
    {
        var keys = ReportDashboardCatalog.SubReports(ReportDashboardCategory.Invitation)
            .Select(s => s.Key)
            .ToList();

        Assert.Contains("in-process", keys);
        Assert.Contains(ReportDashboardCatalog.InvitationInProcessLongKey, keys);
    }

    [Fact]
    public void ResolveListViewTarget_LongProcess_UsesDedicatedListViewId()
    {
        var shortTarget = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.Invitation, "in-process");
        var longTarget = ReportDashboardCatalog.ResolveListViewTarget(
            ReportDashboardCategory.Invitation, ReportDashboardCatalog.InvitationInProcessLongKey);

        Assert.Equal("VwRdInvitationInProcess_ListView", shortTarget.ListViewId);
        Assert.Equal(typeof(VwRdInvitationInProcess), shortTarget.ListViewType);
        Assert.Equal("VwRdInvitationInProcessLong_ListView", longTarget.ListViewId);
        Assert.Equal(typeof(VwRdInvitationInProcess), longTarget.ListViewType);
    }

    [Fact]
    public void BuildListCriteria_ShortProcess_RequiresIsShortVisaPeriodTrue()
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.Invitation,
            projectKey: null,
            statusLabel: null,
            subReport: "in-process");

        Assert.Contains("[IsShortVisaPeriod] = True", criteria, StringComparison.Ordinal);
        Assert.DoesNotContain("[IsShortVisaPeriod] = False", criteria, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildListCriteria_LongProcess_RequiresIsShortVisaPeriodFalse()
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.Invitation,
            projectKey: null,
            statusLabel: null,
            subReport: ReportDashboardCatalog.InvitationInProcessLongKey);

        Assert.Contains("[IsShortVisaPeriod] = False", criteria, StringComparison.Ordinal);
        Assert.DoesNotContain("[IsShortVisaPeriod] = True", criteria, StringComparison.Ordinal);
    }

    [Fact]
    public void ExcelTemplate_CanonicalKey_MapsLongProcessToSharedInProcessTwin()
    {
        Assert.Equal(
            "in-process",
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.Invitation,
                ReportDashboardCatalog.InvitationInProcessLongKey));
        Assert.True(ReportDashboardExcelTemplateService.Supports(
            ReportDashboardCategory.Invitation,
            ReportDashboardCatalog.InvitationInProcessLongKey));
    }
}
