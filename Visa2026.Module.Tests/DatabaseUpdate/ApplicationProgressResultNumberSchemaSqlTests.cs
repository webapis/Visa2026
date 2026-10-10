using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProgressResultNumberSchemaSqlTests
{
    [Fact]
    public void Postgres_AddsNullableResultNumberVarchar100()
    {
        Assert.Contains(
            "ApplicationProfileInstanceProgresses",
            ApplicationProfileInstanceProgressResultNumberSchemaSql.EnsureColumnPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "ADD COLUMN IF NOT EXISTS \"ResultNumber\" character varying(100) NULL",
            ApplicationProfileInstanceProgressResultNumberSchemaSql.EnsureColumnPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "to_regclass('public.\"ApplicationProfileInstanceProgresses\"') IS NULL",
            ApplicationProfileInstanceProgressResultNumberSchemaSql.EnsureColumnPostgres,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SqlServer_AddsNullableResultNumberNvarchar100WhenMissing()
    {
        Assert.Contains(
            "COL_LENGTH(N'dbo.ApplicationProfileInstanceProgresses', N'ResultNumber') IS NULL",
            ApplicationProfileInstanceProgressResultNumberSchemaSql.EnsureColumnSqlServer,
            StringComparison.Ordinal);
        Assert.Contains(
            "ADD ResultNumber nvarchar(100) NULL",
            ApplicationProfileInstanceProgressResultNumberSchemaSql.EnsureColumnSqlServer,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ApplyIfMissing_NoOpsOnBlankConnectionString(string? connectionString)
    {
        ApplicationProfileInstanceProgressResultNumberSchemaSql.ApplyIfMissing(connectionString!);
    }
}
