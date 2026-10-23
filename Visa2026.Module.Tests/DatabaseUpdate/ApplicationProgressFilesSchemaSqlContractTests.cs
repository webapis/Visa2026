using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Extra progress-step files table is healed at host start so officers can attach
/// scans when correcting a finished step without reverting later steps.
/// </summary>
public sealed class ApplicationProgressFilesSchemaSqlContractTests
{
    [Fact]
    public void EnsureTablePostgres_CreatesProgressFilesWithSlotAndCascade()
    {
        var sql = ApplicationProfileInstanceProgressFilesSchemaSql.EnsureTablePostgres;

        Assert.Contains("\"ApplicationProfileInstanceProgressFiles\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"ProgressId\" uuid NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("\"Slot\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
        Assert.Contains("\"SortOrder\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("IX_ApplicationProfileInstanceProgressFiles_ProgressId", sql, StringComparison.Ordinal);
        Assert.Contains("to_regclass('public.\"ApplicationProfileInstanceProgresses\"')", sql, StringComparison.Ordinal);
        Assert.Contains("to_regclass('public.\"FileData\"')", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureTableSqlServer_CreatesProgressFilesWithSlotAndCascade()
    {
        var sql = ApplicationProfileInstanceProgressFilesSchemaSql.EnsureTableSqlServer;

        Assert.Contains("ApplicationProfileInstanceProgressFiles", sql, StringComparison.Ordinal);
        Assert.Contains("ProgressId uniqueidentifier NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("Slot int NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("IX_ApplicationProfileInstanceProgressFiles_ProgressId", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyIfMissing_BlankOrNull_IsNoOp()
    {
        ApplicationProfileInstanceProgressFilesSchemaSql.ApplyIfMissing(null!);
        ApplicationProfileInstanceProgressFilesSchemaSql.ApplyIfMissing("");
        ApplicationProfileInstanceProgressFilesSchemaSql.ApplyIfMissing("   ");
    }
}
