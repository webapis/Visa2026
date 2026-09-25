using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Contract tests for the roster join skip-nav heal (ResolvedLinks retarget + join table reshape).
/// </summary>
public class ApplicationProfileInstancePeopleSkipNavSchemaSqlTests
{
    [Fact]
    public void HealPostgres_retargets_resolved_links_to_instance_and_person()
    {
        var sql = ApplicationProfileInstancePeopleSkipNavSchemaSql.HealPostgres;

        Assert.Contains("ApplicationProfileInstancePersonResolvedLinks", sql, StringComparison.Ordinal);
        Assert.Contains("ApplicationProfileInstanceId", sql, StringComparison.Ordinal);
        Assert.Contains("PersonId", sql, StringComparison.Ordinal);
        Assert.Contains("ApplicationProfileInstancePersonId", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void HealPostgres_touches_people_join_table()
    {
        var sql = ApplicationProfileInstancePeopleSkipNavSchemaSql.HealPostgres;

        Assert.Contains("ApplicationProfileInstancePeople", sql, StringComparison.Ordinal);
        Assert.Contains("information_schema.columns", sql, StringComparison.Ordinal);
    }
}
