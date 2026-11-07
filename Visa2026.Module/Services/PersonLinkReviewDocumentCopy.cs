using System;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Linked-records dialog eye preview: which kinds store scans, record keys, and file presence.
/// </summary>
public static class PersonLinkReviewDocumentCopy
{
    public static bool Supports(ApplicationProfileInstancePersonLinkKind kind) => kind switch
    {
        ApplicationProfileInstancePersonLinkKind.Passport => true,
        ApplicationProfileInstancePersonLinkKind.Education => true,
        ApplicationProfileInstancePersonLinkKind.AddressOfResidence => true,
        ApplicationProfileInstancePersonLinkKind.Visa => true,
        ApplicationProfileInstancePersonLinkKind.InvitationItem => true,
        ApplicationProfileInstancePersonLinkKind.WorkPermitItem => true,
        ApplicationProfileInstancePersonLinkKind.BorderZoneItem => true,
        ApplicationProfileInstancePersonLinkKind.MedicalRecord => true,
        ApplicationProfileInstancePersonLinkKind.RejectionItem => true,
        ApplicationProfileInstancePersonLinkKind.Position => true,
        _ => false,
    };

    public static void Refresh(IObjectSpace objectSpace, PersonLinkReviewLine line)
    {
        if (line == null)
            return;

        if (!Supports(line.Kind) || line.OmitFromJoin)
        {
            line.SupportsDocumentCopy = false;
            line.HasDocumentCopy = false;
            line.DocumentCopyRecordKey = null;
            return;
        }

        line.SupportsDocumentCopy = true;
        if (line.ObjectId == Guid.Empty || objectSpace == null)
        {
            line.HasDocumentCopy = false;
            line.DocumentCopyRecordKey = null;
            return;
        }

        var resolved = TryResolve(objectSpace, line.Kind, line.ObjectId);
        line.DocumentCopyRecordKey = resolved.RecordKey;
        line.HasDocumentCopy = resolved.HasFiles;
    }

    public static void RefreshAll(IObjectSpace objectSpace, System.Collections.Generic.IEnumerable<PersonLinkReviewLine>? lines)
    {
        if (lines == null)
            return;
        foreach (var line in lines)
            Refresh(objectSpace, line);
    }

    public static (string? RecordKey, bool HasFiles) TryResolve(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid objectId)
    {
        if (objectSpace == null || objectId == Guid.Empty)
            return (null, false);

        switch (kind)
        {
            case ApplicationProfileInstancePersonLinkKind.Passport:
                return ($"Passport:{objectId:N}",
                    HasDocs<PassportDocument>(objectSpace, d => d.Passport.ID == objectId));

            case ApplicationProfileInstancePersonLinkKind.Education:
                return ($"Education:{objectId:N}",
                    HasDocs<EducationDocument>(objectSpace, d => d.Education.ID == objectId));

            case ApplicationProfileInstancePersonLinkKind.AddressOfResidence:
            {
                var address = objectSpace.GetObjectByKey<AddressOfResidence>(objectId);
                var hasAddress = HasDocs<AddressOfResidenceDocument>(objectSpace, d => d.AddressOfResidence.ID == objectId);
                var hasLodging = address?.Type == ResidenceType.Lodging
                    && address.Lodging != null
                    && HasDocs<LodgingDocument>(objectSpace, d => d.Lodging.ID == address.Lodging.ID);
                return ($"AddressOfResidence:{objectId:N}", hasAddress || hasLodging);
            }

            case ApplicationProfileInstancePersonLinkKind.Visa:
            {
                var visa = objectSpace.GetObjectByKey<Visa>(objectId);
                if (visa?.Passport == null)
                    return (null, false);
                var passportId = visa.Passport.ID;
                return ($"Passport:{passportId:N}/Visa:{objectId:N}",
                    HasDocs<VisaDocument>(objectSpace, d => d.Visa.ID == objectId));
            }

            case ApplicationProfileInstancePersonLinkKind.InvitationItem:
            {
                var item = objectSpace.GetObjectByKey<InvitationItem>(objectId);
                var invitation = item?.Invitation;
                return ($"InvitationItem:{objectId:N}",
                    invitation != null && HasDocs<InvitationDocument>(objectSpace, d => d.Invitation.ID == invitation.ID));
            }

            case ApplicationProfileInstancePersonLinkKind.WorkPermitItem:
            {
                var item = objectSpace.GetObjectByKey<WorkPermitItem>(objectId);
                var workPermit = item?.WorkPermit;
                return ($"WorkPermitItem:{objectId:N}",
                    workPermit != null && HasDocs<WorkPermitDocument>(objectSpace, d => d.WorkPermit.ID == workPermit.ID));
            }

            case ApplicationProfileInstancePersonLinkKind.BorderZoneItem:
            {
                var item = objectSpace.GetObjectByKey<BorderZoneItem>(objectId);
                var borderZone = item?.BorderZone;
                return ($"BorderZoneItem:{objectId:N}",
                    borderZone != null && HasDocs<BorderZoneDocument>(objectSpace, d => d.BorderZone.ID == borderZone.ID));
            }

            case ApplicationProfileInstancePersonLinkKind.MedicalRecord:
                return ($"MedicalRecord:{objectId:N}",
                    HasDocs<MedicalRecordDocument>(objectSpace, d => d.MedicalRecord.ID == objectId));

            case ApplicationProfileInstancePersonLinkKind.RejectionItem:
            {
                var item = objectSpace.GetObjectByKey<RejectionItem>(objectId);
                var rejection = item?.Rejection;
                return ($"RejectionItem:{objectId:N}",
                    rejection != null && HasDocs<RejectionDocument>(objectSpace, d => d.Rejection.ID == rejection.ID));
            }

            case ApplicationProfileInstancePersonLinkKind.Position:
                return ($"EmployeePositionHistory:{objectId:N}",
                    HasDocs<EmployeePositionHistoryDocument>(objectSpace, d => d.EmployeePositionHistory.ID == objectId));

            default:
                return (null, false);
        }
    }

    private static bool HasDocs<TDocument>(
        IObjectSpace objectSpace,
        System.Linq.Expressions.Expression<Func<TDocument, bool>> filter)
        where TDocument : DocumentBase
    {
        return objectSpace.GetObjectsQuery<TDocument>()
            .Where(filter)
            .Include(d => d.File)
            .AsEnumerable()
            .Any(d => d.File != null && d.File.Size > 0);
    }
}