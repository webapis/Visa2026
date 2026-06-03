using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Drops removed <c>ContactPerson</c> and <c>PhoneNumber</c> columns from <see cref="BusinessObjects.Subcontractor"/>.</summary>
public sealed class SubcontractorContactColumnsCleanupUpdater : ModuleUpdater
{
    public SubcontractorContactColumnsCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        DropColumnIfExists("Subcontractors", "ContactPerson");
        DropColumnIfExists("Subcontractors", "PhoneNumber");
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        DropColumnIfExists("Subcontractors", "ContactPerson");
        DropColumnIfExists("Subcontractors", "PhoneNumber");
    }

    private void DropColumnIfExists(string tableName, string columnName)
    {
        ExecuteNonQueryCommand($@"
IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NULL
    RETURN;

DECLARE @sql nvarchar(max);
SELECT @sql = N'ALTER TABLE dbo.{tableName} DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.{tableName}')
  AND c.name = N'{columnName}';
IF @sql IS NOT NULL
    EXEC sp_executesql @sql;

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
    WHERE fk.parent_object_id = OBJECT_ID(N'dbo.{tableName}') AND c.name = N'{columnName}')
BEGIN
    SELECT @sql = N'ALTER TABLE dbo.{tableName} DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
    WHERE fk.parent_object_id = OBJECT_ID(N'dbo.{tableName}') AND c.name = N'{columnName}';
    EXEC sp_executesql @sql;
END

ALTER TABLE dbo.{tableName} DROP COLUMN [{columnName}];", false);
    }
}
