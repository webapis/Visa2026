using System;
using System.Linq;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class IssuedDocumentStatusColumnsCleanupSchemaSqlTests
{
    [Fact]
    public void DropViewStatements_cover_dashboard_views_that_referenced_status_flags()
    {
        var sql = string.Join('\n', IssuedDocumentStatusColumnsCleanupSchemaSql.DropViewStatements);

        Assert.Contains("vw_rd_invitation_used", sql, StringComparison.Ordinal);
        Assert.Contains("vw_rd_invitation_ready", sql, StringComparison.Ordinal);
        Assert.Contains("vw_rd_visa_by_days_remaining", sql, StringComparison.Ordinal);
        Assert.Contains("vw_rd_work_permit_active", sql, StringComparison.Ordinal);
        Assert.Contains("View_VisaExtensionStatus", sql, StringComparison.Ordinal);
        Assert.All(
            IssuedDocumentStatusColumnsCleanupSchemaSql.DropViewStatements,
            statement => Assert.Contains("CASCADE", statement, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DropColumnStatements_guard_missing_tables_and_drop_lifecycle_flags()
    {
        var sql = Assert.Single(IssuedDocumentStatusColumnsCleanupSchemaSql.DropColumnStatements);

        Assert.Contains("to_regclass('public.\"InvitationItems\"')", sql, StringComparison.Ordinal);
        Assert.Contains("to_regclass('public.\"Visas\"')", sql, StringComparison.Ordinal);
        Assert.Contains("to_regclass('public.\"WorkPermitItems\"')", sql, StringComparison.Ordinal);
        Assert.Contains("DROP COLUMN IF EXISTS \"IsCancelled\"", sql, StringComparison.Ordinal);
        Assert.Contains("DROP COLUMN IF EXISTS \"IsChanged\"", sql, StringComparison.Ordinal);
        Assert.Contains("DROP COLUMN IF EXISTS \"IsUsed\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"BorderZoneItems\"", sql, StringComparison.Ordinal);
        Assert.Equal(12, IssuedDocumentStatusColumnsCleanupSchemaSql.DropViewStatements.Length);
        Assert.True(IssuedDocumentStatusColumnsCleanupSchemaSql.DropColumnStatements.All(s => s.Contains("DO $$", StringComparison.Ordinal)));
    }
}
