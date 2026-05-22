using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Migrates <see cref="Visa"/> border zones from legacy <see cref="City"/> link rows to comma-separated
/// <c>BorderZoneLocation</c> nvarchar and <see cref="BorderZoneName"/> catalog.
/// </summary>
public sealed class VisaBorderZoneLocationStringUpdater : ModuleUpdater
{
    public VisaBorderZoneLocationStringUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        EnsureVisaBorderZoneLocationColumn();
        CopyPermittedCitiesToVisaString();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        SeedBorderZoneNamesFromVisas();
        DropVisaCityJoinTable();
        DropHasBorderZonePermitColumn();
    }

    private void EnsureVisaBorderZoneLocationColumn()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Visas', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.Visas', N'BorderZoneLocation') IS NULL
    ALTER TABLE dbo.Visas ADD BorderZoneLocation nvarchar(500) NULL;", false);
    }

    private void CopyPermittedCitiesToVisaString()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Visas', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.Visas', N'BorderZoneLocation') IS NULL
    RETURN;
IF OBJECT_ID(N'dbo.Cities', N'U') IS NULL
    RETURN;

DECLARE @joinTable sysname;
SELECT TOP 1 @joinTable = t.name
FROM sys.tables t
WHERE t.name NOT IN (N'Visas', N'BorderZoneLocations', N'Cities')
  AND EXISTS (
      SELECT 1 FROM sys.columns c
      WHERE c.object_id = t.object_id
        AND c.name IN (N'VisaID', N'VisaId', N'VisasID', N'VisasId'))
  AND EXISTS (
      SELECT 1 FROM sys.columns c
      WHERE c.object_id = t.object_id
        AND c.name IN (N'CityID', N'CityId', N'CitiesID', N'CitiesId'));

IF @joinTable IS NULL
    RETURN;

DECLARE @visaFk sysname =
    CASE
        WHEN COL_LENGTH(@joinTable, N'VisaID') IS NOT NULL THEN N'VisaID'
        WHEN COL_LENGTH(@joinTable, N'VisaId') IS NOT NULL THEN N'VisaId'
        WHEN COL_LENGTH(@joinTable, N'VisasID') IS NOT NULL THEN N'VisasID'
        ELSE N'VisasId' END;
DECLARE @cityFk sysname =
    CASE
        WHEN COL_LENGTH(@joinTable, N'CityID') IS NOT NULL THEN N'CityID'
        WHEN COL_LENGTH(@joinTable, N'CityId') IS NOT NULL THEN N'CityId'
        WHEN COL_LENGTH(@joinTable, N'CitiesID') IS NOT NULL THEN N'CitiesID'
        ELSE N'CitiesId' END;

DECLARE @sql nvarchar(max) = N'
UPDATE v
SET v.BorderZoneLocation = agg.Names
FROM dbo.Visas v
INNER JOIN (
    SELECT l.' + QUOTENAME(@visaFk) + N' AS VisaId,
           STRING_AGG(LTRIM(RTRIM(c.NameTm)), N'', '') WITHIN GROUP (ORDER BY c.NameTm) AS Names
    FROM dbo.' + QUOTENAME(@joinTable) + N' l
    INNER JOIN dbo.Cities c ON c.ID = l.' + QUOTENAME(@cityFk) + N'
    WHERE c.NameTm IS NOT NULL AND LTRIM(RTRIM(c.NameTm)) <> N''''
    GROUP BY l.' + QUOTENAME(@visaFk) + N'
) agg ON agg.VisaId = v.ID;';

EXEC sys.sp_executesql @sql;

UPDATE dbo.Visas
SET BorderZoneLocation = N''
WHERE BorderZoneLocation IS NULL;", false);
    }

    private void SeedBorderZoneNamesFromVisas()
    {
        if (ObjectSpace == null)
        {
            return;
        }

        var names = ObjectSpace.GetObjectsQuery<Visa>()
            .Select(v => v.BorderZoneLocation)
            .ToList()
            .SelectMany(s => (s ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var nameTm in names)
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

    private void DropHasBorderZonePermitColumn()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Visas', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.Visas', N'HasBorderZonePermit') IS NOT NULL
    ALTER TABLE dbo.Visas DROP COLUMN HasBorderZonePermit;", false);
    }

    private void DropVisaCityJoinTable()
    {
        ExecuteNonQueryCommand(@"
DECLARE @joinTable sysname;
SELECT TOP 1 @joinTable = t.name
FROM sys.tables t
WHERE t.name NOT IN (N'Visas', N'BorderZoneLocations', N'Cities')
  AND EXISTS (
      SELECT 1 FROM sys.columns c
      WHERE c.object_id = t.object_id
        AND c.name IN (N'VisaID', N'VisaId', N'VisasID', N'VisasId'))
  AND EXISTS (
      SELECT 1 FROM sys.columns c
      WHERE c.object_id = t.object_id
        AND c.name IN (N'CityID', N'CityId', N'CitiesID', N'CitiesId'));

IF @joinTable IS NULL
    RETURN;

DECLARE @sql nvarchar(max) = N'DROP TABLE dbo.' + QUOTENAME(@joinTable) + N';';
EXEC sys.sp_executesql @sql;", false);
    }
}
