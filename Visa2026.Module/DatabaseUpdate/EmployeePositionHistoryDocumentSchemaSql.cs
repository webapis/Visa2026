using System.Data.Common;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent <c>EmployeePositionHistoryDocuments</c> table for order-for-assignment scans.
/// Host-start heal when ModuleUpdater / EF schema sync is skipped (ModuleInfo already current).
/// </summary>
public static class EmployeePositionHistoryDocumentSchemaSql
{
    internal const string EnsureSchemaPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."EmployeePositionHistories"') IS NULL THEN
            RETURN;
          END IF;

          IF to_regclass('public."EmployeePositionHistoryDocuments"') IS NULL THEN
            CREATE TABLE "EmployeePositionHistoryDocuments" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NULL DEFAULT 0,
                "OptimisticLockField" integer NULL DEFAULT 0,
                "EmployeePositionHistoryID" uuid NOT NULL,
                "FileID" uuid NULL,
                "Description" character varying(255) NULL,
                CONSTRAINT "PK_EmployeePositionHistoryDocuments" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_EmployeePositionHistoryDocuments_EmployeePositionHistories"
                    FOREIGN KEY ("EmployeePositionHistoryID") REFERENCES "EmployeePositionHistories" ("ID") ON DELETE CASCADE
            );
            CREATE INDEX "IX_EmployeePositionHistoryDocuments_EmployeePositionHistoryID"
                ON "EmployeePositionHistoryDocuments" ("EmployeePositionHistoryID");
          END IF;
        END $$;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (!DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        using var connection = new NpgsqlConnection(cleaned);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = EnsureSchemaPostgres;
        command.ExecuteNonQuery();
    }
}