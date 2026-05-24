using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate
{
    /// <summary>
    /// Ensures the global <see cref="SystemSettings"/> singleton exists (expiration thresholds, upload limits).
    /// Organization tenant data (company, signatory, application numbering, etc.) is filled from tenant JSON by
    /// <see cref="LookupCatalogSyncUpdater"/>.
    /// </summary>
    public class OrganizationSingletonSeedUpdater : ModuleUpdater
    {
        public OrganizationSingletonSeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
            : base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            _ = SystemSettings.GetOrCreateInstance(ObjectSpace);
            ObjectSpace.CommitChanges();
        }
    }
}
