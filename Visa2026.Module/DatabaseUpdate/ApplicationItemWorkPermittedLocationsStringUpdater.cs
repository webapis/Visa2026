using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="BusinessObjects.ApplicationItem.WorkPermittedLocations"/> nvarchar column exists.
/// </summary>
public sealed class ApplicationItemWorkPermittedLocationsStringUpdater : ModuleUpdater
{
    public ApplicationItemWorkPermittedLocationsStringUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        EnsureApplicationItemWorkPermittedLocationsColumn();
    }

    private void EnsureApplicationItemWorkPermittedLocationsColumn()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL
    RETURN;

IF COL_LENGTH(N'dbo.ApplicationItems', N'WorkPermittedLocations') IS NULL
    ALTER TABLE dbo.ApplicationItems ADD WorkPermittedLocations nvarchar(500) NULL;", false);

        ExecuteNonQueryCommand(@"
UPDATE dbo.ApplicationItems
SET WorkPermittedLocations = N''
WHERE WorkPermittedLocations IS NULL;", false);
    }
}
