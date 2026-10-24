using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Host-start heal for shared Excel bundle order singleton — NOT NULL lock columns
/// and default empty OrderJson so Npgsql mapping does not fail after Save.
/// </summary>
public sealed class ReportDashboardExcelBundleSettingsSchemaSqlContractTests
{
    [Fact]
    public void EnsureTablePostgres_CreatesSingletonTableWithNonNullLockColumns()
    {
        var sql = ReportDashboardExcelBundleSettingsSchemaSql.EnsureTablePostgres;

        Assert.Contains("\"ReportDashboardExcelBundleSettings\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"OrderJson\" text NOT NULL DEFAULT '[]'", sql, StringComparison.Ordinal);
        Assert.Contains("\"GCRecord\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
        Assert.Contains("\"OptimisticLockField\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
        Assert.Contains("SET \"GCRecord\" = 0 WHERE \"GCRecord\" IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("SET \"OrderJson\" = '[]' WHERE \"OrderJson\" IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER COLUMN \"GCRecord\" SET NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER COLUMN \"OrderJson\" SET NOT NULL", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyIfMissing_BlankOrNull_IsNoOp()
    {
        ReportDashboardExcelBundleSettingsSchemaSql.ApplyIfMissing(null!);
        ReportDashboardExcelBundleSettingsSchemaSql.ApplyIfMissing("");
        ReportDashboardExcelBundleSettingsSchemaSql.ApplyIfMissing("   ");
    }

    [Fact]
    public void ApplyIfMissing_NonPostgres_IsNoOp()
    {
        ReportDashboardExcelBundleSettingsSchemaSql.ApplyIfMissing(
            "Server=localhost;Database=Visa2026;Trusted_Connection=True;EFCoreProvider=SqlServer");
    }
}
