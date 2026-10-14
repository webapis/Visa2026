using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Active Invitation must stay limited to invitations linked to an application instance.
/// </summary>
public sealed class ReportDashboardInvitationReadyViewSqlTests
{
    [Fact]
    public void ReadyView_RequiresApplicationProfileInstanceLink()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_invitation_ready.postgres.sql");

        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains(
            "INNER JOIN \"ApplicationProfileInstances\" a",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "inv.\"ApplicationProfileInstanceID\" IS NOT NULL",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "a.\"ID\" = inv.\"ApplicationProfileInstanceID\"",
            sql,
            StringComparison.Ordinal);
    }
}
