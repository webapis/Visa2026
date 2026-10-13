using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Contract guards for retiring ApplicationMigrationSlaProfiles after SLA days moved to ApplicationProfiles.
/// </summary>
public class ApplicationMigrationSlaProfileDropSchemaSqlTests
{
    [Fact]
    public void DropPostgres_CopiesSlaDays_ThenDropsFkColumnAndTable()
    {
        var sql = ApplicationMigrationSlaProfileDropSchemaSql.DropPostgres;

        Assert.Contains("ApplicationMigrationSlaProfiles", sql, StringComparison.Ordinal);
        Assert.Contains("MigrationSlaDays", sql, StringComparison.Ordinal);
        Assert.Contains("MaxDaysInReview", sql, StringComparison.Ordinal);
        Assert.Contains("DROP COLUMN IF EXISTS \"MigrationSlaProfileID\"", sql, StringComparison.Ordinal);
        Assert.Contains("DROP TABLE IF EXISTS \"ApplicationMigrationSlaProfiles\" CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("to_regclass", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyIfMissing_BlankOrNull_IsNoOp()
    {
        ApplicationMigrationSlaProfileDropSchemaSql.ApplyIfMissing(null!);
        ApplicationMigrationSlaProfileDropSchemaSql.ApplyIfMissing("");
        ApplicationMigrationSlaProfileDropSchemaSql.ApplyIfMissing("   ");
    }
}
