using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Normalizes empty <see cref="BusinessObjects.Visa.BorderZoneLocation"/> values to the required sentinel <c>Ýok</c>.
/// </summary>
public sealed class VisaBorderZoneLocationYokDefaultUpdater : ModuleUpdater
{
    public VisaBorderZoneLocationYokDefaultUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        NormalizeEmptyVisaBorderZones();
    }

    private void NormalizeEmptyVisaBorderZones()
    {
        ExecuteNonQueryCommand(@"
IF OBJECT_ID(N'dbo.Visas', N'U') IS NULL
    RETURN;
IF COL_LENGTH(N'dbo.Visas', N'BorderZoneLocation') IS NULL
    RETURN;

UPDATE dbo.Visas
SET BorderZoneLocation = N'Ýok'
WHERE BorderZoneLocation IS NULL OR LTRIM(RTRIM(BorderZoneLocation)) = N'';", false);
    }
}
