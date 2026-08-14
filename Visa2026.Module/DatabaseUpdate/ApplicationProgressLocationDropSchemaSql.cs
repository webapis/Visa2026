namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops <c>ApplicationProfileInstanceProgresses.LocationID</c> after progress became state-only.
/// </summary>
public static class ApplicationProfileInstanceProgressLocationDropSchemaSql
{
    internal const string DropLocationFkAndColumnSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationProfileInstanceProgresses', N'LocationID') IS NULL
            RETURN;

        DECLARE @fk sysname;
        SELECT TOP 1 @fk = fk.name
        FROM sys.foreign_keys fk
        INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE fk.parent_object_id = OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses')
          AND c.name = N'LocationID';
        IF @fk IS NOT NULL
            EXEC(N'ALTER TABLE dbo.ApplicationProfileInstanceProgresses DROP CONSTRAINT [' + @fk + N']');

        DECLARE @ix sysname;
        DECLARE ix_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT i.name
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses')
              AND c.name = N'LocationID'
              AND i.is_primary_key = 0;
        OPEN ix_cursor;
        FETCH NEXT FROM ix_cursor INTO @ix;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC(N'DROP INDEX [' + @ix + N'] ON dbo.ApplicationProfileInstanceProgresses');
            FETCH NEXT FROM ix_cursor INTO @ix;
        END
        CLOSE ix_cursor;
        DEALLOCATE ix_cursor;

        IF COL_LENGTH(N'dbo.ApplicationProfileInstanceProgresses', N'LocationID') IS NOT NULL
            ALTER TABLE dbo.ApplicationProfileInstanceProgresses DROP COLUMN LocationID;
        """;

    internal const string DropLocationFkAndColumnPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileInstanceProgresses"') IS NULL THEN
            RETURN;
          END IF;
          IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'ApplicationProfileInstanceProgresses' AND column_name = 'LocationID')
          THEN
            ALTER TABLE "ApplicationProfileInstanceProgresses" DROP COLUMN IF EXISTS "LocationID" CASCADE;
          END IF;
        END $$;
        """;
}