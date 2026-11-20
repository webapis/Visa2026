using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ApplicationProfileAddress2Recent"/>.
/// </summary>
public static class ApplicationProfileAddress2RecentSchemaSql
{
    internal const string EnsureTablePostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileAddress2Recents"') IS NOT NULL THEN
            RETURN;
          END IF;
          IF to_regclass('public."ApplicationProfiles"') IS NULL THEN
            RETURN;
          END IF;

          CREATE TABLE "ApplicationProfileAddress2Recents" (
            "ID" uuid NOT NULL,
            "GCRecord" integer NOT NULL DEFAULT 0,
            "OptimisticLockField" integer NOT NULL DEFAULT 0,
            "ApplicationProfileId" uuid NOT NULL,
            "ProjectContractId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
            "TextKey" character varying(450) NOT NULL DEFAULT '',
            "Address2Text" text NULL,
            "UsedAtUtc" timestamp without time zone NOT NULL DEFAULT TIMESTAMP '0001-01-01 00:00:00',
            CONSTRAINT "PK_ApplicationProfileAddress2Recents" PRIMARY KEY ("ID"),
            CONSTRAINT "FK_ApplicationProfileAddress2Recents_ApplicationProfiles_ApplicationProfileId"
                FOREIGN KEY ("ApplicationProfileId") REFERENCES "ApplicationProfiles" ("ID") ON DELETE CASCADE
          );
          CREATE UNIQUE INDEX "IX_ApplicationProfileAddress2Recents_Profile_Project_Text"
            ON "ApplicationProfileAddress2Recents" ("ApplicationProfileId", "ProjectContractId", "TextKey")
            WHERE "GCRecord" IS NULL;
        END $$;
        """;

    internal const string EnsureTableSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProfileAddress2Recents', N'U') IS NOT NULL
            RETURN;
        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NULL
            RETURN;

        CREATE TABLE dbo.ApplicationProfileAddress2Recents (
            ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationProfileAddress2Recents PRIMARY KEY,
            GCRecord int NOT NULL CONSTRAINT DF_ApplicationProfileAddress2Recents_GCRecord DEFAULT (0),
            OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationProfileAddress2Recents_OLF DEFAULT (0),
            ApplicationProfileId uniqueidentifier NOT NULL,
            ProjectContractId uniqueidentifier NOT NULL CONSTRAINT DF_ApplicationProfileAddress2Recents_Project DEFAULT ('00000000-0000-0000-0000-000000000000'),
            TextKey nvarchar(450) NOT NULL CONSTRAINT DF_ApplicationProfileAddress2Recents_TextKey DEFAULT (N''),
            Address2Text nvarchar(max) NULL,
            UsedAtUtc datetime2 NOT NULL CONSTRAINT DF_ApplicationProfileAddress2Recents_UsedAt DEFAULT ('0001-01-01'),
            CONSTRAINT FK_ApplicationProfileAddress2Recents_ApplicationProfiles_ApplicationProfileId
                FOREIGN KEY (ApplicationProfileId) REFERENCES dbo.ApplicationProfiles(ID) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IX_ApplicationProfileAddress2Recents_Profile_Project_Text
            ON dbo.ApplicationProfileAddress2Recents (ApplicationProfileId, ProjectContractId, TextKey)
            WHERE [GCRecord] IS NULL;
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