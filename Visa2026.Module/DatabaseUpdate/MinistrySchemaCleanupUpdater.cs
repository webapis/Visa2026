using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops the legacy <c>Ministries</c> table after the Ministry lookup BO is removed from the EF model.
/// </summary>
public sealed class MinistrySchemaCleanupUpdater : ModuleUpdater
{
    public MinistrySchemaCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        DropMinistriesTable();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        DropMinistriesTable();
    }

    private void DropMinistriesTable()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Ministries', N'U') IS NULL
    RETURN;

DECLARE @sql nvarchar(max);
SELECT @sql = STRING_AGG(
    CAST(N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
        + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) AS nvarchar(max)),
    N'; ')
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.Ministries');

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

DROP TABLE dbo.Ministries;", false);
    }
}
