#nullable enable

using System;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Maglumat / Sanaw SQL must keep one-person bundle encoding and issued additional-location gates.
/// </summary>
public sealed class ReportDashboardWorkPermitMaglumatViewSqlTests
{
    [Fact]
    public void ActiveView_builds_bundle_with_separators_and_issued_change_workpermit_apps()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_work_permit_active.postgres.sql");

        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains("CREATE VIEW vw_rd_work_permit_active", sql, StringComparison.Ordinal);
        Assert.Contains("\"WorkPermitBundle\"", sql, StringComparison.Ordinal);
        Assert.Contains("E'\\x1e'", sql, StringComparison.Ordinal);
        Assert.Contains("chr(29)", sql, StringComparison.Ordinal);
        Assert.Contains("chr(31)", sql, StringComparison.Ordinal);
        Assert.Contains("change_workpermit", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PROCESS_ISSUED", sql, StringComparison.Ordinal);
        Assert.Contains("ActionFamily\" = 1", sql, StringComparison.Ordinal);
        Assert.Contains("(v.\"ExpirationDate\")::date >= CURRENT_DATE", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE VIEW vw_rd_work_permit_active_locations", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE VIEW vw_rd_work_permit_a_maglumat_3", sql, StringComparison.Ordinal);
        Assert.Contains("DROP VIEW IF EXISTS vw_rd_work_permit_sanaw", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationItems", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SanawView_reads_active_people_and_exposes_family_columns()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_work_permit_sanaw.postgres.sql");

        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains("CREATE VIEW vw_rd_work_permit_sanaw", sql, StringComparison.Ordinal);
        Assert.Contains("FROM vw_rd_work_permit_active a", sql, StringComparison.Ordinal);
        Assert.Contains("\"FamilyMembersLabel\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"FamilyMemberCount\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"ForeignAddressLabel\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"LastName\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"FirstName\"", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationItems", sql, StringComparison.Ordinal);
    }
}
