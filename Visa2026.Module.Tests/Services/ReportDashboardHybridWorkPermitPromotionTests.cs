#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Maglumat 2/3 and Sanaw must stay on the real SQL path in the hybrid query service
/// so officers do not silently fall back to mock rows after Goşmaça / Sanaw tabs shipped.
/// </summary>
public sealed class ReportDashboardHybridWorkPermitPromotionTests
{
    [Theory]
    [InlineData("active-by-project")]
    [InlineData("a-maglumat-2")]
    [InlineData("a-maglumat-3")]
    [InlineData("sanaw")]
    public void RealSubReports_includes_work_permit_maglumat_and_sanaw_tabs(string subReport)
    {
        var promoted = ReadRealSubReports();

        Assert.Contains(
            (ReportDashboardCategory.WorkPermit, subReport),
            promoted);
    }

    [Fact]
    public void RealSubReports_uses_catalog_constants_for_maglumat3_and_sanaw()
    {
        var promoted = ReadRealSubReports();

        Assert.Contains(
            (ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat2Key),
            promoted);
        Assert.Contains(
            (ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat3Key),
            promoted);
        Assert.Contains(
            (ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitSanawKey),
            promoted);
        Assert.DoesNotContain(
            (ReportDashboardCategory.WorkPermit, "by-status"),
            promoted);
    }

    private static HashSet<(ReportDashboardCategory Category, string SubReport)> ReadRealSubReports()
    {
        var field = typeof(ReportDashboardHybridQueryService).GetField(
            "RealSubReports",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var value = field!.GetValue(null);
        Assert.NotNull(value);
        return (HashSet<(ReportDashboardCategory, string)>)value!;
    }
}
