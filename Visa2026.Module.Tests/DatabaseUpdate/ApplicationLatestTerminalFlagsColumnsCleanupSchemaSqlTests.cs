using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Contract guards for dropping denormalized LatestIsCancelled / LatestIsRejected
/// (terminal state lives on progress history only).
/// </summary>
public class ApplicationLatestTerminalFlagsColumnsCleanupSchemaSqlTests
{
    [Fact]
    public void DropColumnsSqlServer_DropsLatestTerminalFlags_WithDefaultConstraints()
    {
        var sql = ApplicationLatestTerminalFlagsColumnsCleanupSchemaSql.DropColumnsSqlServer;

        Assert.Contains("ApplicationProfileInstances", sql, StringComparison.Ordinal);
        Assert.Contains("LatestIsCancelled", sql, StringComparison.Ordinal);
        Assert.Contains("LatestIsRejected", sql, StringComparison.Ordinal);
        Assert.Contains("sys.default_constraints", sql, StringComparison.Ordinal);
        Assert.Contains("DROP COLUMN LatestIsCancelled", sql, StringComparison.Ordinal);
        Assert.Contains("DROP COLUMN LatestIsRejected", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void DropColumnsPostgres_GuardsTable_ThenDropsTerminalFlagColumns()
    {
        var sql = ApplicationLatestTerminalFlagsColumnsCleanupSchemaSql.DropColumnsPostgres;

        Assert.Contains("to_regclass('public.\"ApplicationProfileInstances\"')", sql, StringComparison.Ordinal);
        Assert.Contains("DROP COLUMN IF EXISTS \"LatestIsCancelled\" CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("DROP COLUMN IF EXISTS \"LatestIsRejected\" CASCADE", sql, StringComparison.Ordinal);
    }
}
