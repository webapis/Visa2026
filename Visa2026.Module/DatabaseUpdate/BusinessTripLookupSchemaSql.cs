using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.BusinessTripAddress"/> and
/// <see cref="BusinessObjects.BusinessTripPurpose"/> lookup tables.
/// </summary>
/// <remarks>
/// These types existed as XAF BOs but had no <c>DbSet</c> until Application Profile case-summary
/// lookups queried them. When ModuleInfo is already current, EF EnsureCreated does not run again.
/// </remarks>
public static class BusinessTripLookupSchemaSql
{
    internal const string EnsureTablesPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."BusinessTripAddress"') IS NULL
             AND to_regclass('public."Cities"') IS NOT NULL THEN
            CREATE TABLE "BusinessTripAddress" (
                "ID" uuid NOT NULL CONSTRAINT "PK_BusinessTripAddress" PRIMARY KEY,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "FullAddress" character varying(255) NULL,
                "CityID" uuid NULL,
                CONSTRAINT "FK_BusinessTripAddress_Cities_CityID"
                    FOREIGN KEY ("CityID") REFERENCES "Cities" ("ID")
            );
            CREATE INDEX "IX_BusinessTripAddress_CityID" ON "BusinessTripAddress" ("CityID");
          END IF;

          IF to_regclass('public."BusinessTripPurpose"') IS NULL THEN
            CREATE TABLE "BusinessTripPurpose" (
                "ID" uuid NOT NULL CONSTRAINT "PK_BusinessTripPurpose" PRIMARY KEY,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "Name" character varying(200) NULL,
                "Description" character varying(2000) NULL
            );
          END IF;
        END $$;
        """;

    internal const string EnsureTablesSqlServer = """
        IF OBJECT_ID(N'dbo.BusinessTripAddress', N'U') IS NULL
           AND OBJECT_ID(N'dbo.Cities', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE dbo.BusinessTripAddress (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_BusinessTripAddress PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_BusinessTripAddress_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_BusinessTripAddress_OLF DEFAULT (0),
                FullAddress nvarchar(255) NULL,
                CityID uniqueidentifier NULL,
                CONSTRAINT FK_BusinessTripAddress_Cities_CityID FOREIGN KEY (CityID) REFERENCES dbo.Cities (ID)
            );
            CREATE INDEX IX_BusinessTripAddress_CityID ON dbo.BusinessTripAddress (CityID);
        END;

        IF OBJECT_ID(N'dbo.BusinessTripPurpose', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.BusinessTripPurpose (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_BusinessTripPurpose PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_BusinessTripPurpose_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_BusinessTripPurpose_OLF DEFAULT (0),
                Name nvarchar(200) NULL,
                Description nvarchar(2000) NULL
            );
        END;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        if (DatabaseProviderDetector.IsPostgreSql(connectionString)
            && !PostgresRelationExists.All(connectionString, "People"))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (DatabaseProviderDetector.IsPostgreSql(connectionString))
        {
            using var connection = new NpgsqlConnection(cleaned);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = EnsureTablesPostgres;
            command.ExecuteNonQuery();
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        using var sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.CommandText = EnsureTablesSqlServer;
        sqlCommand.ExecuteNonQuery();
    }
}
