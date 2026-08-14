using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Seeds <see cref="BusinessObjects.ApplicationProfile"/> rows from deprecated
/// <see cref="BusinessObjects.ApplicationType"/> and backfills ApplicationProfileInstance FKs.
/// Runs after type configuration and migration SLA profile links.
/// </summary>
public sealed class ApplicationProfileSeedUpdater : ModuleUpdater
{
    public ApplicationProfileSeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ApplicationProfileSeedSync.Sync(ObjectSpace);
    }
}
