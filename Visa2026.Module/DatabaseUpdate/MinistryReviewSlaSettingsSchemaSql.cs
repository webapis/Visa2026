using Microsoft.Data.SqlClient;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.MinistryReviewSlaSettings"/> when XAF schema
/// update did not run (e.g. hot reload after pulling new Module DLLs).
/// </summary>
public static class MinistryReviewSlaSettingsSchemaSql
{
    internal const string EnsureTableSql = """
        IF OBJECT_ID(N'dbo.MinistryReviewSlaSettings', N'U') IS NOT NULL
            RETURN;

        CREATE TABLE dbo.MinistryReviewSlaSettings (
            ID uniqueidentifier NOT NULL CONSTRAINT PK_MinistryReviewSlaSettings PRIMARY KEY,
            MaxDaysInReview int NOT NULL CONSTRAINT DF_MinistryReviewSlaSettings_MaxDaysInReview DEFAULT (4),
            WarningDaysBeforeMax int NULL,
            GCRecord int NOT NULL CONSTRAINT DF_MinistryReviewSlaSettings_GCRecord DEFAULT (0),
            OptimisticLockField int NOT NULL CONSTRAINT DF_MinistryReviewSlaSettings_OLF DEFAULT (0)
        );
        """;

    internal const string EnsureDefaultRowSql = """
        IF OBJECT_ID(N'dbo.MinistryReviewSlaSettings', N'U') IS NULL
            RETURN;

        IF EXISTS (SELECT 1 FROM dbo.MinistryReviewSlaSettings WHERE GCRecord = 0)
            RETURN;

        INSERT INTO dbo.MinistryReviewSlaSettings (ID, MaxDaysInReview, WarningDaysBeforeMax, GCRecord, OptimisticLockField)
        VALUES (NEWID(), 4, 1, 0, 0);
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = EnsureTableSql;
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = EnsureDefaultRowSql;
            command.ExecuteNonQuery();
        }
    }
}
