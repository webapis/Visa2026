using DevExpress.ExpressApp;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Upserts shared <c>ApprovalLegProfile</c> rows (+ nested legs) from tenant
/// <c>approval-leg-profile.json</c>.
/// Runs from host start (<c>ApplicationProfileSeedGate</c>) because ModuleUpdater
/// is skipped when the module DB version is already current.
/// </summary>
public static class ApprovalLegProfileCatalogSeed
{
    public static void EnsureSynced(IObjectSpace objectSpace)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        // Profiles must be committed before nested legs — EF can MERGE legs first and hit FK.
        ApprovalLegProfileMinistryLegCatalogSync.EnsureProfiles(objectSpace);
        objectSpace.CommitChanges();
        ApprovalLegProfileMinistryLegCatalogSync.SyncMinistryLegs(objectSpace);
        objectSpace.CommitChanges();
    }
}
