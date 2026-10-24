using System.Linq;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// uzun dönem Invitation Process and uzatımlar company sheets show the last three
/// approval decisions (third-from-end → last). Wrong header/cell order corrupts the
/// company workbook officers download.
/// </summary>
public sealed class InvitationProcessLongTrailingApprovalsTests
{
    [Fact]
    public void ProcessCompanyLongHeaders_places_trailing_approvals_before_migration()
    {
        var headers = ReportDashboardInvitationTableExcel.ProcessCompanyLongHeaders;
        var third = Array.IndexOf(headers, ReportDashboardInvitationTableExcel.ApprovalThirdLastHeader);
        var second = Array.IndexOf(headers, ReportDashboardInvitationTableExcel.ApprovalSecondLastHeader);
        var last = Array.IndexOf(headers, ReportDashboardInvitationTableExcel.ApprovalLastHeader);
        var migration = Array.IndexOf(headers, "Yabancı Şubeye veriliş tarihi");

        Assert.True(third >= 0);
        Assert.Equal(third + 1, second);
        Assert.Equal(second + 1, last);
        Assert.Equal(last + 1, migration);
    }

    [Fact]
    public void EnglishTableHeaders_in_process_long_includes_trailing_approval_captions()
    {
        var headers = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.Invitation,
            ReportDashboardCatalog.InvitationInProcessLongKey);

        Assert.Contains(ReportDashboardInvitationTableExcel.ApprovalThirdLastHeader, headers);
        Assert.Contains(ReportDashboardInvitationTableExcel.ApprovalSecondLastHeader, headers);
        Assert.Contains(ReportDashboardInvitationTableExcel.ApprovalLastHeader, headers);
        var third = Array.IndexOf(headers, ReportDashboardInvitationTableExcel.ApprovalThirdLastHeader);
        Assert.Equal(
            ReportDashboardInvitationTableExcel.ApprovalSecondLastHeader,
            headers[third + 1]);
        Assert.Equal(
            ReportDashboardInvitationTableExcel.ApprovalLastHeader,
            headers[third + 2]);
    }

    [Fact]
    public void ProcessLongCells_maps_ministry_cells_then_falls_back_to_last_approval()
    {
        var withThree = new ReportDashboardPreviewRow
        {
            Name = "Ada",
            ColumnA = "Engineer",
            Department = "Ops",
            ColumnB = "Firm",
            ColumnC = "APP-1",
            ColumnD = "01.01.2026",
            ColumnH = "1 yıl",
            ColumnG = "02.02.2026",
            ColumnI = "№ CO-9",
            ColumnE = "Migration",
            MinistryCells = ["third-leg", "second-leg", "last-leg"],
            LastApproval = "should-not-win",
        };
        var cells = ReportDashboardInvitationTableExcel.ProcessLongCells(withThree, 1);
        Assert.Equal("1", cells[0]);
        Assert.Equal("Ada", cells[1]);
        Assert.Equal("third-leg", cells[8]);
        Assert.Equal("second-leg", cells[9]);
        Assert.Equal("last-leg", cells[10]);
        Assert.Equal(string.Empty, cells[^1]); // Açıklama

        var withOne = new ReportDashboardPreviewRow
        {
            Name = "Bob",
            MinistryCells = ["only"],
            LastApproval = "fallback-last",
        };
        var shortCells = ReportDashboardInvitationTableExcel.ProcessLongCells(withOne, 2);
        Assert.Equal("only", shortCells[8]);
        Assert.Equal(string.Empty, shortCells[9]);
        Assert.Equal("fallback-last", shortCells[10]);
    }

    [Fact]
    public void VisaUzatilanlarExcel_headers_include_same_trailing_approval_triplet()
    {
        var headers = ReportDashboardVisaUzatilanlarExcel.Headers;
        var third = Array.IndexOf(headers, ReportDashboardInvitationTableExcel.ApprovalThirdLastHeader);
        Assert.True(third >= 0);
        Assert.Equal(
            ReportDashboardInvitationTableExcel.ApprovalSecondLastHeader,
            headers[third + 1]);
        Assert.Equal(
            ReportDashboardInvitationTableExcel.ApprovalLastHeader,
            headers[third + 2]);
    }

    [Fact]
    public void ExcelBundleReports_includes_in_process_long_and_uzatilanlar()
    {
        var keys = ReportDashboardCatalog.ExcelBundleReports()
            .Select(x => (x.Category, x.Key))
            .ToHashSet();

        Assert.Contains(
            (ReportDashboardCategory.Invitation, ReportDashboardCatalog.InvitationInProcessLongKey),
            keys);
        Assert.Contains(
            (ReportDashboardCategory.VisaExtension, ReportDashboardCatalog.VisaUzatilanlarKey),
            keys);
        Assert.Equal(
            keys.Count,
            ReportDashboardCatalog.ExcelBundleReports().Count);
    }
}
