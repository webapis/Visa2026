using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate
{
    public class SystemSettingsUpdater : ModuleUpdater
    {
        public SystemSettingsUpdater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            var s = SystemSettings.GetOrCreateInstance(ObjectSpace);
            if (s.MaxDocumentSizeInMB > SystemSettings.MaxDocumentSizeInMBCap)
                s.MaxDocumentSizeInMB = SystemSettings.MaxDocumentSizeInMBCap;
            if (s.MaxImageSizeInMB > SystemSettings.MaxImageSizeInMBCap)
                s.MaxImageSizeInMB = SystemSettings.MaxImageSizeInMBCap;
            if (s.MaxDocumentSizeInMB < 1)
                s.MaxDocumentSizeInMB = SystemSettings.DefaultMaxDocumentSizeInMB;
            if (s.MaxImageSizeInMB < 1)
                s.MaxImageSizeInMB = SystemSettings.DefaultMaxImageSizeInMB;
            ObjectSpace.CommitChanges();
        }
    }
}