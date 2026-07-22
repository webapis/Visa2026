using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops obsolete <c>OrganizationTypes</c> and any leftover <c>OrganizationTypeID</c> FKs
/// after <c>OrganizationType</c> is removed from the EF model.
/// </summary>
public sealed class OrganizationTypeSchemaCleanupUpdater : ModuleUpdater
{
    public OrganizationTypeSchemaCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        DropOrganizationTypeArtifacts();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        DropOrganizationTypeArtifacts();
    }

    private void DropOrganizationTypeArtifacts()
    {
        DropColumnIfExists("Applications", "OrganizationTypeID");
        DropColumnIfExists("ApplicationTypes", "OrganizationTypeID");
        DropOrganizationTypesTable();
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

    private void DropOrganizationTypesTable()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.OrganizationTypes', N'U') IS NULL
    RETURN;

DECLARE @sql nvarchar(max);
SELECT @sql = STRING_AGG(
    CAST(N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
        + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) AS nvarchar(max)),
    N'; ')
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.OrganizationTypes');

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

DROP TABLE dbo.OrganizationTypes;", false);
    }
}