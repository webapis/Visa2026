using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Syncs <see cref="ApplicationProfileTemplate"/> nested rows from tenant
/// <c>application-profile-nested-templates*.json</c> (Wave 3). Runs after
/// <see cref="ApplicationProfileTenantCatalogSeedUpdater"/>.
/// </summary>
public sealed class ApplicationProfileNestedTemplateTenantCatalogSeedUpdater : ModuleUpdater
{
    public ApplicationProfileNestedTemplateTenantCatalogSeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        SyncNow(ObjectSpace);
    }

    public static void SyncNow(IObjectSpace objectSpace)
    {
        ApplicationProfileNestedTemplateTenantCatalogSync.Sync(objectSpace);
        objectSpace.CommitChanges();
        var count = objectSpace.GetObjectsQuery<ApplicationProfileTemplate>().Count();
        Tracing.Tracer.LogText(
            $"ApplicationProfileNestedTemplateTenantCatalogSeedUpdater.SyncNow: nested templates in database={count}.");
    }

    public static int CountApprovedCatalogRows() =>
        ApplicationProfileNestedTemplateTenantCatalogLoader.TryLoadRows(out var rows) ? rows.Count : 0;
}
