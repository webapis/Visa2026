using System;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Module.Services.HeaderLinkedDocuments;

public static class HeaderDocumentCopiesOpenHelper
{
    public static bool TryOpen(
        XafApplication application,
        View view,
        HeaderDocumentCopiesFamily family,
        Guid parentId,
        Guid? contextItemId = null)
    {
        if (application == null || view == null || parentId == Guid.Empty)
            return false;

        var slotService = application.ServiceProvider?.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
        {
            application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("HeaderDocumentCopies.Preview.Error"),
                InformationType.Error);
            return false;
        }

        slotService.OpenHeaderDocumentCopiesAsync(new HeaderDocumentCopiesSlotRequest
        {
            Family = family,
            ParentId = parentId,
            ContextItemId = contextItemId,
        }, VisaPreviewSlotViewHelper.ResolveOwnerViewId(view)).GetAwaiter().GetResult();

        return true;
    }

    public static bool TryOpenFromViewObject(XafApplication application, View view, object? target)
    {
        if (application == null || view == null || target == null)
            return false;

        return target switch
        {
            WorkPermit workPermit => TryOpen(application, view, HeaderDocumentCopiesFamily.WorkPermit, workPermit.ID),
            WorkPermitItem workPermitItem when workPermitItem.WorkPermit != null => TryOpen(
                application,
                view,
                HeaderDocumentCopiesFamily.WorkPermit,
                workPermitItem.WorkPermit.ID,
                workPermitItem.ID),
            Invitation invitation => TryOpen(application, view, HeaderDocumentCopiesFamily.Invitation, invitation.ID),
            InvitationItem invitationItem when invitationItem.Invitation != null => TryOpen(
                application,
                view,
                HeaderDocumentCopiesFamily.Invitation,
                invitationItem.Invitation.ID,
                invitationItem.ID),
            Rejection rejection => TryOpen(application, view, HeaderDocumentCopiesFamily.Rejection, rejection.ID),
            RejectionItem rejectionItem when rejectionItem.Rejection != null => TryOpen(
                application,
                view,
                HeaderDocumentCopiesFamily.Rejection,
                rejectionItem.Rejection.ID,
                rejectionItem.ID),
            BorderZone borderZone => TryOpen(application, view, HeaderDocumentCopiesFamily.BorderZone, borderZone.ID),
            BorderZoneItem borderZoneItem when borderZoneItem.BorderZone != null => TryOpen(
                application,
                view,
                HeaderDocumentCopiesFamily.BorderZone,
                borderZoneItem.BorderZone.ID,
                borderZoneItem.ID),
            _ => false,
        };
    }

    public static bool TryGetFamilyForType(Type? objectType, out HeaderDocumentCopiesFamily family)
    {
        family = default;
        if (objectType == null)
            return false;

        if (objectType == typeof(WorkPermit) || objectType == typeof(WorkPermitItem))
        {
            family = HeaderDocumentCopiesFamily.WorkPermit;
            return true;
        }

        if (objectType == typeof(Invitation) || objectType == typeof(InvitationItem))
        {
            family = HeaderDocumentCopiesFamily.Invitation;
            return true;
        }

        if (objectType == typeof(Rejection) || objectType == typeof(RejectionItem))
        {
            family = HeaderDocumentCopiesFamily.Rejection;
            return true;
        }

        if (objectType == typeof(BorderZone) || objectType == typeof(BorderZoneItem))
        {
            family = HeaderDocumentCopiesFamily.BorderZone;
            return true;
        }

        return false;
    }
}
