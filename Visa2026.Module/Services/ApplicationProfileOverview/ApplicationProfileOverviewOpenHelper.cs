using System;
using System.Globalization;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ApplicationProfileOverview;

namespace Visa2026.Module.Services.ApplicationProfileOverview;

public static class ApplicationProfileOverviewOpenHelper
{
    public static DetailView? CreateOverviewView(
        XafApplication application,
        IObjectSpace sourceObjectSpace,
        ApplicationProfile profile)
    {
        if (application == null || sourceObjectSpace == null || profile == null)
            return null;

        var profileId = ResolveId(sourceObjectSpace, profile);
        return profileId == null ? null : CreateOverviewView(application, profileId.Value);
    }

    public static DetailView? CreateOverviewView(XafApplication application, Guid applicationProfileId)
    {
        if (application == null || applicationProfileId == Guid.Empty)
            return null;

        ApplicationProfileOverviewPendingOpenGate.Set(application, applicationProfileId);

        var objectSpace = application.CreateObjectSpace(typeof(ApplicationProfileOverviewHost));
        var host = objectSpace.CreateObject<ApplicationProfileOverviewHost>();
        host.ApplicationProfileId = applicationProfileId;

        var detailView = application.CreateDetailView(objectSpace, host);
        detailView.ViewEditMode = ViewEditMode.View;
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
