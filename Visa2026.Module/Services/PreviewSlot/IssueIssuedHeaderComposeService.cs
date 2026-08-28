using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.Services.PreviewSlot;

/// <summary>
/// Builds and persists issued headers (Invitation / WorkPermit / Rejection / BorderZone)
/// from the case workspace preview-slot compose UI.
/// </summary>
public static class IssueIssuedHeaderComposeService
{
    public static bool TryResolveKind(string catalogKey, out IssueIssuedHeaderKind kind)
    {
        kind = default;
        if (string.Equals(catalogKey, ApplicationWorkspaceIssuedRecordsCatalog.Invitation, StringComparison.OrdinalIgnoreCase))
        {
            kind = IssueIssuedHeaderKind.Invitation;
            return true;
        }

        if (string.Equals(catalogKey, ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, StringComparison.OrdinalIgnoreCase))
        {
            kind = IssueIssuedHeaderKind.WorkPermit;
            return true;
        }

        if (string.Equals(catalogKey, ApplicationWorkspaceIssuedRecordsCatalog.Rejection, StringComparison.OrdinalIgnoreCase))
        {
            kind = IssueIssuedHeaderKind.Rejection;
            return true;
        }

        if (string.Equals(catalogKey, ApplicationWorkspaceIssuedRecordsCatalog.BorderZone, StringComparison.OrdinalIgnoreCase))
        {
            kind = IssueIssuedHeaderKind.BorderZone;
            return true;
        }

        return false;
    }

    public static string CatalogKeyFor(IssueIssuedHeaderKind kind) => kind switch
    {
        IssueIssuedHeaderKind.Invitation => ApplicationWorkspaceIssuedRecordsCatalog.Invitation,
        IssueIssuedHeaderKind.WorkPermit => ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit,
        IssueIssuedHeaderKind.Rejection => ApplicationWorkspaceIssuedRecordsCatalog.Rejection,
        IssueIssuedHeaderKind.BorderZone => ApplicationWorkspaceIssuedRecordsCatalog.BorderZone,
        _ => string.Empty,
    };

    public static IssueIssuedHeaderCreateResult Delete(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        IssueIssuedHeaderKind kind,
        Guid headerId)
    {
        if (objectSpace == null || applicationProfileInstanceId == Guid.Empty || headerId == Guid.Empty)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Delete is not available.",
            };
        }

        try
        {
            return kind switch
            {
                IssueIssuedHeaderKind.Invitation => DeleteInvitation(objectSpace, applicationProfileInstanceId, headerId),
                IssueIssuedHeaderKind.WorkPermit => DeleteWorkPermit(objectSpace, applicationProfileInstanceId, headerId),
                IssueIssuedHeaderKind.Rejection => DeleteRejection(objectSpace, applicationProfileInstanceId, headerId),
                IssueIssuedHeaderKind.BorderZone => DeleteBorderZone(objectSpace, applicationProfileInstanceId, headerId),
                _ => new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Unknown header kind." },
            };
        }
        catch (Exception ex)
        {
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = ex.Message };
        }
    }

    private static IssueIssuedHeaderCreateResult DeleteInvitation(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        Guid headerId)
    {
        var invitation = objectSpace.GetObjectByKey<Invitation>(headerId);
        if (invitation == null)
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Invitation was not found." };

        if (invitation.ApplicationProfileInstance == null
            || invitation.ApplicationProfileInstance.ID != applicationProfileInstanceId)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "This invitation does not belong to this application.",
            };
        }

        var items = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(i => i.Invitation != null && i.Invitation.ID == invitation.ID)
            .ToList();
        if (items.Any(i => i.IsUsed || i.IssuedVisa != null))
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Cannot delete this invitation — a visa was already issued from it.",
            };
        }

        foreach (var doc in (invitation.Documents ?? Array.Empty<InvitationDocument>()).ToList())
            objectSpace.Delete(doc);
        foreach (var image in (invitation.Images ?? Array.Empty<InvitationImage>()).ToList())
            objectSpace.Delete(image);
        foreach (var item in items)
            objectSpace.Delete(item);

        var caption = invitation.InvitationNumber?.Trim() ?? string.Empty;
        objectSpace.Delete(invitation);
        objectSpace.CommitChanges();

        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = Guid.Empty,
            HeaderCaption = caption,
        };
    }

    private static IssueIssuedHeaderCreateResult DeleteWorkPermit(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        Guid headerId)
    {
        var workPermit = objectSpace.GetObjectByKey<WorkPermit>(headerId);
        if (workPermit == null)
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Work permit was not found." };
        if (workPermit.ApplicationProfileInstance == null
            || workPermit.ApplicationProfileInstance.ID != applicationProfileInstanceId)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "This work permit does not belong to this application.",
            };
        }

        foreach (var item in (workPermit.WorkPermitItems ?? Array.Empty<WorkPermitItem>()).ToList())
            objectSpace.Delete(item);
        var caption = workPermit.WorkPermitNumber?.Trim() ?? string.Empty;
        objectSpace.Delete(workPermit);
        objectSpace.CommitChanges();
        return new IssueIssuedHeaderCreateResult { Succeeded = true, HeaderCaption = caption };
    }

    private static IssueIssuedHeaderCreateResult DeleteRejection(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        Guid headerId)
    {
        var rejection = objectSpace.GetObjectByKey<Rejection>(headerId);
        if (rejection == null)
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Rejection was not found." };
        if (rejection.ApplicationProfileInstance == null
            || rejection.ApplicationProfileInstance.ID != applicationProfileInstanceId)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "This rejection does not belong to this application.",
            };
        }

        foreach (var item in (rejection.RejectionItems ?? Array.Empty<RejectionItem>()).ToList())
            objectSpace.Delete(item);
        var caption = rejection.RejectedDocNumber?.Trim() ?? string.Empty;
        objectSpace.Delete(rejection);
        objectSpace.CommitChanges();
        return new IssueIssuedHeaderCreateResult { Succeeded = true, HeaderCaption = caption };
    }

    private static IssueIssuedHeaderCreateResult DeleteBorderZone(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        Guid headerId)
    {
        var borderZone = objectSpace.GetObjectByKey<BorderZone>(headerId);
        if (borderZone == null)
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Border zone was not found." };
        if (borderZone.ApplicationProfileInstance == null
            || borderZone.ApplicationProfileInstance.ID != applicationProfileInstanceId)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "This border zone does not belong to this application.",
            };
        }

        foreach (var item in (borderZone.BorderZoneItems ?? Array.Empty<BorderZoneItem>()).ToList())
            objectSpace.Delete(item);
        var caption = borderZone.BorderZoneNumber?.Trim() ?? string.Empty;
        objectSpace.Delete(borderZone);
        objectSpace.CommitChanges();
        return new IssueIssuedHeaderCreateResult { Succeeded = true, HeaderCaption = caption };
    }

    public static string? ValidateDocumentBytes(IObjectSpace objectSpace, string fileName, byte[] content)
    {
        if (content == null || content.Length == 0)
            return "The file is empty.";

        var maxMb = SystemSettings.TryGetInstance(objectSpace)?.MaxDocumentSizeInMB
            ?? SystemSettings.DefaultMaxDocumentSizeInMB;
        var maxBytes = Math.Max(1, maxMb) * 1024L * 1024L;
        if (content.LongLength > maxBytes)
            return $"The file exceeds the maximum allowed size of {maxMb} MB.";

        var probe = objectSpace.CreateObject<FileData>();
        probe.FileName = string.IsNullOrWhiteSpace(fileName) ? "invitation-copy.pdf" : Path.GetFileName(fileName.Trim());
        probe.Content = content;
        probe.Size = content.Length;
        try
        {
            if (!DocumentFileUploadConstraints.TryValidate(probe, out var validationError))
                return validationError ?? "The file is not valid.";
            return null;
        }
        finally
        {
            objectSpace.Delete(probe);
        }
    }

    public static IssueIssuedHeaderCreateResult AddDocument(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        IssueIssuedHeaderKind kind,
        Guid headerId,
        string fileName,
        byte[] content)
    {
        if (objectSpace == null || applicationProfileInstanceId == Guid.Empty || headerId == Guid.Empty)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Upload is not available.",
            };
        }

        var validationError = ValidateDocumentBytes(objectSpace, fileName, content);
        if (!string.IsNullOrEmpty(validationError))
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = validationError,
            };
        }

        try
        {
            if (!HeaderBelongsToInstance(objectSpace, kind, headerId, applicationProfileInstanceId))
            {
                return new IssueIssuedHeaderCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "This record does not belong to this application.",
                };
            }

            var file = objectSpace.CreateObject<FileData>();
            file.FileName = string.IsNullOrWhiteSpace(fileName) ? "invitation-copy.pdf" : Path.GetFileName(fileName.Trim());
            file.Content = content;
            file.Size = content.Length;

            switch (kind)
            {
                case IssueIssuedHeaderKind.Invitation:
                {
                    var invitation = objectSpace.GetObjectByKey<Invitation>(headerId);
                    var doc = objectSpace.CreateObject<InvitationDocument>();
                    doc.Invitation = invitation;
                    doc.File = file;
                    invitation!.Documents ??= new System.Collections.ObjectModel.ObservableCollection<InvitationDocument>();
                    invitation.Documents.Add(doc);
                    break;
                }
                case IssueIssuedHeaderKind.WorkPermit:
                {
                    var workPermit = objectSpace.GetObjectByKey<WorkPermit>(headerId);
                    var doc = objectSpace.CreateObject<WorkPermitDocument>();
                    doc.WorkPermit = workPermit;
                    doc.File = file;
                    workPermit!.Documents ??= new System.Collections.ObjectModel.ObservableCollection<WorkPermitDocument>();
                    workPermit.Documents.Add(doc);
                    break;
                }
                case IssueIssuedHeaderKind.Rejection:
                {
                    var rejection = objectSpace.GetObjectByKey<Rejection>(headerId);
                    var doc = objectSpace.CreateObject<RejectionDocument>();
                    doc.Rejection = rejection;
                    doc.File = file;
                    rejection!.Documents ??= new System.Collections.ObjectModel.ObservableCollection<RejectionDocument>();
                    rejection.Documents.Add(doc);
                    break;
                }
                case IssueIssuedHeaderKind.BorderZone:
                {
                    var borderZone = objectSpace.GetObjectByKey<BorderZone>(headerId);
                    var doc = objectSpace.CreateObject<BorderZoneDocument>();
                    doc.BorderZone = borderZone;
                    doc.File = file;
                    borderZone!.Documents ??= new System.Collections.ObjectModel.ObservableCollection<BorderZoneDocument>();
                    borderZone.Documents.Add(doc);
                    break;
                }
                default:
                    objectSpace.Delete(file);
                    return new IssueIssuedHeaderCreateResult
                    {
                        Succeeded = false,
                        ErrorMessage = "Unknown header kind.",
                    };
            }

            objectSpace.CommitChanges();
            return new IssueIssuedHeaderCreateResult { Succeeded = true, HeaderId = headerId };
        }
        catch (Exception ex)
        {
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = ex.Message };
        }
    }

    public static IssueIssuedHeaderCreateResult RemoveDocument(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        IssueIssuedHeaderKind kind,
        Guid headerId,
        Guid documentId)
    {
        if (objectSpace == null || headerId == Guid.Empty || documentId == Guid.Empty)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Remove is not available.",
            };
        }

        try
        {
            if (!HeaderBelongsToInstance(objectSpace, kind, headerId, applicationProfileInstanceId))
            {
                return new IssueIssuedHeaderCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "This record does not belong to this application.",
                };
            }

            switch (kind)
            {
                case IssueIssuedHeaderKind.Invitation:
                {
                    var doc = objectSpace.GetObjectByKey<InvitationDocument>(documentId);
                    if (doc?.Invitation == null || doc.Invitation.ID != headerId)
                        return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "File was not found." };
                    if (doc.File != null)
                        objectSpace.Delete(doc.File);
                    objectSpace.Delete(doc);
                    break;
                }
                case IssueIssuedHeaderKind.WorkPermit:
                {
                    var doc = objectSpace.GetObjectByKey<WorkPermitDocument>(documentId);
                    if (doc?.WorkPermit == null || doc.WorkPermit.ID != headerId)
                        return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "File was not found." };
                    if (doc.File != null)
                        objectSpace.Delete(doc.File);
                    objectSpace.Delete(doc);
                    break;
                }
                case IssueIssuedHeaderKind.Rejection:
                {
                    var doc = objectSpace.GetObjectByKey<RejectionDocument>(documentId);
                    if (doc?.Rejection == null || doc.Rejection.ID != headerId)
                        return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "File was not found." };
                    if (doc.File != null)
                        objectSpace.Delete(doc.File);
                    objectSpace.Delete(doc);
                    break;
                }
                case IssueIssuedHeaderKind.BorderZone:
                {
                    var doc = objectSpace.GetObjectByKey<BorderZoneDocument>(documentId);
                    if (doc?.BorderZone == null || doc.BorderZone.ID != headerId)
                        return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "File was not found." };
                    if (doc.File != null)
                        objectSpace.Delete(doc.File);
                    objectSpace.Delete(doc);
                    break;
                }
                default:
                    return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Unknown header kind." };
            }

            objectSpace.CommitChanges();
            return new IssueIssuedHeaderCreateResult { Succeeded = true, HeaderId = headerId };
        }
        catch (Exception ex)
        {
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = ex.Message };
        }
    }

    private static bool HeaderBelongsToInstance(
        IObjectSpace objectSpace,
        IssueIssuedHeaderKind kind,
        Guid headerId,
        Guid applicationProfileInstanceId)
    {
        return kind switch
        {
            IssueIssuedHeaderKind.Invitation => objectSpace.GetObjectByKey<Invitation>(headerId)
                is Invitation inv && inv.ApplicationProfileInstance?.ID == applicationProfileInstanceId,
            IssueIssuedHeaderKind.WorkPermit => objectSpace.GetObjectByKey<WorkPermit>(headerId)
                is WorkPermit wp && wp.ApplicationProfileInstance?.ID == applicationProfileInstanceId,
            IssueIssuedHeaderKind.Rejection => objectSpace.GetObjectByKey<Rejection>(headerId)
                is Rejection rj && rj.ApplicationProfileInstance?.ID == applicationProfileInstanceId,
            IssueIssuedHeaderKind.BorderZone => objectSpace.GetObjectByKey<BorderZone>(headerId)
                is BorderZone bz && bz.ApplicationProfileInstance?.ID == applicationProfileInstanceId,
            _ => false,
        };
    }

    private static void BindDocuments(
        IObjectSpace objectSpace,
        IssueIssuedHeaderComposeDraft draft,
        IssueIssuedHeaderKind kind,
        Guid headerId)
    {
        draft.Documents.Clear();
        foreach (var row in QueryDocumentRows(objectSpace, kind, headerId))
            draft.Documents.Add(row);
    }

    public static IReadOnlyList<IssueIssuedHeaderDocumentRow> ListDocuments(
        IObjectSpace objectSpace,
        IssueIssuedHeaderKind kind,
        Guid headerId) =>
        QueryDocumentRows(objectSpace, kind, headerId);

    private static IReadOnlyList<IssueIssuedHeaderDocumentRow> QueryDocumentRows(
        IObjectSpace objectSpace,
        IssueIssuedHeaderKind kind,
        Guid headerId)
    {
        if (objectSpace == null || headerId == Guid.Empty)
            return Array.Empty<IssueIssuedHeaderDocumentRow>();

        return kind switch
        {
            IssueIssuedHeaderKind.Invitation => objectSpace.GetObjectsQuery<InvitationDocument>()
                .Where(d => d.Invitation != null && d.Invitation.ID == headerId)
                .ToList()
                .Select(ToDocumentRow)
                .Where(r => r != null)
                .Select(r => r!)
                .ToList(),
            IssueIssuedHeaderKind.WorkPermit => objectSpace.GetObjectsQuery<WorkPermitDocument>()
                .Where(d => d.WorkPermit != null && d.WorkPermit.ID == headerId)
                .ToList()
                .Select(ToDocumentRow)
                .Where(r => r != null)
                .Select(r => r!)
                .ToList(),
            IssueIssuedHeaderKind.Rejection => objectSpace.GetObjectsQuery<RejectionDocument>()
                .Where(d => d.Rejection != null && d.Rejection.ID == headerId)
                .ToList()
                .Select(ToDocumentRow)
                .Where(r => r != null)
                .Select(r => r!)
                .ToList(),
            IssueIssuedHeaderKind.BorderZone => objectSpace.GetObjectsQuery<BorderZoneDocument>()
                .Where(d => d.BorderZone != null && d.BorderZone.ID == headerId)
                .ToList()
                .Select(ToDocumentRow)
                .Where(r => r != null)
                .Select(r => r!)
                .ToList(),
            _ => Array.Empty<IssueIssuedHeaderDocumentRow>(),
        };
    }

    private static IssueIssuedHeaderDocumentRow? ToDocumentRow(DocumentBase? doc)
    {
        if (doc == null)
            return null;
        var name = doc.File?.FileName?.Trim();
        if (string.IsNullOrWhiteSpace(name) && (doc.File == null || doc.File.Size <= 0))
            return null;
        return new IssueIssuedHeaderDocumentRow
        {
            DocumentId = doc.ID,
            FileName = string.IsNullOrWhiteSpace(name) ? "Attachment" : name,
            SizeBytes = doc.File?.Size ?? 0,
        };
    }

    public static IssueIssuedHeaderComposeDraft? LoadDraft(IObjectSpace objectSpace, Guid applicationId, IssueIssuedHeaderKind kind, Guid? excludeInvitationIdFromOccupancy = null)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return null;

        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        if (instance == null)
            return null;

        var catalogKey = CatalogKeyFor(kind);
        if (!ApplicationWorkspaceIssuedRecordsCatalog.IsVisible(instance, catalogKey))
            return null;

        // Person ids already on an issued invitation letter for this case (query — not lazy navigations).
        // When editing one invitation, exclude it so its own people are not locked as "on another".
        var onOtherInvitation = kind == IssueIssuedHeaderKind.Invitation
            ? LoadInvitationOccupancyMap(objectSpace, applicationId, excludeInvitationIdFromOccupancy)
            : new Dictionary<Guid, string>();

        var defaultWpLocations = instance.MovementPermitLocation?.Trim() ?? string.Empty;
        var people = ApplicationRosterHelper.GetRosterPeople(instance)
            .Where(p => p != null)
            .Where(p => kind != IssueIssuedHeaderKind.WorkPermit || p!.IsEmployee)
            .OrderBy(p => p!.LastName)
            .ThenBy(p => p!.FirstName)
            .Select(p =>
            {
                var passport = ApplicationProfileInstancePersonValidItems.ResolvePassport(p);
                var ready = passport != null;
                var onOther = onOtherInvitation.TryGetValue(p!.ID, out var otherNumber);
                var include = kind switch
                {
                    IssueIssuedHeaderKind.Invitation => ready && !onOther,
                    IssueIssuedHeaderKind.WorkPermit => p.IsEmployee && ready,
                    _ => true,
                };
                var status = !ready
                    ? "Missing passport"
                    : onOther
                        ? (string.IsNullOrWhiteSpace(otherNumber) ? "Already on an invitation" : $"On invitation {otherNumber}")
                        : "Ready";
                var line = new IssueIssuedHeaderPersonLineDraft
                {
                    PersonId = p.ID,
                    PersonName = p.FullName?.Trim() ?? p.ToString() ?? string.Empty,
                    PassportId = passport?.ID,
                    PassportNumber = passport?.PassportNumber?.Trim() ?? string.Empty,
                    PassportExpiration = passport?.ExpirationDate,
                    Include = include,
                    IncludeLocked = kind == IssueIssuedHeaderKind.Invitation && onOther,
                    IsReady = ready,
                    StatusCaption = status,
                    IsEmployee = p.IsEmployee,
                };
                if (kind == IssueIssuedHeaderKind.WorkPermit)
                    ApplyWorkPermitCardDefaults(p, line, defaultWpLocations, instance);
                return line;
            })
            .Where(line => kind != IssueIssuedHeaderKind.Invitation || !line.IncludeLocked)
            .ToList();

        var visaCategories = kind == IssueIssuedHeaderKind.Invitation
            ? objectSpace.GetObjectsQuery<VisaCategory>()
                .OrderBy(c => c.NameTm)
                .Select(c => new IssueIssuedHeaderLookupOption { Id = c.ID, Caption = c.NameTm ?? c.Code ?? c.ID.ToString("N") })
                .ToList()
            : new List<IssueIssuedHeaderLookupOption>();
        var visaPeriods = kind == IssueIssuedHeaderKind.Invitation
            ? objectSpace.GetObjectsQuery<VisaPeriod>()
                .OrderBy(c => c.NameTm)
                .Select(c => new IssueIssuedHeaderLookupOption { Id = c.ID, Caption = c.NameTm ?? c.Code ?? c.ID.ToString("N") })
                .ToList()
            : new List<IssueIssuedHeaderLookupOption>();
        var validityDurations = kind == IssueIssuedHeaderKind.BorderZone
            ? objectSpace.GetObjectsQuery<ValidityDuration>()
                .OrderBy(c => c.NameTm)
                .Select(c => new IssueIssuedHeaderLookupOption { Id = c.ID, Caption = c.NameTm ?? c.Code ?? c.ID.ToString("N") })
                .ToList()
            : new List<IssueIssuedHeaderLookupOption>();

        Guid? defaultCategoryId = kind == IssueIssuedHeaderKind.Invitation
            ? objectSpace.GetObjectsQuery<VisaCategory>().Where(c => c.IsDefault).Select(c => (Guid?)c.ID).FirstOrDefault()
                ?? visaCategories.FirstOrDefault()?.Id
            : null;
        Guid? defaultPeriodId = kind == IssueIssuedHeaderKind.Invitation
            ? objectSpace.GetObjectsQuery<VisaPeriod>().Where(c => c.IsDefault).Select(c => (Guid?)c.ID).FirstOrDefault()
                ?? visaPeriods.FirstOrDefault()?.Id
            : null;
        Guid? defaultDurationId = kind == IssueIssuedHeaderKind.BorderZone
            ? objectSpace.GetObjectsQuery<ValidityDuration>().Where(c => c.IsDefault).Select(c => (Guid?)c.ID).FirstOrDefault()
                ?? validityDurations.FirstOrDefault()?.Id
            : null;

        var borderZoneNames = kind == IssueIssuedHeaderKind.Invitation
            ? CommaSeparatedCatalogHelper.LoadCatalogNames(
                objectSpace,
                typeof(BorderZoneName),
                CommaSeparatedSelectionHelper.NoneValue)
            : Array.Empty<string>();
        var defaultBorderZone = kind == IssueIssuedHeaderKind.Invitation
            ? BorderZoneSelectionHelper.ResolveForIssuedVisa(null, instance)
            : string.Empty;

        return new IssueIssuedHeaderComposeDraft
        {
            Kind = kind,
            ApplicationProfileInstanceId = applicationId,
            ApplicationCaption = instance.FullApplicationNumber?.Trim()
                ?? instance.ApplicationNumber?.ToString()
                ?? applicationId.ToString("N")[..8],
            Title = kind switch
            {
                IssueIssuedHeaderKind.Invitation => "New invitation",
                IssueIssuedHeaderKind.WorkPermit => "New work permit",
                IssueIssuedHeaderKind.Rejection => "New rejection",
                IssueIssuedHeaderKind.BorderZone => "New border zone",
                _ => "New issued record",
            },
            HeaderNumber = string.Empty,
            PrimaryDate = DateTime.Today,
            ExpirationDate = kind == IssueIssuedHeaderKind.Invitation ? DateTime.Today.AddMonths(6) : null,
            VisaCategoryId = defaultCategoryId,
            VisaPeriodId = defaultPeriodId,
            BorderZoneLocation = defaultBorderZone,
            ValidityDurationId = defaultDurationId,
            VisaCategories = visaCategories,
            VisaPeriods = visaPeriods,
            BorderZoneNames = borderZoneNames,
            ValidityDurations = validityDurations,
            People = people,
        };
    }

    public static IssueIssuedHeaderCreateResult Create(
        IObjectSpace objectSpace,
        IssueIssuedHeaderComposeDraft draft)
    {
        if (objectSpace == null || draft == null)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Compose session is not available.",
            };
        }

        var selected = draft.Kind == IssueIssuedHeaderKind.Invitation
            ? SanitizeInvitationPersonSelection(draft)
            : draft.Kind == IssueIssuedHeaderKind.WorkPermit
                ? draft.People.Where(p => p.Include && p.IsEmployee).ToList()
                : draft.People.Where(p => p.Include).ToList();
        if (selected.Count == 0)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Select at least one person for the letter.",
            };
        }

        var notReady = selected.Where(p => !p.IsReady).ToList();
        if (notReady.Count > 0)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = $"Cannot create — {notReady.Count} selected person(s) have no passport.",
            };
        }

        if (string.IsNullOrWhiteSpace(draft.HeaderNumber))
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = draft.Kind switch
                {
                    IssueIssuedHeaderKind.Invitation => "Invitation number is required.",
                    IssueIssuedHeaderKind.WorkPermit => "Work permit number is required.",
                    IssueIssuedHeaderKind.Rejection => "Rejected document number is required.",
                    IssueIssuedHeaderKind.BorderZone => "Border zone number is required.",
                    _ => "Number is required.",
                },
            };
        }

        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(draft.ApplicationProfileInstanceId);
        if (instance == null)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Application profile instance was not found.",
            };
        }

        if ((draft.Kind == IssueIssuedHeaderKind.Invitation || draft.Kind == IssueIssuedHeaderKind.WorkPermit)
            && !ApplicationProcessNumberHelper.TryRequireForIssued(instance, out var processNumberError))
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = processNumberError,
            };
        }

        if (draft.Kind == IssueIssuedHeaderKind.WorkPermit)
        {
            var already = objectSpace.GetObjectsQuery<WorkPermit>()
                .Any(w => w.ApplicationProfileInstance != null
                    && w.ApplicationProfileInstance.ID == instance.ID);
            if (already)
            {
                return new IssueIssuedHeaderCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "This case already has a work permit.",
                };
            }

            var wpError = ValidateWorkPermitCards(selected);
            if (wpError != null)
            {
                return new IssueIssuedHeaderCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = wpError,
                };
            }
        }

        if (draft.Kind == IssueIssuedHeaderKind.Invitation)
        {
            var dup = FindPeopleAlreadyOnInvitation(objectSpace, instance.ID, selected.Select(p => p.PersonId), excludeInvitationId: null);
            if (dup.Count > 0)
            {
                return new IssueIssuedHeaderCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = FormatAlreadyOnInvitationError(dup),
                };
            }
        }

        try
        {
            return draft.Kind switch
            {
                IssueIssuedHeaderKind.Invitation => CreateInvitation(objectSpace, instance, draft, selected),
                IssueIssuedHeaderKind.WorkPermit => CreateWorkPermit(objectSpace, instance, draft, selected),
                IssueIssuedHeaderKind.Rejection => CreateRejection(objectSpace, instance, draft, selected),
                IssueIssuedHeaderKind.BorderZone => CreateBorderZone(objectSpace, instance, draft, selected),
                _ => new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Unknown header kind." },
            };
        }
        catch (Exception ex)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = ex.Message,
            };
        }
    }


    /// <summary>
    /// Invitation edit/create: never treat "on another letter" rows as selected, even if Include is stale.
    /// </summary>
    private static List<IssueIssuedHeaderPersonLineDraft> SanitizeInvitationPersonSelection(
        IssueIssuedHeaderComposeDraft draft)
    {
        if (draft?.People == null)
            return new List<IssueIssuedHeaderPersonLineDraft>();

        return draft.People
            .Where(p =>
                p.Include
                && p.PersonId != Guid.Empty
                && (p.ExistingLineId is Guid || !p.IncludeLocked))
            .ToList();
    }

    private static HashSet<Guid> QueryInvitationPersonIds(IObjectSpace objectSpace, Guid invitationId)
    {
        if (objectSpace == null || invitationId == Guid.Empty)
            return new HashSet<Guid>();

        return IssuedDocumentLifecycle.WhereInvitationItemNotCancelled(
            objectSpace.GetObjectsQuery<InvitationItem>()
                .Where(ii =>
                    ii.Invitation != null
                    && ii.Invitation.ID == invitationId
                    && ii.Person != null))
            .Select(ii => ii.Person!.ID)
            .ToHashSet();
    }
    private static Dictionary<Guid, string> LoadInvitationOccupancyMap(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        Guid? excludeInvitationId)
    {
        if (objectSpace == null || applicationProfileInstanceId == Guid.Empty)
            return new Dictionary<Guid, string>();

        var invitationQuery = objectSpace.GetObjectsQuery<Invitation>()
            .Where(inv =>
                inv.ApplicationProfileInstance != null
                && inv.ApplicationProfileInstance.ID == applicationProfileInstanceId);
        if (excludeInvitationId is Guid excludeId && excludeId != Guid.Empty)
            invitationQuery = invitationQuery.Where(inv => inv.ID != excludeId);

        var invitationNumbers = invitationQuery
            .Select(inv => new { inv.ID, Number = inv.InvitationNumber ?? string.Empty })
            .ToList();
        if (invitationNumbers.Count == 0)
            return new Dictionary<Guid, string>();

        var invitationIds = invitationNumbers.Select(x => x.ID).ToHashSet();
        var numberByInvitation = invitationNumbers.ToDictionary(x => x.ID, x => x.Number.Trim());

        return IssuedDocumentLifecycle.WhereInvitationItemNotCancelled(
            objectSpace.GetObjectsQuery<InvitationItem>()
                .Where(ii =>
                    ii.Person != null
                    && ii.Invitation != null
                    && invitationIds.Contains(ii.Invitation.ID)))
            .Select(ii => new { PersonId = ii.Person!.ID, InvitationId = ii.Invitation!.ID })
            .AsEnumerable()
            .GroupBy(x => x.PersonId)
            .ToDictionary(
                g => g.Key,
                g => numberByInvitation.TryGetValue(g.First().InvitationId, out var n) ? n : string.Empty);
    }
    /// <summary>
    /// People already listed on an invitation letter for this case (DB query — not navigation collections).
    /// </summary>
    private static List<(string PersonName, string InvitationNumber)> FindPeopleAlreadyOnInvitation(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        IEnumerable<Guid> personIds,
        Guid? excludeInvitationId)
    {
        var wanted = personIds.Where(id => id != Guid.Empty).ToHashSet();
        if (wanted.Count == 0 || applicationProfileInstanceId == Guid.Empty)
            return new List<(string, string)>();

        var invitationQuery = objectSpace.GetObjectsQuery<Invitation>()
            .Where(inv =>
                inv.ApplicationProfileInstance != null
                && inv.ApplicationProfileInstance.ID == applicationProfileInstanceId);
        if (excludeInvitationId is Guid excludeId && excludeId != Guid.Empty)
            invitationQuery = invitationQuery.Where(inv => inv.ID != excludeId);

        var invitationIds = invitationQuery.Select(inv => inv.ID).ToHashSet();
        if (invitationIds.Count == 0)
            return new List<(string, string)>();

        return IssuedDocumentLifecycle.WhereInvitationItemNotCancelled(
            objectSpace.GetObjectsQuery<InvitationItem>()
                .Where(ii =>
                    ii.Person != null
                    && ii.Invitation != null
                    && invitationIds.Contains(ii.Invitation.ID)
                    && wanted.Contains(ii.Person.ID)))
            .Select(ii => new
            {
                PersonName = ii.Person!.FullName ?? string.Empty,
                InvitationNumber = ii.Invitation!.InvitationNumber ?? string.Empty,
            })
            .AsEnumerable()
            .Select(x => (
                PersonName: x.PersonName.Trim(),
                InvitationNumber: x.InvitationNumber.Trim()))
            .GroupBy(x => x.PersonName + "|" + x.InvitationNumber, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();
    }

    private static string FormatAlreadyOnInvitationError(List<(string PersonName, string InvitationNumber)> dup)
    {
        var parts = dup.Select(d =>
            string.IsNullOrWhiteSpace(d.InvitationNumber)
                ? d.PersonName
                : $"{d.PersonName} (invitation {d.InvitationNumber})");
        return "Each person may have only one invitation on this application. Already invited: "
            + string.Join(", ", parts) + ".";
    }
    private static IssueIssuedHeaderCreateResult CreateInvitation(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        IssueIssuedHeaderComposeDraft draft,
        List<IssueIssuedHeaderPersonLineDraft> selected)
    {
        var invitation = objectSpace.CreateObject<Invitation>();
        invitation.ApplicationProfileInstance = instance;
        invitation.InvitationNumber = draft.HeaderNumber.Trim();
        invitation.IssuedDate = draft.PrimaryDate.Date;
        if (draft.ExpirationDate.HasValue)
            invitation.ExpirationDate = draft.ExpirationDate.Value.Date;

        if (draft.VisaCategoryId is Guid categoryId && categoryId != Guid.Empty)
            invitation.VisaCategory = objectSpace.GetObjectByKey<VisaCategory>(categoryId);
        if (draft.VisaPeriodId is Guid periodId && periodId != Guid.Empty)
            invitation.VisaPeriod = objectSpace.GetObjectByKey<VisaPeriod>(periodId);

        invitation.BorderZoneLocation = string.IsNullOrWhiteSpace(draft.BorderZoneLocation)
            ? BorderZoneSelectionHelper.ResolveForIssuedVisa(null, instance)
            : draft.BorderZoneLocation.Trim();
        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(invitation);

        invitation.IsVisaStartAndEndDateDefined = draft.IsVisaStartAndEndDateDefined;
        invitation.VisaStartDate = draft.IsVisaStartAndEndDateDefined ? draft.VisaStartDate?.Date : null;
        invitation.VisaEndDate = draft.IsVisaStartAndEndDateDefined ? draft.VisaEndDate?.Date : null;

        if (invitation.InvitationItems == null) invitation.InvitationItems = new System.Collections.ObjectModel.ObservableCollection<InvitationItem>();
        var lines = new List<IssueIssuedHeaderCreatedLine>();
        foreach (var row in selected)
        {
            var person = objectSpace.GetObjectByKey<Person>(row.PersonId);
            if (person == null)
                continue;

            var item = objectSpace.CreateObject<InvitationItem>();
            item.Invitation = invitation;
            item.Person = person;
            item.Passport = row.PassportId is Guid pid
                ? objectSpace.GetObjectByKey<Passport>(pid)
                : ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
            invitation.InvitationItems.Add(item);
            lines.Add(new IssueIssuedHeaderCreatedLine
            {
                LineId = item.ID,
                PersonId = person.ID,
                PersonName = row.PersonName,
                PassportNumber = row.PassportNumber,
                CanIssueVisa = true,
            });
        }

        objectSpace.CommitChanges();

        // Refresh line ids after commit
        lines = invitation.InvitationItems
            .Where(i => i != null)
            .Select(i => new IssueIssuedHeaderCreatedLine
            {
                LineId = i.ID,
                PersonId = i.Person?.ID ?? Guid.Empty,
                PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
                CanIssueVisa = !i.IsUsed && !i.IsCancelled && !i.IsChanged && i.Passport != null,
            })
            .ToList();

        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = invitation.ID,
            HeaderCaption = invitation.InvitationNumber,
            Lines = lines,
        };
    }

    private static IssueIssuedHeaderCreateResult CreateWorkPermit(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        IssueIssuedHeaderComposeDraft draft,
        List<IssueIssuedHeaderPersonLineDraft> selected)
    {
        var workPermit = objectSpace.CreateObject<WorkPermit>();
        workPermit.ApplicationProfileInstance = instance;
        workPermit.WorkPermitNumber = draft.HeaderNumber.Trim();
        workPermit.IssuedDate = draft.PrimaryDate.Date;

        var defaultLocations = instance.MovementPermitLocation?.Trim() ?? string.Empty;
        if (workPermit.WorkPermitItems == null) workPermit.WorkPermitItems = new System.Collections.ObjectModel.ObservableCollection<WorkPermitItem>();
        foreach (var row in selected)
        {
            var person = objectSpace.GetObjectByKey<Person>(row.PersonId);
            if (person == null || !person.IsEmployee)
                continue;

            var item = objectSpace.CreateObject<WorkPermitItem>();
            item.WorkPermit = workPermit;
            item.Person = person;
            ApplyWorkPermitItemFields(objectSpace, item, row, defaultLocations);
            workPermit.WorkPermitItems.Add(item);
        }

        objectSpace.CommitChanges();

        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = workPermit.ID,
            HeaderCaption = workPermit.WorkPermitNumber,
            Lines = workPermit.WorkPermitItems.Select(ToWorkPermitCreatedLine).ToList(),
        };
    }

    private static IssueIssuedHeaderCreateResult CreateRejection(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        IssueIssuedHeaderComposeDraft draft,
        List<IssueIssuedHeaderPersonLineDraft> selected)
    {
        var rejection = objectSpace.CreateObject<Rejection>();
        rejection.ApplicationProfileInstance = instance;
        rejection.RejectedDocNumber = draft.HeaderNumber.Trim();
        rejection.Date = draft.PrimaryDate.Date;
        rejection.Reason = draft.Reason?.Trim() ?? string.Empty;
        if (rejection.RejectionItems == null) rejection.RejectionItems = new System.Collections.ObjectModel.ObservableCollection<RejectionItem>();

        foreach (var row in selected)
        {
            var person = objectSpace.GetObjectByKey<Person>(row.PersonId);
            if (person == null)
                continue;

            var item = objectSpace.CreateObject<RejectionItem>();
            item.Rejection = rejection;
            item.Person = person;
            item.Passport = row.PassportId is Guid pid
                ? objectSpace.GetObjectByKey<Passport>(pid)
                : ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
            rejection.RejectionItems.Add(item);
        }

        objectSpace.CommitChanges();

        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = rejection.ID,
            HeaderCaption = rejection.RejectedDocNumber,
            Lines = rejection.RejectionItems.Select(i => new IssueIssuedHeaderCreatedLine
            {
                LineId = i.ID,
                PersonId = i.Person?.ID ?? Guid.Empty,
                PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
            }).ToList(),
        };
    }

    private static IssueIssuedHeaderCreateResult CreateBorderZone(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        IssueIssuedHeaderComposeDraft draft,
        List<IssueIssuedHeaderPersonLineDraft> selected)
    {
        if (draft.ValidityDurationId is not Guid durationId || durationId == Guid.Empty)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Validity duration is required.",
            };
        }

        var borderZone = objectSpace.CreateObject<BorderZone>();
        borderZone.ApplicationProfileInstance = instance;
        borderZone.BorderZoneNumber = draft.HeaderNumber.Trim();
        borderZone.StartDate = draft.PrimaryDate.Date;
        borderZone.ValidityDuration = objectSpace.GetObjectByKey<ValidityDuration>(durationId);
        if (borderZone.BorderZoneItems == null) borderZone.BorderZoneItems = new System.Collections.ObjectModel.ObservableCollection<BorderZoneItem>();

        foreach (var row in selected)
        {
            var person = objectSpace.GetObjectByKey<Person>(row.PersonId);
            if (person == null)
                continue;

            var item = objectSpace.CreateObject<BorderZoneItem>();
            item.BorderZone = borderZone;
            item.Person = person;
            item.Passport = row.PassportId is Guid pid
                ? objectSpace.GetObjectByKey<Passport>(pid)
                : ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
            borderZone.BorderZoneItems.Add(item);
        }

        objectSpace.CommitChanges();

        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = borderZone.ID,
            HeaderCaption = borderZone.BorderZoneNumber,
            Lines = borderZone.BorderZoneItems.Select(i => new IssueIssuedHeaderCreatedLine
            {
                LineId = i.ID,
                PersonId = i.Person?.ID ?? Guid.Empty,
                PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
            }).ToList(),
        };
    }
    public static IssueIssuedHeaderComposeDraft? LoadExistingDraft(
        IObjectSpace objectSpace,
        Guid applicationId,
        IssueIssuedHeaderKind kind,
        Guid headerId)
    {
        var draft = LoadDraft(objectSpace, applicationId, kind, excludeInvitationIdFromOccupancy: headerId);
        if (draft == null || headerId == Guid.Empty)
            return null;

        draft.ExistingHeaderId = headerId;

        switch (kind)
        {
            case IssueIssuedHeaderKind.Invitation:
            {
                var invitation = objectSpace.GetObjectByKey<Invitation>(headerId);
                if (invitation == null)
                    return null;

                draft.Title = string.IsNullOrWhiteSpace(invitation.InvitationNumber)
                    ? "Edit invitation"
                    : $"Edit invitation {invitation.InvitationNumber.Trim()}";
                draft.HeaderNumber = invitation.InvitationNumber?.Trim() ?? string.Empty;
                draft.PrimaryDate = invitation.IssuedDate == default ? DateTime.Today : invitation.IssuedDate.Date;
                draft.ExpirationDate = invitation.ExpirationDate?.Date;
                draft.VisaCategoryId = invitation.VisaCategory?.ID;
                draft.VisaPeriodId = invitation.VisaPeriod?.ID;
                draft.BorderZoneLocation = string.IsNullOrWhiteSpace(invitation.BorderZoneLocation)
                    ? BorderZoneSelectionHelper.NoneValue
                    : invitation.BorderZoneLocation.Trim();
                draft.IsVisaStartAndEndDateDefined = invitation.IsVisaStartAndEndDateDefined;
                draft.VisaStartDate = invitation.VisaStartDate?.Date;
                draft.VisaEndDate = invitation.VisaEndDate?.Date;

                // Force collection load (avoid empty/stale InvitationItems after switching headers).
                var items = (invitation.InvitationItems ?? Array.Empty<InvitationItem>())
                    .Where(i => i != null)
                    .ToList();
                if (items.Count == 0)
                {
                    objectSpace.ReloadObject(invitation);
                    items = (invitation.InvitationItems ?? Array.Empty<InvitationItem>())
                        .Where(i => i != null)
                        .ToList();
                }

                // Replace people lines so Blazor cannot reuse prior include/status state by reference.
                var rebuilt = new List<IssueIssuedHeaderPersonLineDraft>(draft.People.Count);
                foreach (var person in draft.People)
                {
                    var item = items.FirstOrDefault(i => i.Person != null && i.Person.ID == person.PersonId);
                    var line = new IssueIssuedHeaderPersonLineDraft
                    {
                        PersonId = person.PersonId,
                        PersonName = person.PersonName,
                        PassportId = person.PassportId,
                        PassportNumber = person.PassportNumber,
                        PassportExpiration = person.PassportExpiration,
                        IsReady = person.IsReady,
                        IsEmployee = person.IsEmployee,
                        Include = false,
                        IncludeLocked = person.IncludeLocked,
                        StatusCaption = person.StatusCaption,
                    };

                    if (item != null)
                    {
                        line.Include = true;
                        line.ExistingLineId = item.ID;
                        line.CanIssueVisa = !item.IsUsed && !item.IsCancelled && !item.IsChanged && item.Passport != null;
                        line.IncludeLocked = item.IsUsed || item.IssuedVisa != null;
                        if (item.Passport != null)
                        {
                            line.PassportId = item.Passport.ID;
                            line.PassportNumber = item.Passport.PassportNumber?.Trim() ?? line.PassportNumber;
                            line.IsReady = true;
                        }
                        line.StatusCaption = line.CanIssueVisa ? "On this invitation" : "On this invitation (visa issued/closed)";
                    }
                    else
                    {
                        line.ExistingLineId = null;
                        line.CanIssueVisa = false;
                        line.Include = false;
                    }

                    rebuilt.Add(line);
                }

                draft.People.Clear();
                foreach (var line in rebuilt)
                    draft.People.Add(line);

                BindDocuments(objectSpace, draft, kind, headerId);
                return draft;
            }
            case IssueIssuedHeaderKind.WorkPermit:
            {
                var workPermit = objectSpace.GetObjectByKey<WorkPermit>(headerId);
                if (workPermit == null)
                    return null;

                draft.Title = "Edit work permit";
                draft.HeaderNumber = workPermit.WorkPermitNumber?.Trim() ?? string.Empty;
                draft.PrimaryDate = workPermit.IssuedDate == default ? DateTime.Today : workPermit.IssuedDate.Date;

                var items = workPermit.WorkPermitItems?.Where(i => i != null).ToList()
                    ?? new List<WorkPermitItem>();
                foreach (var person in draft.People)
                {
                    var item = items.FirstOrDefault(i => i.Person != null && i.Person.ID == person.PersonId);
                    if (item == null)
                    {
                        person.Include = false;
                        continue;
                    }

                    person.Include = true;
                    person.ExistingLineId = item.ID;
                    person.IncludeLocked = true;
                    person.StatusCaption = "On this work permit";
                    BindWorkPermitItemToCard(item, person);
                    var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
                    ApplyInstanceProcessNumberToWorkPermitCard(person, instance);
                }

                BindDocuments(objectSpace, draft, kind, headerId);
                return draft;
            }
            case IssueIssuedHeaderKind.Rejection:
            {
                var rejection = objectSpace.GetObjectByKey<Rejection>(headerId);
                if (rejection == null)
                    return null;

                draft.Title = "Edit rejection";
                draft.HeaderNumber = rejection.RejectedDocNumber?.Trim() ?? string.Empty;
                draft.PrimaryDate = rejection.Date == default ? DateTime.Today : rejection.Date.Date;
                draft.Reason = rejection.Reason ?? string.Empty;

                var items = rejection.RejectionItems?.Where(i => i != null).ToList()
                    ?? new List<RejectionItem>();
                foreach (var person in draft.People)
                {
                    var item = items.FirstOrDefault(i => i.Person != null && i.Person.ID == person.PersonId);
                    person.Include = item != null;
                    if (item != null)
                    {
                        person.ExistingLineId = item.ID;
                        person.IncludeLocked = true;
                        person.StatusCaption = "On this rejection";
                        if (item.Passport != null)
                        {
                            person.PassportId = item.Passport.ID;
                            person.PassportNumber = item.Passport.PassportNumber?.Trim() ?? person.PassportNumber;
                            person.IsReady = true;
                        }
                    }
                }

                BindDocuments(objectSpace, draft, kind, headerId);
                return draft;
            }
            case IssueIssuedHeaderKind.BorderZone:
            {
                var borderZone = objectSpace.GetObjectByKey<BorderZone>(headerId);
                if (borderZone == null)
                    return null;

                draft.Title = "Edit border zone";
                draft.HeaderNumber = borderZone.BorderZoneNumber?.Trim() ?? string.Empty;
                draft.PrimaryDate = borderZone.StartDate == default ? DateTime.Today : borderZone.StartDate.Date;
                draft.ValidityDurationId = borderZone.ValidityDuration?.ID;
                draft.ExpirationDate = borderZone.ExpirationDate?.Date;

                var items = borderZone.BorderZoneItems?.Where(i => i != null).ToList()
                    ?? new List<BorderZoneItem>();
                foreach (var person in draft.People)
                {
                    var item = items.FirstOrDefault(i => i.Person != null && i.Person.ID == person.PersonId);
                    person.Include = item != null;
                    if (item != null)
                    {
                        person.ExistingLineId = item.ID;
                        person.IncludeLocked = true;
                        person.StatusCaption = "On this border zone";
                        if (item.Passport != null)
                        {
                            person.PassportId = item.Passport.ID;
                            person.PassportNumber = item.Passport.PassportNumber?.Trim() ?? person.PassportNumber;
                            person.IsReady = true;
                        }
                    }
                }

                BindDocuments(objectSpace, draft, kind, headerId);
                return draft;
            }
            default:
                return null;
        }
    }

    public static IssueIssuedHeaderCreateResult Update(IObjectSpace objectSpace, IssueIssuedHeaderComposeDraft draft)
    {
        if (objectSpace == null || draft?.ExistingHeaderId is not Guid headerId || headerId == Guid.Empty)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Edit session is not available.",
            };
        }

        var syncPeople = draft.SyncPeopleOnSave;
        var selected = draft.Kind == IssueIssuedHeaderKind.Invitation
            ? SanitizeInvitationPersonSelection(draft)
            : draft.Kind == IssueIssuedHeaderKind.WorkPermit
                ? draft.People.Where(p => p.Include && p.IsEmployee).ToList()
                : draft.People.Where(p => p.Include).ToList();

        if (syncPeople || draft.Kind != IssueIssuedHeaderKind.Invitation)
        {
            if (selected.Count == 0)
            {
                return new IssueIssuedHeaderCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "Select at least one person for the letter.",
                };
            }

            var notReady = selected.Where(p => !p.IsReady).ToList();
            if (notReady.Count > 0)
            {
                return new IssueIssuedHeaderCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = $"Cannot save — {notReady.Count} selected person(s) have no passport.",
                };
            }

            if (draft.Kind == IssueIssuedHeaderKind.WorkPermit)
            {
                var wpError = ValidateWorkPermitCards(selected);
                if (wpError != null)
                {
                    return new IssueIssuedHeaderCreateResult
                    {
                        Succeeded = false,
                        ErrorMessage = wpError,
                    };
                }
            }
        }

        if (string.IsNullOrWhiteSpace(draft.HeaderNumber))
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Number is required.",
            };
        }

        try
        {
            return draft.Kind switch
            {
                IssueIssuedHeaderKind.Invitation => UpdateInvitation(objectSpace, draft, headerId, selected, syncPeople),
                IssueIssuedHeaderKind.WorkPermit => UpdateWorkPermit(objectSpace, draft, headerId, selected),
                IssueIssuedHeaderKind.Rejection => UpdateRejection(objectSpace, draft, headerId, selected),
                IssueIssuedHeaderKind.BorderZone => UpdateBorderZone(objectSpace, draft, headerId, selected),
                _ => new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Unknown header kind." },
            };
        }
        catch (Exception ex)
        {
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = ex.Message };
        }
    }

    private static IssueIssuedHeaderCreateResult UpdateInvitation(
        IObjectSpace objectSpace,
        IssueIssuedHeaderComposeDraft draft,
        Guid headerId,
        List<IssueIssuedHeaderPersonLineDraft> selected,
        bool syncPeople)
    {
        var invitation = objectSpace.GetObjectByKey<Invitation>(headerId);
        if (invitation == null)
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Invitation was not found." };

        invitation.InvitationNumber = draft.HeaderNumber.Trim();
        invitation.IssuedDate = draft.PrimaryDate.Date;
        invitation.ExpirationDate = draft.ExpirationDate?.Date;
        invitation.VisaCategory = draft.VisaCategoryId is Guid categoryId && categoryId != Guid.Empty
            ? objectSpace.GetObjectByKey<VisaCategory>(categoryId)
            : null;
        invitation.VisaPeriod = draft.VisaPeriodId is Guid periodId && periodId != Guid.Empty
            ? objectSpace.GetObjectByKey<VisaPeriod>(periodId)
            : null;
        invitation.BorderZoneLocation = string.IsNullOrWhiteSpace(draft.BorderZoneLocation)
            ? BorderZoneSelectionHelper.NoneValue
            : draft.BorderZoneLocation.Trim();
        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(invitation);
        invitation.IsVisaStartAndEndDateDefined = draft.IsVisaStartAndEndDateDefined;
        invitation.VisaStartDate = draft.IsVisaStartAndEndDateDefined ? draft.VisaStartDate?.Date : null;
        invitation.VisaEndDate = draft.IsVisaStartAndEndDateDefined ? draft.VisaEndDate?.Date : null;

        if (invitation.InvitationItems == null)
            invitation.InvitationItems = new System.Collections.ObjectModel.ObservableCollection<InvitationItem>();

        var appId = invitation.ApplicationProfileInstance?.ID
            ?? draft.ApplicationProfileInstanceId;

        // Panel sets SyncPeopleOnSave=false for header-only edits so stale Include flags cannot reshuffle people.
        if (syncPeople)
        {
            var persistedIds = QueryInvitationPersonIds(objectSpace, invitation.ID);
            var selectedIds = selected.Select(s => s.PersonId).Where(id => id != Guid.Empty).ToHashSet();

            if (!selectedIds.SetEquals(persistedIds))
            {
            var newlyAdded = selectedIds.Where(id => !persistedIds.Contains(id)).ToList();
            if (newlyAdded.Count > 0)
            {
                var dup = FindPeopleAlreadyOnInvitation(objectSpace, appId, newlyAdded, excludeInvitationId: invitation.ID);
                if (dup.Count > 0)
                {
                    return new IssueIssuedHeaderCreateResult
                    {
                        Succeeded = false,
                        ErrorMessage = FormatAlreadyOnInvitationError(dup),
                    };
                }
            }

            var persistedItems = objectSpace.GetObjectsQuery<InvitationItem>()
                .Where(i => i.Invitation != null && i.Invitation.ID == invitation.ID)
                .ToList();

            foreach (var item in persistedItems)
            {
                var personId = item.Person?.ID ?? Guid.Empty;
                if (personId == Guid.Empty || selectedIds.Contains(personId))
                    continue;
                if (item.IsUsed || item.IssuedVisa != null || item.IsCancelled || item.IsChanged)
                    continue;
                invitation.InvitationItems.Remove(item);
                objectSpace.Delete(item);
            }

            foreach (var row in selected)
            {
                if (persistedIds.Contains(row.PersonId))
                    continue;
                if (invitation.InvitationItems.Any(i => i.Person != null && i.Person.ID == row.PersonId))
                    continue;

                var person = objectSpace.GetObjectByKey<Person>(row.PersonId);
                if (person == null)
                    continue;

                var item = objectSpace.CreateObject<InvitationItem>();
                item.Invitation = invitation;
                item.Person = person;
                item.Passport = row.PassportId is Guid pid
                    ? objectSpace.GetObjectByKey<Passport>(pid)
                    : ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
                invitation.InvitationItems.Add(item);
            }
            }
        }

        objectSpace.CommitChanges();

        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = invitation.ID,
            HeaderCaption = invitation.InvitationNumber,
            Lines = invitation.InvitationItems.Where(i => i != null).Select(i => new IssueIssuedHeaderCreatedLine
            {
                LineId = i.ID,
                PersonId = i.Person?.ID ?? Guid.Empty,
                PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
                CanIssueVisa = !i.IsUsed && !i.IsCancelled && !i.IsChanged && i.Passport != null,
            }).ToList(),
        };
    }

    private static IssueIssuedHeaderCreateResult UpdateWorkPermit(
        IObjectSpace objectSpace,
        IssueIssuedHeaderComposeDraft draft,
        Guid headerId,
        List<IssueIssuedHeaderPersonLineDraft> selected)
    {
        var workPermit = objectSpace.GetObjectByKey<WorkPermit>(headerId);
        if (workPermit == null)
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Work permit was not found." };

        workPermit.WorkPermitNumber = draft.HeaderNumber.Trim();
        workPermit.IssuedDate = draft.PrimaryDate.Date;

        if (workPermit.WorkPermitItems == null)
            workPermit.WorkPermitItems = new System.Collections.ObjectModel.ObservableCollection<WorkPermitItem>();

        var defaultLocations = workPermit.ApplicationProfileInstance?.MovementPermitLocation?.Trim() ?? string.Empty;
        foreach (var row in selected)
        {
            var person = objectSpace.GetObjectByKey<Person>(row.PersonId);
            if (person == null || !person.IsEmployee)
                continue;

            var item = workPermit.WorkPermitItems.FirstOrDefault(i => i.Person != null && i.Person.ID == row.PersonId);
            if (item == null)
            {
                item = objectSpace.CreateObject<WorkPermitItem>();
                item.WorkPermit = workPermit;
                item.Person = person;
                workPermit.WorkPermitItems.Add(item);
            }

            ApplyWorkPermitItemFields(objectSpace, item, row, defaultLocations);
        }

        objectSpace.CommitChanges();

        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = workPermit.ID,
            HeaderCaption = workPermit.WorkPermitNumber,
            Lines = workPermit.WorkPermitItems.Select(ToWorkPermitCreatedLine).ToList(),
        };
    }

    private static IssueIssuedHeaderCreateResult UpdateRejection(
        IObjectSpace objectSpace,
        IssueIssuedHeaderComposeDraft draft,
        Guid headerId,
        List<IssueIssuedHeaderPersonLineDraft> selected)
    {
        var rejection = objectSpace.GetObjectByKey<Rejection>(headerId);
        if (rejection == null)
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Rejection was not found." };

        rejection.RejectedDocNumber = draft.HeaderNumber.Trim();
        rejection.Date = draft.PrimaryDate.Date;
        rejection.Reason = draft.Reason?.Trim() ?? string.Empty;

        if (rejection.RejectionItems == null)
            rejection.RejectionItems = new System.Collections.ObjectModel.ObservableCollection<RejectionItem>();

        foreach (var row in selected)
        {
            if (rejection.RejectionItems.Any(i => i.Person != null && i.Person.ID == row.PersonId))
                continue;

            var person = objectSpace.GetObjectByKey<Person>(row.PersonId);
            if (person == null)
                continue;

            var item = objectSpace.CreateObject<RejectionItem>();
            item.Rejection = rejection;
            item.Person = person;
            item.Passport = row.PassportId is Guid pid
                ? objectSpace.GetObjectByKey<Passport>(pid)
                : ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
            rejection.RejectionItems.Add(item);
        }

        objectSpace.CommitChanges();

        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = rejection.ID,
            HeaderCaption = rejection.RejectedDocNumber,
            Lines = rejection.RejectionItems.Select(i => new IssueIssuedHeaderCreatedLine
            {
                LineId = i.ID,
                PersonId = i.Person?.ID ?? Guid.Empty,
                PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
            }).ToList(),
        };
    }

    private static IssueIssuedHeaderCreateResult UpdateBorderZone(
        IObjectSpace objectSpace,
        IssueIssuedHeaderComposeDraft draft,
        Guid headerId,
        List<IssueIssuedHeaderPersonLineDraft> selected)
    {
        if (draft.ValidityDurationId is not Guid durationId || durationId == Guid.Empty)
        {
            return new IssueIssuedHeaderCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Validity duration is required.",
            };
        }

        var borderZone = objectSpace.GetObjectByKey<BorderZone>(headerId);
        if (borderZone == null)
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Border zone was not found." };

        borderZone.BorderZoneNumber = draft.HeaderNumber.Trim();
        borderZone.StartDate = draft.PrimaryDate.Date;
        borderZone.ValidityDuration = objectSpace.GetObjectByKey<ValidityDuration>(durationId);

        if (borderZone.BorderZoneItems == null)
            borderZone.BorderZoneItems = new System.Collections.ObjectModel.ObservableCollection<BorderZoneItem>();

        foreach (var row in selected)
        {
            if (borderZone.BorderZoneItems.Any(i => i.Person != null && i.Person.ID == row.PersonId))
                continue;

            var person = objectSpace.GetObjectByKey<Person>(row.PersonId);
            if (person == null)
                continue;

            var item = objectSpace.CreateObject<BorderZoneItem>();
            item.BorderZone = borderZone;
            item.Person = person;
            item.Passport = row.PassportId is Guid pid
                ? objectSpace.GetObjectByKey<Passport>(pid)
                : ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
            borderZone.BorderZoneItems.Add(item);
        }

        objectSpace.CommitChanges();

        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = borderZone.ID,
            HeaderCaption = borderZone.BorderZoneNumber,
            Lines = borderZone.BorderZoneItems.Select(i => new IssueIssuedHeaderCreatedLine
            {
                LineId = i.ID,
                PersonId = i.Person?.ID ?? Guid.Empty,
                PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
            }).ToList(),
        };
    }

    public static IssueIssuedHeaderCreateResult? LoadExistingResult(
        IObjectSpace objectSpace,
        IssueIssuedHeaderKind kind,
        Guid headerId)
    {
        if (objectSpace == null || headerId == Guid.Empty)
            return null;

        switch (kind)
        {
            case IssueIssuedHeaderKind.Invitation:
            {
                var invitation = objectSpace.GetObjectByKey<Invitation>(headerId);
                if (invitation == null)
                    return null;
                return new IssueIssuedHeaderCreateResult
                {
                    Succeeded = true,
                    HeaderId = invitation.ID,
                    HeaderCaption = invitation.InvitationNumber ?? string.Empty,
                    Lines = (invitation.InvitationItems ?? Array.Empty<InvitationItem>())
                        .Where(i => i != null)
                        .Select(i => new IssueIssuedHeaderCreatedLine
                        {
                            LineId = i.ID,
                            PersonId = i.Person?.ID ?? Guid.Empty,
                            PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                            PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
                            CanIssueVisa = !i.IsUsed && !i.IsCancelled && !i.IsChanged && i.Passport != null,
                        })
                        .ToList(),
                };
            }
            case IssueIssuedHeaderKind.WorkPermit:
            {
                var workPermit = objectSpace.GetObjectByKey<WorkPermit>(headerId);
                if (workPermit == null)
                    return null;
                return new IssueIssuedHeaderCreateResult
                {
                    Succeeded = true,
                    HeaderId = workPermit.ID,
                    HeaderCaption = workPermit.WorkPermitNumber ?? string.Empty,
                    Lines = (workPermit.WorkPermitItems ?? Array.Empty<WorkPermitItem>())
                        .Where(i => i != null)
                        .Select(i => new IssueIssuedHeaderCreatedLine
                        {
                            LineId = i.ID,
                            PersonId = i.Person?.ID ?? Guid.Empty,
                            PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                            PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
                        })
                        .ToList(),
                };
            }
            case IssueIssuedHeaderKind.Rejection:
            {
                var rejection = objectSpace.GetObjectByKey<Rejection>(headerId);
                if (rejection == null)
                    return null;
                return new IssueIssuedHeaderCreateResult
                {
                    Succeeded = true,
                    HeaderId = rejection.ID,
                    HeaderCaption = rejection.RejectedDocNumber ?? string.Empty,
                    Lines = (rejection.RejectionItems ?? Array.Empty<RejectionItem>())
                        .Where(i => i != null)
                        .Select(i => new IssueIssuedHeaderCreatedLine
                        {
                            LineId = i.ID,
                            PersonId = i.Person?.ID ?? Guid.Empty,
                            PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                            PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
                        })
                        .ToList(),
                };
            }
            case IssueIssuedHeaderKind.BorderZone:
            {
                var borderZone = objectSpace.GetObjectByKey<BorderZone>(headerId);
                if (borderZone == null)
                    return null;
                return new IssueIssuedHeaderCreateResult
                {
                    Succeeded = true,
                    HeaderId = borderZone.ID,
                    HeaderCaption = borderZone.BorderZoneNumber ?? string.Empty,
                    Lines = (borderZone.BorderZoneItems ?? Array.Empty<BorderZoneItem>())
                        .Where(i => i != null)
                        .Select(i => new IssueIssuedHeaderCreatedLine
                        {
                            LineId = i.ID,
                            PersonId = i.Person?.ID ?? Guid.Empty,
                            PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                            PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
                        })
                        .ToList(),
                };
            }
            default:
                return null;
        }
    }

    public static bool TryCopyDatesFromLastWorkPermit(WorkPermitItem? last, DateTime today, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;
        if (last == null || last.StartDate == default || last.ExpirationDate == default)
            return false;
        if (last.ExpirationDate.Date < today.Date)
            return false;

        start = last.StartDate.Date;
        end = last.ExpirationDate.Date;
        return true;
    }

    public static bool IsWorkPermitCardComplete(IssueIssuedHeaderPersonLineDraft row)
    {
        if (row == null)
            return false;
        if (string.IsNullOrWhiteSpace(row.ItemNumber) || string.IsNullOrWhiteSpace(row.ASNumber))
            return false;
        if (row.PositionId is not Guid posId || posId == Guid.Empty)
            return false;
        if (row.PassportId is null || row.PassportId == Guid.Empty)
            return false;
        if (row.ItemStartDate is not DateTime start || start == default)
            return false;
        if (row.ItemExpirationDate is not DateTime end || end.Date <= start.Date)
            return false;
        return !string.IsNullOrWhiteSpace(row.WorkPermittedLocations);
    }

    private static string? ValidateWorkPermitCards(List<IssueIssuedHeaderPersonLineDraft> selected)
    {
        foreach (var row in selected)
        {
            if (string.IsNullOrWhiteSpace(row.ItemNumber))
                return $"{row.PersonName}: item work-permit number is required.";
            if (string.IsNullOrWhiteSpace(row.ASNumber))
                return $"{row.PersonName}: AS number is required.";
            if (row.PositionId is not Guid posId || posId == Guid.Empty)
                return $"{row.PersonName}: position is required.";
            if (row.PassportId is null || row.PassportId == Guid.Empty)
                return $"{row.PersonName}: passport is required.";
            if (row.ItemStartDate is not DateTime start || start == default)
                return $"{row.PersonName}: start date is required.";
            if (row.ItemExpirationDate is not DateTime end || end.Date <= start.Date)
                return $"{row.PersonName}: end date must be later than start date.";
            if (string.IsNullOrWhiteSpace(row.WorkPermittedLocations))
                return $"{row.PersonName}: work-permitted locations are required.";
        }

        return null;
    }

    private static void ApplyWorkPermitCardDefaults(
        Person person,
        IssueIssuedHeaderPersonLineDraft line,
        string defaultLocations,
        ApplicationProfileInstance instance)
    {
        var currentPosition = PersonCurrentItems.GetCurrentPositionHistory(person);
        line.Positions = LoadPositionOptions(person);
        line.PositionId = currentPosition?.ID;
        if (line.PassportId is null || line.PassportId == Guid.Empty)
        {
            line.IsReady = false;
            line.StatusCaption = "Missing passport";
            line.Include = false;
        }
        else if (currentPosition == null)
        {
            line.IsReady = false;
            line.StatusCaption = "No position";
            line.Include = false;
        }
        else
        {
            line.IsReady = true;
            line.StatusCaption = "Ready";
            line.Include = true;
        }

        var last = PersonCurrentItems.GetCurrentWorkPermitItem(person);
        if (TryCopyDatesFromLastWorkPermit(last, DateTime.Today, out var start, out var end))
        {
            line.ItemStartDate = start;
            line.ItemExpirationDate = end;
            var number = last?.WorkPermitNumber?.Trim();
            line.DatePrefillNote = string.IsNullOrWhiteSpace(number)
                ? "Start and End copied from last work permit (still valid)."
                : $"Start and End copied from last work permit {number} (still valid).";
        }
        else if (last != null && last.ExpirationDate != default)
        {
            var number = last.WorkPermitNumber?.Trim();
            line.DatePrefillNote = string.IsNullOrWhiteSpace(number)
                ? $"Last work permit expired {last.ExpirationDate:dd MMM yyyy} — enter Start and End."
                : $"Last work permit {number} expired {last.ExpirationDate:dd MMM yyyy} — enter Start and End.";
        }
        else
        {
            line.DatePrefillNote = "No previous work permit — enter Start and End.";
        }

        line.WorkPermittedLocations = defaultLocations;
        ApplyInstanceProcessNumberToWorkPermitCard(line, instance);
    }

    private static void ApplyInstanceProcessNumberToWorkPermitCard(
        IssueIssuedHeaderPersonLineDraft line,
        ApplicationProfileInstance? instance)
    {
        var copied = ApplicationProcessNumberHelper.CopyForIssuedDocument(instance);
        if (string.IsNullOrWhiteSpace(copied))
            return;

        line.ASNumber = copied;
        line.AsNumberReadOnly = true;
    }

    private static void BindWorkPermitItemToCard(WorkPermitItem item, IssueIssuedHeaderPersonLineDraft person)
    {
        if (item.Passport != null)
        {
            person.PassportId = item.Passport.ID;
            person.PassportNumber = item.Passport.PassportNumber?.Trim() ?? person.PassportNumber;
            person.IsReady = true;
        }

        person.ItemNumber = item.WorkPermitNumber?.Trim() ?? string.Empty;
        person.ASNumber = item.ASNumber?.Trim() ?? string.Empty;
        person.PositionId = item.CurrentPositionHistory?.ID;
        if (item.Person != null)
            person.Positions = LoadPositionOptions(item.Person);
        person.ItemStartDate = item.StartDate == default ? null : item.StartDate.Date;
        person.ItemExpirationDate = item.ExpirationDate == default ? null : item.ExpirationDate.Date;
        person.WorkPermittedLocations = item.WorkPermittedLocations?.Trim() ?? string.Empty;
        person.DatePrefillNote = string.Empty;
        if (person.PositionId is null || person.PositionId == Guid.Empty || person.PassportId is null)
        {
            person.IsReady = false;
            person.StatusCaption = person.PassportId is null ? "Missing passport" : "No position";
        }
    }

    private static List<IssueIssuedHeaderLookupOption> LoadPositionOptions(Person person)
    {
        return (person.PositionHistory ?? Array.Empty<EmployeePositionHistory>())
            .Where(h => h != null)
            .OrderByDescending(h => h.StartDate)
            .ThenByDescending(h => h.ID)
            .Select(h => new IssueIssuedHeaderLookupOption
            {
                Id = h.ID,
                Caption = string.IsNullOrWhiteSpace(h.Position?.NameTm)
                    ? (h.Title?.Trim() ?? h.ID.ToString("N")[..8])
                    : h.Position!.NameTm,
            })
            .ToList();
    }

    private static void ApplyWorkPermitItemFields(
        IObjectSpace objectSpace,
        WorkPermitItem item,
        IssueIssuedHeaderPersonLineDraft row,
        string defaultLocations)
    {
        item.WorkPermitNumber = row.ItemNumber.Trim();
        item.ASNumber = row.ASNumber.Trim();
        item.Passport = row.PassportId is Guid pid
            ? objectSpace.GetObjectByKey<Passport>(pid)
            : ApplicationProfileInstancePersonValidItems.ResolvePassport(item.Person);
        item.CurrentPositionHistory = row.PositionId is Guid posId
            ? objectSpace.GetObjectByKey<EmployeePositionHistory>(posId)
            : PersonCurrentItems.GetCurrentPositionHistory(item.Person);
        item.StartDate = row.ItemStartDate?.Date ?? default;
        item.ExpirationDate = row.ItemExpirationDate?.Date ?? default;
        item.WorkPermittedLocations = string.IsNullOrWhiteSpace(row.WorkPermittedLocations)
            ? defaultLocations
            : row.WorkPermittedLocations.Trim();
    }

    private static IssueIssuedHeaderCreatedLine ToWorkPermitCreatedLine(WorkPermitItem item) =>
        new()
        {
            LineId = item.ID,
            PersonId = item.Person?.ID ?? Guid.Empty,
            PersonName = item.Person?.FullName?.Trim() ?? string.Empty,
            PassportNumber = item.Passport?.PassportNumber?.Trim() ?? string.Empty,
            ItemNumber = item.WorkPermitNumber?.Trim() ?? string.Empty,
            ASNumber = item.ASNumber?.Trim() ?? string.Empty,
            PositionCaption = item.CurrentPositionHistory?.Position?.NameTm
                ?? item.CurrentPositionHistory?.Title
                ?? string.Empty,
            StartDate = item.StartDate == default ? null : item.StartDate.Date,
            ExpirationDate = item.ExpirationDate == default ? null : item.ExpirationDate.Date,
            LocationsCaption = item.WorkPermittedLocations?.Trim() ?? string.Empty,
        };
}