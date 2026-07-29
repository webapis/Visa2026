using System;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.Person"/> incomplete-data columns
/// (soft officer flag + missing-area checkboxes + notes).
/// Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).
/// </summary>
public static class PersonIncompleteDataSchemaSql
{
    internal const string EnsureColumnsSqlServer = """
        IF OBJECT_ID(N'dbo.People', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.People', N'IsDataIncomplete') IS NULL
            ALTER TABLE dbo.People ADD IsDataIncomplete bit NOT NULL
                CONSTRAINT DF_People_IsDataIncomplete DEFAULT (0);

        IF COL_LENGTH(N'dbo.People', N'IncompleteMissingPersonalData') IS NULL
            ALTER TABLE dbo.People ADD IncompleteMissingPersonalData bit NOT NULL
                CONSTRAINT DF_People_IncompleteMissingPersonalData DEFAULT (0);

        IF COL_LENGTH(N'dbo.People', N'IncompleteMissingPassport') IS NULL
            ALTER TABLE dbo.People ADD IncompleteMissingPassport bit NOT NULL
                CONSTRAINT DF_People_IncompleteMissingPassport DEFAULT (0);

        IF COL_LENGTH(N'dbo.People', N'IncompleteMissingCv') IS NULL
            ALTER TABLE dbo.People ADD IncompleteMissingCv bit NOT NULL
                CONSTRAINT DF_People_IncompleteMissingCv DEFAULT (0);

        IF COL_LENGTH(N'dbo.People', N'IncompleteMissingPhoto') IS NULL
            ALTER TABLE dbo.People ADD IncompleteMissingPhoto bit NOT NULL
                CONSTRAINT DF_People_IncompleteMissingPhoto DEFAULT (0);

        IF COL_LENGTH(N'dbo.People', N'IncompleteMissingEducation') IS NULL
            ALTER TABLE dbo.People ADD IncompleteMissingEducation bit NOT NULL
                CONSTRAINT DF_People_IncompleteMissingEducation DEFAULT (0);

        IF COL_LENGTH(N'dbo.People', N'IncompleteMissingMedical') IS NULL
            ALTER TABLE dbo.People ADD IncompleteMissingMedical bit NOT NULL
                CONSTRAINT DF_People_IncompleteMissingMedical DEFAULT (0);

        IF COL_LENGTH(N'dbo.People', N'IncompleteMissingAddress') IS NULL
            ALTER TABLE dbo.People ADD IncompleteMissingAddress bit NOT NULL
                CONSTRAINT DF_People_IncompleteMissingAddress DEFAULT (0);

        IF COL_LENGTH(N'dbo.People', N'IncompleteMissingFamilyDocs') IS NULL
            ALTER TABLE dbo.People ADD IncompleteMissingFamilyDocs bit NOT NULL
                CONSTRAINT DF_People_IncompleteMissingFamilyDocs DEFAULT (0);

        IF COL_LENGTH(N'dbo.People', N'IncompleteMissingOther') IS NULL
            ALTER TABLE dbo.People ADD IncompleteMissingOther bit NOT NULL
                CONSTRAINT DF_People_IncompleteMissingOther DEFAULT (0);

        IF COL_LENGTH(N'dbo.People', N'IncompleteNotes') IS NULL
            ALTER TABLE dbo.People ADD IncompleteNotes nvarchar(max) NULL;

        IF COL_LENGTH(N'dbo.People', N'IncompleteMarkedOn') IS NULL
            ALTER TABLE dbo.People ADD IncompleteMarkedOn datetime2 NULL;

        IF COL_LENGTH(N'dbo.People', N'IncompleteMarkedBy') IS NULL
            ALTER TABLE dbo.People ADD IncompleteMarkedBy nvarchar(255) NULL;
        """;

    internal const string EnsureColumnsPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."People"') IS NULL THEN
            RETURN;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IsDataIncomplete')
          THEN
            ALTER TABLE "People" ADD COLUMN "IsDataIncomplete" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteMissingPersonalData')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteMissingPersonalData" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteMissingPassport')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteMissingPassport" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteMissingCv')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteMissingCv" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteMissingPhoto')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteMissingPhoto" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteMissingEducation')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteMissingEducation" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteMissingMedical')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteMissingMedical" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteMissingAddress')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteMissingAddress" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteMissingFamilyDocs')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteMissingFamilyDocs" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteMissingOther')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteMissingOther" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteNotes')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteNotes" text NULL;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteMarkedOn')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteMarkedOn" timestamp without time zone NULL;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'IncompleteMarkedBy')
          THEN
            ALTER TABLE "People" ADD COLUMN "IncompleteMarkedBy" character varying(255) NULL;
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
            command.CommandText = EnsureColumnsPostgres;
            command.ExecuteNonQuery();
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        using var sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.CommandText = EnsureColumnsSqlServer;
        sqlCommand.ExecuteNonQuery();
    }
}