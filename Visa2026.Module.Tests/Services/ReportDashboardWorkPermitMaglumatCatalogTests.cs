#nullable enable

using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Maglumat / Sanaw tabs must keep distinct ListViews, headers, and Excel template keys.
/// </summary>
public sealed class ReportDashboardWorkPermitMaglumatCatalogTests
{
    [Theory]
    [InlineData("active-by-project", true)]
    [InlineData("a-maglumat-2", true)]
    [InlineData("a-maglumat-3", true)]
    [InlineData("sanaw", false)]
    [InlineData("by-status", false)]
    [InlineData(null, false)]
    public void IsWorkPermitMaglumatSubReport_covers_three_maglumat_tabs_only(
        string? subReport,
        bool expected)
    {
        Assert.Equal(expected, ReportDashboardCatalog.IsWorkPermitMaglumatSubReport(subReport));
    }

    [Theory]
    [InlineData("active-by-project", true)]
    [InlineData("a-maglumat-2", true)]
    [InlineData("a-maglumat-3", true)]
    [InlineData("sanaw", true)]
    [InlineData("by-days-remaining", true)]
    [InlineData("by-validity", true)]
    [InlineData("by-status", false)]
    public void SubReportCountsValidWorkPermits_includes_maglumat_and_sanaw(
        string subReport,
        bool expected)
    {
        Assert.Equal(expected, ReportDashboardCatalog.SubReportCountsValidWorkPermits(subReport));
    }

    [Fact]
    public void ResolveListViewTarget_maps_each_work_permit_tab_to_its_view_type()
    {
        Assert.Equal(
            ("VwRdWorkPermitActive_ListView", typeof(VwRdWorkPermitActive)),
            ReportDashboardCatalog.ResolveListViewTarget(
                ReportDashboardCategory.WorkPermit, "active-by-project"));
        Assert.Equal(
            ("VwRdWorkPermitActiveLocations_ListView", typeof(VwRdWorkPermitActiveLocations)),
            ReportDashboardCatalog.ResolveListViewTarget(
                ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat2Key));
        Assert.Equal(
            ("VwRdWorkPermitMaglumat3_ListView", typeof(VwRdWorkPermitMaglumat3)),
            ReportDashboardCatalog.ResolveListViewTarget(
                ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat3Key));
        Assert.Equal(
            ("VwRdWorkPermitSanaw_ListView", typeof(VwRdWorkPermitSanaw)),
            ReportDashboardCatalog.ResolveListViewTarget(
                ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitSanawKey));
    }

    [Fact]
    public void EnglishTableHeaders_maglumat2_adds_locations_and_maglumat3_adds_goşmaça()
    {
        var maglumat = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.WorkPermit, "active-by-project");
        var maglumat2 = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat2Key);
        var maglumat3 = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat3Key);
        var sanaw = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitSanawKey);

        Assert.Equal(10, maglumat.Length);
        Assert.DoesNotContain("Rugsat berlen ýerler", maglumat);
        Assert.Equal(11, maglumat2.Length);
        Assert.Contains("Rugsat berlen ýerler", maglumat2);
        Assert.DoesNotContain("Goşmaça barjak ýerleri", maglumat2);
        Assert.Equal(12, maglumat3.Length);
        Assert.Contains("Rugsat berlen ýerler", maglumat3);
        Assert.Contains("Goşmaça barjak ýerleri", maglumat3);
        Assert.Equal(16, sanaw.Length);
        Assert.Contains("Familiýasy", sanaw);
        Assert.Contains("Maşgala agzalary", sanaw);
    }

    [Fact]
    public void CanonicalKey_supports_maglumat_and_sanaw_company_templates()
    {
        Assert.Equal(
            "active-by-project",
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.WorkPermit, "active-by-project"));
        Assert.Equal(
            ReportDashboardCatalog.WorkPermitMaglumat2Key,
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat2Key));
        Assert.Equal(
            ReportDashboardCatalog.WorkPermitMaglumat3Key,
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat3Key));
        Assert.Equal(
            ReportDashboardCatalog.WorkPermitSanawKey,
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitSanawKey));
        Assert.Null(
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.WorkPermit, "by-status"));
        Assert.True(
            ReportDashboardExcelTemplateService.Supports(
                ReportDashboardCategory.WorkPermit, "a-maglumat-3"));
        Assert.False(
            ReportDashboardExcelTemplateService.Supports(
                ReportDashboardCategory.Invitation, "sanaw"));
    }

    [Fact]
    public void WorkPermit_category_publishes_maglumat_and_sanaw_subreport_keys()
    {
        var keys = ReportDashboardCatalog.SubReports(ReportDashboardCategory.WorkPermit)
            .Select(s => s.Key)
            .ToList();
        Assert.Contains("active-by-project", keys);
        Assert.Contains(ReportDashboardCatalog.WorkPermitMaglumat2Key, keys);
        Assert.Contains(ReportDashboardCatalog.WorkPermitMaglumat3Key, keys);
        Assert.Contains(ReportDashboardCatalog.WorkPermitSanawKey, keys);
    }
}
