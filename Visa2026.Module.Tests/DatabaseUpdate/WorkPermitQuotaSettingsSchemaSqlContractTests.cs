using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Host-start heal for WorkPermitQuotaSettings must create NOT NULL GCRecord columns;
/// nullable GCRecord breaks Npgsql after Save (InvalidCastException).
/// </summary>
public sealed class WorkPermitQuotaSettingsSchemaSqlContractTests
{
    [Fact]
    public void EnsureTablePostgres_CreatesSingletonTableWithNonNullLockColumns()
    {
        var sql = WorkPermitQuotaSettingsSchemaSql.EnsureTablePostgres;

        Assert.Contains("\"WorkPermitQuotaSettings\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"LocalPersonnelCount\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
        Assert.Contains("\"ForeignRatio\" integer NOT NULL DEFAULT 10", sql, StringComparison.Ordinal);
        Assert.Contains("\"LocalRatio\" integer NOT NULL DEFAULT 90", sql, StringComparison.Ordinal);
        Assert.Contains("\"GCRecord\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
        Assert.Contains("\"OptimisticLockField\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
        Assert.Contains("SET \"GCRecord\" = 0 WHERE \"GCRecord\" IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER COLUMN \"GCRecord\" SET NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER COLUMN \"OptimisticLockField\" SET NOT NULL", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyIfMissing_BlankOrNull_IsNoOp()
    {
        WorkPermitQuotaSettingsSchemaSql.ApplyIfMissing(null!);
        WorkPermitQuotaSettingsSchemaSql.ApplyIfMissing("");
        WorkPermitQuotaSettingsSchemaSql.ApplyIfMissing("   ");
    }

    [Fact]
    public void ApplyIfMissing_NonPostgres_IsNoOp()
    {
        WorkPermitQuotaSettingsSchemaSql.ApplyIfMissing(
            "Server=localhost;Database=Visa2026;Trusted_Connection=True;EFCoreProvider=SqlServer");
    }
}
