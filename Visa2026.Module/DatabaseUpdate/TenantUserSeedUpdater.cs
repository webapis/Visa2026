using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Updating;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Creates or updates tenant officers from embedded <c>LookupCatalogs/tenant/tenant-users.json</c>
/// (optional disk overlay). Runs after <see cref="Updater"/> so roles exist.
/// </summary>
public sealed class TenantUserSeedUpdater : ModuleUpdater
{
    public TenantUserSeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        var userManager = ObjectSpace.ServiceProvider.GetRequiredService<UserManager>();
        TenantUserCatalogSync.Sync(ObjectSpace, userManager);
        ObjectSpace.CommitChanges();
    }
}
