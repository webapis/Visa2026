using System;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileInstanceChildSkipNavSchemaSqlTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ApplyIfMissing_blank_connection_is_no_op(string? connectionString)
    {
        ApplicationProfileInstanceChildSkipNavSchemaSql.ApplyIfMissing(connectionString!);
    }

    [Fact]
    public void BuildHealSql_creates_person_child_joins_including_work_duties()
    {
        var sql = ApplicationProfileInstanceChildSkipNavSchemaSql.BuildHealSql();

        Assert.Contains("CREATE TABLE \"ApplicationProfileInstancePassports\"", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE \"ApplicationProfileInstanceWorkDuties\"", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE \"ApplicationProfileInstanceTravelHistories\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"WorkDutyId\" uuid NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE RESTRICT", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildHealSql_backfills_from_resolved_links_with_work_duty_kind()
    {
        var sql = ApplicationProfileInstanceChildSkipNavSchemaSql.BuildHealSql();

        Assert.Contains("ApplicationProfileInstancePersonResolvedLinks", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE rl.\"LinkKind\" = 12", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE rl.\"LinkKind\" = 0", sql, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT DO NOTHING", sql, StringComparison.Ordinal);
        Assert.Contains("COALESCE(rl.\"GCRecord\", 0) = 0", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildHealSql_drops_mistaken_header_skip_nav_joins()
    {
        var sql = ApplicationProfileInstanceChildSkipNavSchemaSql.BuildHealSql();

        Assert.Contains("DROP TABLE IF EXISTS \"ApplicationProfileInstanceInvitations\" CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("DROP TABLE IF EXISTS \"ApplicationProfileInstanceWorkPermits\" CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("DROP TABLE IF EXISTS \"ApplicationProfileInstanceBorderZones\" CASCADE", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildHealSql_renames_legacy_border_zone_item_table_when_needed()
    {
        var sql = ApplicationProfileInstanceChildSkipNavSchemaSql.BuildHealSql();

        Assert.Contains("to_regclass('public.\"BorderZoneItem\"')", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"BorderZoneItem\" RENAME TO \"BorderZoneItems\"", sql, StringComparison.Ordinal);
    }
}
