using System;
using System.Text.RegularExpressions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// A work-permit cancellation link closes the permit. LatestPrimaryStateCode / PROCESS_ISSUED
/// must not be required on that WorkPermitItemId predicate (visa / change-permit clauses may
/// still mention PROCESS_ISSUED elsewhere in the same script).
/// </summary>
public class WorkPermitCancellationLinkSqlContractTests
{
    [Theory]
    [InlineData("vw_rd_work_permit.postgres.sql")]
    [InlineData("vw_rd_work_permit_active.postgres.sql")]
    public void CancellationLinkOnWorkPermitItem_DoesNotRequireIssuedState(string leaf)
    {
        var sql = ReportDashboardSqlViewResource.Load(leaf);
        Assert.False(string.IsNullOrWhiteSpace(sql));

        var compact = Regex.Replace(sql, @"\s+", " ");
        var family = (int)ApplicationProfileActionFamily.Cancellation;
        var workPermitCancellationWindows = 0;

        foreach (Match match in Regex.Matches(
            compact,
            $"ActionFamily\"\\s*=\\s*{family}",
            RegexOptions.IgnoreCase))
        {
            var start = Math.Max(0, match.Index - 250);
            var before = compact.Substring(start, match.Index - start);
            if (before.IndexOf("WorkPermitItemId", StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            workPermitCancellationWindows++;
            var afterLength = Math.Min(160, compact.Length - match.Index);
            var after = compact.Substring(match.Index, afterLength);
            Assert.DoesNotContain("PROCESS_ISSUED", after, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("LatestPrimaryStateCode", after, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(
            workPermitCancellationWindows >= 1,
            $"{leaf}: expected ActionFamily={family} cancellation gate near WorkPermitItemId.");
    }

    [Fact]
    public void DaysRemainingView_KeepsNonExpiredPersonScopedRows()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_work_permit.postgres.sql");

        Assert.Contains("CREATE VIEW vw_rd_work_permit", sql, StringComparison.Ordinal);
        Assert.Contains("wpi.\"PersonID\" IS NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("(wpi.\"ExpirationDate\")::date >= CURRENT_DATE", sql, StringComparison.Ordinal);
        Assert.Contains(
            $"ap.\"ActionFamily\" = {(int)ApplicationProfileActionFamily.Cancellation}",
            sql,
            StringComparison.Ordinal);
    }
}
