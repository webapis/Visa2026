using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects.OfficerShell;

namespace Visa2026.Module.Services.OfficerShell;

public static class OfficerShellOpenHelper
{
    public static DetailView? CreateShellView(XafApplication application, OfficerShellPage page = OfficerShellPage.Staged)
    {
        if (application == null)
            return null;

        OfficerShellPendingOpenGate.Set(application, page, Guid.Empty);

        var objectSpace = application.CreateObjectSpace(typeof(OfficerShellHost));
        var host = objectSpace.CreateObject<OfficerShellHost>();
        var detailView = application.CreateDetailView(objectSpace, host);
        detailView.ViewEditMode = ViewEditMode.View;
        return detailView;
    }

    public static DetailView? CreateShellView(XafApplication application, Guid caseApplicationProfileInstanceId)
    {
        if (application == null || caseApplicationProfileInstanceId == Guid.Empty)
            return null;

        OfficerShellPendingOpenGate.Set(application, OfficerShellPage.Case, caseApplicationProfileInstanceId);

        var objectSpace = application.CreateObjectSpace(typeof(OfficerShellHost));
        var host = objectSpace.CreateObject<OfficerShellHost>();
        var detailView = application.CreateDetailView(objectSpace, host);
        detailView.ViewEditMode = ViewEditMode.View;
        return detailView;
    }
}
