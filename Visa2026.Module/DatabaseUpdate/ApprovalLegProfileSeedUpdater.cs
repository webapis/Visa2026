using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Syncs <see cref="BusinessObjects.ApprovalLegProfile"/> rows and nested ministry legs from
/// tenant <c>approval-leg-profile.json</c> only. Does not read or update <see cref="Application"/>.
/// Per-application <see cref="Application.ApprovalLegProfile"/> is set by VISA2014 import / PATCH.
/// </summary>
public sealed class ApprovalLegProfileSeedUpdater : ModuleUpdater
{
    public ApprovalLegProfileSeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ApprovalLegProfileCatalogSeed.EnsureSynced(ObjectSpace);
    }
}
