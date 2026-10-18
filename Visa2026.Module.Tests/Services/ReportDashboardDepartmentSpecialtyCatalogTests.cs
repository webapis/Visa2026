#nullable enable

using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Officer table headers must expose department on invitations and specialty/family on Maglumat/Sanaw.
/// </summary>
public sealed class ReportDashboardDepartmentSpecialtyCatalogTests
{
    [Theory]
    [InlineData("ready-by-project")]
    [InlineData("expired-invitation")]
    [InlineData("in-process")]
    [InlineData("used")]
    public void Invitation_headers_include_department_column(string subReport)
    {
        var headers = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.Invitation, subReport);

        Assert.Contains("Bölümi", headers);
        Assert.Equal(2, System.Array.IndexOf(headers, "Bölümi"));
    }

    [Fact]
    public void Cancelled_invitation_headers_put_department_after_name()
    {
        var headers = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.Invitation, "cancelled-invitation");

        Assert.Equal(
            ["Full name", "Bölümi", "Invitation #", "Issued date", "Expires", "Period", "App #", "Date", "Migration"],
            headers);
    }

    [Fact]
    public void Maglumat_headers_insert_specialty_before_position()
    {
        var maglumat = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.WorkPermit, "active-by-project");
        var maglumat2 = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat2Key);
        var maglumat3 = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat3Key);

        Assert.Equal(11, maglumat.Length);
        Assert.Equal("Bilimine görä hünäri", maglumat[5]);
        Assert.Equal("Wezipesi", maglumat[6]);
        Assert.Equal(12, maglumat2.Length);
        Assert.Contains("Bilimine görä hünäri", maglumat2);
        Assert.Contains("Rugsat berlen ýerler", maglumat2);
        Assert.Equal(13, maglumat3.Length);
        Assert.Contains("Goşmaça barjak ýerleri", maglumat3);
    }

    [Fact]
    public void Sanaw_headers_keep_specialty_and_family_member_columns()
    {
        var sanaw = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitSanawKey);

        Assert.Contains("Bilimine görä hünäri", sanaw);
        Assert.Contains("Maşgala agzalary", sanaw);
        Assert.Contains("Adam sany", sanaw);
        Assert.Equal(16, sanaw.Length);
    }
}
