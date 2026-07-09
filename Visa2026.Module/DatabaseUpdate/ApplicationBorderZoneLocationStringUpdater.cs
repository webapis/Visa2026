using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Migrates Application border zone from FK (BorderZoneLocationID) to comma-separated
/// BorderZoneLocation nvarchar backed by BorderZoneName.
/// </summary>
public sealed class ApplicationBorderZoneLocationStringUpdater : ModuleUpdater
{
    public ApplicationBorderZoneLocationStringUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        EnsureApplicationBorderZoneLocationColumn();
        CopyBorderZoneLookupToApplicationString();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        DropApplicationBorderZoneLocationForeignKey();
        NormalizeEmptyApplicationBorderZones();
    }

    private void EnsureApplicationBorderZoneLocationColumn()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Applications', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.Applications', N'BorderZoneLocation') IS NULL
    ALTER TABLE dbo.Applications ADD BorderZoneLocation nvarchar(500) NULL;", false);
    }

    private void CopyBorderZoneLookupToApplicationString()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Applications', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.Applications', N'BorderZoneLocation') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.Applications', N'BorderZoneLocationID') IS NULL
   AND COL_LENGTH(N'dbo.Applications', N'BorderZoneLocationId') IS NULL
    RETURN;
IF OBJECT_ID(N'dbo.BorderZoneLocations', N'U') IS NULL
    RETURN;

DECLARE @fkColumn sysname = CASE
    WHEN COL_LENGTH(N'dbo.Applications', N'BorderZoneLocationID') IS NOT NULL THEN N'BorderZoneLocationID'
    ELSE N'BorderZoneLocationId' END;

DECLARE @copySql nvarchar(max) = N'
UPDATE a
SET a.BorderZoneLocation = COALESCE(NULLIF(LTRIM(RTRIM(bzl.NameTm)), N''''), N''Ýok'')
FROM dbo.Applications a
LEFT JOIN dbo.BorderZoneLocations bzl ON bzl.ID = a.' + QUOTENAME(@fkColumn) + N';

UPDATE dbo.Applications
SET BorderZoneLocation = N''Ýok''
WHERE BorderZoneLocation IS NULL OR LTRIM(RTRIM(BorderZoneLocation)) = N'''';';

EXEC sys.sp_executesql @copySql;", false);
    }

    private void DropApplicationBorderZoneLocationForeignKey()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Applications', N'U') IS NULL
    RETURN;

DECLARE @fkColumn sysname = CASE
    WHEN COL_LENGTH(N'dbo.Applications', N'BorderZoneLocationID') IS NOT NULL THEN N'BorderZoneLocationID'
    WHEN COL_LENGTH(N'dbo.Applications', N'BorderZoneLocationId') IS NOT NULL THEN N'BorderZoneLocationId'
    ELSE NULL END;

IF @fkColumn IS NULL
    RETURN;

DECLARE @sql nvarchar(max);

SELECT @sql = STRING_AGG(
    CAST(N'ALTER TABLE dbo.Applications DROP CONSTRAINT ' + QUOTENAME(fk.name) AS nvarchar(max)),
    N'; ')
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Applications')
  AND c.name = @fkColumn;

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

DECLARE @dropColumnSql nvarchar(max) =
    N'ALTER TABLE dbo.Applications DROP COLUMN ' + QUOTENAME(@fkColumn) + N';';
EXEC sys.sp_executesql @dropColumnSql;", false);
    }

    private void NormalizeEmptyApplicationBorderZones()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Applications', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.Applications', N'BorderZoneLocation') IS NULL
    RETURN;

UPDATE dbo.Applications
SET BorderZoneLocation = N'Ýok'
WHERE BorderZoneLocation IS NULL OR LTRIM(RTRIM(BorderZoneLocation)) = N'';", false);
    }
}