using System;
using System.Globalization;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ApplicationWorkspace;

namespace Visa2026.Module.Services.ApplicationWorkspace;

public static class ApplicationWorkspaceOpenHelper
{
    public static DetailView? CreateWorkspaceView(XafApplication application, IObjectSpace sourceObjectSpace, ApplicationProfileInstance applicationBo)
    {
        if (application == null || sourceObjectSpace == null || applicationBo == null)
            return null;

        var applicationId = ResolveId(sourceObjectSpace, applicationBo);
        return applicationId == null ? null : CreateWorkspaceView(application, applicationId.Value);
    }

    public static DetailView? CreateWorkspaceView(XafApplication application, Guid applicationId)
    {
        if (application == null || applicationId == Guid.Empty)
            return null;

        ApplicationWorkspacePendingOpenGate.Set(application, applicationId);

        var objectSpace = application.CreateObjectSpace(typeof(ApplicationWorkspaceHost));
        var host = objectSpace.CreateObject<ApplicationWorkspaceHost>();
        host.ApplicationProfileInstanceId = applicationId;

        var detailView = application.CreateDetailView(objectSpace, host);
        detailView.ViewEditMode = ViewEditMode.View;
        return detailView;
    }

    private static Guid? ResolveId(IObjectSpace objectSpace, ApplicationProfileInstance applicationBo)
    {
        var key = objectSpace.GetKeyValue(applicationBo);
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
