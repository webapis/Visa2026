using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

/// <summary>
/// Document copies from sticky <see cref="ApplicationProfileInstancePersonResolvedLink"/> rows.
/// One group per linked record, labeled by identification number — not ApplicationItem
/// Current/Previous/Next slots.
/// </summary>
public static class ApplicationItemLinkedDocumentsLinkedRecordResolver
{
    public static ApplicationItemLinkedDocumentsSnapshot Resolve(
        IObjectSpace objectSpace,
        Person person,
        IEnumerable<ApplicationProfileInstancePersonResolvedLink>? links)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (person == null)
        {
            return new ApplicationItemLinkedDocumentsSnapshot
            {
                ApplicationItemId = Guid.Empty,
                Groups = Array.Empty<ApplicationItemLinkedDocumentGroup>()
            };
        }

        person = objectSpace.GetObject(person) ?? person;
        var groups = new List<ApplicationItemLinkedDocumentGroup>();

        foreach (var link in (links ?? [])
            .Where(l => l?.LinkedObjectId is Guid id && id != Guid.Empty && l.LinkKind != null)
            .OrderBy(l => KindOrder(l.LinkKind!.Value))
            .ThenBy(l => l.LinkedObjectId))
        {
            AddGroupsForLink(objectSpace, link.LinkKind!.Value, link.LinkedObjectId!.Value, groups);
        }

        if (person.PersonRole == PersonRecordRole.FamilyMember)
            AddFamilyRelationshipGroup(objectSpace, person, groups);

        return new ApplicationItemLinkedDocumentsSnapshot
        {
            ApplicationItemId = person.ID,
            Groups = groups
        };
    }

    public static string SlotKey(string family, Guid sourceId) =>
        family + "." + sourceId.ToString("N");

    internal static int KindOrder(ApplicationProfileInstancePersonLinkKind kind) => kind switch
    {
        ApplicationProfileInstancePersonLinkKind.Passport => 10,
        ApplicationProfileInstancePersonLinkKind.Education => 20,
        ApplicationProfileInstancePersonLinkKind.AddressOfResidence => 30,
        ApplicationProfileInstancePersonLinkKind.Visa => 40,
        ApplicationProfileInstancePersonLinkKind.InvitationItem => 50,
        ApplicationProfileInstancePersonLinkKind.WorkPermitItem => 60,
        ApplicationProfileInstancePersonLinkKind.MedicalRecord => 70,
        ApplicationProfileInstancePersonLinkKind.RejectionItem => 80,
        ApplicationProfileInstancePersonLinkKind.BorderZoneItem => 90,
        _ => 200
    };

    private static void AddGroupsForLink(
        IObjectSpace os,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid linkedId,
        List<ApplicationItemLinkedDocumentGroup> groups)
    {
        switch (kind)
        {
            case ApplicationProfileInstancePersonLinkKind.Passport:
                AddPassport(os, linkedId, groups);
                break;
            case ApplicationProfileInstancePersonLinkKind.Visa:
                AddVisa(os, linkedId, groups);
                break;
            case ApplicationProfileInstancePersonLinkKind.Education:
                AddEducation(os, linkedId, groups);
                break;
            case ApplicationProfileInstancePersonLinkKind.AddressOfResidence:
                AddAddress(os, linkedId, groups);
                break;
            case ApplicationProfileInstancePersonLinkKind.MedicalRecord:
                AddMedical(os, linkedId, groups);
                break;
            case ApplicationProfileInstancePersonLinkKind.InvitationItem:
                AddInvitation(os, linkedId, groups);
                break;
            case ApplicationProfileInstancePersonLinkKind.WorkPermitItem:
                AddWorkPermit(os, linkedId, groups);
                break;
            case ApplicationProfileInstancePersonLinkKind.RejectionItem:
                AddRejection(os, linkedId, groups);
                break;
            case ApplicationProfileInstancePersonLinkKind.BorderZoneItem:
                AddBorderZone(os, linkedId, groups);
                break;
        }
    }

    private static void AddPassport(IObjectSpace os, Guid id, List<ApplicationItemLinkedDocumentGroup> groups)
    {
        var passport = os.GetObjectByKey<Passport>(id);
        if (passport == null)
            return;

        groups.Add(Group(
            SlotKey("Passport", passport.ID),
            PersonDocumentCopiesLocalization.FormatPassportRecord(passport.PassportNumber),
            typeof(Passport),
            passport.ID,
            passport.PassportNumber,
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<PassportDocument>(os, d => d.Passport.ID == passport.ID)));
    }

    private static void AddVisa(IObjectSpace os, Guid id, List<ApplicationItemLinkedDocumentGroup> groups)
    {
        var visa = os.GetObjectByKey<Visa>(id);
        if (visa == null)
            return;

        groups.Add(Group(
            SlotKey("Visa", visa.ID),
            PersonDocumentCopiesLocalization.FormatVisaRecord(visa.VisaNumber),
            typeof(Visa),
            visa.ID,
            visa.VisaNumber,
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<VisaDocument>(os, d => d.Visa.ID == visa.ID)));
    }

    private static void AddEducation(IObjectSpace os, Guid id, List<ApplicationItemLinkedDocumentGroup> groups)
    {
        var education = os.GetObjectByKey<Education>(id);
        if (education == null)
            return;

        var caption = BuildEducationCaption(education);
        groups.Add(Group(
            SlotKey("Education", education.ID),
            PersonDocumentCopiesLocalization.FormatEducationRecord(caption),
            typeof(Education),
            education.ID,
            caption,
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<EducationDocument>(os, d => d.Education.ID == education.ID)));
    }

    private static void AddAddress(IObjectSpace os, Guid id, List<ApplicationItemLinkedDocumentGroup> groups)
    {
        var address = os.GetObjectByKey<AddressOfResidence>(id);
        if (address == null)
            return;

        groups.Add(Group(
            SlotKey("AddressOfResidence", address.ID),
            PersonDocumentCopiesLocalization.FormatAddressRecord(address.FullAddress),
            typeof(AddressOfResidence),
            address.ID,
            address.FullAddress,
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<AddressOfResidenceDocument>(os, d => d.AddressOfResidence.ID == address.ID)));

        if (address.Type != ResidenceType.Lodging || address.Lodging == null)
            return;

        var lodging = os.GetObject(address.Lodging);
        groups.Add(Group(
            SlotKey("AddressOfResidence.Lodging", lodging.ID),
            PersonDocumentCopiesLocalization.FormatLodgingRecord(lodging.FullAddress),
            typeof(Lodging),
            lodging.ID,
            lodging.FullAddress,
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<LodgingDocument>(os, d => d.Lodging.ID == lodging.ID)));
    }

    private static void AddMedical(IObjectSpace os, Guid id, List<ApplicationItemLinkedDocumentGroup> groups)
    {
        var medical = os.GetObjectByKey<MedicalRecord>(id);
        if (medical == null)
            return;

        groups.Add(Group(
            SlotKey("MedicalRecord", medical.ID),
            PersonDocumentCopiesLocalization.FormatMedicalRecord(medical.DocumentNumber),
            typeof(MedicalRecord),
            medical.ID,
            medical.DocumentNumber,
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<MedicalRecordDocument>(os, d => d.MedicalRecord.ID == medical.ID)));
    }

    private static void AddInvitation(IObjectSpace os, Guid id, List<ApplicationItemLinkedDocumentGroup> groups)
    {
        var item = os.GetObjectByKey<InvitationItem>(id);
        var invitation = item?.Invitation;
        if (invitation == null)
            return;

        invitation = os.GetObject(invitation);
        groups.Add(Group(
            SlotKey("Invitation", invitation.ID),
            PersonDocumentCopiesLocalization.FormatInvitationRecord(invitation.InvitationNumber),
            typeof(Invitation),
            invitation.ID,
            invitation.InvitationNumber,
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<InvitationDocument>(os, d => d.Invitation.ID == invitation.ID)));
    }

    private static void AddWorkPermit(IObjectSpace os, Guid id, List<ApplicationItemLinkedDocumentGroup> groups)
    {
        var item = os.GetObjectByKey<WorkPermitItem>(id);
        var workPermit = item?.WorkPermit;
        if (workPermit == null)
            return;

        workPermit = os.GetObject(workPermit);
        groups.Add(Group(
            SlotKey("WorkPermit", workPermit.ID),
            PersonDocumentCopiesLocalization.FormatWorkPermitRecord(workPermit.WorkPermitNumber),
            typeof(WorkPermit),
            workPermit.ID,
            workPermit.WorkPermitNumber,
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<WorkPermitDocument>(os, d => d.WorkPermit.ID == workPermit.ID)));
    }

    private static void AddRejection(IObjectSpace os, Guid id, List<ApplicationItemLinkedDocumentGroup> groups)
    {
        var item = os.GetObjectByKey<RejectionItem>(id);
        var rejection = item?.Rejection;
        if (rejection == null)
            return;

        rejection = os.GetObject(rejection);
        groups.Add(Group(
            SlotKey("Rejection", rejection.ID),
            PersonDocumentCopiesLocalization.FormatRejectionRecord(rejection.RejectionTitle),
            typeof(Rejection),
            rejection.ID,
            rejection.RejectionTitle,
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<RejectionDocument>(os, d => d.Rejection.ID == rejection.ID)));
    }

    private static void AddBorderZone(IObjectSpace os, Guid id, List<ApplicationItemLinkedDocumentGroup> groups)
    {
        var item = os.GetObjectByKey<BorderZoneItem>(id);
        var zone = item?.BorderZone;
        if (zone == null)
            return;

        zone = os.GetObject(zone);
        groups.Add(Group(
            SlotKey("BorderZone", zone.ID),
            PersonDocumentCopiesLocalization.FormatBorderZoneRecord(zone.BorderZoneNumber),
            typeof(BorderZone),
            zone.ID,
            zone.BorderZoneNumber,
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<BorderZoneDocument>(os, d => d.BorderZone.ID == zone.ID)));
    }

    private static void AddFamilyRelationshipGroup(
        IObjectSpace os,
        Person person,
        List<ApplicationItemLinkedDocumentGroup> groups)
    {
        groups.Add(Group(
            SlotKey("FamilyRelationship", person.ID),
            VisaUiMessages.Get("ApplicationItemDocumentCopies.Slot.FamilyRelationship"),
            typeof(Person),
            person.ID,
            person.FullName,
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<PersonFamilyRelationDocument>(os, d => d.Person.ID == person.ID)));
    }

    private static ApplicationItemLinkedDocumentGroup Group(
        string slotKey,
        string slotLabel,
        Type sourceType,
        Guid sourceId,
        string? sourceCaption,
        IReadOnlyList<ApplicationItemLinkedDocumentFile> files) =>
        new()
        {
            SlotKey = slotKey,
            SlotLabel = slotLabel,
            SourceObjectType = sourceType,
            SourceObjectId = sourceId,
            SourceCaption = sourceCaption,
            Files = files
        };

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
        return year + " — " + inst;
    }
}