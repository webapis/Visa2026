using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.AlternativeAddressesForInvitation"/> and
/// <see cref="BusinessObjects.InvitationAddress"/>.
/// </summary>
/// <remarks>
/// Case summary loads the catalog on every create-wizard step. When ModuleInfo is already
/// current, EF does not create the new tables, so the query fails with 42P01.
/// </remarks>
public static class InvitationAddressSchemaSql
{
    internal const string EnsureTablesPostgres = """
        CREATE TABLE IF NOT EXISTS "AlternativeAddressesForInvitation" (
            "ID" uuid NOT NULL CONSTRAINT "PK_AlternativeAddressesForInvitation" PRIMARY KEY,
            "GCRecord" integer NOT NULL DEFAULT 0,
            "OptimisticLockField" integer NOT NULL DEFAULT 0,
            "AddressLine" character varying(2000) NULL
        );

        DO $$
        BEGIN
          IF to_regclass('public."InvitationAddress"') IS NULL
             AND to_regclass('public."AlternativeAddressesForInvitation"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstances"') IS NOT NULL
             AND to_regclass('public."Regions"') IS NOT NULL
             AND to_regclass('public."Cities"') IS NOT NULL
          THEN
            CREATE TABLE "InvitationAddress" (
                "ID" uuid NOT NULL CONSTRAINT "PK_InvitationAddress" PRIMARY KEY,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "ApplicationProfileInstanceId" uuid NULL,
                "RegionId" uuid NULL,
                "CityId" uuid NULL,
                "Type" integer NULL,
                "LodgingId" uuid NULL,
                "HotelId" uuid NULL,
                "HospitalId" uuid NULL,
                "OtherSiteId" uuid NULL,
                "PrivateHouseAddress" character varying(255) NULL,
                "AlternativeAddressId" uuid NULL,
                CONSTRAINT "FK_InvitationAddress_Instance"
                    FOREIGN KEY ("ApplicationProfileInstanceId") REFERENCES "ApplicationProfileInstances" ("ID") ON DELETE CASCADE,
                CONSTRAINT "FK_InvitationAddress_Region"
                    FOREIGN KEY ("RegionId") REFERENCES "Regions" ("ID"),
                CONSTRAINT "FK_InvitationAddress_City"
                    FOREIGN KEY ("CityId") REFERENCES "Cities" ("ID"),
                CONSTRAINT "FK_InvitationAddress_Alternative"
                    FOREIGN KEY ("AlternativeAddressId") REFERENCES "AlternativeAddressesForInvitation" ("ID") ON DELETE SET NULL,
                CONSTRAINT "FK_InvitationAddress_Lodging"
                    FOREIGN KEY ("LodgingId") REFERENCES "Lodgings" ("ID"),
                CONSTRAINT "FK_InvitationAddress_Hotel"
                    FOREIGN KEY ("HotelId") REFERENCES "Hotels" ("ID"),
                CONSTRAINT "FK_InvitationAddress_Hospital"
                    FOREIGN KEY ("HospitalId") REFERENCES "Hospitals" ("ID"),
                CONSTRAINT "FK_InvitationAddress_OtherSite"
                    FOREIGN KEY ("OtherSiteId") REFERENCES "OtherSites" ("ID")
            );
            CREATE UNIQUE INDEX "IX_InvitationAddress_ApplicationProfileInstanceId"
                ON "InvitationAddress" ("ApplicationProfileInstanceId")
                WHERE "ApplicationProfileInstanceId" IS NOT NULL AND "GCRecord" IS NULL;
            CREATE INDEX "IX_InvitationAddress_RegionId" ON "InvitationAddress" ("RegionId");
            CREATE INDEX "IX_InvitationAddress_CityId" ON "InvitationAddress" ("CityId");
            CREATE INDEX "IX_InvitationAddress_AlternativeAddressId" ON "InvitationAddress" ("AlternativeAddressId");
            CREATE INDEX "IX_InvitationAddress_LodgingId" ON "InvitationAddress" ("LodgingId");
            CREATE INDEX "IX_InvitationAddress_HotelId" ON "InvitationAddress" ("HotelId");
            CREATE INDEX "IX_InvitationAddress_HospitalId" ON "InvitationAddress" ("HospitalId");
            CREATE INDEX "IX_InvitationAddress_OtherSiteId" ON "InvitationAddress" ("OtherSiteId");
          END IF;
        END $$;

        DO $$
        BEGIN
          IF to_regclass('public."AlternativeAddressesForInvitation"') IS NOT NULL THEN
            UPDATE "AlternativeAddressesForInvitation" SET "GCRecord" = 0 WHERE "GCRecord" IS NULL;
            ALTER TABLE "AlternativeAddressesForInvitation" ALTER COLUMN "GCRecord" SET DEFAULT 0;
            ALTER TABLE "AlternativeAddressesForInvitation" ALTER COLUMN "GCRecord" SET NOT NULL;
          END IF;
          IF to_regclass('public."InvitationAddress"') IS NOT NULL THEN
            ALTER TABLE "InvitationAddress" ADD COLUMN IF NOT EXISTS "Type" integer NULL;
            ALTER TABLE "InvitationAddress" ADD COLUMN IF NOT EXISTS "LodgingId" uuid NULL;
            ALTER TABLE "InvitationAddress" ADD COLUMN IF NOT EXISTS "HotelId" uuid NULL;
            ALTER TABLE "InvitationAddress" ADD COLUMN IF NOT EXISTS "HospitalId" uuid NULL;
            ALTER TABLE "InvitationAddress" ADD COLUMN IF NOT EXISTS "OtherSiteId" uuid NULL;
            ALTER TABLE "InvitationAddress" ADD COLUMN IF NOT EXISTS "PrivateHouseAddress" character varying(255) NULL;
            UPDATE "InvitationAddress" SET "GCRecord" = 0 WHERE "GCRecord" IS NULL;
            ALTER TABLE "InvitationAddress" ALTER COLUMN "GCRecord" SET DEFAULT 0;
            ALTER TABLE "InvitationAddress" ALTER COLUMN "GCRecord" SET NOT NULL;

            IF to_regclass('public."Lodgings"') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_InvitationAddress_Lodging') THEN
              ALTER TABLE "InvitationAddress"
                ADD CONSTRAINT "FK_InvitationAddress_Lodging"
                FOREIGN KEY ("LodgingId") REFERENCES "Lodgings" ("ID");
              CREATE INDEX IF NOT EXISTS "IX_InvitationAddress_LodgingId" ON "InvitationAddress" ("LodgingId");
            END IF;
            IF to_regclass('public."Hotels"') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_InvitationAddress_Hotel') THEN
              ALTER TABLE "InvitationAddress"
                ADD CONSTRAINT "FK_InvitationAddress_Hotel"
                FOREIGN KEY ("HotelId") REFERENCES "Hotels" ("ID");
              CREATE INDEX IF NOT EXISTS "IX_InvitationAddress_HotelId" ON "InvitationAddress" ("HotelId");
            END IF;
            IF to_regclass('public."Hospitals"') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_InvitationAddress_Hospital') THEN
              ALTER TABLE "InvitationAddress"
                ADD CONSTRAINT "FK_InvitationAddress_Hospital"
                FOREIGN KEY ("HospitalId") REFERENCES "Hospitals" ("ID");
              CREATE INDEX IF NOT EXISTS "IX_InvitationAddress_HospitalId" ON "InvitationAddress" ("HospitalId");
            END IF;
            IF to_regclass('public."OtherSites"') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_InvitationAddress_OtherSite') THEN
              ALTER TABLE "InvitationAddress"
                ADD CONSTRAINT "FK_InvitationAddress_OtherSite"
                FOREIGN KEY ("OtherSiteId") REFERENCES "OtherSites" ("ID");
              CREATE INDEX IF NOT EXISTS "IX_InvitationAddress_OtherSiteId" ON "InvitationAddress" ("OtherSiteId");
            END IF;
          END IF;
        END $$;
        """;

    internal const string EnsureTablesSqlServer = """
        IF OBJECT_ID(N'dbo.AlternativeAddressesForInvitation', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.AlternativeAddressesForInvitation (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_AlternativeAddressesForInvitation PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_AlternativeAddressesForInvitation_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_AlternativeAddressesForInvitation_OLF DEFAULT (0),
                AddressLine nvarchar(2000) NULL
            );
        END;

        IF OBJECT_ID(N'dbo.InvitationAddress', N'U') IS NULL
           AND OBJECT_ID(N'dbo.AlternativeAddressesForInvitation', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.Regions', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.Cities', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE dbo.InvitationAddress (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_InvitationAddress PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_InvitationAddress_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_InvitationAddress_OLF DEFAULT (0),
                ApplicationProfileInstanceId uniqueidentifier NULL,
                RegionId uniqueidentifier NULL,
                CityId uniqueidentifier NULL,
                Type int NULL,
                LodgingId uniqueidentifier NULL,
                HotelId uniqueidentifier NULL,
                HospitalId uniqueidentifier NULL,
                OtherSiteId uniqueidentifier NULL,
                PrivateHouseAddress nvarchar(255) NULL,
                AlternativeAddressId uniqueidentifier NULL,
                CONSTRAINT FK_InvitationAddress_Instance
                    FOREIGN KEY (ApplicationProfileInstanceId) REFERENCES dbo.ApplicationProfileInstances (ID) ON DELETE CASCADE,
                CONSTRAINT FK_InvitationAddress_Region
                    FOREIGN KEY (RegionId) REFERENCES dbo.Regions (ID),
                CONSTRAINT FK_InvitationAddress_City
                    FOREIGN KEY (CityId) REFERENCES dbo.Cities (ID),
                CONSTRAINT FK_InvitationAddress_Alternative
                    FOREIGN KEY (AlternativeAddressId) REFERENCES dbo.AlternativeAddressesForInvitation (ID) ON DELETE SET NULL
            );
            CREATE UNIQUE INDEX IX_InvitationAddress_ApplicationProfileInstanceId
                ON dbo.InvitationAddress (ApplicationProfileInstanceId)
                WHERE ApplicationProfileInstanceId IS NOT NULL AND GCRecord IS NULL;
            CREATE INDEX IX_InvitationAddress_RegionId ON dbo.InvitationAddress (RegionId);
            CREATE INDEX IX_InvitationAddress_CityId ON dbo.InvitationAddress (CityId);
            CREATE INDEX IX_InvitationAddress_AlternativeAddressId ON dbo.InvitationAddress (AlternativeAddressId);
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
