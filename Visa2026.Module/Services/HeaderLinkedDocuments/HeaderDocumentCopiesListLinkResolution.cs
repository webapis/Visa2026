using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.HeaderLinkedDocuments;

public static class HeaderDocumentCopiesListLinkResolution
{
    public static bool TryResolve(
        object? row,
        out HeaderDocumentCopiesFamily family,
        out Guid parentId,
        out Guid? contextItemId)
    {
        family = default;
        parentId = Guid.Empty;
        contextItemId = null;

        if (row == null)
            return false;

        switch (row)
        {
            case WorkPermit workPermit:
                family = HeaderDocumentCopiesFamily.WorkPermit;
                parentId = workPermit.ID;
                return parentId != Guid.Empty;
            case WorkPermitItem workPermitItem when workPermitItem.WorkPermit != null:
                family = HeaderDocumentCopiesFamily.WorkPermit;
                parentId = workPermitItem.WorkPermit.ID;
                contextItemId = workPermitItem.ID;
                return parentId != Guid.Empty;
            case Invitation invitation:
                family = HeaderDocumentCopiesFamily.Invitation;
                parentId = invitation.ID;
                return parentId != Guid.Empty;
            case InvitationItem invitationItem when invitationItem.Invitation != null:
                family = HeaderDocumentCopiesFamily.Invitation;
                parentId = invitationItem.Invitation.ID;
                contextItemId = invitationItem.ID;
                return parentId != Guid.Empty;
            case Rejection rejection:
                family = HeaderDocumentCopiesFamily.Rejection;
                parentId = rejection.ID;
                return parentId != Guid.Empty;
            case RejectionItem rejectionItem when rejectionItem.Rejection != null:
                family = HeaderDocumentCopiesFamily.Rejection;
                parentId = rejectionItem.Rejection.ID;
                contextItemId = rejectionItem.ID;
                return parentId != Guid.Empty;
            case BorderZone borderZone:
                family = HeaderDocumentCopiesFamily.BorderZone;
                parentId = borderZone.ID;
                return parentId != Guid.Empty;
            case BorderZoneItem borderZoneItem when borderZoneItem.BorderZone != null:
                family = HeaderDocumentCopiesFamily.BorderZone;
                parentId = borderZoneItem.BorderZone.ID;
                contextItemId = borderZoneItem.ID;
                return parentId != Guid.Empty;
            default:
                return false;
        }
    }
}
