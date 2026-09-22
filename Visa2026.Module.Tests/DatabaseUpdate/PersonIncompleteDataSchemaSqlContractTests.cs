using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Contract tests for Person incomplete-data column heal SQL (officer Mark incomplete workflow).
/// </summary>
public sealed class PersonIncompleteDataSchemaSqlContractTests
{
    private static readonly string[] RequiredColumns =
    [
        "IsDataIncomplete",
        "IncompleteMissingPersonalData",
        "IncompleteMissingPassport",
        "IncompleteMissingCv",
        "IncompleteMissingPhoto",
        "IncompleteMissingEducation",
        "IncompleteMissingMedical",
        "IncompleteMissingAddress",
        "IncompleteMissingFamilyDocs",
        "IncompleteMissingOther",
        "IncompleteNotes",
        "IncompleteMarkedOn",
        "IncompleteMarkedBy",
    ];

    [Fact]
    public void Postgres_ContainsAllIncompleteColumns_WithPeopleGuard()
    {
        var sql = PersonIncompleteDataSchemaSql.EnsureColumnsPostgres;

        Assert.Contains("to_regclass('public.\"People\"')", sql);
        foreach (var column in RequiredColumns)
        {
            Assert.Contains($"column_name = '{column}'", sql);
            Assert.Contains($"ADD COLUMN \"{column}\"", sql);
        }

        Assert.Contains("boolean NOT NULL DEFAULT false", sql);
        Assert.Contains("\"IncompleteNotes\" text NULL", sql);
        Assert.Contains("\"IncompleteMarkedBy\" character varying(255)", sql);
    }

    [Fact]
    public void SqlServer_ContainsAllIncompleteColumns_WithPeopleGuard()
    {
        var sql = PersonIncompleteDataSchemaSql.EnsureColumnsSqlServer;

        Assert.Contains("OBJECT_ID(N'dbo.People'", sql);
        foreach (var column in RequiredColumns)
        {
            Assert.Contains($"COL_LENGTH(N'dbo.People', N'{column}')", sql);
            Assert.Contains($"ADD {column}", sql);
        }

        Assert.Contains("IsDataIncomplete bit NOT NULL", sql);
        Assert.Contains("IncompleteNotes nvarchar(max) NULL", sql);
        Assert.Contains("IncompleteMarkedBy nvarchar(255) NULL", sql);
        Assert.Contains("DF_People_IsDataIncomplete", sql);
    }

    [Fact]
    public void ApplyIfMissing_BlankConnectionString_NoThrow()
    {
        PersonIncompleteDataSchemaSql.ApplyIfMissing(null!);
        PersonIncompleteDataSchemaSql.ApplyIfMissing("  ");
    }
}
