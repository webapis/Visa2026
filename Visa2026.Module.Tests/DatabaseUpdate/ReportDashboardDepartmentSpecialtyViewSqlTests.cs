#nullable enable

using System;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Invitation / Maglumat / Sanaw SQL must keep department, specialty, and family-member labels.
/// </summary>
public sealed class ReportDashboardDepartmentSpecialtyViewSqlTests
{
    [Theory]
    [InlineData("vw_rd_invitation_ready.postgres.sql", "vw_rd_invitation_ready")]
    [InlineData("vw_rd_invitation_expired.postgres.sql", "vw_rd_invitation_expired")]
    [InlineData("vw_rd_invitation_cancelled.postgres.sql", "vw_rd_invitation_cancelled")]
    [InlineData("vw_rd_invitation_used.postgres.sql", "vw_rd_invitation_used")]
    [InlineData("vw_rd_invitation_in_process.postgres.sql", "vw_rd_invitation_in_process")]
    public void Invitation_views_expose_department_from_current_position_history(
        string leaf,
        string viewName)
    {
        var sql = ReportDashboardSqlViewResource.Load(leaf);

        Assert.Contains("CREATE VIEW " + viewName, sql, StringComparison.Ordinal);
        Assert.Contains("\"DepartmentLabel\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"Departments\" dep", sql, StringComparison.Ordinal);
        Assert.Contains("dept_eph.\"DepartmentID\"", sql, StringComparison.Ordinal);
        Assert.Contains("SponsoringEmployeeID", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationItems", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Active_work_permit_view_exposes_specialty_from_latest_education()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_work_permit_active.postgres.sql");

        Assert.Contains("\"SpecialtyLabel\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"Specialties\"", sql, StringComparison.Ordinal);
        Assert.Contains("GraduationYear", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE VIEW vw_rd_work_permit_active_locations", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE VIEW vw_rd_work_permit_a_maglumat_3", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanaw_view_builds_numbered_family_member_names_for_sponsored_dependents()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_work_permit_sanaw.postgres.sql");

        Assert.Contains("\"SpecialtyLabel\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"FamilyMembersLabel\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"FamilyMemberCount\"", sql, StringComparison.Ordinal);
        Assert.Contains("SponsoringEmployeeID", sql, StringComparison.Ordinal);
        Assert.Contains("PersonRole\" = 1", sql, StringComparison.Ordinal);
        Assert.Contains("RelationName\" || '-' || member.\"FullName\"", sql, StringComparison.Ordinal);
        Assert.Contains("string_agg", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM vw_rd_work_permit_active a", sql, StringComparison.Ordinal);
    }
}
