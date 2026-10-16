using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014TargetIdMapRebuildPostgresSqlTests
{
    [Fact]
    public void Passport_match_sql_uses_quoted_postgres_identifiers()
    {
        const string sqlServer = """
            SELECT TOP 1 CAST(ID AS varchar(36))
            FROM Passports
            WHERE (GCRecord IS NULL OR GCRecord = 0)
              AND PersonID = @personId
              AND PassportNumber = @passportNumber
            ORDER BY ID
            """;

        var postgres = Visa2014TargetIdMapRebuild.ToPostgresIdMatchSql(sqlServer);

        Assert.Contains("SELECT \"ID\"::text", postgres);
        Assert.Contains("FROM \"Passports\"", postgres);
        Assert.Contains("\"PersonID\" = @personId", postgres);
        Assert.Contains("\"PassportNumber\" = @passportNumber", postgres);
        Assert.Contains("LIMIT 1", postgres);
        Assert.DoesNotContain("TOP 1", postgres);
    }

    [Fact]
    public void Date_cast_becomes_postgres_cast()
    {
        const string sqlServer = """
            SELECT TOP 1 CAST(ID AS varchar(36))
            FROM WorkPermits
            WHERE (GCRecord IS NULL OR GCRecord = 0)
              AND CAST(StartDate AS date) = @issuedDate
            ORDER BY ID
            """;

        var postgres = Visa2014TargetIdMapRebuild.ToPostgresIdMatchSql(sqlServer);

        Assert.Contains("\"StartDate\"::date = @issuedDate", postgres);
        Assert.DoesNotContain("CAST(", postgres);
    }

    [Fact]
    public void Unique_number_sql_keeps_the_single_row_guard()
    {
        var postgres = Visa2014TargetIdMapRebuild.ToPostgresIdMatchSql(
            Visa2014TargetIdMapRebuild.UniqueLiveRowSql("WorkPermits", "WorkPermitNumber = @workPermitNumber"));

        Assert.Contains("FROM \"WorkPermits\"", postgres);
        Assert.Contains("\"WorkPermitNumber\" = @workPermitNumber", postgres);
        Assert.Contains("COUNT(*)", postgres);
        Assert.Contains("LIMIT 1", postgres);
        Assert.DoesNotContain("TOP 1", postgres);
    }
}