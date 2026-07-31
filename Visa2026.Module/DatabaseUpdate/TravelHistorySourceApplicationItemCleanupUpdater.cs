using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Clears then drops <c>TravelHistories.SourceApplicationItemID</c>.
/// Travel history is officer-maintained only; registration applications no longer auto-link rows.
/// Existing travel history data is kept; only the FK is removed.
/// </summary>
public sealed class TravelHistorySourceApplicationItemCleanupUpdater : ModuleUpdater
{
    public TravelHistorySourceApplicationItemCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        ClearAndDropSourceApplicationItemColumn();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ClearAndDropSourceApplicationItemColumn();
    }

    private void ClearAndDropSourceApplicationItemColumn()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.TravelHistories', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.TravelHistories', N'SourceApplicationItemID') IS NULL
    RETURN;

UPDATE dbo.TravelHistories SET SourceApplicationItemID = NULL WHERE SourceApplicationItemID IS NOT NULL;

DECLARE @sql nvarchar(max);

SELECT @sql = STRING_AGG(
    CAST(N'ALTER TABLE dbo.TravelHistories DROP CONSTRAINT ' + QUOTENAME(fk.name) AS nvarchar(max)),
    N'; ')
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.TravelHistories')
  AND c.name = N'SourceApplicationItemID';

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

SELECT @sql = STRING_AGG(
    CAST(N'ALTER TABLE dbo.TravelHistories DROP CONSTRAINT ' + QUOTENAME(dc.name) AS nvarchar(max)),
    N'; ')
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TravelHistories')
  AND c.name = N'SourceApplicationItemID';

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

SELECT @sql = STRING_AGG(
    CAST(N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.TravelHistories' AS nvarchar(max)),
    N'; ')
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.TravelHistories')
  AND i.is_primary_key = 0
  AND c.name = N'SourceApplicationItemID';

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

ALTER TABLE dbo.TravelHistories DROP COLUMN [SourceApplicationItemID];", false);
    }
}
