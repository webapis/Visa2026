using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops <c>Invitation.IsCancelled</c> and <c>Invitation.IsChanged</c> columns; status lives on <see cref="BusinessObjects.InvitationItem"/> only.
/// </summary>
public sealed class InvitationHeaderStatusColumnsCleanupUpdater : ModuleUpdater
{
    public InvitationHeaderStatusColumnsCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        DropColumnIfExists("Invitations", "IsCancelled");
        DropColumnIfExists("Invitations", "IsChanged");
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        DropColumnIfExists("Invitations", "IsCancelled");
        DropColumnIfExists("Invitations", "IsChanged");
    }

    private void DropColumnIfExists(string tableName, string columnName)
    {
        ExecuteNonQueryCommand($@"
IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NULL
    RETURN;

DECLARE @sql nvarchar(max);

SELECT @sql = STRING_AGG(
    CAST(N'ALTER TABLE dbo.{tableName} DROP CONSTRAINT ' + QUOTENAME(fk.name) AS nvarchar(max)),
    N'; ')
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.{tableName}')
  AND c.name = N'{columnName}';

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

SELECT @sql = STRING_AGG(
    CAST(N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.{tableName}' AS nvarchar(max)),
    N'; ')
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.{tableName}')
  AND i.is_primary_key = 0
  AND c.name = N'{columnName}';

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

ALTER TABLE dbo.{tableName} DROP COLUMN [{columnName}];", false);
    }
}
