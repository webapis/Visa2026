using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Expired / Cancelled / Used Invitation SQL populations must keep their distinct business gates.
/// </summary>
public sealed class ReportDashboardInvitationLifecycleViewSqlTests
{
    [Fact]
    public void ExpiredView_RequiresLinkedInstancePastExpiryAndNoIssuedVisa()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_invitation_expired.postgres.sql");

        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains("CREATE VIEW vw_rd_invitation_expired", sql, StringComparison.Ordinal);
        Assert.Contains("inv.\"ApplicationProfileInstanceID\" IS NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("(inv.\"ExpirationDate\")::date < CURRENT_DATE", sql, StringComparison.Ordinal);
        Assert.Contains("vis.\"IssuingInvitationItemID\" = ii.\"ID\"", sql, StringComparison.Ordinal);
        Assert.Contains("NOT EXISTS", sql, StringComparison.Ordinal);
        Assert.Contains("ActionFamily\" IN (1, 4)", sql, StringComparison.Ordinal);
        Assert.Contains("PROCESS_ISSUED", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationItems", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void CancelledView_RequiresCompletedCancellationFamily()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_invitation_cancelled.postgres.sql");

        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains("CREATE VIEW vw_rd_invitation_cancelled", sql, StringComparison.Ordinal);
        Assert.Contains("ApplicationProfileInstanceInvitationItems", sql, StringComparison.Ordinal);
        Assert.Contains("ActionFamily\" = 1", sql, StringComparison.Ordinal);
        Assert.Contains("PROCESS_ISSUED", sql, StringComparison.Ordinal);
        Assert.Contains("CancelApplicationOid", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("IssuingInvitationItemID", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationItems", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void UsedView_RequiresLinkedInstanceAndVisaFromInvitationItem()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_invitation_used.postgres.sql");

        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains("CREATE VIEW vw_rd_invitation_used", sql, StringComparison.Ordinal);
        Assert.Contains("INNER JOIN \"ApplicationProfileInstances\" a", sql, StringComparison.Ordinal);
        Assert.Contains("inv.\"ApplicationProfileInstanceID\" IS NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("vis.\"IssuingInvitationItemID\" = ii.\"ID\"", sql, StringComparison.Ordinal);
        Assert.Contains("issued_visa.\"ID\" IS NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("\"VisaNumber\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"VisaIssueDate\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"VisaStartDate\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"VisaEndDate\"", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationItems", sql, StringComparison.Ordinal);
    }
}
