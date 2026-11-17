using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.PersonPendingItem"/>.
/// Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).
/// </summary>
public static class PersonPendingItemSchemaSql
{
    internal const string EnsureTableSqlServer = """
        IF OBJECT_ID(N'dbo.PersonPendingItems', N'U') IS NOT NULL
            RETURN;

        CREATE TABLE dbo.PersonPendingItems (
            ID uniqueidentifier NOT NULL CONSTRAINT PK_PersonPendingItems PRIMARY KEY,
            TargetKey nvarchar(128) NOT NULL,
            AreaKey nvarchar(64) NOT NULL,
            Note nvarchar(2000) NULL,
            PersonID uniqueidentifier NULL,
            GCRecord int NOT NULL CONSTRAINT DF_PersonPendingItems_GCRecord DEFAULT (0),
            OptimisticLockField int NOT NULL CONSTRAINT DF_PersonPendingItems_OLF DEFAULT (0)
        );

        CREATE INDEX IX_PersonPendingItems_PersonID ON dbo.PersonPendingItems (PersonID);

        IF OBJECT_ID(N'dbo.People', N'U') IS NOT NULL
            ALTER TABLE dbo.PersonPendingItems
                ADD CONSTRAINT FK_PersonPendingItems_People_PersonID
                FOREIGN KEY (PersonID) REFERENCES dbo.People (ID);
        """;

    internal const string EnsureTablePostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."PersonPendingItems"') IS NOT NULL THEN
            RETURN;
          END IF;

          CREATE TABLE "PersonPendingItems" (
            "ID" uuid NOT NULL CONSTRAINT "PK_PersonPendingItems" PRIMARY KEY,
            "TargetKey" character varying(128) NOT NULL,
            "AreaKey" character varying(64) NOT NULL,
            "Note" character varying(2000) NULL,
            "PersonID" uuid NULL,
            "GCRecord" integer NOT NULL DEFAULT 0,
            "OptimisticLockField" integer NOT NULL DEFAULT 0
          );

          CREATE INDEX "IX_PersonPendingItems_PersonID" ON "PersonPendingItems" ("PersonID");

          IF to_regclass('public."People"') IS NOT NULL THEN
            ALTER TABLE "PersonPendingItems"
              ADD CONSTRAINT "FK_PersonPendingItems_People_PersonID"
              FOREIGN KEY ("PersonID") REFERENCES "People"("ID");
          END IF;
        END $$;
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
