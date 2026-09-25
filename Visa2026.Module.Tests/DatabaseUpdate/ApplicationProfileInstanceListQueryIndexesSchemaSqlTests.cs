using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Contract tests for Application Profile Instance ListView performance indexes.
/// </summary>
public class ApplicationProfileInstanceListQueryIndexesSchemaSqlTests
{
    [Fact]
    public void EnsureIndexesPostgres_creates_application_date_and_staged_queue_indexes()
    {
        var sql = ApplicationProfileInstanceListQueryIndexesSchemaSql.EnsureIndexesPostgres;

        Assert.Contains("IX_ApplicationProfileInstances_ApplicationDate", sql, StringComparison.Ordinal);
        Assert.Contains("IX_ApplicationProfileInstances_StagedQueue_State", sql, StringComparison.Ordinal);
        Assert.Contains("\"ApplicationDate\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"HasLeftStagedQueue\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"LatestPrimaryStateCode\"", sql, StringComparison.Ordinal);
        Assert.Contains("to_regclass('public.\"ApplicationProfileInstances\"')", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureIndexesSqlServer_mirrors_postgres_index_names()
    {
        var sql = ApplicationProfileInstanceListQueryIndexesSchemaSql.EnsureIndexesSqlServer;

        Assert.Contains("IX_ApplicationProfileInstances_ApplicationDate", sql, StringComparison.Ordinal);
        Assert.Contains("IX_ApplicationProfileInstances_StagedQueue_State", sql, StringComparison.Ordinal);
        Assert.Contains("ApplicationProfileInstances", sql, StringComparison.Ordinal);
    }
}
