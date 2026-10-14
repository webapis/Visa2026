using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public sealed class ReportDashboardExcelTemplateSchemaSqlTests
{
    [Fact]
    public void EnsureTablePostgres_CreatesUniqueCategorySubReportKeyAndFileFk()
    {
        var sql = ReportDashboardExcelTemplateSchemaSql.EnsureTablePostgres;

        Assert.Contains("ReportDashboardExcelTemplates", sql, StringComparison.Ordinal);
        Assert.Contains("to_regclass('public.\"ReportDashboardExcelTemplates\"')", sql, StringComparison.Ordinal);
        Assert.Contains("\"Category\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"SubReportKey\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"TemplateFileID\"", sql, StringComparison.Ordinal);
        Assert.Contains(
            "IX_ReportDashboardExcelTemplates_Category_SubReportKey",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "FK_ReportDashboardExcelTemplates_FileData_TemplateFileID",
            sql,
            StringComparison.Ordinal);
        Assert.Contains("REFERENCES \"FileData\"(\"ID\")", sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ApplyIfMissing_BlankConnection_IsNoOp(string? connectionString)
    {
        ReportDashboardExcelTemplateSchemaSql.ApplyIfMissing(connectionString!);
    }
}
