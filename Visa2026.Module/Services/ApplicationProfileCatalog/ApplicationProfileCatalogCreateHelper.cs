using System;
using System.Globalization;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileWizard;

namespace Visa2026.Module.Services.ApplicationProfileCatalog;

public static class ApplicationProfileCatalogCreateHelper
{
    public static DetailView? CreateNewProfileAndOpenWizard(XafApplication application)
    {
        if (application == null)
            return null;

        using var objectSpace = application.CreateObjectSpace(typeof(ApplicationProfile));
        var profile = objectSpace.CreateObject<ApplicationProfile>();
        profile.Name = "New Application Profile";
        profile.Code = "NEW-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        profile.IsActive = true;
        objectSpace.CommitChanges();

        var profileId = profile.ID;
        return ApplicationProfileWizardOpenHelper.CreateWizardView(application, profileId);
    }
}