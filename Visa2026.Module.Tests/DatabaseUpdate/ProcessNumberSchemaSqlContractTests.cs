using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// String-contract guards for ProcessNumber schema heal SQL (Visa + ApplicationProgress/Application).
/// Prevents silent drift of column types / backfill filters used at host start.
/// </summary>
public sealed class ProcessNumberSchemaSqlContractTests
{
    [Fact]
    public void VisaProcessNumber_Postgres_RenamesMistakenUuidThenAddsStringColumn()
    {
        var sql = VisaProcessNumberSchemaSql.EnsureColumnsPostgres;

        Assert.Contains("to_regclass('public.\"Visas\"')", sql);
        Assert.Contains("data_type = 'uuid'", sql);
        Assert.Contains("LegacyPersonInApplicationOid", sql);
        Assert.Contains("RENAME COLUMN \"ProcessNumber\" TO \"LegacyPersonInApplicationOid\"", sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"ProcessNumber\" character varying(100)", sql);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"LegacyPersonInApplicationOid\" uuid", sql);
    }

    [Fact]
    public void VisaProcessNumber_SqlServer_RenamesUniqueidentifierThenAddsNvarchar()
    {
        var sql = VisaProcessNumberSchemaSql.EnsureColumnsSqlServer;

        Assert.Contains("OBJECT_ID(N'dbo.Visas'", sql);
        Assert.Contains("uniqueidentifier", sql);
        Assert.Contains("sp_rename N'dbo.Visas.ProcessNumber', N'LegacyPersonInApplicationOid'", sql);
        Assert.Contains("ADD ProcessNumber nvarchar(100)", sql);
        Assert.Contains("ADD LegacyPersonInApplicationOid uniqueidentifier", sql);
    }

    [Fact]
    public void ApplicationProgressProcessNumber_EnsureColumns_BothProviders()
    {
        var pg = ApplicationProgressProcessNumberSchemaSql.EnsureColumnsPostgres;
        var ss = ApplicationProgressProcessNumberSchemaSql.EnsureColumnsSqlServer;

        Assert.Contains("ApplicationProgresses", pg);
        Assert.Contains("Applications", pg);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"ProcessNumber\" character varying(100)", pg);

        Assert.Contains("dbo.ApplicationProgresses", ss);
        Assert.Contains("dbo.Applications", ss);
        Assert.Contains("ADD ProcessNumber nvarchar(100)", ss);
    }

    [Fact]
    public void ApplicationProgressProcessNumber_BackfillFromDescription_OnlyProcessStarted()
    {
        var pg = ApplicationProgressProcessNumberSchemaSql.BackfillProgressFromDescriptionPostgres;
        var ss = ApplicationProgressProcessNumberSchemaSql.BackfillProgressFromDescriptionSqlServer;

        Assert.Contains("PROCESS_STARTED", pg);
        Assert.Contains("PROCESS_STARTED", ss);
        Assert.Contains("LEFT(TRIM(ap.\"Description\"), 100)", pg);
        Assert.Contains("LEFT(LTRIM(RTRIM(ap.Description)), 100)", ss);
        Assert.Contains("ap.\"ProcessNumber\" IS NULL", pg);
        Assert.Contains("ap.ProcessNumber IS NULL", ss);
    }

    [Fact]
    public void ApplicationProgressProcessNumber_BackfillApplication_UsesEarliestProcessStarted()
    {
        var pg = ApplicationProgressProcessNumberSchemaSql.BackfillApplicationFromProgressPostgres;
        var ss = ApplicationProgressProcessNumberSchemaSql.BackfillApplicationFromProgressSqlServer;

        Assert.Contains("PROCESS_STARTED", pg);
        Assert.Contains("PROCESS_STARTED", ss);
        Assert.Contains("ROW_NUMBER() OVER", pg);
        Assert.Contains("ROW_NUMBER() OVER", ss);
        Assert.Contains("\"ProgressOrder\" ASC", pg);
        Assert.Contains("ProgressOrder ASC", ss);
        Assert.Contains("src.rn = 1", pg);
        Assert.Contains("src.rn = 1", ss);
        Assert.Contains("a.\"ProcessNumber\" IS NULL", pg);
        Assert.Contains("a.ProcessNumber IS NULL", ss);
    }
}
