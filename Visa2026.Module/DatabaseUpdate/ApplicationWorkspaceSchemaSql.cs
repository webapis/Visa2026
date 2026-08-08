using System;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for Application workspace M2M tables (<see cref="BusinessObjects.ApplicationPerson"/>).
/// </summary>
public static class ApplicationWorkspaceSchemaSql
{
    internal const string EnsureSchemaPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationPeople"') IS NULL
             AND to_regclass('public."Applications"') IS NOT NULL
             AND to_regclass('public."People"') IS NOT NULL THEN
            CREATE TABLE "ApplicationPeople" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "ApplicationId" uuid NOT NULL,
                "PersonId" uuid NOT NULL,
                "LinkedAt" timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                CONSTRAINT "PK_ApplicationPeople" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_ApplicationPeople_Applications_ApplicationId"
                    FOREIGN KEY ("ApplicationId") REFERENCES "Applications" ("ID") ON DELETE CASCADE,
                CONSTRAINT "FK_ApplicationPeople_People_PersonId"
                    FOREIGN KEY ("PersonId") REFERENCES "People" ("ID") ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX "IX_ApplicationPeople_Application_Person"
                ON "ApplicationPeople" ("ApplicationId", "PersonId");
          END IF;

          IF to_regclass('public."ApplicationPersonResolvedLinks"') IS NULL
             AND to_regclass('public."ApplicationPeople"') IS NOT NULL THEN
            CREATE TABLE "ApplicationPersonResolvedLinks" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "ApplicationPersonId" uuid NOT NULL,
                "LinkKind" integer NOT NULL DEFAULT 0,
                "LinkedObjectId" uuid NOT NULL,
                CONSTRAINT "PK_ApplicationPersonResolvedLinks" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_ApplicationPersonResolvedLinks_ApplicationPeople_ApplicationPersonId"
                    FOREIGN KEY ("ApplicationPersonId") REFERENCES "ApplicationPeople" ("ID") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX "IX_ApplicationPersonResolvedLinks_PersonRow_Kind"
                ON "ApplicationPersonResolvedLinks" ("ApplicationPersonId", "LinkKind");
          END IF;
        END $$;
        """;

    internal const string EnsureSchemaSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationPeople', N'U') IS NULL
           AND OBJECT_ID(N'dbo.Applications', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.People', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE dbo.ApplicationPeople (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationPeople PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationPeople_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationPeople_OLF DEFAULT (0),
                ApplicationId uniqueidentifier NOT NULL,
                PersonId uniqueidentifier NOT NULL,
                LinkedAt datetime2 NOT NULL CONSTRAINT DF_ApplicationPeople_LinkedAt DEFAULT (SYSUTCDATETIME()),
                CONSTRAINT FK_ApplicationPeople_Applications_ApplicationId
                    FOREIGN KEY (ApplicationId) REFERENCES dbo.Applications(ID) ON DELETE CASCADE,
                CONSTRAINT FK_ApplicationPeople_People_PersonId
                    FOREIGN KEY (PersonId) REFERENCES dbo.People(ID)
            );
            CREATE UNIQUE INDEX IX_ApplicationPeople_Application_Person
                ON dbo.ApplicationPeople (ApplicationId, PersonId);
        END;

        IF OBJECT_ID(N'dbo.ApplicationPersonResolvedLinks', N'U') IS NULL
           AND OBJECT_ID(N'dbo.ApplicationPeople', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE dbo.ApplicationPersonResolvedLinks (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationPersonResolvedLinks PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationPersonResolvedLinks_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationPersonResolvedLinks_OLF DEFAULT (0),
                ApplicationPersonId uniqueidentifier NOT NULL,
                LinkKind int NOT NULL CONSTRAINT DF_ApplicationPersonResolvedLinks_LinkKind DEFAULT (0),
                LinkedObjectId uniqueidentifier NOT NULL,
                CONSTRAINT FK_ApplicationPersonResolvedLinks_ApplicationPeople_ApplicationPersonId
                    FOREIGN KEY (ApplicationPersonId) REFERENCES dbo.ApplicationPeople(ID) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IX_ApplicationPersonResolvedLinks_PersonRow_Kind
                ON dbo.ApplicationPersonResolvedLinks (ApplicationPersonId, LinkKind);
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
