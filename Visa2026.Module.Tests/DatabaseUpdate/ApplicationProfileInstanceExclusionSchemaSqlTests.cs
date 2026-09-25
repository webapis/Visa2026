using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Contract tests for Seretmezlik DDL heal SQL. Drift here breaks exclusion letter tables on Postgres hosts.
/// </summary>
public class ApplicationProfileInstanceExclusionSchemaSqlTests
{
    [Fact]
    public void EnsureSchemaPostgres_creates_exclusion_header_people_and_template_tables()
    {
        var sql = ApplicationProfileInstanceExclusionSchemaSql.EnsureSchemaPostgres;

        Assert.Contains("ApplicationProfileInstanceExclusions", sql, StringComparison.Ordinal);
        Assert.Contains("ApplicationProfileInstanceExclusionPeople", sql, StringComparison.Ordinal);
        Assert.Contains("ApplicationProfileInstanceExclusionTemplates", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureSchemaPostgres_includes_letter_numbering_and_addressee_columns()
    {
        var sql = ApplicationProfileInstanceExclusionSchemaSql.EnsureSchemaPostgres;

        Assert.Contains("\"LetterNumber\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"SequenceNumber\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"AppNumberPrefix\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"AddresseeKind\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"AddresseeLeg\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"OriginalRosterCount\"", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureSchemaPostgres_links_people_to_exclusion_and_person()
    {
        var sql = ApplicationProfileInstanceExclusionSchemaSql.EnsureSchemaPostgres;

        Assert.Contains("\"ExclusionId\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"PersonId\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"PassportNumber\"", sql, StringComparison.Ordinal);
        Assert.Contains(
            "FK_ApplicationProfileInstanceExclusionPeople_ApplicationProfileInstanceExclusions_ExclusionId",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "FK_ApplicationProfileInstanceExclusionPeople_People_PersonId",
            sql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureSchemaPostgres_is_idempotent_guarded_by_to_regclass()
    {
        var sql = ApplicationProfileInstanceExclusionSchemaSql.EnsureSchemaPostgres;

        Assert.Contains("to_regclass('public.\"ApplicationProfileInstances\"')", sql, StringComparison.Ordinal);
        Assert.Contains("to_regclass('public.\"ApplicationProfileInstanceExclusions\"')", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE INDEX IF NOT EXISTS", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyIfMissing_noops_on_blank_or_non_postgres_connection_string()
    {
        ApplicationProfileInstanceExclusionSchemaSql.ApplyIfMissing(null!);
        ApplicationProfileInstanceExclusionSchemaSql.ApplyIfMissing("   ");
        ApplicationProfileInstanceExclusionSchemaSql.ApplyIfMissing(
            "Server=localhost;Database=x;Trusted_Connection=True;");
    }
}
