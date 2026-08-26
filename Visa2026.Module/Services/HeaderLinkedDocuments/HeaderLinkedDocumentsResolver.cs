using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.HeaderLinkedDocuments;

public static class HeaderLinkedDocumentsResolver
{
    public static HeaderLinkedDocumentsSnapshot Resolve(
        IObjectSpace objectSpace,
        HeaderDocumentCopiesFamily family,
        Guid parentId,
        Guid? contextItemId = null)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);

        if (parentId == Guid.Empty)
        {
            return new HeaderLinkedDocumentsSnapshot
            {
                Family = family,
                ParentId = Guid.Empty,
                ContextItemId = contextItemId,
            };
        }

        return family switch
        {
            HeaderDocumentCopiesFamily.WorkPermit => ResolveWorkPermit(objectSpace, parentId, contextItemId),
            HeaderDocumentCopiesFamily.Invitation => ResolveInvitation(objectSpace, parentId, contextItemId),
            HeaderDocumentCopiesFamily.Rejection => ResolveRejection(objectSpace, parentId, contextItemId),
            HeaderDocumentCopiesFamily.BorderZone => ResolveBorderZone(objectSpace, parentId, contextItemId),
            HeaderDocumentCopiesFamily.Visa => ResolveVisa(objectSpace, parentId, contextItemId),
            _ => throw new ArgumentOutOfRangeException(nameof(family), family, null),
        };
    }

    private static HeaderLinkedDocumentsSnapshot ResolveWorkPermit(
        IObjectSpace os,
        Guid parentId,
        Guid? contextItemId)
    {
        var workPermit = os.GetObjectByKey<WorkPermit>(parentId);
        if (workPermit == null)
        {
            return new HeaderLinkedDocumentsSnapshot
            {
                Family = HeaderDocumentCopiesFamily.WorkPermit,
                ParentId = parentId,
                ContextItemId = contextItemId,
            };
        }

        workPermit = os.GetObject(workPermit);
        var itemCount = workPermit.WorkPermitItems?.Count(w => w != null) ?? 0;

        return new HeaderLinkedDocumentsSnapshot
        {
            Family = HeaderDocumentCopiesFamily.WorkPermit,
            ParentId = workPermit.ID,
            ContextItemId = contextItemId,
            HeaderTitle = workPermit.WorkPermitNumber ?? string.Empty,
            Subtitle = BuildWorkPermitSubtitle(os, workPermit, contextItemId),
            ShowSharedScansHint = itemCount > 1,
            Records = LoadDocumentRecords<WorkPermitDocument>(
                os,
                d => d.WorkPermit.ID == workPermit.ID,
                doc => $"WorkPermitDocument:{doc.ID:N}"),
        };
    }

    private static HeaderLinkedDocumentsSnapshot ResolveInvitation(
        IObjectSpace os,
        Guid parentId,
        Guid? contextItemId)
    {
        var invitation = os.GetObjectByKey<Invitation>(parentId);
        if (invitation == null)
        {
            return new HeaderLinkedDocumentsSnapshot
            {
                Family = HeaderDocumentCopiesFamily.Invitation,
                ParentId = parentId,
                ContextItemId = contextItemId,
            };
        }

        invitation = os.GetObject(invitation);
        var itemCount = invitation.InvitationItems?.Count(i => i != null) ?? 0;

        return new HeaderLinkedDocumentsSnapshot
        {
            Family = HeaderDocumentCopiesFamily.Invitation,
            ParentId = invitation.ID,
            ContextItemId = contextItemId,
            HeaderTitle = invitation.InvitationNumber ?? string.Empty,
            Subtitle = BuildInvitationSubtitle(os, invitation, contextItemId),
            ShowSharedScansHint = itemCount > 1,
            Records = LoadDocumentRecords<InvitationDocument>(
                os,
                d => d.Invitation.ID == invitation.ID,
                doc => $"InvitationDocument:{doc.ID:N}"),
        };
    }

    private static HeaderLinkedDocumentsSnapshot ResolveRejection(
        IObjectSpace os,
        Guid parentId,
        Guid? contextItemId)
    {
        var rejection = os.GetObjectByKey<Rejection>(parentId);
        if (rejection == null)
        {
            return new HeaderLinkedDocumentsSnapshot
            {
                Family = HeaderDocumentCopiesFamily.Rejection,
                ParentId = parentId,
                ContextItemId = contextItemId,
            };
        }

        rejection = os.GetObject(rejection);
        var itemCount = rejection.RejectionItems?.Count(i => i != null) ?? 0;

        return new HeaderLinkedDocumentsSnapshot
        {
            Family = HeaderDocumentCopiesFamily.Rejection,
            ParentId = rejection.ID,
            ContextItemId = contextItemId,
            HeaderTitle = rejection.RejectionTitle ?? string.Empty,
            Subtitle = BuildRejectionSubtitle(os, rejection, contextItemId),
            ShowSharedScansHint = itemCount > 1,
            Records = LoadDocumentRecords<RejectionDocument>(
                os,
                d => d.Rejection.ID == rejection.ID,
                doc => $"RejectionDocument:{doc.ID:N}"),
        };
    }

    private static HeaderLinkedDocumentsSnapshot ResolveBorderZone(
        IObjectSpace os,
        Guid parentId,
        Guid? contextItemId)
    {
        var borderZone = os.GetObjectByKey<BorderZone>(parentId);
        if (borderZone == null)
        {
            return new HeaderLinkedDocumentsSnapshot
            {
                Family = HeaderDocumentCopiesFamily.BorderZone,
                ParentId = parentId,
                ContextItemId = contextItemId,
            };
        }

        borderZone = os.GetObject(borderZone);
        var itemCount = borderZone.BorderZoneItems?.Count(i => i != null) ?? 0;

        return new HeaderLinkedDocumentsSnapshot
        {
            Family = HeaderDocumentCopiesFamily.BorderZone,
            ParentId = borderZone.ID,
            ContextItemId = contextItemId,
            HeaderTitle = borderZone.BorderZoneNumber ?? string.Empty,
            Subtitle = BuildBorderZoneSubtitle(os, borderZone, contextItemId),
            ShowSharedScansHint = itemCount > 1,
            Records = LoadDocumentRecords<BorderZoneDocument>(
                os,
                d => d.BorderZone.ID == borderZone.ID,
                doc => $"BorderZoneDocument:{doc.ID:N}"),
        };
    }

    private static HeaderLinkedDocumentsSnapshot ResolveVisa(
        IObjectSpace os,
        Guid parentId,
        Guid? contextItemId)
    {
        var visa = os.GetObjectByKey<Visa>(parentId);
        if (visa == null)
        {
            return new HeaderLinkedDocumentsSnapshot
            {
                Family = HeaderDocumentCopiesFamily.Visa,
                ParentId = parentId,
                ContextItemId = contextItemId,
            };
        }

        visa = os.GetObject(visa);

        return new HeaderLinkedDocumentsSnapshot
        {
            Family = HeaderDocumentCopiesFamily.Visa,
            ParentId = visa.ID,
            ContextItemId = contextItemId,
            HeaderTitle = visa.VisaNumber ?? string.Empty,
            Subtitle = BuildVisaSubtitle(visa),
            ShowSharedScansHint = false,
            Records = LoadDocumentRecords<VisaDocument>(
                os,
                d => d.Visa.ID == visa.ID,
                doc => $"VisaDocument:{doc.ID:N}"),
        };
    }

    private static string? BuildWorkPermitSubtitle(IObjectSpace os, WorkPermit workPermit, Guid? contextItemId)
    {
        if (contextItemId is Guid itemId && itemId != Guid.Empty)
        {
            var item = os.GetObjectByKey<WorkPermitItem>(itemId);
            if (item?.Person != null)
            {
                return VisaUiMessages.Format(
                    "WorkPermitDocumentCopies.Subtitle.FromItem",
                    item.Person.FullName,
                    workPermit.WorkPermitNumber ?? string.Empty);
            }
        }

        return BuildApplicationSubtitle(workPermit.ApplicationProfileInstance);
    }

    private static string? BuildInvitationSubtitle(IObjectSpace os, Invitation invitation, Guid? contextItemId)
    {
        if (contextItemId is Guid itemId && itemId != Guid.Empty)
        {
            var item = os.GetObjectByKey<InvitationItem>(itemId);
            if (item?.Person != null)
            {
                return VisaUiMessages.Format(
                    "InvitationDocumentCopies.Subtitle.FromItem",
                    item.Person.FullName,
                    invitation.InvitationNumber ?? string.Empty);
            }
        }

        return BuildApplicationSubtitle(invitation.ApplicationProfileInstance);
    }

    private static string? BuildRejectionSubtitle(IObjectSpace os, Rejection rejection, Guid? contextItemId)
    {
        if (contextItemId is Guid itemId && itemId != Guid.Empty)
        {
            var item = os.GetObjectByKey<RejectionItem>(itemId);
            if (item?.Person != null)
            {
                var date = rejection.Date.ToString("d", CultureInfo.CurrentUICulture);
                return VisaUiMessages.Format(
                    "RejectionDocumentCopies.Subtitle.FromItem",
                    item.Person.FullName,
                    date);
            }
        }

        return BuildApplicationSubtitle(rejection.ApplicationProfileInstance);
    }

    private static string? BuildBorderZoneSubtitle(IObjectSpace os, BorderZone borderZone, Guid? contextItemId)
    {
        if (contextItemId is Guid itemId && itemId != Guid.Empty)
        {
            var item = os.GetObjectByKey<BorderZoneItem>(itemId);
            if (item?.Person != null)
            {
                return VisaUiMessages.Format(
                    "BorderZoneDocumentCopies.Subtitle.FromItem",
                    item.Person.FullName,
                    borderZone.BorderZoneNumber ?? string.Empty);
            }
        }

        return BuildApplicationSubtitle(borderZone.ApplicationProfileInstance);
    }

    private static string? BuildVisaSubtitle(Visa visa)
    {
        var personName = visa.Passport?.Person?.FullName;
        if (!string.IsNullOrWhiteSpace(personName))
            return personName.Trim();

        return BuildApplicationSubtitle(visa.IssuingApplicationProfileInstance);
    }

    private static string? BuildApplicationSubtitle(ApplicationProfileInstance? application)
    {
        if (application == null)
            return null;

        var number = application.ApplicationNumber;
        if (string.IsNullOrWhiteSpace(number))
            return null;

        return VisaUiMessages.Format("HeaderDocumentCopies.Subtitle.ApplicationProfileInstance", number);
    }

    private static IReadOnlyList<HeaderLinkedDocumentRecord> LoadDocumentRecords<TDocument>(
        IObjectSpace os,
        Expression<Func<TDocument, bool>> filter,
        Func<TDocument, string> recordKeyFactory)
        where TDocument : DocumentBase
    {
        return os.GetObjectsQuery<TDocument>()
            .Where(filter)
            .OrderBy(d => d.ID)
            .Include(d => d.File)
            .AsEnumerable()
            .Select(doc => new HeaderLinkedDocumentRecord
            {
                RecordKey = recordKeyFactory(doc),
                RecordLabel = BuildRecordLabel(doc),
                Files = new[] { MapDocumentFile(doc) },
            })
            .ToList();
    }

    private static string BuildRecordLabel(DocumentBase doc)
    {
        if (!string.IsNullOrWhiteSpace(doc.Description))
            return doc.Description.Trim();

        var fileName = doc.File?.FileName;
        if (!string.IsNullOrWhiteSpace(fileName))
            return fileName.Trim();

        return VisaUiMessages.Get("HeaderDocumentCopies.Record.Unnamed");
    }

    private static HeaderLinkedDocumentFile MapDocumentFile(DocumentBase doc)
    {
        var file = doc.File;
        if (file == null)
        {
            return new HeaderLinkedDocumentFile
            {
                DocumentRowId = doc.ID,
                DocumentTypeName = doc.GetType().Name,
                FileName = string.Empty,
                HasContent = false,
            };
        }

        return new HeaderLinkedDocumentFile
        {
            FileDataId = file.ID,
            DocumentRowId = doc.ID,
            DocumentTypeName = doc.GetType().Name,
            FileName = file.FileName ?? string.Empty,
            SizeBytes = file.Size,
            HasContent = file.Size > 0,
        };
    }
}
