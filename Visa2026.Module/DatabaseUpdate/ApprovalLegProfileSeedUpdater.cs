using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

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
        // Profiles must be committed before nested legs — EF can MERGE legs first in one batch and hit FK_ApprovalLegProfileMinistryLegs_ApprovalLegProfileId.
        ApprovalLegProfileMinistryLegCatalogSync.EnsureProfiles(ObjectSpace);
        ObjectSpace.CommitChanges();
        ApprovalLegProfileMinistryLegCatalogSync.SyncMinistryLegs(ObjectSpace);
        ObjectSpace.CommitChanges();
    }
}
