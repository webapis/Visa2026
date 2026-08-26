using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.PreviewSlot;

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

        // Invitation / WorkPermit / Rejection / BorderZone: compose in #visa-preview-slot (never modal DetailView).
        if (IssueIssuedHeaderComposeService.TryResolveKind(key, out _))
        {
            return ApplicationWorkspaceIssueIssuedHeaderOpenHelper.TryOpenCompose(
                application,
                applicationProfileInstanceId,
                key,
                ownerViewId: null);
        }

        // Issued visa: compose in the slot (invitation lines or case roster).
        if (string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase)
            && ApplicationWorkspaceIssueIssuedVisaOpenHelper.TryOpenCompose(
                application,
                applicationProfileInstanceId,
                ownerViewId: null))
        {
            return true;
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

        // Issued headers Inv/WP/RJ/BZ open in preview-slot (Blazor), not modal DetailView.
        if (IssueIssuedHeaderComposeService.TryResolveKind(key, out _))
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

        // Issued visa: Blazor opens the compose slot (invitation or roster source).
        if (header is Visa visa
            && string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase)
            && IssueIssuedVisaComposeService.CanOpenInSlot(visa.IssuingApplicationProfileInstance))
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
                InvitationIssuedRosterItemsHelper.EnsureRosterInvitationItems(invitation);
                return true;
            case WorkPermit workPermit:
                workPermit.ApplicationProfileInstance = instance;
                WorkPermitIssuedRosterItemsHelper.EnsureRosterWorkPermitItems(workPermit);
                return true;
            case BorderZone borderZone:
                borderZone.ApplicationProfileInstance = instance;
                return true;
            case Rejection rejection:
                rejection.ApplicationProfileInstance = instance;
                return true;
            case Visa visa when string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase):
                visa.IssuingApplicationProfileInstance = instance;
                VisaIssuingLinkPathAMatcher.TryApplyOnce(visa);
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
