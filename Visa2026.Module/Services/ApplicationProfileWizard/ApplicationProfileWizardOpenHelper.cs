using System;
using System.Globalization;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ApplicationProfileWizard;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

public static class ApplicationProfileWizardOpenHelper
{
    public static DetailView? CreateWizardView(
        XafApplication application,
        IObjectSpace sourceObjectSpace,
        ApplicationProfile profile)
    {
        if (application == null || sourceObjectSpace == null || profile == null)
            return null;

        var profileId = ResolveId(sourceObjectSpace, profile);
        return profileId == null ? null : CreateWizardView(application, profileId.Value);
    }

    public static DetailView? CreateWizardView(XafApplication application, Guid applicationProfileId)
    {
        if (application == null || applicationProfileId == Guid.Empty)
            return null;

        ApplicationProfileWizardPendingOpenGate.Set(application, applicationProfileId);

        var objectSpace = application.CreateObjectSpace(typeof(ApplicationProfileWizardHost));
        var host = objectSpace.CreateObject<ApplicationProfileWizardHost>();
        host.ApplicationProfileId = applicationProfileId;

        var detailView = application.CreateDetailView(objectSpace, host);
        detailView.ViewEditMode = ViewEditMode.Edit;
        return detailView;
    }

    private static Guid? ResolveId(IObjectSpace objectSpace, ApplicationProfile profile)
    {
        var key = objectSpace.GetKeyValue(profile);
        return key switch
        {
            Guid guid => guid,
            null => null,
            _ => Guid.TryParse(Convert.ToString(key, CultureInfo.InvariantCulture), out var parsed)
                ? parsed
                : null,
        };
    }
}
