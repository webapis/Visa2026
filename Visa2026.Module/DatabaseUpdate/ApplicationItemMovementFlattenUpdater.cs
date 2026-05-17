using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Copies legacy <c>ApplicationItems.MovementRecordId</c> travel data onto <see cref="BusinessObjects.ApplicationItem"/>
/// scalar columns, then drops the FK column after schema sync.
/// </summary>
public class ApplicationItemMovementFlattenUpdater : ModuleUpdater
{
    private const string CopyMovementRecordSql = @"
UPDATE ai
SET
    ai.TravelDate = th.TravelDate,
    ai.TravelType = th.TravelType,
    ai.MovementType = th.MovementType,
    ai.CheckPointID = th.CheckPointID,
    ai.PurposeOfTravelID = th.PurposeOfTravelID,
    ai.TravelNotes = th.Notes
FROM dbo.ApplicationItems ai
INNER JOIN dbo.TravelHistories th ON th.ID = ai.MovementRecordId
WHERE ai.MovementRecordId IS NOT NULL;";

    public ApplicationItemMovementFlattenUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseBeforeUpdateSchema()
    {
        base.UpdateDatabaseBeforeUpdateSchema();
        EnsureApplicationItemTravelColumns();
        CopyMovementRecordDataToApplicationItems();
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        DropLegacyApplicationItemColumns();
    }

    /// <summary>Adds flattened travel columns before EF schema sync (separate batch from data copy).</summary>
    private void EnsureApplicationItemTravelColumns()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.ApplicationItems', N'TravelDate') IS NULL
    ALTER TABLE dbo.ApplicationItems ADD TravelDate datetime2 NULL;
IF COL_LENGTH(N'dbo.ApplicationItems', N'TravelType') IS NULL
    ALTER TABLE dbo.ApplicationItems ADD TravelType int NULL;
IF COL_LENGTH(N'dbo.ApplicationItems', N'MovementType') IS NULL
    ALTER TABLE dbo.ApplicationItems ADD MovementType int NULL;
IF COL_LENGTH(N'dbo.ApplicationItems', N'CheckPointID') IS NULL
    ALTER TABLE dbo.ApplicationItems ADD CheckPointID uniqueidentifier NULL;
IF COL_LENGTH(N'dbo.ApplicationItems', N'PurposeOfTravelID') IS NULL
    ALTER TABLE dbo.ApplicationItems ADD PurposeOfTravelID uniqueidentifier NULL;
IF COL_LENGTH(N'dbo.ApplicationItems', N'TravelNotes') IS NULL
    ALTER TABLE dbo.ApplicationItems ADD TravelNotes nvarchar(max) NULL;", false);
    }

    /// <summary>
    /// Uses dynamic SQL so the batch compiles only when <c>MovementRecordId</c> still exists
    /// (idempotent after a prior partial migration).
    /// </summary>
    private void CopyMovementRecordDataToApplicationItems()
    {
        ExecuteNonQueryCommand($@"
IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL
    RETURN;
IF OBJECT_ID(N'dbo.TravelHistories', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.ApplicationItems', N'MovementRecordId') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.ApplicationItems', N'TravelDate') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.TravelHistories', N'TravelDate') IS NULL
    RETURN;

EXEC sys.sp_executesql N'{CopyMovementRecordSql.Replace("'", "''")}';", false);
    }

    private void DropLegacyApplicationItemColumns()
    {
        DropApplicationItemColumnIfPresent("MovementRecordId");
        DropApplicationItemColumnIfPresent("CurrentRegistrationId");
        DropApplicationItemColumnIfPresent("CurrentRegistrationID");
        DropApplicationItemColumnIfPresent("CurrentBusinessUnitId");
        DropApplicationItemColumnIfPresent("CurrentBusinessUnitID");
        DropApplicationItemColumnIfPresent("CurrentBusinessTripId");
        DropApplicationItemColumnIfPresent("CurrentBusinessTripID");
    }

    private void DropApplicationItemColumnIfPresent(string columnName)
    {
        ExecuteNonQueryCommand($@"
IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.ApplicationItems', N'{columnName}') IS NULL
    RETURN;

DECLARE @sql nvarchar(max);

SELECT @sql = STRING_AGG(
    CAST(N'ALTER TABLE dbo.ApplicationItems DROP CONSTRAINT ' + QUOTENAME(fk.name) AS nvarchar(max)),
    N'; ')
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.ApplicationItems')
  AND c.name = N'{columnName}';

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

EXEC sys.sp_executesql N'ALTER TABLE dbo.ApplicationItems DROP COLUMN [{columnName}];';", false);
    }
}
