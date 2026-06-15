using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Syncs <see cref="BusinessObjects.ProjectContractMinistryLeg"/> rows from nested
/// <c>MinistryLegs</c> on tenant <c>project-contract.json</c> (after
/// <see cref="LookupCatalogSyncUpdater"/>). Migration service is not a ministry leg.
/// </summary>
public sealed class ProjectContractMinistrySeedUpdater : ModuleUpdater
{
    public ProjectContractMinistrySeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ProjectContractMinistryLegCatalogSync.Sync(ObjectSpace);
        ObjectSpace.CommitChanges();
    }
}
