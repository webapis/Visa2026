using System;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for ApplicationProfileInstance skip-navigation People join + sticky ResolvedLinks.
/// Existing BaseObject-shaped join tables are converted by
/// <see cref="ApplicationProfileInstancePeopleSkipNavSchemaSql"/>.
/// </summary>
public static class ApplicationWorkspaceSchemaSql
{
    internal const string EnsureSchemaPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileInstancePeople"') IS NULL
             AND to_regclass('public."ApplicationProfileInstances"') IS NOT NULL
             AND to_regclass('public."People"') IS NOT NULL THEN
            CREATE TABLE "ApplicationProfileInstancePeople" (
                "ApplicationProfileInstanceId" uuid NOT NULL,
                "PersonId" uuid NOT NULL,
                CONSTRAINT "PK_ApplicationProfileInstancePeople" PRIMARY KEY
                    ("ApplicationProfileInstanceId", "PersonId"),
                CONSTRAINT "FK_ApplicationProfileInstancePeople_ApplicationProfileInstances_ApplicationProfileInstanceId"
                    FOREIGN KEY ("ApplicationProfileInstanceId") REFERENCES "ApplicationProfileInstances" ("ID") ON DELETE CASCADE,
                CONSTRAINT "FK_ApplicationProfileInstancePeople_People_PersonId"
                    FOREIGN KEY ("PersonId") REFERENCES "People" ("ID") ON DELETE RESTRICT
            );
          END IF;

          IF to_regclass('public."ApplicationProfileInstancePersonResolvedLinks"') IS NULL
             AND to_regclass('public."ApplicationProfileInstances"') IS NOT NULL
             AND to_regclass('public."People"') IS NOT NULL THEN
            CREATE TABLE "ApplicationProfileInstancePersonResolvedLinks" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "ApplicationProfileInstanceId" uuid NOT NULL,
                "PersonId" uuid NOT NULL,
                "LinkKind" integer NOT NULL DEFAULT 0,
                "LinkedObjectId" uuid NOT NULL,
                CONSTRAINT "PK_ApplicationProfileInstancePersonResolvedLinks" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_ApplicationProfileInstancePersonResolvedLinks_ApplicationProfileInstances_ApplicationProfileInstanceId"
                    FOREIGN KEY ("ApplicationProfileInstanceId") REFERENCES "ApplicationProfileInstances" ("ID") ON DELETE CASCADE,
                CONSTRAINT "FK_ApplicationProfileInstancePersonResolvedLinks_People_PersonId"
                    FOREIGN KEY ("PersonId") REFERENCES "People" ("ID") ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX "IX_ApplicationProfileInstancePersonResolvedLinks_Instance_Person_Kind"
                ON "ApplicationProfileInstancePersonResolvedLinks" ("ApplicationProfileInstanceId", "PersonId", "LinkKind");
          END IF;
        END $$;
        """;

    internal const string EnsureSchemaSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstancePeople', N'U') IS NULL
           AND OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.People', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE dbo.ApplicationProfileInstancePeople (
                ApplicationProfileInstanceId uniqueidentifier NOT NULL,
                PersonId uniqueidentifier NOT NULL,
                CONSTRAINT PK_ApplicationProfileInstancePeople PRIMARY KEY (ApplicationProfileInstanceId, PersonId),
                CONSTRAINT FK_ApplicationProfileInstancePeople_Applications_ApplicationProfileInstanceId
                    FOREIGN KEY (ApplicationProfileInstanceId) REFERENCES dbo.ApplicationProfileInstances(ID) ON DELETE CASCADE,
                CONSTRAINT FK_ApplicationProfileInstancePeople_People_PersonId
                    FOREIGN KEY (PersonId) REFERENCES dbo.People(ID)
            );
            CREATE UNIQUE INDEX IX_ApplicationProfileInstancePeople_Application_Person
                ON dbo.ApplicationProfileInstancePeople (ApplicationProfileInstanceId, PersonId);
        END;

        IF OBJECT_ID(N'dbo.ApplicationProfileInstancePersonResolvedLinks', N'U') IS NULL
           AND OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.People', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE dbo.ApplicationProfileInstancePersonResolvedLinks (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationProfileInstancePersonResolvedLinks PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationProfileInstancePersonResolvedLinks_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationProfileInstancePersonResolvedLinks_OLF DEFAULT (0),
                ApplicationProfileInstanceId uniqueidentifier NOT NULL,
                PersonId uniqueidentifier NOT NULL,
                LinkKind int NOT NULL CONSTRAINT DF_ApplicationProfileInstancePersonResolvedLinks_LinkKind DEFAULT (0),
                LinkedObjectId uniqueidentifier NOT NULL,
                CONSTRAINT FK_ApplicationProfileInstancePersonResolvedLinks_ApplicationProfileInstances_ApplicationProfileInstanceId
                    FOREIGN KEY (ApplicationProfileInstanceId) REFERENCES dbo.ApplicationProfileInstances(ID) ON DELETE CASCADE,
                CONSTRAINT FK_ApplicationProfileInstancePersonResolvedLinks_People_PersonId
                    FOREIGN KEY (PersonId) REFERENCES dbo.People(ID)
            );
            CREATE UNIQUE INDEX IX_ApplicationProfileInstancePersonResolvedLinks_Instance_Person_Kind
                ON dbo.ApplicationProfileInstancePersonResolvedLinks (ApplicationProfileInstanceId, PersonId, LinkKind);
        END;
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
            Execute(connection, EnsureSchemaPostgres);
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        Execute(sqlConnection, EnsureSchemaSqlServer);
    }

    private static void Execute(System.Data.Common.DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
