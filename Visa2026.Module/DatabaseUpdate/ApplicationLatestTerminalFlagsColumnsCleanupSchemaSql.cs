namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops denormalized <c>Applications.LatestIsCancelled</c> and <c>LatestIsRejected</c>;
/// terminal workflow state lives on <see cref="BusinessObjects.ApplicationProgress"/> only.
/// </summary>
public static class ApplicationLatestTerminalFlagsColumnsCleanupSchemaSql
{
    internal const string DropColumnsSqlServer = """
        IF OBJECT_ID(N'dbo.Applications', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.Applications', N'LatestIsCancelled') IS NOT NULL
        BEGIN
            DECLARE @dfCancelled sysname;
            SELECT @dfCancelled = dc.name
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Applications')
              AND c.name = N'LatestIsCancelled';
            IF @dfCancelled IS NOT NULL
                EXEC(N'ALTER TABLE dbo.Applications DROP CONSTRAINT [' + @dfCancelled + N']');
            ALTER TABLE dbo.Applications DROP COLUMN LatestIsCancelled;
        END;

        IF COL_LENGTH(N'dbo.Applications', N'LatestIsRejected') IS NOT NULL
        BEGIN
            DECLARE @dfRejected sysname;
            SELECT @dfRejected = dc.name
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Applications')
              AND c.name = N'LatestIsRejected';
            IF @dfRejected IS NOT NULL
                EXEC(N'ALTER TABLE dbo.Applications DROP CONSTRAINT [' + @dfRejected + N']');
            ALTER TABLE dbo.Applications DROP COLUMN LatestIsRejected;
        END;
        """;

    internal const string DropColumnsPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."Applications"') IS NULL THEN
            RETURN;
          END IF;
          IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'Applications' AND column_name = 'LatestIsCancelled')
          THEN
            ALTER TABLE "Applications" DROP COLUMN IF EXISTS "LatestIsCancelled" CASCADE;
          END IF;
          IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'Applications' AND column_name = 'LatestIsRejected')
          THEN
            ALTER TABLE "Applications" DROP COLUMN IF EXISTS "LatestIsRejected" CASCADE;
          END IF;
        END $$;
        """;
}
