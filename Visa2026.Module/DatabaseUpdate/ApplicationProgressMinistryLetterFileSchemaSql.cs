using Microsoft.Data.SqlClient;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ApplicationProgress.MinistryLetterFile"/> schema column.
/// </summary>
public static class ApplicationProgressMinistryLetterFileSchemaSql
{
    internal const string EnsureMinistryLetterFileIdColumnSql = """
        IF OBJECT_ID(N'dbo.ApplicationProgresses', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationProgresses', N'MinistryLetterFileID') IS NOT NULL
            RETURN;
        ALTER TABLE dbo.ApplicationProgresses ADD MinistryLetterFileID uniqueidentifier NULL;
        """;

    internal const string EnsureMinistryLetterFileFkSql = """
        IF OBJECT_ID(N'dbo.ApplicationProgresses', N'U') IS NULL
            OR OBJECT_ID(N'dbo.FileData', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationProgresses', N'MinistryLetterFileID') IS NULL
            RETURN;
        IF EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID(N'dbo.ApplicationProgresses')
              AND name = N'FK_ApplicationProgresses_FileData_MinistryLetterFileID')
            RETURN;
        ALTER TABLE dbo.ApplicationProgresses
            ADD CONSTRAINT FK_ApplicationProgresses_FileData_MinistryLetterFileID
            FOREIGN KEY (MinistryLetterFileID) REFERENCES dbo.FileData(ID)
            ON DELETE NO ACTION;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        using var connection = new SqlConnection(connectionString);
        connection.Open();
        ExecuteBatch(connection, EnsureMinistryLetterFileIdColumnSql);
        ExecuteBatch(connection, EnsureMinistryLetterFileFkSql);
    }

    private static void ExecuteBatch(SqlConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
