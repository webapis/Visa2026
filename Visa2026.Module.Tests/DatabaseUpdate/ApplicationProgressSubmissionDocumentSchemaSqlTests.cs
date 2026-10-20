using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Host-start heal SQL for migration-submission columns and SUBMITTED_MIGRATION state seed.
/// </summary>
public class ApplicationProgressSubmissionDocumentSchemaSqlTests
{
    [Fact]
    public void EnsureColumnsPostgres_AddsSubmissionNumberAndFileId()
    {
        var sql = ApplicationProfileInstanceProgressSubmissionDocumentSchemaSql.EnsureColumnsPostgres;

        Assert.Contains("ApplicationProfileInstanceProgresses", sql, StringComparison.Ordinal);
        Assert.Contains("\"SubmissionDocumentNumber\"", sql, StringComparison.Ordinal);
        Assert.Contains("character varying(100)", sql, StringComparison.Ordinal);
        Assert.Contains("\"SubmissionDocumentFileID\"", sql, StringComparison.Ordinal);
        Assert.Contains("uuid", sql, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureStatePostgres_SeedsSubmittedMigrationOnce()
    {
        var sql = ApplicationProfileInstanceProgressSubmissionDocumentSchemaSql.EnsureStatePostgres;

        Assert.Contains("ApplicationStates", sql, StringComparison.Ordinal);
        Assert.Contains("'SUBMITTED_MIGRATION'", sql, StringComparison.Ordinal);
        Assert.Contains("'SUBMITTED_TO_MIGRATION'", sql, StringComparison.Ordinal);
        Assert.Contains("NOT EXISTS", sql, StringComparison.Ordinal);
        Assert.Contains("\"Code\" = 'SUBMITTED_MIGRATION'", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureColumnsSqlServer_AddsSubmissionNumberAndFileId()
    {
        var sql = ApplicationProfileInstanceProgressSubmissionDocumentSchemaSql.EnsureColumnsSqlServer;

        Assert.Contains("SubmissionDocumentNumber", sql, StringComparison.Ordinal);
        Assert.Contains("nvarchar(100)", sql, StringComparison.Ordinal);
        Assert.Contains("SubmissionDocumentFileID", sql, StringComparison.Ordinal);
        Assert.Contains("uniqueidentifier", sql, StringComparison.Ordinal);
        Assert.Contains("COL_LENGTH", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyIfMissing_BlankOrNull_IsNoOp()
    {
        ApplicationProfileInstanceProgressSubmissionDocumentSchemaSql.ApplyIfMissing(null!);
        ApplicationProfileInstanceProgressSubmissionDocumentSchemaSql.ApplyIfMissing("");
        ApplicationProfileInstanceProgressSubmissionDocumentSchemaSql.ApplyIfMissing("   ");
    }
}
