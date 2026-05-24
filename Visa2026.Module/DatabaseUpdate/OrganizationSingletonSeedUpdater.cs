using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate
{
    /// <summary>
    /// Ensures default application-number fields on <see cref="SystemSettings"/> exist.
    /// Organization singleton BOs (CompanyProfile, AuthorizedSignatory, AuthorizedRepresentative)
    /// are filled from tenant JSON by <see cref="LookupCatalogSyncUpdater"/>.
    /// Legacy organization table migration is handled by <see cref="OrganizationLegacySchemaCleanupUpdater"/> (Phase 5).
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

            var settings = SystemSettings.GetOrCreateInstance(ObjectSpace);
            if (string.IsNullOrWhiteSpace(settings.AppNumberPrefix))
                settings.AppNumberPrefix = "TRM-2026-";
            if (string.IsNullOrWhiteSpace(settings.AppNumberFormat))
                settings.AppNumberFormat = "{PREFIX}{YEAR}-{NUMBER}";
            if (settings.ApplicationNumberPadding <= 0)
                settings.ApplicationNumberPadding = 3;

            ObjectSpace.CommitChanges();
        }
    }
}
