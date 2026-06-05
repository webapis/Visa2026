using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

/// <summary>
/// Resolves line-scoped document copies for an <see cref="ApplicationItem"/> using the same FK rules as
/// <see cref="ApplicationSupportingDocumentsPacker"/> (no fallback to live <see cref="Person"/> collections).
/// </summary>
public static class ApplicationItemLinkedDocumentsResolver
{
    public static ApplicationItemLinkedDocumentsSnapshot Resolve(IObjectSpace objectSpace, ApplicationItem item)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (item == null)
        {
            return new ApplicationItemLinkedDocumentsSnapshot
            {
                ApplicationItemId = Guid.Empty,
                Groups = Array.Empty<ApplicationItemLinkedDocumentGroup>()
            };
        }

        var itemId = item.ID;
        if (item.Person == null)
        {
            return new ApplicationItemLinkedDocumentsSnapshot
            {
                ApplicationItemId = itemId,
                Groups = Array.Empty<ApplicationItemLinkedDocumentGroup>()
            };
        }

        item = objectSpace.GetObject(item);
        var appType = item.Application?.ApplicationType;
        var groups = new List<ApplicationItemLinkedDocumentGroup>();

        AddPassportGroups(objectSpace, item, appType, groups);
        AddVisaGroups(objectSpace, item, appType, groups);
        AddWorkPermitGroups(objectSpace, item, appType, groups);
        AddEducationGroup(objectSpace, item, appType, groups);
        AddInvitationGroups(objectSpace, item, appType, groups);
        AddAddressGroup(objectSpace, item, appType, groups);
        AddMedicalGroup(objectSpace, item, appType, groups);
        AddFamilyRelationshipGroup(objectSpace, item, groups);

        return new ApplicationItemLinkedDocumentsSnapshot
        {
            ApplicationItemId = itemId,
            Groups = groups
        };
    }

    public static ApplicationItemLinkedDocumentsLineSnapshot ResolveLine(IObjectSpace objectSpace, ApplicationItem item)
    {
        var snapshot = Resolve(objectSpace, item);
        return new ApplicationItemLinkedDocumentsLineSnapshot
        {
            ApplicationItemId = snapshot.ApplicationItemId,
            LineLabel = item?.ApplicationItemName ?? string.Empty,
            Groups = snapshot.Groups
        };
    }

    public static IReadOnlyList<ApplicationItemLinkedDocumentsLineSnapshot> ResolveMany(
        IObjectSpace objectSpace,
        IEnumerable<ApplicationItem> items)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (items == null)
            return Array.Empty<ApplicationItemLinkedDocumentsLineSnapshot>();

        return items
            .Where(item => item != null)
            .Select(item => ResolveLine(objectSpace, item))
            .ToList();
    }

    private static void AddPassportGroups(
        IObjectSpace os,
        ApplicationItem item,
        ApplicationType? appType,
        List<ApplicationItemLinkedDocumentGroup> groups)
    {
        groups.Add(BuildPassportGroup(os, item.CurrentPassport, "Passport.Current"));

        if (appType?.ShowPreviousPassport == true)
        {
            var cur = item.CurrentPassport;
            var prev = item.PreviousPassport;
            if (prev == null || cur == null || prev.ID != cur.ID)
                groups.Add(BuildPassportGroup(os, prev, "Passport.Previous"));
        }
    }

    private static ApplicationItemLinkedDocumentGroup BuildPassportGroup(
        IObjectSpace os,
        Passport? passport,
        string slotKey)
    {
        bool isPrevious = slotKey.EndsWith(".Previous", StringComparison.Ordinal);
        string slotLabelKey = isPrevious
            ? "ApplicationItemDocumentCopies.Slot.Passport.Previous"
            : "ApplicationItemDocumentCopies.Slot.Passport.Current";

        if (passport == null)
        {
            return new ApplicationItemLinkedDocumentGroup
            {
                SlotKey = slotKey,
                SlotLabel = VisaUiMessages.Get(slotLabelKey),
                SourceObjectType = typeof(Passport),
                LinkMissing = true
            };
        }

        passport = os.GetObject(passport);
        return new ApplicationItemLinkedDocumentGroup
        {
            SlotKey = slotKey,
            SlotLabel = VisaUiMessages.Get(slotLabelKey),
            SourceObjectType = typeof(Passport),
            SourceObjectId = passport.ID,
            SourceCaption = passport.PassportNumber,
            Files = LoadDocumentFiles<PassportDocument>(os, d => d.Passport.ID == passport.ID)
        };
    }

    private static void AddVisaGroups(
        IObjectSpace os,
        ApplicationItem item,
        ApplicationType? appType,
        List<ApplicationItemLinkedDocumentGroup> groups)
    {
        if (appType?.ShowCurrentVisa == true)
            groups.Add(BuildVisaGroup(os, item.CurrentVisa, "Visa.Current", "ApplicationItemDocumentCopies.Slot.Visa.Current"));

        if (appType?.ShowNextVisa == true)
            groups.Add(BuildVisaGroup(os, item.NextVisa, "Visa.Next", "ApplicationItemDocumentCopies.Slot.Visa.Next"));
    }

    private static ApplicationItemLinkedDocumentGroup BuildVisaGroup(
        IObjectSpace os,
        Visa? visa,
        string slotKey,
        string slotLabelKey)
    {
        if (visa == null)
        {
            return new ApplicationItemLinkedDocumentGroup
            {
                SlotKey = slotKey,
                SlotLabel = VisaUiMessages.Get(slotLabelKey),
                SourceObjectType = typeof(Visa),
                LinkMissing = true
            };
        }

        visa = os.GetObject(visa);
        return new ApplicationItemLinkedDocumentGroup
        {
            SlotKey = slotKey,
            SlotLabel = VisaUiMessages.Get(slotLabelKey),
            SourceObjectType = typeof(Visa),
            SourceObjectId = visa.ID,
            SourceCaption = visa.VisaNumber,
            Files = LoadDocumentFiles<VisaDocument>(os, d => d.Visa.ID == visa.ID)
        };
    }

    private static void AddWorkPermitGroups(
        IObjectSpace os,
        ApplicationItem item,
        ApplicationType? appType,
        List<ApplicationItemLinkedDocumentGroup> groups)
    {
        if (item.Person?.IsEmployee != true)
            return;

        if (appType?.ShowCurrentWorkPermitItem == true)
        {
            var curWp = item.CurrentWorkPermitItem?.WorkPermit;
            groups.Add(BuildWorkPermitGroup(os, curWp, "WorkPermit.Current", "ApplicationItemDocumentCopies.Slot.WorkPermit.Current"));
        }

        if (appType?.ShowPreviousWorkPermitItem == true)
        {
            var prevWp = item.PreviousWorkPermitItem?.WorkPermit;
            var curWp = item.CurrentWorkPermitItem?.WorkPermit;
            if (prevWp == null || curWp == null || prevWp.ID != curWp.ID)
                groups.Add(BuildWorkPermitGroup(os, prevWp, "WorkPermit.Previous", "ApplicationItemDocumentCopies.Slot.WorkPermit.Previous"));
        }
    }

    private static ApplicationItemLinkedDocumentGroup BuildWorkPermitGroup(
        IObjectSpace os,
        WorkPermit? workPermit,
        string slotKey,
        string slotLabelKey)
    {
        if (workPermit == null)
        {
            return new ApplicationItemLinkedDocumentGroup
            {
                SlotKey = slotKey,
                SlotLabel = VisaUiMessages.Get(slotLabelKey),
                SourceObjectType = typeof(WorkPermit),
                LinkMissing = true
            };
        }

        workPermit = os.GetObject(workPermit);
        return new ApplicationItemLinkedDocumentGroup
        {
            SlotKey = slotKey,
            SlotLabel = VisaUiMessages.Get(slotLabelKey),
            SourceObjectType = typeof(WorkPermit),
            SourceObjectId = workPermit.ID,
            SourceCaption = workPermit.WorkPermitNumber,
            Files = LoadDocumentFiles<WorkPermitDocument>(os, d => d.WorkPermit.ID == workPermit.ID)
        };
    }

    private static void AddEducationGroup(
        IObjectSpace os,
        ApplicationItem item,
        ApplicationType? appType,
        List<ApplicationItemLinkedDocumentGroup> groups)
    {
        if (appType?.ShowCurrentEducation != true)
            return;

        if (IsRegistrationApplicationItem(item))
            return;

        var education = item.CurrentEducation;
        if (education == null)
        {
            groups.Add(new ApplicationItemLinkedDocumentGroup
            {
                SlotKey = "Education.Current",
                SlotLabel = VisaUiMessages.Get("ApplicationItemDocumentCopies.Slot.Education.Current"),
                SourceObjectType = typeof(Education),
                LinkMissing = true
            });
            return;
        }

        education = os.GetObject(education);
        string caption = BuildEducationCaption(education);
        groups.Add(new ApplicationItemLinkedDocumentGroup
        {
            SlotKey = "Education.Current",
            SlotLabel = VisaUiMessages.Get("ApplicationItemDocumentCopies.Slot.Education.Current"),
            SourceObjectType = typeof(Education),
            SourceObjectId = education.ID,
            SourceCaption = caption,
            Files = LoadDocumentFiles<EducationDocument>(os, d => d.Education.ID == education.ID)
        });
    }

    private static void AddInvitationGroups(
        IObjectSpace os,
        ApplicationItem item,
        ApplicationType? appType,
        List<ApplicationItemLinkedDocumentGroup> groups)
    {
        if (appType?.ShowCurrentInvitationItem == true)
            groups.Add(BuildInvitationGroup(os, item.CurrentInvitationItem?.Invitation, "Invitation.Current", "ApplicationItemDocumentCopies.Slot.Invitation.Current"));

        if (appType?.ShowPreviousInvitationItem == true)
        {
            var cur = item.CurrentInvitationItem?.Invitation;
            var prev = item.PreviousInvitationItem?.Invitation;
            if (prev == null || cur == null || prev.ID != cur.ID)
                groups.Add(BuildInvitationGroup(os, prev, "Invitation.Previous", "ApplicationItemDocumentCopies.Slot.Invitation.Previous"));
        }
    }

    private static ApplicationItemLinkedDocumentGroup BuildInvitationGroup(
        IObjectSpace os,
        Invitation? invitation,
        string slotKey,
        string slotLabelKey)
    {
        if (invitation == null)
        {
            return new ApplicationItemLinkedDocumentGroup
            {
                SlotKey = slotKey,
                SlotLabel = VisaUiMessages.Get(slotLabelKey),
                SourceObjectType = typeof(Invitation),
                LinkMissing = true
            };
        }

        invitation = os.GetObject(invitation);
        return new ApplicationItemLinkedDocumentGroup
        {
            SlotKey = slotKey,
            SlotLabel = VisaUiMessages.Get(slotLabelKey),
            SourceObjectType = typeof(Invitation),
            SourceObjectId = invitation.ID,
            SourceCaption = invitation.InvitationNumber,
            Files = LoadDocumentFiles<InvitationDocument>(os, d => d.Invitation.ID == invitation.ID)
        };
    }

    private static void AddAddressGroup(
        IObjectSpace os,
        ApplicationItem item,
        ApplicationType? appType,
        List<ApplicationItemLinkedDocumentGroup> groups)
    {
        if (appType?.ShowCurrentAddressOfResidence != true)
            return;

        var address = item.CurrentAddressOfResidence;
        if (address == null)
        {
            groups.Add(new ApplicationItemLinkedDocumentGroup
            {
                SlotKey = "AddressOfResidence.Current",
                SlotLabel = VisaUiMessages.Get("ApplicationItemDocumentCopies.Slot.Address.Current"),
                SourceObjectType = typeof(AddressOfResidence),
                LinkMissing = true
            });
            return;
        }

        address = os.GetObject(address);
        groups.Add(new ApplicationItemLinkedDocumentGroup
        {
            SlotKey = "AddressOfResidence.Current",
            SlotLabel = VisaUiMessages.Get("ApplicationItemDocumentCopies.Slot.Address.Current"),
            SourceObjectType = typeof(AddressOfResidence),
            SourceObjectId = address.ID,
            SourceCaption = address.FullAddress,
            Files = LoadDocumentFiles<AddressOfResidenceDocument>(os, d => d.AddressOfResidence.ID == address.ID)
        });

        if (address.Type == ResidenceType.Lodging && address.Lodging != null)
        {
            var lodging = os.GetObject(address.Lodging);
            groups.Add(new ApplicationItemLinkedDocumentGroup
            {
                SlotKey = "AddressOfResidence.Lodging",
                SlotLabel = VisaUiMessages.Get("ApplicationItemDocumentCopies.Slot.Address.Lodging"),
                SourceObjectType = typeof(Lodging),
                SourceObjectId = lodging.ID,
                SourceCaption = lodging.FullAddress,
                Files = LoadDocumentFiles<LodgingDocument>(os, d => d.Lodging.ID == lodging.ID)
            });
        }
    }

    private static void AddMedicalGroup(
        IObjectSpace os,
        ApplicationItem item,
        ApplicationType? appType,
        List<ApplicationItemLinkedDocumentGroup> groups)
    {
        if (appType?.ShowCurrentMedicalRecord != true)
            return;

        var medical = item.CurrentMedicalRecord;
        if (medical == null)
        {
            groups.Add(new ApplicationItemLinkedDocumentGroup
            {
                SlotKey = "MedicalRecord.Current",
                SlotLabel = VisaUiMessages.Get("ApplicationItemDocumentCopies.Slot.MedicalRecord.Current"),
                SourceObjectType = typeof(MedicalRecord),
                LinkMissing = true
            });
            return;
        }

        medical = os.GetObject(medical);
        groups.Add(new ApplicationItemLinkedDocumentGroup
        {
            SlotKey = "MedicalRecord.Current",
            SlotLabel = VisaUiMessages.Get("ApplicationItemDocumentCopies.Slot.MedicalRecord.Current"),
            SourceObjectType = typeof(MedicalRecord),
            SourceObjectId = medical.ID,
            SourceCaption = medical.DocumentNumber,
            Files = LoadDocumentFiles<MedicalRecordDocument>(os, d => d.MedicalRecord.ID == medical.ID)
        });
    }

    private static void AddFamilyRelationshipGroup(
        IObjectSpace os,
        ApplicationItem item,
        List<ApplicationItemLinkedDocumentGroup> groups)
    {
        if (item.Person?.PersonRole != PersonRecordRole.FamilyMember)
            return;

        var person = os.GetObject(item.Person);
        groups.Add(new ApplicationItemLinkedDocumentGroup
        {
            SlotKey = "FamilyRelationship.Current",
            SlotLabel = VisaUiMessages.Get("ApplicationItemDocumentCopies.Slot.FamilyRelationship"),
            SourceObjectType = typeof(Person),
            SourceObjectId = person.ID,
            SourceCaption = person.FullName,
            Files = LoadDocumentFiles<PersonFamilyRelationDocument>(os, d => d.Person.ID == person.ID)
        });
    }

    private static bool IsRegistrationApplicationItem(ApplicationItem item) =>
        item.Application?.ApplicationType?.ShowRegistrations == true;

    private static string? BuildEducationCaption(Education education)
    {
        var inst = education.EducationInstitution?.NameTm
                   ?? education.EducationInstitution?.Name;
        var year = education.GraduationYear?.Trim();
        if (string.IsNullOrWhiteSpace(inst) && string.IsNullOrWhiteSpace(year))
            return null;
        if (string.IsNullOrWhiteSpace(inst))
            return year;
        if (string.IsNullOrWhiteSpace(year))
            return inst;
        return $"{year} — {inst}";
    }

    private static IReadOnlyList<ApplicationItemLinkedDocumentFile> LoadDocumentFiles<TDocument>(
        IObjectSpace os,
        System.Linq.Expressions.Expression<Func<TDocument, bool>> filter)
        where TDocument : DocumentBase
    {
        return os.GetObjectsQuery<TDocument>()
            .Where(filter)
            .OrderBy(d => d.ID)
            .Include(d => d.File)
            .AsEnumerable()
            .Select(MapDocumentFile)
            .ToList();
    }

    private static ApplicationItemLinkedDocumentFile MapDocumentFile(DocumentBase doc)
    {
        var file = doc.File;
        if (file == null)
        {
            return new ApplicationItemLinkedDocumentFile
            {
                DocumentRowId = doc.ID,
                DocumentTypeName = doc.GetType().Name,
                FileName = string.Empty,
                HasContent = false
            };
        }

        bool hasContent = file.Size > 0;
        return new ApplicationItemLinkedDocumentFile
        {
            FileDataId = file.ID,
            DocumentRowId = doc.ID,
            DocumentTypeName = doc.GetType().Name,
            FileName = string.IsNullOrWhiteSpace(file.FileName) ? "document" : file.FileName,
            SizeBytes = (int)Math.Min(int.MaxValue, file.Size),
            HasContent = hasContent
        };
    }
}
