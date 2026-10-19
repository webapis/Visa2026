#nullable enable

using System;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Goşmaça barjak ýerleri (Maglumat 3) reads additional movement places from the active view.
/// Guard the Additional aggregation + alias so Maglumat 3 does not silently drop the column.
/// </summary>
public sealed class ReportDashboardMaglumat3AdditionalLocationsViewSqlTests
{
    [Fact]
    public void ActiveView_exposes_additional_locations_label_for_maglumat3()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_work_permit_active.postgres.sql");

        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains("\"WorkPermitAdditionalLocationsLabel\"", sql, StringComparison.Ordinal);
        Assert.Contains("all_permits.\"Additional\"", sql, StringComparison.Ordinal);
        Assert.Contains("AS \"Additional\"", sql, StringComparison.Ordinal);
        Assert.Contains("MovementPermitLocation", sql, StringComparison.Ordinal);
        Assert.Contains("chr(29)", sql, StringComparison.Ordinal);
        Assert.Contains("chr(31)", sql, StringComparison.Ordinal);
        Assert.Contains(
            "CREATE VIEW vw_rd_work_permit_a_maglumat_3 AS",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "SELECT * FROM vw_rd_work_permit_active",
            sql,
            StringComparison.Ordinal);
    }
}
