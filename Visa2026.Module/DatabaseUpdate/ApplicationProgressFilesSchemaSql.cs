using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Extra progress-step files. Host-start heal when ModuleUpdater is skipped.
/// </summary>
public static class ApplicationProfileInstanceProgressFilesSchemaSql
{
    internal const string EnsureTablePostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileInstanceProgresses"') IS NULL
             OR to_regclass('public."FileData"') IS NULL THEN
            RETURN;
          END IF;

          IF to_regclass('public."ApplicationProfileInstanceProgressFiles"') IS NULL THEN
            CREATE TABLE "ApplicationProfileInstanceProgressFiles" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "ProgressId" uuid NOT NULL,
                "FileID" uuid NULL,
                "Slot" integer NOT NULL DEFAULT 0,
                "SortOrder" integer NOT NULL DEFAULT 0,
                CONSTRAINT "PK_ApplicationProfileInstanceProgressFiles" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_ApplicationProfileInstanceProgressFiles_Progress"
                    FOREIGN KEY ("ProgressId") REFERENCES "ApplicationProfileInstanceProgresses" ("ID") ON DELETE CASCADE,
                CONSTRAINT "FK_ApplicationProfileInstanceProgressFiles_FileData"
                    FOREIGN KEY ("FileID") REFERENCES "FileData" ("ID") ON DELETE NO ACTION
            );
          END IF;

          CREATE INDEX IF NOT EXISTS "IX_ApplicationProfileInstanceProgressFiles_ProgressId"
            ON "ApplicationProfileInstanceProgressFiles" ("ProgressId");
        END $$;
        """;

    internal const string EnsureTableSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.FileData', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.ApplicationProfileInstanceProgressFiles', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ApplicationProfileInstanceProgressFiles (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationProfileInstanceProgressFiles PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationProfileInstanceProgressFiles_GCRecord DEFAULT 0,
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationProfileInstanceProgressFiles_OptimisticLockField DEFAULT 0,
                ProgressId uniqueidentifier NOT NULL,
                FileID uniqueidentifier NULL,
                Slot int NOT NULL CONSTRAINT DF_ApplicationProfileInstanceProgressFiles_Slot DEFAULT 0,
                SortOrder int NOT NULL CONSTRAINT DF_ApplicationProfileInstanceProgressFiles_SortOrder DEFAULT 0,
                CONSTRAINT FK_ApplicationProfileInstanceProgressFiles_Progress
                    FOREIGN KEY (ProgressId) REFERENCES dbo.ApplicationProfileInstanceProgresses (ID) ON DELETE CASCADE,
                CONSTRAINT FK_ApplicationProfileInstanceProgressFiles_FileData
                    FOREIGN KEY (FileID) REFERENCES dbo.FileData (ID)
            );
            CREATE INDEX IX_ApplicationProfileInstanceProgressFiles_ProgressId
                ON dbo.ApplicationProfileInstanceProgressFiles (ProgressId);
        END
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (DatabaseProviderDetector.IsPostgreSql(connectionString))
        {
            using var connection = new NpgsqlConnection(cleaned);
            connection.Open();
            Execute(connection, EnsureTablePostgres);
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        Execute(sqlConnection, EnsureTableSqlServer);
    }

    private static void Execute(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
