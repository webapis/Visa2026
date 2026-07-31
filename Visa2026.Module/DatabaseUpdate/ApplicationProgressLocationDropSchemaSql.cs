namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops <c>ApplicationProgresses.LocationID</c> after progress became state-only.
/// </summary>
public static class ApplicationProgressLocationDropSchemaSql
{
    internal const string DropLocationFkAndColumnSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProgresses', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationProgresses', N'LocationID') IS NULL
            RETURN;

        DECLARE @fk sysname;
        SELECT TOP 1 @fk = fk.name
        FROM sys.foreign_keys fk
        INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE fk.parent_object_id = OBJECT_ID(N'dbo.ApplicationProgresses')
          AND c.name = N'LocationID';
        IF @fk IS NOT NULL
            EXEC(N'ALTER TABLE dbo.ApplicationProgresses DROP CONSTRAINT [' + @fk + N']');

        DECLARE @ix sysname;
        DECLARE ix_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT i.name
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID(N'dbo.ApplicationProgresses')
              AND c.name = N'LocationID'
              AND i.is_primary_key = 0;
        OPEN ix_cursor;
        FETCH NEXT FROM ix_cursor INTO @ix;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC(N'DROP INDEX [' + @ix + N'] ON dbo.ApplicationProgresses');
            FETCH NEXT FROM ix_cursor INTO @ix;
        END
        CLOSE ix_cursor;
        DEALLOCATE ix_cursor;

        IF COL_LENGTH(N'dbo.ApplicationProgresses', N'LocationID') IS NOT NULL
            ALTER TABLE dbo.ApplicationProgresses DROP COLUMN LocationID;
        """;

    internal const string DropLocationFkAndColumnPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProgresses"') IS NULL THEN
            RETURN;
          END IF;
          IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'ApplicationProgresses' AND column_name = 'LocationID')
          THEN
            ALTER TABLE "ApplicationProgresses" DROP COLUMN IF EXISTS "LocationID" CASCADE;
          END IF;
        END $$;
        """;
}