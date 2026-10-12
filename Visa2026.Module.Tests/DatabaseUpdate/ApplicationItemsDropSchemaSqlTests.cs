using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Contract guards for Phase B hard-drop of legacy ApplicationItems
/// (host-start heal when ModuleUpdater is skipped).
/// </summary>
public class ApplicationItemsDropSchemaSqlTests
{
    [Fact]
    public void DropPostgres_GuardsMissingTable_DropsReferencingFks_ThenCascades()
    {
        var sql = ApplicationItemsDropSchemaSql.DropPostgres;

        Assert.Contains("to_regclass('public.\"ApplicationItems\"')", sql);
        Assert.Contains("con.contype = 'f'", sql);
        Assert.Contains("con.confrelid = 'public.\"ApplicationItems\"'::regclass", sql);
        Assert.Contains("ALTER TABLE %I DROP CONSTRAINT IF EXISTS %I", sql);
        Assert.Contains("DROP TABLE IF EXISTS \"ApplicationItems\" CASCADE", sql);
    }

    [Fact]
    public void ApplyIfMissing_BlankOrNull_IsNoOp()
    {
        ApplicationItemsDropSchemaSql.ApplyIfMissing(null!);
        ApplicationItemsDropSchemaSql.ApplyIfMissing("");
        ApplicationItemsDropSchemaSql.ApplyIfMissing("   ");
    }
}
