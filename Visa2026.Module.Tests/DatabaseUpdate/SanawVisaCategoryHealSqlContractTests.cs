using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Sanaw heal must recreate the view when VisaCategoryLabel is missing from the definition.
/// </summary>
public class SanawVisaCategoryHealSqlContractTests
{
    [Fact]
    public void SanawViewSql_ProjectsVisaCategoryLabelAndFamilyAggregation()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_work_permit_sanaw.postgres.sql");

        Assert.Contains("VisaCategoryLabel", sql, StringComparison.Ordinal);
        Assert.Contains("VisaStartDate", sql, StringComparison.Ordinal);
        Assert.Contains("VisaEndDate", sql, StringComparison.Ordinal);
        Assert.Contains("VisaNumber", sql, StringComparison.Ordinal);
        Assert.Contains("fm.\"FirstName\"", sql, StringComparison.Ordinal);
        Assert.Contains("eph.\"PositionID\"", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("SalaryStart", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("SalaryAmount", sql, StringComparison.Ordinal);
    }
}
