using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Contract checks for embedded Application schema SQL so column/index renames
/// do not silently break updaters.
/// </summary>
public sealed class ApplicationSchemaSqlContractTests
{
    [Fact]
    public void LatestProgressSchema_AddsExpectedColumnsAndFk()
    {
        Assert.Contains("LatestProgressId", ApplicationLatestProgressSchemaSql.EnsureColumnsSql);
        Assert.Contains("LatestPrimaryStateCode", ApplicationLatestProgressSchemaSql.EnsureColumnsSql);
        Assert.Contains("LatestProgressDisplay", ApplicationLatestProgressSchemaSql.EnsureColumnsSql);
        Assert.Contains(
            "FK_Applications_ApplicationProgresses_LatestProgressId",
            ApplicationLatestProgressSchemaSql.EnsureColumnsSql);

        Assert.Contains("LatestProgressId", ApplicationLatestProgressSchemaSql.BackfillLatestProgressIdSql);
        Assert.Contains("ProgressOrder DESC", ApplicationLatestProgressSchemaSql.BackfillLatestProgressIdSql);
    }

    [Fact]
    public void TerminalFlagsCleanup_DropsLegacyLatestIsCancelledAndRejected()
    {
        Assert.Contains("LatestIsCancelled", ApplicationLatestTerminalFlagsColumnsCleanupSchemaSql.DropColumnsSqlServer);
        Assert.Contains("LatestIsRejected", ApplicationLatestTerminalFlagsColumnsCleanupSchemaSql.DropColumnsSqlServer);
        Assert.Contains("DROP COLUMN", ApplicationLatestTerminalFlagsColumnsCleanupSchemaSql.DropColumnsSqlServer);

        Assert.Contains("LatestIsCancelled", ApplicationLatestTerminalFlagsColumnsCleanupSchemaSql.DropColumnsPostgres);
        Assert.Contains("LatestIsRejected", ApplicationLatestTerminalFlagsColumnsCleanupSchemaSql.DropColumnsPostgres);
        Assert.Contains("DROP COLUMN IF EXISTS", ApplicationLatestTerminalFlagsColumnsCleanupSchemaSql.DropColumnsPostgres);
    }

    [Fact]
    public void ListQueryPerformance_CreatesExpectedIndexes()
    {
        var sql = ApplicationListQueryPerformanceSchemaSql.EnsureIndexesSql;

        Assert.Contains("IX_ApplicationProgresses_ApplicationID_ProgressOrder", sql);
        Assert.Contains("IX_ApplicationApprovalLegSnapshots_ApplicationId", sql);
        Assert.Contains("IX_Applications_ApplicationTypeID_List", sql);
        Assert.Contains("ProgressOrder DESC", sql);
        Assert.Contains("INCLUDE (StateID, Date)", sql);
    }
}
