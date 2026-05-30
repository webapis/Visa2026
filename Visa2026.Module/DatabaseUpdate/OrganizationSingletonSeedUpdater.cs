using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

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

            var removed = OrganizationSingletonHelper.CollapseToSingleRow(
                ObjectSpace, (SystemSettings _) => "Settings");
            if (removed > 0)
            {
                var line = $"OrganizationSingletonSeedUpdater: removed {removed} duplicate SystemSettings row(s).";
                Tracing.Tracer.LogText(line);
                System.Diagnostics.Trace.WriteLine(line);
            }

            ObjectSpace.CommitChanges();
        }
    }
}
