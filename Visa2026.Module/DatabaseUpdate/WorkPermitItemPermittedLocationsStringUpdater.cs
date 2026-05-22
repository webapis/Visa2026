using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Migrates <see cref="WorkPermitItem"/> permitted locations from legacy city link rows to comma-separated
/// <c>WorkPermittedLocations</c> nvarchar and <see cref="WorkPermittedLocationName"/> catalog.
/// </summary>
public sealed class WorkPermitItemPermittedLocationsStringUpdater : ModuleUpdater
{
    public WorkPermitItemPermittedLocationsStringUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        EnsureWorkPermitItemPermittedLocationsColumn();
        CopyPermittedCitiesToWorkPermitItemString();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        SeedWorkPermittedLocationNamesFromItems();
        DropWorkPermitItemPermittedCityTable();
    }

    private void EnsureWorkPermitItemPermittedLocationsColumn()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.WorkPermitItems', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.WorkPermitItems', N'WorkPermittedLocations') IS NULL
    ALTER TABLE dbo.WorkPermitItems ADD WorkPermittedLocations nvarchar(500) NULL;", false);
    }

    private void CopyPermittedCitiesToWorkPermitItemString()
    {
        ExecuteNonQueryCommand(@"
DECLARE @sql nvarchar(max);
IF OBJECT_ID(N'dbo.WorkPermitItems', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.WorkPermitItems', N'WorkPermittedLocations') IS NULL
    RETURN;
IF OBJECT_ID(N'dbo.WorkPermitItemPermittedCities', N'U') IS NULL
    RETURN;
IF OBJECT_ID(N'dbo.Cities', N'U') IS NULL
    RETURN;

DECLARE @cityFk sysname =
    CASE
        WHEN COL_LENGTH(N'dbo.WorkPermitItemPermittedCities', N'CityId') IS NOT NULL THEN N'CityId'
        WHEN COL_LENGTH(N'dbo.WorkPermitItemPermittedCities', N'CityID') IS NOT NULL THEN N'CityID'
        ELSE NULL END;
IF @cityFk IS NULL
    RETURN;

SET @sql = N'
UPDATE wpi
SET wpi.WorkPermittedLocations = agg.Names
FROM dbo.WorkPermitItems wpi
INNER JOIN (
    SELECT l.WorkPermitItemId,
           STRING_AGG(LTRIM(RTRIM(c.NameTm)), N'', '') WITHIN GROUP (ORDER BY c.NameTm) AS Names
    FROM dbo.WorkPermitItemPermittedCities l
    INNER JOIN dbo.Cities c ON c.ID = l.' + QUOTENAME(@cityFk) + N'
    WHERE c.NameTm IS NOT NULL AND LTRIM(RTRIM(c.NameTm)) <> N''''
    GROUP BY l.WorkPermitItemId
) agg ON agg.WorkPermitItemId = wpi.ID;';
EXEC sp_executesql @sql;

UPDATE dbo.WorkPermitItems
SET WorkPermittedLocations = N''
WHERE WorkPermittedLocations IS NULL;", false);
    }

    private void SeedWorkPermittedLocationNamesFromItems()
    {
        if (ObjectSpace == null)
        {
            return;
        }

        var names = ObjectSpace.GetObjectsQuery<WorkPermitItem>()
            .Select(wpi => wpi.WorkPermittedLocations)
            .ToList()
            .SelectMany(s => (s ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var nameTm in names)
        {
            EnsureWorkPermittedLocationName(nameTm);
        }
    }

    private void EnsureWorkPermittedLocationName(string nameTm)
    {
        if (string.IsNullOrWhiteSpace(nameTm))
        {
            return;
        }

        var trimmed = nameTm.Trim();
        var existing = ObjectSpace.FirstOrDefault<WorkPermittedLocationName>(z => z.NameTm == trimmed);
        if (existing != null)
        {
            return;
        }

        var entry = ObjectSpace.CreateObject<WorkPermittedLocationName>();
        entry.NameTm = trimmed;
        entry.Name = trimmed;
        ObjectSpace.CommitChanges();
    }

    private void DropWorkPermitItemPermittedCityTable()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.WorkPermitItemPermittedCities', N'U') IS NOT NULL
    DROP TABLE dbo.WorkPermitItemPermittedCities;", false);
    }
}
