using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate
{
    /// <summary>
    /// Ensures organization singleton rows and default application-number fields on <see cref="SystemSettings"/> exist.
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

            _ = CompanyProfile.TryGetInstance(ObjectSpace)
                ?? ObjectSpace.CreateObject<CompanyProfile>();
            _ = AuthorizedSignatory.TryGetInstance(ObjectSpace)
                ?? ObjectSpace.CreateObject<AuthorizedSignatory>();
            _ = AuthorizedRepresentative.TryGetInstance(ObjectSpace)
                ?? ObjectSpace.CreateObject<AuthorizedRepresentative>();

            var settings = SystemSettings.GetOrCreateInstance(ObjectSpace);
            if (string.IsNullOrWhiteSpace(settings.AppNumberFormat))
                settings.AppNumberFormat = "{PREFIX}{YEAR}-{NUMBER}";
            if (settings.ApplicationNumberPadding <= 0)
                settings.ApplicationNumberPadding = SystemSettings.DefaultApplicationNumberPadding;

            ObjectSpace.CommitChanges();
        }
    }
}
