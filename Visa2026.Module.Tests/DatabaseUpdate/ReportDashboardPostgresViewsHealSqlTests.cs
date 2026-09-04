using System.Linq;
using System.Reflection;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Guards Report Dashboard host-start heal inventory (work-permit / incomplete / person-search)
/// and early-outs that must never open a DB connection.
/// </summary>
public sealed class ReportDashboardPostgresViewsHealSqlTests
{
    private static readonly string[] RequiredEmbeddedLeaves =
    [
        "vw_rd_work_permit.postgres.sql",
        "vw_rd_work_permit_active.postgres.sql",
        "vw_rd_work_permit_app_progress.postgres.sql",
        "vw_rd_incomplete_persons_by_missing_area.postgres.sql",
        "vw_rd_person_search.postgres.sql",
        "vw_rd_visa_by_period.postgres.sql",
        "vw_rd_visa_app_progress.postgres.sql",
        "vw_rd_visa_on_extension.postgres.sql",
        "vw_rd_application_via_ministry_invitation_on_process.postgres.sql",
        "vw_rd_application_direct_migration_on_process_a.postgres.sql",
    ];

    [Fact]
    public void ApplyIfMissing_BlankOrWhitespace_IsNoOp()
    {
        ReportDashboardPostgresViewsHealSql.ApplyIfMissing(null!);
        ReportDashboardPostgresViewsHealSql.ApplyIfMissing("");
        ReportDashboardPostgresViewsHealSql.ApplyIfMissing("   ");
    }

    [Fact]
    public void ApplyIfMissing_NonPostgresConnectionString_IsNoOp()
    {
        // Must not attempt Npgsql open against a SQL Server-shaped string.
        ReportDashboardPostgresViewsHealSql.ApplyIfMissing(
            "Server=localhost;Database=visa2026;Trusted_Connection=True;TrustServerCertificate=True");
        ReportDashboardPostgresViewsHealSql.ApplyIfMissing(
            "Data Source=localhost;Initial Catalog=visa2026;Integrated Security=True");
    }

    [Fact]
    public void RequiredDashboardViewSqlResources_AreEmbedded()
    {
        var assembly = typeof(ReportDashboardPostgresViewsHealSql).Assembly;
        var names = assembly.GetManifestResourceNames();

        foreach (var leaf in RequiredEmbeddedLeaves)
        {
            var resourceName = "Visa2026.Module.SqlViews." + leaf;
            Assert.Contains(resourceName, names);
            using var stream = assembly.GetManifestResourceStream(resourceName);
            Assert.NotNull(stream);
            Assert.True(stream!.Length > 0, resourceName + " should not be empty");
        }
    }

    [Fact]
    public void WorkPermitHealViews_AreIsolatedFromViaMinistryStandaloneBulk()
    {
        // Regression: work-permit heals must stay out of StandaloneViews so a missing
        // wp view does not force a full via-ministry re-heal (see HealSql remarks).
        var fields = typeof(ReportDashboardPostgresViewsHealSql)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic);

        var workPermit = fields.Single(f => f.Name == "WorkPermitViews");
        var standalone = fields.Single(f => f.Name == "StandaloneViews");

        var workPermitLeaves = ReadResourceLeaves(workPermit.GetValue(null)!);
        var standaloneLeaves = ReadResourceLeaves(standalone.GetValue(null)!);

        Assert.Contains("vw_rd_work_permit.postgres.sql", workPermitLeaves);
        Assert.Contains("vw_rd_work_permit_active.postgres.sql", workPermitLeaves);
        Assert.Contains("vw_rd_work_permit_app_progress.postgres.sql", workPermitLeaves);

        Assert.DoesNotContain(workPermitLeaves, leaf => standaloneLeaves.Contains(leaf));
        Assert.DoesNotContain(
            standaloneLeaves,
            leaf => leaf.Contains("work_permit", System.StringComparison.OrdinalIgnoreCase));
    }

    private static string[] ReadResourceLeaves(object arrayValue)
    {
        // Arrays of ValueTuple<string, string> or ValueTuple<string, string, string>
        var enumerable = ((System.Collections.IEnumerable)arrayValue).Cast<object>();
        return enumerable
            .Select(item =>
            {
                var type = item.GetType();
                // last field is ResourceLeaf on both BaseViews (3) and Wrapper/Standalone/WorkPermit (2)
                var fields = type.GetFields();
                return (string)fields[^1].GetValue(item)!;
            })
            .ToArray();
    }
}
