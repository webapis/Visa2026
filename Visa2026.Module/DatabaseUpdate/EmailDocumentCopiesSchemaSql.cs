using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent PostgreSQL DDL for the email document copies tables (<c>EmailContacts</c>,
/// <c>EmailTemplates</c>, <c>SentEmails</c>, <c>SentEmailPersons</c>, <c>OfficerMailConnections</c>,
/// <c>EmailSmtpSettings</c>) and the officer <c>Email</c> column on <c>PermissionPolicyUser</c>.
/// </summary>
/// <remarks>
/// No EF migrations in this repository: when <c>ModuleInfo</c> is already current XAF skips the schema
/// pass, so new tables ship with a ModuleUpdater plus a host-start heal (same as
/// <see cref="PersonExportBatchSchemaSql"/>). Column shapes must match what EF EnsureCreated emits.
/// </remarks>
public static class EmailDocumentCopiesSchemaSql
{
    internal const string EnsureTablesPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."EmailContacts"') IS NULL THEN
            CREATE TABLE "EmailContacts" (
              "ID" uuid NOT NULL CONSTRAINT "PK_EmailContacts" PRIMARY KEY,
              "Name" character varying(256) NULL,
              "Organization" character varying(256) NULL,
              "Email" character varying(320) NULL,
              "IsShared" boolean NOT NULL DEFAULT FALSE,
              "OwnerID" uuid NULL,
              "TimesUsed" integer NOT NULL DEFAULT 0,
              "LastUsedOnUtc" timestamp without time zone NULL,
              "Notes" character varying(1024) NULL,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
            CREATE INDEX "IX_EmailContacts_OwnerID" ON "EmailContacts" ("OwnerID");
          END IF;

          IF to_regclass('public."EmailTemplates"') IS NULL THEN
            CREATE TABLE "EmailTemplates" (
              "ID" uuid NOT NULL CONSTRAINT "PK_EmailTemplates" PRIMARY KEY,
              "Name" character varying(256) NULL,
              "Subject" character varying(512) NULL,
              "Body" text NULL,
              "IsDefault" boolean NOT NULL DEFAULT FALSE,
              "SortOrder" integer NOT NULL DEFAULT 0,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
          END IF;

          IF to_regclass('public."SentEmails"') IS NULL THEN
            CREATE TABLE "SentEmails" (
              "ID" uuid NOT NULL CONSTRAINT "PK_SentEmails" PRIMARY KEY,
              "SentOnUtc" timestamp without time zone NOT NULL,
              "Status" integer NOT NULL DEFAULT 0,
              "SentByID" uuid NULL,
              "SentByName" character varying(256) NULL,
              "FromAddress" character varying(320) NULL,
              "ToAddresses" text NULL,
              "CcAddresses" text NULL,
              "Subject" character varying(512) NULL,
              "Body" text NULL,
              "ErrorMessage" character varying(2048) NULL,
              "PersonCount" integer NOT NULL DEFAULT 0,
              "AttachmentCount" integer NOT NULL DEFAULT 0,
              "AttachmentTotalBytes" bigint NOT NULL DEFAULT 0,
              "AttachmentNames" text NULL,
              "SourceDescription" character varying(256) NULL,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
            CREATE INDEX "IX_SentEmails_SentByID" ON "SentEmails" ("SentByID");
            CREATE INDEX "IX_SentEmails_SentOnUtc" ON "SentEmails" ("SentOnUtc");
          END IF;

          IF to_regclass('public."SentEmailPersons"') IS NULL THEN
            CREATE TABLE "SentEmailPersons" (
              "ID" uuid NOT NULL CONSTRAINT "PK_SentEmailPersons" PRIMARY KEY,
              "SentEmailID" uuid NULL,
              "PersonID" uuid NULL,
              "PersonDisplayName" character varying(512) NULL,
              "RecordLabels" text NULL,
              "RecordKeys" text NULL,
              "AttachmentFileName" character varying(512) NULL,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
            CREATE INDEX "IX_SentEmailPersons_SentEmailID" ON "SentEmailPersons" ("SentEmailID");
            CREATE INDEX "IX_SentEmailPersons_PersonID" ON "SentEmailPersons" ("PersonID");
            ALTER TABLE "SentEmailPersons"
              ADD CONSTRAINT "FK_SentEmailPersons_SentEmails_SentEmailID"
              FOREIGN KEY ("SentEmailID") REFERENCES "SentEmails"("ID") ON DELETE CASCADE;
          END IF;

          IF to_regclass('public."OfficerMailConnections"') IS NULL THEN
            CREATE TABLE "OfficerMailConnections" (
              "ID" uuid NOT NULL CONSTRAINT "PK_OfficerMailConnections" PRIMARY KEY,
              "UserID" uuid NOT NULL,
              "AccountEmail" character varying(320) NULL,
              "AccountDisplayName" character varying(256) NULL,
              "ProtectedRefreshToken" text NULL,
              "ConnectedOnUtc" timestamp without time zone NOT NULL,
              "LastUsedOnUtc" timestamp without time zone NULL,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
            CREATE INDEX "IX_OfficerMailConnections_UserID" ON "OfficerMailConnections" ("UserID");
          END IF;

          ALTER TABLE "SentEmails" ADD COLUMN IF NOT EXISTS "BatchId" uuid NULL;
          ALTER TABLE "SentEmails" ADD COLUMN IF NOT EXISTS "PartNumber" integer NOT NULL DEFAULT 1;
          ALTER TABLE "SentEmails" ADD COLUMN IF NOT EXISTS "PartCount" integer NOT NULL DEFAULT 1;
          ALTER TABLE "EmailContacts" ADD COLUMN IF NOT EXISTS "MaxEmailSizeMb" integer NULL;

          IF to_regclass('public."EmailSmtpSettings"') IS NULL THEN
            CREATE TABLE "EmailSmtpSettings" (
              "ID" uuid NOT NULL CONSTRAINT "PK_EmailSmtpSettings" PRIMARY KEY,
              "Host" character varying(256) NULL,
              "Port" integer NOT NULL DEFAULT 587,
              "Security" integer NOT NULL DEFAULT 0,
              "UserName" character varying(320) NULL,
              "ProtectedPassword" text NULL,
              "FromAddress" character varying(320) NULL,
              "FromName" character varying(256) NULL,
              "BccOfficer" boolean NOT NULL DEFAULT TRUE,
              "MaxTotalAttachmentBytes" bigint NOT NULL DEFAULT 20971520,
              "TimeoutSeconds" integer NOT NULL DEFAULT 120,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
          END IF;

          IF to_regclass('public."PermissionPolicyUser"') IS NOT NULL THEN
            ALTER TABLE "PermissionPolicyUser" ADD COLUMN IF NOT EXISTS "Email" character varying(320) NULL;
          END IF;
        END $$;
        """;

    /// <summary>Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).</summary>
    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || !DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        // Greenfield: creating these tables alone makes EF skip EnsureCreated (no users / no other tables).
        if (!PostgresRelationExists.All(connectionString, "People"))
            return;

        using var connection = new NpgsqlConnection(DatabaseProviderDetector.StripEfCoreProvider(connectionString));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = EnsureTablesPostgres;
        command.ExecuteNonQuery();
    }
}
