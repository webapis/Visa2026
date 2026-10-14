using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Hidden (V) invitation twins must share one company workbook with the visible (P) tab.
/// </summary>
public sealed class ReportDashboardExcelTemplateCanonicalKeyTests
{
    [Theory]
    [InlineData("ready-by-project", "ready-by-project")]
    [InlineData("ready-by-period-category", "ready-by-project")]
    [InlineData("in-process", "in-process")]
    [InlineData("in-process-by-period-category-type", "in-process")]
    [InlineData("process-result", "process-result")]
    [InlineData("process-result-by-period-category-type", "process-result")]
    [InlineData("rejected-by-project", "process-result")]
    [InlineData("rejected-by-period-category-type", "process-result")]
    [InlineData("used", "used")]
    [InlineData("used-by-period-category-type", "used")]
    [InlineData("valid-until", "valid-until")]
    [InlineData("expired", "valid-until")]
    public void CanonicalKey_MapsTwinSubReportsOntoVisibleWorkbook(string subReport, string expected)
    {
        Assert.Equal(expected, ReportDashboardExcelTemplateService.CanonicalKey(subReport));
        Assert.True(ReportDashboardExcelTemplateService.Supports(subReport));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("by-project")]
    [InlineData("check-in-by-city")]
    [InlineData("person-search")]
    public void CanonicalKey_RejectsUnsupportedSubReports(string? subReport)
    {
        Assert.Null(ReportDashboardExcelTemplateService.CanonicalKey(subReport));
        Assert.False(ReportDashboardExcelTemplateService.Supports(subReport));
    }
}
