using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Syncs <see cref="BusinessObjects.ApplicationProfile"/> from tenant
/// <c>application-profile*.json</c> (Çalik legacy Wave 0 sign-off). Runs after
/// <see cref="ApplicationProfileSeedUpdater"/> so tenant rows override type-derived defaults.
/// </summary>
public sealed class ApplicationProfileTenantCatalogSeedUpdater : ModuleUpdater
{
    public ApplicationProfileTenantCatalogSeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        SyncNow(ObjectSpace);
    }

    /// <summary>Idempotent sync from embedded tenant <c>application-profile*.json</c> (import/patch tooling).</summary>
    public static void SyncNow(IObjectSpace objectSpace)
    {
        ApplicationProfileTenantCatalogSync.Sync(objectSpace);
        objectSpace.CommitChanges();
        var count = objectSpace.GetObjectsQuery<ApplicationProfile>().Count();
        Tracing.Tracer.LogText($"ApplicationProfileTenantCatalogSeedUpdater.SyncNow: profiles in database={count}.");
    }
}
