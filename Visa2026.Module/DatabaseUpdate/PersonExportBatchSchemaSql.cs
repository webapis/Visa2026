using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.PersonExportBatch"/> (director hand-over export queue).
/// </summary>
/// <remarks>
/// This repository does not use EF migrations: when <c>ModuleInfo</c> already reports the current
/// version, XAF skips the schema pass and a newly added table is never created. New batch tables
/// therefore ship with a schema updater plus a host-start heal, the same way
/// <see cref="ApplicationRuntimeLogSchemaSql"/> and <see cref="PersonIncompleteDataSchemaSql"/> do.
/// Column shape mirrors <c>PdfGenerationBatches</c> so EF sees exactly what it expects.
/// </remarks>
public static class PersonExportBatchSchemaSql
{
    internal const string EnsureTableSqlServer = """
        IF OBJECT_ID(N'dbo.PersonExportBatches', N'U') IS NOT NULL
            RETURN;

        CREATE TABLE dbo.PersonExportBatches (
            ID uniqueidentifier NOT NULL CONSTRAINT PK_PersonExportBatches PRIMARY KEY,
            CreatedOnUtc datetime2 NOT NULL,
            RequestedBy nvarchar(256) NULL,
            RequestedCulture nvarchar(10) NULL,
            Status int NOT NULL,
            PersonID uniqueidentifier NULL,
            PersonDisplayName nvarchar(512) NULL,
            TotalRecords int NOT NULL CONSTRAINT DF_PersonExportBatches_TotalRecords DEFAULT (0),
            ProcessedRecords int NOT NULL CONSTRAINT DF_PersonExportBatches_ProcessedRecords DEFAULT (0),
            ErrorMessage nvarchar(1024) NULL,
            ExportNotes nvarchar(max) NULL,
            ZipFileID uniqueidentifier NULL,
            GCRecord int NOT NULL CONSTRAINT DF_PersonExportBatches_GCRecord DEFAULT (0),
            OptimisticLockField int NOT NULL CONSTRAINT DF_PersonExportBatches_OLF DEFAULT (0)
        );

        CREATE INDEX IX_PersonExportBatches_ZipFileID ON dbo.PersonExportBatches (ZipFileID);
        CREATE INDEX IX_PersonExportBatches_PersonID ON dbo.PersonExportBatches (PersonID);
        CREATE INDEX IX_PersonExportBatches_CreatedOnUtc ON dbo.PersonExportBatches (CreatedOnUtc);
        """;

    internal const string EnsureTablePostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."PersonExportBatches"') IS NOT NULL THEN
            RETURN;
          END IF;

          CREATE TABLE "PersonExportBatches" (
            "ID" uuid NOT NULL CONSTRAINT "PK_PersonExportBatches" PRIMARY KEY,
            "CreatedOnUtc" timestamp without time zone NOT NULL,
            "RequestedBy" character varying(256) NULL,
            "RequestedCulture" character varying(10) NULL,
            "Status" integer NOT NULL,
            "PersonID" uuid NULL,
            "PersonDisplayName" character varying(512) NULL,
            "TotalRecords" integer NOT NULL DEFAULT 0,
            "ProcessedRecords" integer NOT NULL DEFAULT 0,
            "ErrorMessage" character varying(1024) NULL,
            "ExportNotes" text NULL,
            "ZipFileID" uuid NULL,
            "GCRecord" integer NOT NULL DEFAULT 0,
            "OptimisticLockField" integer NOT NULL DEFAULT 0
          );

          CREATE INDEX "IX_PersonExportBatches_ZipFileID" ON "PersonExportBatches" ("ZipFileID");
          CREATE INDEX "IX_PersonExportBatches_PersonID" ON "PersonExportBatches" ("PersonID");
          CREATE INDEX "IX_PersonExportBatches_CreatedOnUtc" ON "PersonExportBatches" ("CreatedOnUtc");

          IF to_regclass('public."FileData"') IS NOT NULL THEN
            ALTER TABLE "PersonExportBatches"
              ADD CONSTRAINT "FK_PersonExportBatches_FileData_ZipFileID"
              FOREIGN KEY ("ZipFileID") REFERENCES "FileData"("ID");
          END IF;
        END $$;
        """;

    /// <summary>Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).</summary>
    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (DatabaseProviderDetector.IsPostgreSql(connectionString))
        {
            using var connection = new NpgsqlConnection(cleaned);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = EnsureTablePostgres;
            command.ExecuteNonQuery();
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        using var sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.CommandText = EnsureTableSqlServer;
        sqlCommand.ExecuteNonQuery();
    }
}
