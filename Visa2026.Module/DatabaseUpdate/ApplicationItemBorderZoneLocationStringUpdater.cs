using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;
namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Migrates <see cref="ApplicationItem"/> border zone from FK (<c>BorderZoneLocationID</c>) to comma-separated
/// <c>BorderZoneLocation</c> nvarchar, and seeds <see cref="BorderZoneName"/> catalog entries.
/// </summary>
public sealed class ApplicationItemBorderZoneLocationStringUpdater : ModuleUpdater
{
    private static readonly string[] DefaultBorderZoneNames =
    {
        "Serhetabat etr",
        "Garabogaz şäheri",
        "Sarahs etraby",
    };

    public ApplicationItemBorderZoneLocationStringUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        EnsureApplicationItemBorderZoneLocationColumn();
        CopyBorderZoneLookupToApplicationItemString();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        DropApplicationItemBorderZoneLocationForeignKey();
        SeedBorderZoneNames();
        NormalizeEmptyApplicationItemBorderZones();
    }

    private void EnsureApplicationItemBorderZoneLocationColumn()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.ApplicationItems', N'BorderZoneLocation') IS NULL
    ALTER TABLE dbo.ApplicationItems ADD BorderZoneLocation nvarchar(500) NULL;", false);
    }

    private void CopyBorderZoneLookupToApplicationItemString()
    {
        // Dynamic SQL: referencing BorderZoneLocationID in a static batch fails compile when the column
        // was already dropped or never existed (SQL Server validates the whole batch).
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.ApplicationItems', N'BorderZoneLocation') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.ApplicationItems', N'BorderZoneLocationID') IS NULL
   AND COL_LENGTH(N'dbo.ApplicationItems', N'BorderZoneLocationId') IS NULL
    RETURN;
IF OBJECT_ID(N'dbo.BorderZoneLocations', N'U') IS NULL
    RETURN;

DECLARE @fkColumn sysname = CASE
    WHEN COL_LENGTH(N'dbo.ApplicationItems', N'BorderZoneLocationID') IS NOT NULL THEN N'BorderZoneLocationID'
    ELSE N'BorderZoneLocationId' END;

DECLARE @copySql nvarchar(max) = N'
UPDATE ai
SET ai.BorderZoneLocation = COALESCE(NULLIF(LTRIM(RTRIM(bzl.NameTm)), N''''), N''Ýok'')
FROM dbo.ApplicationItems ai
LEFT JOIN dbo.BorderZoneLocations bzl ON bzl.ID = ai.' + QUOTENAME(@fkColumn) + N';

UPDATE dbo.ApplicationItems
SET BorderZoneLocation = N''Ýok''
WHERE BorderZoneLocation IS NULL OR LTRIM(RTRIM(BorderZoneLocation)) = N'''';';

EXEC sys.sp_executesql @copySql;", false);
    }

    private void DropApplicationItemBorderZoneLocationForeignKey()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL
    RETURN;

DECLARE @fkColumn sysname = CASE
    WHEN COL_LENGTH(N'dbo.ApplicationItems', N'BorderZoneLocationID') IS NOT NULL THEN N'BorderZoneLocationID'
    WHEN COL_LENGTH(N'dbo.ApplicationItems', N'BorderZoneLocationId') IS NOT NULL THEN N'BorderZoneLocationId'
    ELSE NULL END;

IF @fkColumn IS NULL
    RETURN;

DECLARE @sql nvarchar(max);

SELECT @sql = STRING_AGG(
    CAST(N'ALTER TABLE dbo.ApplicationItems DROP CONSTRAINT ' + QUOTENAME(fk.name) AS nvarchar(max)),
    N'; ')
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.ApplicationItems')
  AND c.name = @fkColumn;

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

DECLARE @dropColumnSql nvarchar(max) =
    N'ALTER TABLE dbo.ApplicationItems DROP COLUMN ' + QUOTENAME(@fkColumn) + N';';
EXEC sys.sp_executesql @dropColumnSql;", false);
    }

    private void NormalizeEmptyApplicationItemBorderZones()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.ApplicationItems', N'BorderZoneLocation') IS NULL
    RETURN;

UPDATE dbo.ApplicationItems
SET BorderZoneLocation = N'Ýok'
WHERE BorderZoneLocation IS NULL OR LTRIM(RTRIM(BorderZoneLocation)) = N'';", false);
    }

    private void SeedBorderZoneNames()
    {
        foreach (var nameTm in DefaultBorderZoneNames)
        {
            EnsureBorderZoneName(nameTm);
        }
    }

    private void EnsureBorderZoneName(string nameTm)
    {
        if (string.IsNullOrWhiteSpace(nameTm))
        {
            return;
        }

        var trimmed = nameTm.Trim();
        var existing = ObjectSpace.FirstOrDefault<BorderZoneName>(z => z.NameTm == trimmed);
        if (existing != null)
        {
            return;
        }

        var zone = ObjectSpace.CreateObject<BorderZoneName>();
        zone.NameTm = trimmed;
        zone.Name = trimmed;
        ObjectSpace.CommitChanges();
    }
}
