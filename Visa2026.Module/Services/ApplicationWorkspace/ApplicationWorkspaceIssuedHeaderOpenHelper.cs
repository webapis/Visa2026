using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Opens or creates 1:N issued headers (Invitation / WorkPermit / BorderZone / Rejection / issued Visa)
/// from the case workspace Overview. Sets the issuing FK immediately.
/// </summary>
public static class ApplicationWorkspaceIssuedHeaderOpenHelper
{
    public static bool TryCreate(
        XafApplication application,
        Frame? sourceFrame,
        Guid applicationProfileInstanceId,
        string key)
    {
        if (application == null
            || applicationProfileInstanceId == Guid.Empty
            || !ApplicationWorkspaceIssuedRecordsCatalog.TryGet(key, out _))
        {
            return false;
        }

        var headerType = ApplicationWorkspaceIssuedRecordsCatalog.ResolveHeaderType(key);
        if (headerType == null)
            return false;

        var objectSpace = application.CreateObjectSpace(headerType);
        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationProfileInstanceId);
        if (instance == null)
        {
            objectSpace.Dispose();
            return false;
        }

        if (!ApplicationWorkspaceIssuedRecordsCatalog.IsVisible(instance, key))
        {
            objectSpace.Dispose();
            return false;
        }

        var created = objectSpace.CreateObject(headerType);
        if (!AssignIssuingInstance(created, instance, key))
        {
            objectSpace.Dispose();
            return false;
        }

        var detailView = application.CreateDetailView(objectSpace, created);
        detailView.ViewEditMode = ViewEditMode.Edit;
        Show(application, sourceFrame, detailView, TargetWindow.NewModalWindow);
        return true;
    }

    public static bool TryOpen(
        XafApplication application,
        Frame? sourceFrame,
        string key,
        Guid headerId)
    {
        if (application == null || headerId == Guid.Empty)
            return false;

        var headerType = ApplicationWorkspaceIssuedRecordsCatalog.ResolveHeaderType(key);
        if (headerType == null)
            return false;

        var objectSpace = application.CreateObjectSpace(headerType);
        var header = objectSpace.GetObjectByKey(headerType, headerId);
        if (header == null)
        {
            objectSpace.Dispose();
            return false;
        }

        var detailView = application.CreateDetailView(objectSpace, header);
        detailView.ViewEditMode = ViewEditMode.Edit;
        Show(application, sourceFrame, detailView, TargetWindow.NewModalWindow);
        return true;
    }

    private static bool AssignIssuingInstance(object created, ApplicationProfileInstance instance, string key)
    {
        switch (created)
        {
            case Invitation invitation:
                invitation.ApplicationProfileInstance = instance;
                return true;
            case WorkPermit workPermit:
                workPermit.ApplicationProfileInstance = instance;
                return true;
            case BorderZone borderZone:
                borderZone.ApplicationProfileInstance = instance;
                return true;
            case Rejection rejection:
                rejection.ApplicationProfileInstance = instance;
                return true;
            case Visa visa when string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase):
                visa.IssuingApplicationProfileInstance = instance;
                return true;
            default:
                return false;
        }
    }

    private static void Show(XafApplication application, Frame? sourceFrame, DetailView detailView, TargetWindow targetWindow)
    {
        var frame = sourceFrame ?? application.MainWindow;
        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = targetWindow },
            new ShowViewSource(frame, null));
    }
}
