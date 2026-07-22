namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ApplicationType"/> capability flags:
/// <c>CanIssueVisa</c>, <c>CanIssueInvitation</c>, <c>CanIssueWorkPermit</c>.
/// </summary>
public static class ApplicationTypeCapabilityFlagsSchemaSql
{
    internal const string EnsureColumnsSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationTypes', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.ApplicationTypes', N'CanIssueVisa') IS NULL
        BEGIN
            ALTER TABLE dbo.ApplicationTypes
                ADD CanIssueVisa bit NOT NULL
                    CONSTRAINT DF_ApplicationTypes_CanIssueVisa DEFAULT (0);
        END;

        IF COL_LENGTH(N'dbo.ApplicationTypes', N'CanIssueInvitation') IS NULL
        BEGIN
            ALTER TABLE dbo.ApplicationTypes
                ADD CanIssueInvitation bit NOT NULL
                    CONSTRAINT DF_ApplicationTypes_CanIssueInvitation DEFAULT (0);
        END;

        IF COL_LENGTH(N'dbo.ApplicationTypes', N'CanIssueWorkPermit') IS NULL
        BEGIN
            ALTER TABLE dbo.ApplicationTypes
                ADD CanIssueWorkPermit bit NOT NULL
                    CONSTRAINT DF_ApplicationTypes_CanIssueWorkPermit DEFAULT (0);
        END;
        """;

    internal const string EnsureColumnsPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationTypes"') IS NULL THEN
            RETURN;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'ApplicationTypes' AND column_name = 'CanIssueVisa')
          THEN
            ALTER TABLE "ApplicationTypes"
              ADD COLUMN "CanIssueVisa" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'ApplicationTypes' AND column_name = 'CanIssueInvitation')
          THEN
            ALTER TABLE "ApplicationTypes"
              ADD COLUMN "CanIssueInvitation" boolean NOT NULL DEFAULT false;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'ApplicationTypes' AND column_name = 'CanIssueWorkPermit')
          THEN
            ALTER TABLE "ApplicationTypes"
              ADD COLUMN "CanIssueWorkPermit" boolean NOT NULL DEFAULT false;
          END IF;
        END $$;
        """;
}