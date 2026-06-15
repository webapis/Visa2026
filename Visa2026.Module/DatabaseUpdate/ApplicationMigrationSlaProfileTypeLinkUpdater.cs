using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Links <see cref="BusinessObjects.ApplicationType"/> rows to migration SLA profiles from nested
/// <c>ApplicationTypeNames</c> on tenant <c>application-migration-sla-profile.json</c> (after
/// <see cref="LookupCatalogSyncUpdater"/> and <see cref="ApplicationTypeConfigurationUpdater"/>).
/// </summary>
public sealed class ApplicationMigrationSlaProfileTypeLinkUpdater : ModuleUpdater
{
    public ApplicationMigrationSlaProfileTypeLinkUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        var linked = ApplicationMigrationSlaProfileTypeLinkCatalogSync.Sync(ObjectSpace);
        if (linked > 0)
            ObjectSpace.CommitChanges();
    }
}